using Archipelago.Core.Util;
using Ys8AP.Constants;
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

namespace Ys8AP.Threads
{
    /// <summary>
    /// More complex monitoring of memory/game state than Memory.Monitor methods
    /// </summary>
    internal class HelperThread
    {
        private static ConcurrentDictionary<int, ChestLocation> ChestData = Resources.Embedded.Locations;
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

        private static void CheckChests()
        {
            int ChestID;
            ChestLocation Chest;
            ulong ChestOpenFlag;
            
            for (uint i = 0; i < 3; i++)
            {
                ChestOpenFlag=FlagEnum.GetFlagByte(i, GlobalAddresses.ChestFlagStart + 2);
                if (ChestOpenFlag == 0x30)
                {
                    ChestID = Convert.ToInt32(i);
                    Chest = ChestData[ChestID];
                    Ys8AP.App.SendLocation(Chest.LocationID);
                }
            }
        }
    }
}
