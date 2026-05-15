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

        private Task? queueTask;
        private Task? locationWatcherTask;
        private Task? reconnectTask;
        private Task? PartyWatcherTask;
        private GameClient? Ys8Client;
        private static DeathLinkService? _deathlinkService = null;
        private bool deathFromDeathlink = false;
        private static string slotName = "";
        private static string _apHost = "";
        private static string _apSlot = "";
        private static string _apPassword = "";
        private static bool isShuttingDown = false;
        private static bool ys8ProcessRunning = false;  // Cached process state, updated in Reconnect loop
        private static bool ys8ProcessWasLost = false;  // Track if we lost the game process
        private static DateTime lastReattachAttempt = DateTime.MinValue;  // Prevent rapid reattach spam
        private static volatile bool _apDisconnected = false;  // Set by OnDisconnected event, cleared on successful (re)connect
        private static readonly List<string> Party = new() { "Adol", "Laxia", "Sahad", "Hummel", "Ricotta", "Dana" };
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);

            Context = new MainWindowViewModel() { ConnectButtonEnabled = true };
            Context.ConnectClicked += Context_ConnectClicked;
            Context.CommandReceived += (_, a) => Client?.SendMessage(a.Command);

            Context.Host = "archipelago.gg:";
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
                try
                {
                    Client.Disconnect();
                }
                catch (NullReferenceException)
                {
                    // Client might not be fully initialized
                    Log.Logger.Warning("Disconnect threw NullReferenceException, continuing with cleanup");
                }
                
                // Give threads a moment to exit their loops before nullifying Client
                Thread.Sleep(200);
                Client = null;

                if (_deathlinkService != null)
                {
                    _deathlinkService.OnDeathLinkReceived -= _deathlinkService_OnDeathLinkReceived;
                    _deathlinkService = null;
                }
                
                // Clear task references
                queueTask = null;
                locationWatcherTask = null;
                PartyWatcherTask = null;
            }
        
            isShuttingDown = false;  // Cleanup done, allow Reconnect loop to resume
            ys8ProcessWasLost = false;  // Reset so the warning can fire again if process dies
            _apDisconnected = false;  // Reset disconnect flag on fresh connect
            _apHost = e.Host ?? "localhost:38281";
            _apSlot = e.Slot ?? "Player1";
            _apPassword = e.Password ?? "";
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
                Options.ParseSlotData(slotData);
            }
            catch (FormatException)
            {
                Log.Logger.Error("Failed to parse options");
                Context.ConnectButtonEnabled = true;
                return;
            }

            if (reconnectTask == null)
            {
                reconnectTask = Task.Run(Reconnect);
                Thread.Sleep(100);
            }

            // If the player isn't in a valid game state it's likely due to the inventory address not being loaded yet, so try to initialize addresses and check again.  
            
            _deathlinkService = PartyWatcher.InitializeDeathLink(Client, Options.DeathLinkEnabled, _deathlinkService_OnDeathLinkReceived);

            if (queueTask == null)
            {
                ItemQueue.runThread = true;
                queueTask = ItemQueue.ThreadLoop();
                Thread.Sleep(100);
            }

            if (locationWatcherTask == null && Client.IsConnected)
            {
                locationWatcherTask = LocationWatcher.DoLoop();
                Thread.Sleep(100);
            }

            if (PartyWatcherTask == null && Client.IsConnected)
            {
                PartyWatcherTask = PartyWatcher.DoLoop();
                Thread.Sleep(100);
            }

            Context.ConnectButtonEnabled = true;
        }
        #region Ys8

        private GameClient? Ys8Connect()
        {
            // Check if the Ys 8 process is actually running
            UpdateYs8ProcessState();  // Initialize cache
            if (!ys8ProcessRunning)
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

        internal static bool IsYs8ProcessRunning()
        {
            return ys8ProcessRunning;
        }
        
        private static void UpdateYs8ProcessState()
        {
            var ys8Process = System.Diagnostics.Process.GetProcessesByName("ys8").FirstOrDefault();
            ys8ProcessRunning = ys8Process != null;
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
            string message = "DeathLink: Received from " + deathLink.Source;
            if(!PlayerState.IsPlayerReady || !PlayerState.NotInTown())
            {
                Log.Logger.Information("Awaiting Death...");
                PartyWatcher.SetDeathLinkIncoming(message);
            }
            else
            {
                PartyWatcher.SetDeathLinkIncoming(message);
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
                    // Determine item classification color (matches server-side colors)
                    Color itemColor;
                    if (e.Item.IsProgression)
                        itemColor = Color.FromRgb(221, 160, 221); // Plum (progression)
                    else if (e.Item.IsUseful)
                        itemColor = Color.FromRgb(106, 90, 205); // SlateBlue (useful)
                    else if (e.Item.IsTrap)
                        itemColor = Color.FromRgb(250, 128, 114); // Salmon (trap)
                    else
                        itemColor = Color.FromRgb(0, 255, 255); // Cyan (filler)

                    var textSpan = new TextSpan { Text = e.Item.Name, TextColor = new SolidColorBrush(itemColor) };
                    var logItem = new LogListItem(e.Item.Name);
                    logItem.TextSpans.Clear();
                    logItem.TextSpans.Add(textSpan);
                    Context.ItemList.Add(logItem);
                });
            }
        }

        private void Client_MessageReceived(object? sender, MessageReceivedEventArgs e)
        {
            var messageText = string.Concat(e.Message.Parts.Select(p => p.Text));
            Log.Logger.Debug("MessageReceived: {MsgType} | {MsgText}", e.Message.GetType().Name, messageText);
            
            if (e.Message.Parts.Any(x => x.Text == "[Hint]: "))
            {
                LogHint(e.Message);
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

            // Handle starting skills for locally-found party members.
            // Client_ItemReceived doesn't fire for local items, so we detect them via chat messages.
            foreach (var characterName in Party)
            {
                if (messageText.StartsWith(slotName) &&
                    messageText.Contains($"found their {characterName}") &&
                    Options.StartingSkills.TryGetValue(characterName, out var skillIds))
                {
                    foreach (int skillId in skillIds)
                    {
                        try { InventoryMgmt.GiveItem(skillId); }
                        catch (Exception ex) { Log.Logger.Error(ex, "MessageReceived: failed to give starting skill {SkillId} for {Name}", skillId, characterName); }
                    }
                }
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
            if (!isShuttingDown)
            {
                _apDisconnected = true;
                Log.Logger.Warning("Disconnected from Archipelago. Attempting to reconnect...");
            }
        }

        private async Task Reconnect()
        {
            int waitTime = 100;
            bool reconnectClientReady = false;  // True once we've swapped to a fresh client for the current disconnect episode
            // Give initial connection time to settle before starting monitor loop
            await Task.Delay(2000);

            while (true)
            {
                try
                {
                    if (isShuttingDown || Client == null)
                    {
                        reconnectClientReady = false;
                        await Task.Delay(1000);
                        continue;
                    }

                    UpdateYs8ProcessState();  // Cache the current process state

                    // If game process died, mark it and don't try to access memory
                    if (!ys8ProcessRunning && !ys8ProcessWasLost)
                    {
                        Log.Logger.Warning("Ys8 process lost. Please restart Ys8 and reconnect.");
                        ys8ProcessWasLost = true;
                    }

                    // Reset backoff quickly when a fresh disconnect is signaled so we retry promptly
                    if (_apDisconnected)
                        waitTime = Math.Min(waitTime, 2000);

                    bool needsReconnect = _apDisconnected || Client == null || !Client.IsConnected ||
                        Client.CurrentSession == null || Client.CurrentSession.Socket?.Connected == false;

                    if (needsReconnect)
                    {
                        bool suppressOutput = waitTime > 6_000;

                        // Check if ys8.exe is still running
                        if (!IsYs8ProcessRunning())
                        {
                            if (!suppressOutput)
                                Log.Logger.Warning("Reconnect skipped: Ys8 process is not running.");
                            waitTime = Math.Min(waitTime + 1000, 10_000);
                            await Task.Delay(waitTime);
                            continue;
                        }

                        if (Ys8Client == null)
                        {
                            if (!suppressOutput)
                                Log.Logger.Warning("Reconnect skipped: game client (Ys8Client) is null.");
                            waitTime = Math.Min(waitTime + 1000, 10_000);
                            await Task.Delay(waitTime);
                            continue;
                        }

                        if (!suppressOutput)
                            Log.Logger.Information("Attempting AP reconnect to {Host} as {Slot}...", _apHost, _apSlot);

                        // Only swap to a fresh ArchipelagoClient once per disconnect episode.
                        // Reusing the same client for retries avoids leaking Timer objects
                        // (ArchipelagoClient starts a System.Threading.Timer in its constructor
                        //  that holds a strong GC root; without Dispose() it is never collected).
                        if (!reconnectClientReady)
                        {
                            var oldClient = Client;
                            Client.Connected -= OnConnected;
                            Client.Disconnected -= OnDisconnected;
                            Client.MessageReceived -= Client_MessageReceived;
                            if (Client.ItemManager != null)
                                Client.ItemManager.ItemReceived -= Client_ItemReceived;

                            if (_deathlinkService != null)
                            {
                                _deathlinkService.OnDeathLinkReceived -= _deathlinkService_OnDeathLinkReceived;
                                _deathlinkService = null;
                            }

                            // Dispose old client in background so its Timer is collected and
                            // the ProcessExit handler accumulation is cleaned up.
                            // SaveGameStateAsync in Dispose() returns immediately when _gameStateManager is null.
                            _ = Task.Run(() =>
                            {
                                try { oldClient.Dispose(); }
                                catch (Exception ex) { Log.Logger.Debug("Old client dispose threw {ExType}: {ExMsg}", ex.GetType().Name, ex.Message); }
                            });

                            Client = new ArchipelagoClient(Ys8Client);
                            Client.Connected += OnConnected;
                            reconnectClientReady = true;
                        }

                        await Client.Connect(_apHost, "Ys 8");

                        if (!Client.IsConnected)
                        {
                            if (!isShuttingDown)
                            {
                                waitTime = Math.Min(Math.Max(waitTime * 2, 2_000), 10_000);
                                if (!suppressOutput)
                                    Log.Logger.Warning("Failed to reconnect, retrying in {WaitTime}ms", waitTime);
                            }
                        }
                        else
                        {
                            Client.Disconnected += OnDisconnected;
                            Client.MessageReceived += Client_MessageReceived;

                            await Client.Login(_apSlot, !string.IsNullOrWhiteSpace(_apPassword) ? _apPassword : null,
                                Archipelago.MultiClient.Net.Enums.ItemsHandlingFlags.IncludeStartingInventory);

                            if (!Client.IsConnected || !Client.IsLoggedIn)
                            {
                                if (!isShuttingDown)
                                {
                                    waitTime = Math.Min(Math.Max(waitTime * 2, 2_000), 10_000);
                                    if (!suppressOutput)
                                        Log.Logger.Warning("Reconnect login failed, retrying in {WaitTime}ms", waitTime);
                                }
                            }
                            else
                            {
                                Client.ItemManager.ItemReceived += Client_ItemReceived;
                                Client.ItemManager.ReceiveReady(Client.CurrentSession);

                                _deathlinkService = PartyWatcher.InitializeDeathLink(Client, Options.DeathLinkEnabled, _deathlinkService_OnDeathLinkReceived);
                                ItemQueue.checkItems = true;
                                _apDisconnected = false;
                                reconnectClientReady = false;  // Reset for next disconnect episode

                                Log.Logger.Information("Reconnected to Archipelago");
                                waitTime = 100;
                            }
                        }
                    }
                    else
                    {
                        reconnectClientReady = false;  // Healthy — reset in case we somehow skipped the success path
                        while (locationQueue.TryDequeue(out Location? loc))
                        {
                            Client.SendLocationAsync(loc);
                        }
                    }
                
                    await Task.Delay(waitTime);
                }
                catch (Exception ex)
                {
                    Log.Logger.Error(ex, "Unexpected error in Reconnect loop");
                    reconnectClientReady = false;
                    waitTime = Math.Min(waitTime + 1000, 10_000);
                    await Task.Delay(waitTime);
                }
            }
        }
    } 
}
