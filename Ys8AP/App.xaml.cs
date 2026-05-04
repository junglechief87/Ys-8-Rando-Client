/*
 * MIT License
 *
 * Copyright (c) 2025 ArsonAssassin
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * vfurnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */
using Archipelago.Core;
using Archipelago.Core.Helpers;
using Archipelago.Core.Models;
using Archipelago.Core.Util;
using Archipelago.MultiClient.Net.BounceFeatures.DeathLink;
using Archipelago.MultiClient.Net.MessageLog.Messages;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Ys8AP.GlobalAddresses;
using Ys8AP.Items;
using Ys8AP.Mem;
using Ys8AP.Models;
using Ys8AP.Threads;
using Ys8AP.Utils;
using Ys8AP.ViewModels;
using Ys8AP.Views;
using Newtonsoft.Json;
using ReactiveUI;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Threading;
using System.Threading.Tasks;
using Color = Avalonia.Media.Color;

// Adapted from github.com/ArsonAssassin/Archipelago-Avalonia-Template
namespace Ys8AP
{
    public partial class App : Application
    {
        public static ArchipelagoClient? Client { get; set; }

        private static MainWindowViewModel? Context;
        private static readonly object _lockObject = new();
        private static readonly ConcurrentQueue<Location> locationQueue = new();

        private Thread? queueThread;
        private Thread? locationWatcherThread;
        private Thread? reconnectThread;
        private Thread? PartyWatcherThread;
        private GameClient? Ys8Client;
        private static DeathLinkService? _deathlinkService = null;
        private bool deathFromDeathlink = false;
        private static string slotName = "";
        private static bool isShuttingDown = false;

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);

            Context = new MainWindowViewModel() { ConnectButtonEnabled = true };
            Context.ConnectClicked += Context_ConnectClicked;
            Context.CommandReceived += (_, a) => Client?.SendMessage(a.Command);

            // TODO save last used host/slot?
            //Context.Host = "localhost:38281";
            //Context.Slot = "DC1";

            //InventoryMgmt.InitInventoryMgmt();
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new MainWindow
                {
                    DataContext = Context
                };
            }
            else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
            {
                singleViewPlatform.MainView = new MainWindow
                {
                    DataContext = Context
                };
            }
            base.OnFrameworkInitializationCompleted();
            
            // Start the player state monitoring thread
            PlayerState.StartMonitoring();
        }

        private async void Context_ConnectClicked(object? sender, ConnectClickedEventArgs e)
        {
            if (e.Host != null && e.Host.StartsWith("/connect ")) e.Host = e.Host.Substring("/connect ".Length); // trim "/connect " off front
            // trim extra spaces before defaulting
            e.Host = e.Host?.Trim() ?? e.Host;
            e.Slot = e.Slot?.Trim() ?? e.Slot;
            // default to most basic local-hosted setup if they were empty
            if (string.IsNullOrEmpty(e.Host)) e.Host = "localhost:38281";
            if (string.IsNullOrEmpty(e.Slot)) e.Slot = "Player1";

            if (Context == null)
                return;
            
            isShuttingDown = false;
            Context.ConnectButtonEnabled = false;
            Log.Logger.Information("Connecting...");

            if (Client != null)
            {
                isShuttingDown = true;
                
                Client.Connected -= OnConnected;
                Client.Disconnected -= OnDisconnected;
                Client.MessageReceived -= Client_MessageReceived;
                
                // Stop the worker threads gracefully
                ItemQueue.StopThread();
                
                // Disconnect the client
                Client.Disconnect();
                
                // Give threads a moment to exit their loops before nullifying Client
                Thread.Sleep(200);
                Client = null;

                if (_deathlinkService != null)
                {
                    _deathlinkService.OnDeathLinkReceived -= _deathlinkService_OnDeathLinkReceived;
                    _deathlinkService = null;
                }
                
                // Clear thread references
                queueThread = null;
                locationWatcherThread = null;
                PartyWatcherThread = null;
            }
        
            Ys8Client = Ys8Connect();

            if (Ys8Client == null)
            {
                Context.ConnectButtonEnabled = true;
                return;
            }

            // Connect to archipelago server
            if (Client == null)
            {
                Client = new ArchipelagoClient(Ys8Client);
                AddressInit.InitializeAddresses();
                if (!PlayerState.IsPlayerReady)
                {
                    Log.Logger.Warning("Inventory not connected, make sure you have loaded a save, are not in the main menu, or have started a new game.");
                }
            }
            
            Client.Connected += OnConnected;

            await Client.Connect(e.Host, "Ys 8");
            
            if (!Client.IsConnected)
            {
                Log.Logger.Warning("Connect to AP Server failed");
                Context.ConnectButtonEnabled = true;
                return;

            }

            Client.Disconnected += OnDisconnected;
            Client.MessageReceived += Client_MessageReceived;

            await Client.Login(e.Slot, !string.IsNullOrWhiteSpace(e.Password) ? e.Password : "", 
            Archipelago.MultiClient.Net.Enums.ItemsHandlingFlags.IncludeStartingInventory);

            if (!Client.IsConnected || !Client.IsLoggedIn)
            {
                Context.ConnectButtonEnabled = true;
                return;
            }

            Client.ItemManager.ItemReceived += Client_ItemReceived;
            Client.ItemManager.ReceiveReady(Client.CurrentSession);

            slotName = e.Slot;

            var currentSlot = Client.CurrentSession.ConnectionInfo.Slot;
            var slotData = await Client.CurrentSession.DataStorage.GetSlotDataAsync(currentSlot);
            
            try
            {
                // Pull out options from AP
                Options.ParseOptions(Client.Options);
            }
            catch (FormatException)
            {
                Log.Logger.Error("Failed to parse options");
                Context.ConnectButtonEnabled = true;
                return;
            }

            if (reconnectThread == null)
            {
                reconnectThread = new Thread(new ParameterizedThreadStart(Reconnect))
                {
                    IsBackground = true
                };
                reconnectThread.Start();
            }

            // Initialize things once the player is connected
            // If the player isn't in a valid game state it's likely due to the inventory address not being loaded yet, so try to initialize addresses and check again.  
            
            _deathlinkService = PartyWatcher.InitializeDeathLink(Client, Options.DeathLinkEnabled, _deathlinkService_OnDeathLinkReceived);

            if (queueThread == null)
            {
                ItemQueue.runThread = true;
                queueThread = StartWorkerThread(ItemQueue.ThreadLoop);
            }

            if (locationWatcherThread == null && Client.IsConnected)
                locationWatcherThread = StartWorkerThread(LocationWatcher.DoLoop);

            if (PartyWatcherThread == null && Client.IsConnected)
                PartyWatcherThread = StartWorkerThread(PartyWatcher.DoLoop);

            Context.ConnectButtonEnabled = true;
        }
        #region Ys8

        private Thread StartWorkerThread(ParameterizedThreadStart action)
        {
            var thread = new Thread(action) { IsBackground = true };
            thread.Start();
            return thread;
        }

        private GameClient? Ys8Connect()
        {
            // Check if the Ys 8 process is actually running
            var ys8Process = System.Diagnostics.Process.GetProcessesByName("Ys8").FirstOrDefault();
            if (ys8Process == null)
            {
                Log.Logger.Error("Ys 8 not running, open Ys 8 before connecting!");
                Context.ConnectButtonEnabled = true;
                return null;
            }

            GameClient client = new("ys8");
            try
            {
                client.Connect();
            }
            catch (ArgumentException)
            {
                Log.Logger.Error("Ys 8 not running, open Ys 8 before connecting!");
                Context.ConnectButtonEnabled = true;
                return null;
            }

            Log.Logger.Information("Connected to game.");

            return client;
        }

        private void PrepSeed()
        {
            // On connect reveal the player tracking items in the inventory.
            Contexts.InventoryContext.CheckIfObtainedAndSet(InventoryMgmt.PROGRESSIVE_SHOP_RANK_ID); // Progressive Shop Rank
            Contexts.InventoryContext.CheckIfObtainedAndSet(InventoryMgmt.CASTAWAY_TRACKING_ID); // Castaway
            Contexts.InventoryContext.CheckIfObtainedAndSet(InventoryMgmt.LANDMARK_TRACKING_ID); // Discovery
            if (Options.FinalBossAccess == 2) 
            {
                Contexts.InventoryContext.CheckIfObtainedAndSet(InventoryMgmt.PSYCHES_ITEM_ID); // Psyches
            }

            //ItemQueue.checkItems = true;
        }

        internal static async Task SendLocation(int locId)
        {
            Location loc = new()
            {
                Id = locId
            };

            if (Client.CurrentSession != null && Client.CurrentSession.Socket.Connected) 
                Client.SendLocationAsync(loc);
            else
                locationQueue.Enqueue(loc);
        }

        #endregion

        private void _deathlinkService_OnDeathLinkReceived(DeathLink deathLink)
        {
            
            // Kill player x_x
            if (PlayerState.IsPlayerReady && !PlayerState.GameOver() && !PlayerState.NotInTown())
            {
                PartyWatcher.KillParty();
                Log.Logger.Information("DeathLink: Received from " + deathLink.Source);
            }
        }

        public static void sendDeathLink()
        {
            if (_deathlinkService != null)
            {
                DeathLink dl = new(slotName);
                _deathlinkService.SendDeathLink(dl);
                Log.Logger.Information("DeathLink: Sending Death to your friends...");
            }
        }

        private static void Client_ItemReceived(object? sender, ItemReceivedEventArgs e)
        {
            long itemId = e.Item.Id;
            ItemQueue.AddItem(itemId);
            
            // Display in UI with AP-standard color coding (skip if verifying items from state sync)
            if (!InventoryMgmt.isVerifying && Context != null && e.Item.Name != null)
            {
                RxApp.MainThreadScheduler.Schedule(() =>
                {
                    // Determine item classification color (AP standard)
                    Color itemColor;
                    if (e.Item.IsProgression)
                        itemColor = Color.FromRgb(160, 32, 240); // Purple
                    else if (e.Item.IsUseful)
                        itemColor = Color.FromRgb(0, 100, 255); // Blue
                    else if (e.Item.IsTrap)
                        itemColor = Color.FromRgb(255, 165, 0); // Orange
                    else
                        itemColor = Color.FromRgb(255, 255, 255); // White (filler)

                    var textSpan = new TextSpan { Text = e.Item.Name, TextColor = new SolidColorBrush(itemColor) };
                    var logItem = new LogListItem(e.Item.Name);
                    logItem.TextSpans.Clear();
                    logItem.TextSpans.Add(textSpan);
                    Context.LogList.Add(logItem);
                    Context.ItemList.Add(logItem);
                });
            }
        }

        private void Client_MessageReceived(object? sender, MessageReceivedEventArgs e)
        {
            var messageText = string.Concat(e.Message.Parts.Select(p => p.Text));
            
            if (e.Message.Parts.Any(x => x.Text == "[Hint]: "))
            {
                LogHint(e.Message);
            }
            else if (messageText.Contains("Cheat console:"))
            {
                // Parse !getitem message: "Cheat console: sending "ItemName" to SlotName"
                var match = System.Text.RegularExpressions.Regex.Match(messageText, @"sending\s+""(.+?)""\s+to\s+(\S+)");
                if (match.Success)
                {
                    var itemName = match.Groups[1].Value;
                    var targetSlot = match.Groups[2].Value;
                    
                    // Verify this command is for the current client's slot
                    if (Context?.Slot != targetSlot)
                    {
                        Log.Logger.Information("Ignoring cheat for slot '{TargetSlot}' (current slot: '{CurrentSlot}')", targetSlot, Context?.Slot);
                        return;
                    }
                    
                    // Look up item in Items.json by name
                    var itemEntry = Utils.Resources.Embedded.Items?.FirstOrDefault(kvp => kvp.Value.Name == itemName);
                    if (itemEntry?.Value != null)
                    {
                        long itemId = itemEntry.Value.Key;
                        Log.Logger.Information(messageText);
                        InventoryMgmt.GiveItem(itemId);
                    }
                    else
                    {
                        Log.Logger.Warning("Could not find item ID for cheat item '{ItemName}'", itemName);
                    }
                }
            }
            else
            {
                // Log regular messages with proper colors
                var partData = e.Message.Parts.Select(p => new { p.Text, p.Color }).ToList();
                
                RxApp.MainThreadScheduler.Schedule(() =>
                {
                    if (Context != null)
                    {
                        var spans = new List<TextSpan>();
                        foreach (var part in partData)
                        {
                            Color textColor = Color.FromRgb(part.Color.R, part.Color.G, part.Color.B);
                            spans.Add(new TextSpan { Text = part.Text, TextColor = new SolidColorBrush(textColor) });
                        }
                        Context.LogList.Add(new LogListItem(spans));
                    }
                });
            }
        }

        private static void LogHint(LogMessage message)
        {
            // Extract part data on background thread to avoid threading issues
            var partData = message.Parts.Select(p => new { p.Text, p.Color }).ToList();
            
            RxApp.MainThreadScheduler.Schedule(() =>
            {
                if (Context == null)
                    return;
                
                // Check if hint already exists
                var newMessage = message.Parts.Select(x => x.Text);
                if (Context.HintList.Any(x => x.TextSpans.Select(y => y.Text) == newMessage))
                {
                    return; //Hint already in list
                }
                
                // Create spans on UI thread
                var spans = new List<TextSpan>();
                foreach (var part in partData)
                {
                    Color textColor = Color.FromRgb(part.Color.R, part.Color.G, part.Color.B);
                    spans.Add(new TextSpan { Text = part.Text, TextColor = new SolidColorBrush(textColor) });
                }
                
                var logItem = new LogListItem(spans);
                Context.HintList.Add(logItem);
                Context.LogList.Add(logItem);  // Also add to log like other messages
                
            });
        }

        private static void OnConnected(object? sender, EventArgs? args)
        {
            Log.Logger.Information("Connected to Archipelago");
            Log.Logger.Information($"Playing {Client.CurrentSession.ConnectionInfo.Game} as {Client.CurrentSession.Players.GetPlayerName(Client.CurrentSession.ConnectionInfo.Slot)}");
        }

        private static void OnDisconnected(object? sender, EventArgs? args)
        {
            Log.Logger.Information("Disconnected from Archipelago");
        }

        private async void Reconnect(object? parameters)
        {
            int waitTime = 100;

            while (true)
            {
                if (!PlayerState.IsPlayerReady)
                {
                    PrepSeed();
                    AddressInit.InitializeAddresses();
                }

                if (Client.CurrentSession == null || !Client.CurrentSession.Socket.Connected)
                {
                    waitTime = 0;  // Setup for longer wait time on reconnect attempts

                    if (Client != null)
                    {
                        Client.Disconnect();

                        Client.Connected -= OnConnected;
                        Client.Disconnected -= OnDisconnected;
                        Client.MessageReceived -= Client_MessageReceived;

                        if (_deathlinkService != null)
                        {
                            _deathlinkService.OnDeathLinkReceived -= _deathlinkService_OnDeathLinkReceived;
                            _deathlinkService = null;
                        }
                    }

                    // Connect to archipelago server
                    Client = new ArchipelagoClient(Ys8Client);

                    Client.Connected += OnConnected;

                    await Client.Connect(Context.Host, "Ys 8");

                    if (!Client.IsConnected && waitTime < 10_000)
                    {
                        if (!isShuttingDown)
                            Log.Logger.Warning("Failed to reconnect, retrying in {WaitTime}ms", waitTime + 1000);
                        waitTime += 1000;
                    }
                    else if (Client.IsConnected)
                    {
                        Client.Disconnected += OnDisconnected;
                        Client.MessageReceived += Client_MessageReceived;

                        await Client.Login(Context.Slot, !string.IsNullOrWhiteSpace(Context.Password) ? Context.Password : null);

                        Client.ItemManager.ItemReceived += Client_ItemReceived;
                        Client.ItemManager.ReceiveReady(Client.CurrentSession);

                        Log.Logger.Information("Reconnected to Archipelago");
                        waitTime = 100;
                    }
                }
                else
                {
                    while (locationQueue.TryDequeue(out Location? loc))
                    {
                        Client.SendLocationAsync(loc);
                    }
                }
            
                Thread.Sleep(waitTime);
            }
        }
    } 
}
