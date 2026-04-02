using Archipelago.Core.Util;
using Ys8AP.GlobalAddresses;
using Ys8AP.Mem;
using Ys8AP;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using System;
using Ys8AP.Utils;
using Ys8AP.Items;
using ReactiveUI;
using Archipelago.Core.Models;
using System.Threading.Tasks;

namespace Ys8AP.Threads
{
    /// <summary>
    /// More complex monitoring of memory/game state than Memory.Monitor methods
    /// </summary>
    internal class LocationWatcher
    {
        private static ConcurrentDictionary<int, ChestLocation> ChestData = Resources.Embedded.ChestLocations;
        internal static void DoLoop(object? parameters)
        {
            while (true)
            {
                if (PlayerState.PlayerReady() && App.Client.IsConnected)
                {
                    CheckChests();
                }
                Thread.Sleep(1000);
            }
        }

        private static async Task CheckChests()
        {
            int ChestID;
            ChestLocation Chest;
            
            for (uint i = 0; i < 3; i++)
            {
                if (Contexts.FlagEnumContext.GetChestByID(i).ChestOpened == 0x30)
                {
                    ChestID = Convert.ToInt32(i);
                    Chest = ChestData[ChestID];
                    await Ys8AP.App.SendLocation(Chest.LocationID);
                }
            }
        }
    }
}
