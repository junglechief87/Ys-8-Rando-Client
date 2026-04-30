using Archipelago.Core.Util;
using Ys8AP.GlobalAddresses;
using Ys8AP.Mem;
using Ys8AP;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using System;
using Ys8AP.Utils;
using Ys8AP.Locations;
using ReactiveUI;
using Archipelago.Core.Models;
using System.Threading.Tasks;
using Silk.NET.GLFW;
using System.Linq;

namespace Ys8AP.Threads
{
    /// <summary>
    /// More complex monitoring of memory/game state than Memory.Monitor methods
    /// </summary>
    internal class LocationWatcher
    {
        private static ConcurrentDictionary<int, ChestLocation> ChestData = Resources.Embedded.ChestLocations;
        private static ConcurrentDictionary<int, EventLocation> EventData = Resources.Embedded.EventLocations;
        private static byte EventCheck;
        private static HashSet<long>? allLocationIds;

        internal static void DoLoop(object? parameters)
        {
            if (allLocationIds == null && App.Client.CurrentSession != null)
            {
                allLocationIds = App.Client.CurrentSession.Locations.AllLocations.ToHashSet();
            }

            while (true)
            {
                if (PlayerState.PlayerReady())
                {
                    CheckChests();
                    CheckEvents();
                }
                Thread.Sleep(1000);
            }
        }

        private static async Task CheckChests()
        {
            foreach (int ChestID in ChestData.Keys)
            {
                if (Contexts.FlagEnumContext.GetChestByID((uint)ChestID).ChestOpened == 0x30)
                {
                    if (allLocationIds.Contains(ChestData[ChestID].LocationID)) // Only check chests that are actually in the AP pool
                    {
                        await Ys8AP.App.SendLocation(ChestData[ChestID].LocationID);
                    }
                }
            }
        }

        private static async Task CheckEvents()
        {
            foreach (EventLocation eventLoc in EventData.Values)
            {   
                EventCheck = Memory.ReadByte(Contexts.GameContext.FlagEnumAddress + Convert.ToUInt32(eventLoc.Flag, 16));
                if (EventCheck >= 1)
                {
                    foreach (int check in eventLoc.LocationIDs)
                    {
                        if (allLocationIds.Contains(check)) // Only check events that are actually in the AP pool
                        {
                            await Ys8AP.App.SendLocation(check);
                        }
                    }
                }
            }
        }
    }
}
