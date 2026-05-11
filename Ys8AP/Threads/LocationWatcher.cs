using Archipelago.Core.Util;
using Ys8AP.GlobalAddresses;
using Ys8AP.Threads;
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
        private const byte CHEST_OPENED_FLAG = 0x30;
        
        private static ConcurrentDictionary<int, ChestLocation> ChestData = Resources.Embedded.ChestLocations;
        private static ConcurrentDictionary<int, EventLocation> EventData = Resources.Embedded.EventLocations;
        private static HashSet<long>? allLocationIds;
        private static bool goalCompleted = false;

        internal static async Task DoLoop()
        {
            if (allLocationIds == null && App.Client?.CurrentSession != null)
            {
                allLocationIds = App.Client.CurrentSession.Locations.AllLocations.ToHashSet();
            }

            while (App.Client != null)
            {
                if (PlayerState.IsPlayerReady)
                {
                    CheckChests();
                    CheckEvents();
                    WatchGoal();
                }
                await Task.Delay(1000);
            }
        }

        private static async Task CheckChests()
        {
            foreach (int ChestID in ChestData.Keys)
            {
                ChestParams chestParams = Contexts.FlagEnumContext.GetChestByID((uint)ChestID);
                int locationID = ChestData[ChestID].LocationID;
                
                // Corpses and Driftage (ChestID 901-912) use NonChestEventFlag
                // Regular chests use ChestOpened flag
                bool isCorpseOrDriftage = ChestID >= 901 && ChestID <= 912;
                byte chestFlag = isCorpseOrDriftage 
                    ? chestParams.NonChestEventFlag 
                    : chestParams.ChestOpened;
                
                // Corpses and Driftage (ChestID 901-912) are single events, check for exactly 1
                // Regular chests check for the opened flag (0x30)
                bool isOpened = isCorpseOrDriftage
                    ? chestFlag == 1 
                    : chestFlag == CHEST_OPENED_FLAG;
                
                if (isOpened)
                {
                    if (allLocationIds.Contains(locationID))
                        await App.SendLocation(locationID);
                }
            }
        }

        private static async Task CheckEvents()
        {
            foreach (EventLocation eventLoc in EventData.Values)
            {   
                byte eventCheck = Memory.ReadByte(Contexts.GameContext.FlagEnumAddress + Convert.ToUInt32(eventLoc.Flag, 16));
                if (eventLoc.Name == "The Ruins of Eternia Central Stupa Jade Pendant" && eventCheck != 98)
                    eventCheck = 0; // Special case for this event which has a non-standard flag value when completed, 
                                    // to avoid false positives before the event is actually completed.

                if (eventCheck >= 1)
                {
                    foreach (int check in eventLoc.LocationIDs)
                    {
                        if (allLocationIds.Contains(check)) // Only check events that are actually in the AP pool
                            await App.SendLocation(check);
                    }
                }
            }
        }

        private static void WatchGoal()
        {
            if (!goalCompleted && Contexts.FlagEnumContext.GetGoalFlag() && PlayerState.IsPlayerReady)
            {
                App.Client.SendGoalCompletion();
                goalCompleted = true;
            }
        }
    }
}
