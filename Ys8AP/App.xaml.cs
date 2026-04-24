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
        private static ulong baseAddress = 0;
        private static readonly ConcurrentQueue<Location> locationQueue = new();

        private Thread? queueThread;
        private Thread? locationWatcherThread;
        private Thread? reconnectThread;
        private GameClient? Ys8Client;
        private DeathLinkService? _deathlinkService = null;
        private bool deathFromDeathlink = false;
        private string slotName = "";

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
        }

        private async void Context_ConnectClicked(object? sender, ConnectClickedEventArgs e)
        {
            if (e.Host != null && e.Host.StartsWith("/connect ")) e.Host = e.Host.Substring("/connect ".Length); // trim "/connect " off front
            // trim extra spaces before defaulting
            if (e.Host != null) e.Host.Trim();
            if (e.Slot != null) e.Slot.Trim();
            // default to most basic local-hosted setup if they were empty
            if (e.Host == null || e.Host == "") e.Host = "localhost:38281";
            if (e.Slot == null || e.Slot == "") e.Slot = "Player1";

            if (Context == null)
                return;
                
            Context.ConnectButtonEnabled = false;
            Log.Logger.Information("Connecting...");

            if (Client != null)
            {
                Client.Connected -= OnConnected;
                Client.Disconnected -= OnDisconnected;
                Client.MessageReceived -= Client_MessageReceived;

                if (_deathlinkService != null)
                {
                    _deathlinkService.OnDeathLinkReceived -= _deathlinkService_OnDeathLinkReceived;
                    _deathlinkService = null;
                }
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
                if (!PlayerState.PlayerReady())
                {
                    Log.Logger.Warning("Inventory not connected, make sure you have loaded a save, are not in the main menu, or have started a new game.");
                }
            }
            
            Client.Connected += OnConnected;
            Client.Disconnected += OnDisconnected;

            await Client.Connect(e.Host, "Ys 8");
            
            if (!Client.IsConnected)
            {
                Log.Logger.Warning("Connect to AP Server failed");
                Context.ConnectButtonEnabled = true;
                return;

            }

            await Client.Login(e.Slot, !string.IsNullOrWhiteSpace(e.Password) ? e.Password : "");

            if (!Client.IsConnected || !Client.IsLoggedIn)
            {
                Context.ConnectButtonEnabled = true;
                return;
            }

            Client.ItemManager.ItemReceived += Client_ItemReceived;
            Client.ItemManager.ReceiveReady(Client.CurrentSession);
            Client.MessageReceived += Client_MessageReceived;

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
            if (PlayerState.PlayerReady())
            {
                PlayerReady(slotName);
            }
            else
            {
                AddressInit.InitializeAddresses();
            }

            /*
            if (Options.DeathLink)
            {
                _deathlinkService = Client.EnableDeathLink();
                _deathlinkService.OnDeathLinkReceived += _deathlinkService_OnDeathLinkReceived;
                ListenForDeath();
            }
            */

            if (queueThread == null)
            {
                
                queueThread = new Thread(new ParameterizedThreadStart(ItemQueue.ThreadLoop))
                {
                    IsBackground = true
                };
                queueThread.Start();
                
            }

            if (locationWatcherThread == null && Client.IsConnected)
            {
                locationWatcherThread = new Thread(new ParameterizedThreadStart(LocationWatcher.DoLoop))
                {
                    IsBackground = true
                };
                locationWatcherThread.Start();
            }

            Context.ConnectButtonEnabled = true;
        }
        #region Ys8

        private GameClient? Ys8Connect()
        {

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

        private void PlayerReady(string slotName)
        {
            Thread.Sleep(50);
            string currSlot = OpenMem.GetSlotName();

            // First load for this save, so do extra stuff
            if (currSlot == "")
            {
                OpenMem.SetSlotData(slotName);
            }
            else if (currSlot != slotName)
            {
                // Padding because Avalonia keeps cutting things off...
                Log.Logger.Error("Wrong slot name. Current save is using slot: " + currSlot + "      ");
                return;
            }
            else if (!OpenMem.TestRoomSeed())
            {
                return;
            }

            // Check for any missing items after a connect/reconnect
            //ItemQueue.checkItems = true;
            //WatchGoal();
        }

        private void PlayerNotReady(string slotName)
        {
            ItemQueue.ClearQueues();
            //Memory.MonitorAddressForAction<int>(MiscAddrs.TimeOfDayAddr, () => PlayerReady(slotName), (o) => { return o != 0; });
        }

        internal static async Task SendLocation(int locId)
        {
            Location loc = new()
            {
                Id = locId
            };

            if (Client.CurrentSession != null && Client.CurrentSession.Socket.Connected) 
                App.Client.SendLocationAsync(loc);
            else
                locationQueue.Enqueue(loc);
        }

        private void ListenForDeath()
        {
            /*
            for (int i = 0; i < MiscAddrs.HpAddrs.Length; i++)
            {
                uint addr = MiscAddrs.HpAddrs[i];
                short curValue = Memory.ReadShort(addr);

                // Connected while player is dead, don't send a death and wait for revive (or for the char to be recruited)
                if (curValue <= 0)
                    Memory.MonitorAddressForAction<short>(addr, () => HandleCharRevive(addr), (o) => { return o > 0; });
                else
                    Memory.MonitorAddressForAction<short>(addr, () => HandleCharDeath(addr), (o) => { return o <= 0; });
            }
            */
        }

        private void HandleCharDeath(uint addr)
        {
            return;
            /*
            // Don't death link on game reset
            if (PlayerState.PlayerReady() && !deathFromDeathlink)
            {
                DeathLink dl = new(slotName);
                _deathlinkService.SendDeathLink(dl);
                Log.Logger.Information("DeathLink: Sending Death to your friends...");
            }

            deathFromDeathlink = false;

            // Monitor for the char to be revived.
            Memory.MonitorAddressForAction<short>(addr, () => HandleCharRevive(addr), (o) => { return o > 0; });
            */
        }

        private void HandleCharRevive(uint addr)
        {
            Memory.MonitorAddressForAction<short>(addr, () => HandleCharDeath(addr), (o) => { return o <= 0; });
        }

        private static void WatchGoal()
        {
            return;
            /*
            // For some reason, the Boss Kill Flag doesn't set for Utan so use the floor kill count instead
            if (Options.Goal == 2)
                Memory.MonitorAddressForAction<byte>(MiscAddrs.UtanFlag, () => Client.SendGoalCompletion(), (o) => { return o != 0; });
            else
                Memory.MonitorAddressForAction<short>(MiscAddrs.BossKillAddr, () => Client.SendGoalCompletion(), (o) => { return o == Options.Goal * 100; });
                */
        }
        #endregion

        private void _deathlinkService_OnDeathLinkReceived(DeathLink deathLink)
        {
            return;
            /*
            // Kill player x_x
            if (PlayerState.IsPlayerInDungeon())
            {
                deathFromDeathlink = true;
                byte currChar = Memory.ReadByte(MiscAddrs.CurrCharAddr);
                Memory.Write(MiscAddrs.HpAddrs[currChar], (short)-1);
                Log.Logger.Information("DeathLink: Received from " + deathLink.Source);
            }
                */
        }

        private static void Client_ItemReceived(object? sender, ItemReceivedEventArgs e)
        {
            long itemId = e.Item.Id;
            ItemQueue.AddItem(itemId);
        }

        private void Client_MessageReceived(object? sender, MessageReceivedEventArgs e)
        {
            if (e.Message.Parts.Any(x => x.Text == "[Hint]: "))
            {
                LogHint(e.Message);
                // TODO fix hint logging with Avalonia
            }
            Log.Logger.Information(JsonConvert.SerializeObject(e.Message));
        }

        private static void LogHint(LogMessage message)
        {
            var newMessage = message.Parts.Select(x => x.Text);

            if (Context.HintList.Any(x => x.TextSpans.Select(y => y.Text) == newMessage))
            {
                return; //Hint already in list
            }
            List<TextSpan> spans = new List<TextSpan>();
            foreach (var part in message.Parts)
            {
                spans.Add(new TextSpan() { Text = part.Text, TextColor = new SolidColorBrush(Color.FromRgb(part.Color.R, part.Color.G, part.Color.B)) });
            }
            lock (_lockObject)
            {
                RxApp.MainThreadScheduler.Schedule(() =>
                {
                    Context.HintList.Add(new LogListItem(spans));
                });
            }
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
                    Client.Disconnected += OnDisconnected;

                    await Client.Connect(Context.Host, "Ys 8");

                    if (!Client.IsConnected && waitTime < 10_000)
                    {
                        waitTime += 1000;
                    }
                    else if (Client.IsConnected)
                    {
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
