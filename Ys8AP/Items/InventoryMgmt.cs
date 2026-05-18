using Archipelago.Core.Util;
using Archipelago.MultiClient.Net.Models;
using Ys8AP.GlobalAddresses;
using Ys8AP.Threads;
using Ys8AP.Mem;
using Ys8AP.Threads;
using Ys8AP.Utils;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Logging;


namespace Ys8AP.Items
{
    internal class InventoryMgmt
    {
        internal const int PROGRESSIVE_SHOP_RANK_ID = 139;
        internal const int PROGRESSIVE_RAID_LIST_ID = 764;
        internal const int CASTAWAY_TRACKING_ID = 143;
        internal const int LANDMARK_TRACKING_ID = 148;
        internal const int PSYCHES_ITEM_ID = 831;
        private const int ESSENCE_STONE_BONUS_ID = 32803;
        
        internal static bool isVerifying = false;
        internal static bool processedStartingItems = true; // flag to allow starting items to output messages

        private static ConcurrentDictionary<long, InvItem>? ItemData = Resources.Embedded.Items;
        // Reverse lookup: APSaveID -> item key (string from Items.json)
        private static Dictionary<int, long>? APSaveIDToItemKey;
        private static readonly HashSet<string> FlagsSetTo2 = new() { "0x002C8B70", "0x002C8B94", "0x002C7D24" }; // when these flags are set, they need to be set to 2 instead of 1 to properly trigger events.
        private static Dictionary<int, int>? itemCounts;

        /// <summary>
        /// Handles all receiving logic for items, including setting flags, handling special item cases, and enforcing quantity limits.
        /// </summary>
        internal static void GiveItem(long itemId)
        {
            // ============================================================================
            // Item received, we retrieve item data and setup our new quantity.
            // ============================================================================
            if (ItemData == null || !ItemData.ContainsKey(itemId))
            {
                Log.Logger.Warning("GiveItem: unknown AP item ID {ItemId}, skipping", itemId);
                return;
            }

            InvItem receivedItem = ItemData[itemId];
            int currentQuantity = Memory.ReadUShort(Contexts.InventoryContext.GetItemQuantityAddress(receivedItem.ItemID));
            int newQuantity = currentQuantity + receivedItem.ItemQuantity;
            
            // ============================================================================
            // Special item handling, we give additional items for some items and things like 
            // progressive shop rank, TMemos, and crew members have special handling.
            // If we give extra items here we also handle all their quantities internally 
            // inside the conditional so the base item can continue to process as normal.
            // ============================================================================
            if (receivedItem.ItemID == PROGRESSIVE_SHOP_RANK_ID && currentQuantity >= 7 && !isVerifying) // Progressive shop rank, if we have 7 we give essences stone instead.
            {
                receivedItem = ItemData[ESSENCE_STONE_BONUS_ID]; // Essence Stone Bonus
                currentQuantity = Memory.ReadUShort(Contexts.InventoryContext.GetItemQuantityAddress(receivedItem.ItemID));
                newQuantity = currentQuantity + 5;
            }
            else if (receivedItem.ItemID == PROGRESSIVE_SHOP_RANK_ID && currentQuantity == 0) // Progressive shop rank, if it's the first one we give Kathleen.
            {
                Contexts.FlagEnumContext.SetNPCJoinState(5); // Kathleen Join Flag
                Memory.WriteByte(Contexts.GameContext.FlagEnumAddress + 0x002CB20C, 1); // DF_JOIN_KATRIN
                Memory.WriteByte(Contexts.GameContext.FlagEnumAddress + 0x002C7564, 1); // GF_02MP1201_JOIN_KATRIN

                Contexts.InventoryContext.CheckIfObtainedAndSet(CASTAWAY_TRACKING_ID); // Castaway item for tracking crew member obtained for work totals.
                int currentCastaways = Memory.ReadUShort(Contexts.InventoryContext.GetItemQuantityAddress(CASTAWAY_TRACKING_ID));
                Memory.WriteByte(Contexts.InventoryContext.GetItemQuantityAddress(CASTAWAY_TRACKING_ID), (byte)(currentCastaways + 1));
            }
            else if (receivedItem.Name == "Dina")
            {
                Contexts.InventoryContext.CheckIfObtainedAndSet(580);
                Memory.WriteByte(Contexts.InventoryContext.GetItemQuantityAddress(580), 1); // Give 1 Insect Repellent
            }
            else if (receivedItem.ItemID == PROGRESSIVE_RAID_LIST_ID && currentQuantity == 0) // TMemo Intercept unlocks - progressive unlock based on current count
            {
                Contexts.FlagEnumContext.SetNPCJoinState(1); // Dogi Join Flag
                Memory.WriteByte(Contexts.GameContext.FlagEnumAddress + 0x002CB1F8, 1); // DF_JOIN_DOGI

                Contexts.InventoryContext.CheckIfObtainedAndSet(CASTAWAY_TRACKING_ID); // Castaway item for tracking crew member obtained for work totals.
                int currentCastaways = Memory.ReadUShort(Contexts.InventoryContext.GetItemQuantityAddress(CASTAWAY_TRACKING_ID));
                Memory.WriteByte(Contexts.InventoryContext.GetItemQuantityAddress(CASTAWAY_TRACKING_ID), (byte)(currentCastaways + 1));
            }
            else if (receivedItem.ItemID == 629) // Fishing rod
            {
                Contexts.InventoryContext.CheckIfObtainedAndSet(628);
                int currentBaitQuantity = Memory.ReadUShort(Contexts.InventoryContext.GetItemQuantityAddress(628));
                int newBaitQuantity = currentBaitQuantity + 30;
                if (newBaitQuantity > 999)
                    newBaitQuantity = 999;
                Memory.WriteByte(Contexts.InventoryContext.GetItemQuantityAddress(628), (byte)newBaitQuantity); // Give 30 bait
            }
            else if (receivedItem.ItemID == 218) // Slash Medal
            {
                Contexts.InventoryContext.CheckIfObtainedAndSet(219);
                int currentPierceMedalQuantity = Memory.ReadUShort(Contexts.InventoryContext.GetItemQuantityAddress(219));
                int newPierceMedalQuantity = currentPierceMedalQuantity + 1;
                if (newPierceMedalQuantity > 99)
                    newPierceMedalQuantity = 1;
                Memory.WriteByte(Contexts.InventoryContext.GetItemQuantityAddress(219), (byte)newPierceMedalQuantity); // Pierce Medal
                
                Contexts.InventoryContext.CheckIfObtainedAndSet(220);
                int currentStrikeMedalQuantity = Memory.ReadUShort(Contexts.InventoryContext.GetItemQuantityAddress(220));
                int newStrikeMedalQuantity = currentStrikeMedalQuantity + 1;
                if (newStrikeMedalQuantity > 99)
                    newStrikeMedalQuantity = 1;
                Memory.WriteByte(Contexts.InventoryContext.GetItemQuantityAddress(220), (byte)newStrikeMedalQuantity); // Strike Medal
            }
            else if (receivedItem.ItemID == 206 && Options.FormerSanctuaryCrypt == 1) // Jade Pendant
            {
                receivedItem.Flags = ["0x002C8C44", "0x002C71B4"]; // SF_SYS_CLEARED, GF_SUBEV_PAST_07_CLEAR
            }

            // ============================================================================
            // Here we set flags for items that require event triggers. 
            // A few flags have values other than 1, which we handle as special cases.
            // ============================================================================
            if (receivedItem.Flags != null)
            {
                foreach (string flag in receivedItem.Flags)
                {

                    if (FlagsSetTo2.Contains(flag))
                    {
                        Memory.WriteByte(Contexts.GameContext.FlagEnumAddress + Convert.ToUInt32(flag, 16), 2);
                    }
                    else if (flag == "0x002C7D0C")
                    {
                        Memory.WriteByte(Contexts.GameContext.FlagEnumAddress + Convert.ToUInt32(flag, 16), 7); // GF_CAMP_SHIPYARD_LV
                    }
                    else
                    {
                        Memory.WriteByte(Contexts.GameContext.FlagEnumAddress + Convert.ToUInt32(flag, 16), 1);
                    }
                }
            }

            // ============================================================================
            // Simplest part of the item code, we enforce quantity limits and then write the new quantity to memory.
            // ============================================================================
            if (newQuantity > receivedItem.QuantityMax && !Contexts.FlagEnumContext.GetInfernoFlag())
            {
                newQuantity = receivedItem.QuantityMax;
            }
            else if (newQuantity > receivedItem.QuantityMaxInferno && Contexts.FlagEnumContext.GetInfernoFlag())
            {
                newQuantity = receivedItem.QuantityMaxInferno;
            }

            // ============================================================================
            // Okay maybe this is the simplest, we just set the APSaveID used to track remote
            // items so we can verify them later and prevent item loss via 
            // retries, crashes, file loads, etc..
            // ============================================================================
            Contexts.FlagEnumContext.SetAPSaveValue((uint)receivedItem.APSaveID);

            // ============================================================================
            // Based on the item type we have a final piece to how we have to write the data.
            // We also give our tracking items here. If it's a basic consumable we just give
            // it with little extra processing.
            // ============================================================================
            if (receivedItem.CrewMember)
            {
                Contexts.InventoryContext.CheckIfObtainedAndSet(CASTAWAY_TRACKING_ID); // Castaway item for tracking crew member obtained for work totals.
                int currentCastaways = Memory.ReadUShort(Contexts.InventoryContext.GetItemQuantityAddress(CASTAWAY_TRACKING_ID));
                Memory.WriteByte(Contexts.InventoryContext.GetItemQuantityAddress(CASTAWAY_TRACKING_ID), (byte)(currentCastaways + 1));

                if (receivedItem.CrewJoinID != null)
                {
                    Contexts.FlagEnumContext.SetNPCJoinState((int)receivedItem.CrewJoinID);
                }

                // if we're receiving the starting character then we havne't yet processed starting items
                // this allows the received messages for starting items to be printed.
                if(receivedItem.Name == Options.StartingCharacter)
                    processedStartingItems = false; 

                // Handle starting skills for party members.
                if (Options.StartingSkills.TryGetValue(receivedItem.Name, out var skillIds))
                {
                    foreach (int skillId in skillIds)
                    {
                        try { GiveItem(skillId); }
                        catch (Exception ex) { Log.Logger.Error(ex, "GiveItem: failed to give starting skill {SkillId} for {Name}", skillId, receivedItem.Name); }
                    }
                }

            }
            else if (receivedItem.Landmark)
            {
                Contexts.InventoryContext.CheckIfObtainedAndSet(LANDMARK_TRACKING_ID); // Landmark item for tracking totals.
                int currentLandmarks = Memory.ReadUShort(Contexts.InventoryContext.GetItemQuantityAddress(LANDMARK_TRACKING_ID));
                Memory.WriteByte(Contexts.InventoryContext.GetItemQuantityAddress(LANDMARK_TRACKING_ID), (byte)(currentLandmarks + 1));
            }
            else if (receivedItem.Skill)
            {
                Skill CurrentSkill = Contexts.InventoryContext.GetSkillByCharacterAndID((uint)receivedItem.SkillID, (uint)receivedItem.SkillCharacterID);
                CurrentSkill.SkillLevel = 1;
                CurrentSkill.SkillExperience = 0;
                Contexts.InventoryContext.SetSkillByCharacterAndID((uint)receivedItem.SkillID, (uint)receivedItem.SkillCharacterID, CurrentSkill);
            }
            else
            {
                Contexts.InventoryContext.CheckIfObtainedAndSet(receivedItem.ItemID);
                Memory.WriteByte(Contexts.InventoryContext.GetItemQuantityAddress(receivedItem.ItemID), (byte)newQuantity);
            }

            // ============================================================================
            // Finally we log the item as recieved. We print any messages unless this item
            // is being given as part of the verification process, in which case we want to be silent to avoid spam.
            // ============================================================================
            if (!isVerifying || !processedStartingItems)
            {
                // starting character skills get processed before the starting character
                // due to some recursive calls, so by the time the starting character makes it here
                // we're finished processing starting items. 
                if(receivedItem.Name == Options.StartingCharacter)
                    processedStartingItems = true; 
        
                string msg = "Received " + receivedItem.Name + ".";
                if (PlayerState.IsPlayerReady)
                {
                    ItemQueue.AddMsg(msg);
                }
                else
                {
                    Log.Logger.Information(msg);
                    App.Client.AddOverlayMessage(msg);
                }
            }
            
        }

        /// <summary>
        /// Compares the GameState item counts to how many of each item are saved to memory, giving the player the difference.
        /// </summary>
        internal static void VerifyItems()
        {
            isVerifying = true;
            
            // Build APSaveID to item key lookup if not already built
            if (APSaveIDToItemKey == null)
            {
                APSaveIDToItemKey = new Dictionary<int, long>();
                // ItemData is ConcurrentDictionary<long, InvItem> where key is the item key as long
                foreach (var item in ItemData)
                {
                    // Convert the long key to string to match Items.json keys
                    APSaveIDToItemKey[item.Value.APSaveID] = item.Key;
                }
            }

            // Clear current values, check what the server thinks first, then compare that against the save file.
            ItemQueue.ClearItemQueues();
            
            if (itemCounts == null)
            {
                itemCounts = new Dictionary<int, int>();
            }
            else
            {
                itemCounts.Clear();
            }

            foreach (ItemInfo itemInfo in App.Client.CurrentSession.Items.AllItemsReceived)
            {
                // Skip items that are local to the current player's world
                if (itemInfo.Player == App.Client.CurrentSession.ConnectionInfo.Slot)
                    continue;

                long apId = itemInfo.ItemId;
                if (ItemData == null || !ItemData.ContainsKey(apId))
                {
                    Log.Logger.Warning("VerifyItems: unknown AP item ID {ApId}, skipping", apId);
                    continue;
                }
                InvItem receivedItem = ItemData[apId];
                if (!itemCounts.ContainsKey(receivedItem.APSaveID))
                    itemCounts[receivedItem.APSaveID] = 0;
                itemCounts[receivedItem.APSaveID] += 1;
            }

            foreach (var item in itemCounts)
            {
                int receivedItemCount = Contexts.FlagEnumContext.GetAPSaveValue((uint)item.Key);
                if (receivedItemCount < item.Value)
                {
                    int quantityToGive = item.Value - receivedItemCount;
                    if (!APSaveIDToItemKey.ContainsKey(item.Key))
                    {
                        Log.Logger.Warning("VerifyItems: no item key for APSaveID {Key}, skipping", item.Key);
                        continue;
                    }
                    for (int i = 0; i < quantityToGive; i++)
                    {
                        GiveItem(APSaveIDToItemKey[item.Key]);
                        Log.Logger.Debug("VerifyItems: Gave item with APSaveID {Key} to player, {Current}/{Expected} received", item.Key, receivedItemCount + i + 1, item.Value);
                    }
                }
            }
            
            isVerifying = false;
        }
    }
}
