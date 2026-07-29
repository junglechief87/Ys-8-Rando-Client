using Archipelago.Core.Util;
using Ys8AP.GlobalAddresses;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System;
using Ys8AP.Utils;
using Ys8AP.Locations;
using System.Threading.Tasks;
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
        private static string lastMapSent = "";
        private static string mapID = "";
        private static string SlotID = "";
        private static int randomizedEntranceInd = 0; // Initialized to an invalid entry ID to ensure the first entry is sent to AP on game start

        internal static async Task DoLoop()
        {
            if (allLocationIds == null && App.Client?.CurrentSession != null)
            {
                allLocationIds = App.Client.CurrentSession.Locations.AllLocations.ToHashSet();
            }

            if (SlotID == "" && App.Client?.CurrentSession != null)
            {
                SlotID = "Ys8_" + App.Client.CurrentSession.ConnectionInfo.Team + "_" + App.Client.CurrentSession.ConnectionInfo.Slot + "_";
            }

            while (App.Client != null)
            {
                if (PlayerState.IsPlayerReady)
                {
                    CheckChests();
                    CheckEvents();
                    WatchGoal();

                    mapID = Contexts.InventoryContext.GetMapID();
                    mapID = mapID.Replace("\0", string.Empty); // Remove null terminator if present
                    if (!mapID.StartsWith(lastMapSent))
                        randomizedEntranceInd = Contexts.FlagEnumContext.GetRandomizedEntrance();

                    if (randomizedEntranceInd != 0)
                        mapID += "*"; 
                    
                    if (lastMapSent != mapID)
                        SendMapID(mapID);
                }
                await Task.Delay(200);
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

        private static void SendMapID(string mapID)
        {
            App.Client.CurrentSession.DataStorage[SlotID + "current_map"] = mapID;
            lastMapSent = mapID;
            Contexts.FlagEnumContext.SetRandomizedEntrance(false); // Reset the flag so it's ready for the next randomized entrance trigger
        }
    }
}
