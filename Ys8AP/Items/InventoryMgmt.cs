using Archipelago.Core.Util;
using Archipelago.MultiClient.Net.Models;
using Ys8AP.GlobalAddresses;
using Ys8AP.Threads;
using Ys8AP.Utils;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Ys8AP.Items
{
    internal class InventoryMgmt
    {
        internal const int PROGRESSIVE_SHOP_RANK_ID = 139;
        internal const int CASTAWAY_TRACKING_ID = 143;
        internal const int LANDMARK_TRACKING_ID = 148;
        internal const int PSYCHES_ITEM_ID = 831;
        private const int ESSENCE_STONE_ID = 32800;
        
        internal static bool isVerifying = false;
        
        private static ConcurrentDictionary<long, InvItem>? ItemData = Resources.Embedded.Items;
        // Reverse lookup: APSaveID -> item key (string from Items.json)
        private static Dictionary<int, long>? APSaveIDToItemKey;
        private static readonly HashSet<string> FlagsSetTo2 = new() { "0x002C8B70", "0x002C8B94", "0x002C7D24" }; // when these flags are set, they need to be set to 2 instead of 1 to properly trigger events.
        private static readonly HashSet<long> TMemos = new() { 760, 761, 762, 763 };
        private static Dictionary<int, int>? itemCounts;

        /// <summary>
        /// Handles all receiving logic for items, including setting flags, handling special item cases, and enforcing quantity limits.
        /// </summary>
        internal static void GiveItem(long itemId)
        {   
            InvItem receivedItem = ItemData[itemId];
            int currentQuantity = Memory.ReadUShort(Contexts.InventoryContext.GetItemQuantityAddress(receivedItem.ItemID));
            int newQuantity = currentQuantity + receivedItem.ItemQuantity;
            
            // handle special items
            if (receivedItem.ItemID == PROGRESSIVE_SHOP_RANK_ID && currentQuantity >= 7) // Progressive shop rank, if we have 7 we give essences stone instead.
            {
                receivedItem = ItemData[ESSENCE_STONE_ID]; // Essence Stone
                currentQuantity = Memory.ReadUShort(Contexts.InventoryContext.GetItemQuantityAddress(receivedItem.ItemID));
                newQuantity = currentQuantity + 5;
            }
            else if (receivedItem.ItemID == PROGRESSIVE_SHOP_RANK_ID && currentQuantity == 0) // Progressive shop rank, if it's the first one we give Kathleen.
            {
                Contexts.FlagEnumContext.SetNPCJoinState(5); // Kathleen Join Flag
                Memory.WriteByte(Contexts.GameContext.FlagEnumAddress + 0x002CB20C, 1); // DF_JOIN_KATRIN
                Memory.WriteByte(Contexts.GameContext.FlagEnumAddress + 0x002C7564, 1); // GF_02MP1201_JOIN_KATRIN
            }
            else if (receivedItem.Name == "Dina")
            {
                Contexts.InventoryContext.CheckIfObtainedAndSet(580);
                Memory.WriteByte(Contexts.InventoryContext.GetItemQuantityAddress(580), 0x63); // Give 99 Insect Repellent
            }
            else if (TMemos.Contains(receivedItem.ItemID)) // TMemo Intercept unlocks - progressive unlock based on current count
            {
                // Count how many TMemos are already unlocked
                int unlockedCount = 0;
                if (Contexts.FlagEnumContext.GetTMemo1()) unlockedCount++;
                if (Contexts.FlagEnumContext.GetTMemo2()) unlockedCount++;
                if (Contexts.FlagEnumContext.GetTMemo3()) unlockedCount++;
                if (Contexts.FlagEnumContext.GetTMemo4()) unlockedCount++;

                // Unlock the next TMemo based on current count
                switch (unlockedCount)
                {
                    case 0:
                        Memory.WriteByte(Contexts.GameContext.FlagEnumAddress + 0x002CA578, 1); // Intercept List 1
                        break;
                    case 1:
                        Memory.WriteByte(Contexts.GameContext.FlagEnumAddress + 0x002CA57C, 1); // Intercept List 2
                        Memory.WriteByte(Contexts.GameContext.FlagEnumAddress + 0x002CA560, 1); // Dogi Control Option Unlocked
                        break;
                    case 2:
                        Memory.WriteByte(Contexts.GameContext.FlagEnumAddress + 0x002CA580, 1); // Intercept List 3
                        break;
                    case 3:
                        Memory.WriteByte(Contexts.GameContext.FlagEnumAddress + 0x002CA584, 1); // Intercept List 4
                        break;
                }
            }
            else if (receivedItem.ItemID == 629) // Fishing rod
            {
                Contexts.InventoryContext.CheckIfObtainedAndSet(628);
                int currentBaitQuantity = Memory.ReadUShort(Contexts.InventoryContext.GetItemQuantityAddress(628));
                int newBaitQuantity = currentBaitQuantity + 30;
                if (newBaitQuantity > 999)
                    newBaitQuantity = 30;
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

            // handle event flags
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

            // handle item quantity limits
            if (currentQuantity > receivedItem.QuantityMax)
            {
                newQuantity = receivedItem.QuantityMax;
            }
            else if (currentQuantity > receivedItem.QuantityMaxInferno && Contexts.FlagEnumContext.GetInfernoFlag())
            {
                newQuantity = receivedItem.QuantityMaxInferno;
            }

            Contexts.FlagEnumContext.SetAPSaveValue((uint)receivedItem.APSaveID);

            if (receivedItem.CrewMember)
            {
                Contexts.InventoryContext.CheckIfObtainedAndSet(CASTAWAY_TRACKING_ID); // Castaway item for tracking crew member obtained for work totals.
                Memory.WriteByte(Contexts.InventoryContext.GetItemQuantityAddress(CASTAWAY_TRACKING_ID), (byte)newQuantity);

                if (receivedItem.CrewJoinID != null)
                {
                    Contexts.FlagEnumContext.SetNPCJoinState((int)receivedItem.CrewJoinID);
                }
            }
            else if (receivedItem.Landmark)
            {
                Contexts.InventoryContext.CheckIfObtainedAndSet(LANDMARK_TRACKING_ID); // Landmark item for tracking totals.
                Memory.WriteByte(Contexts.InventoryContext.GetItemQuantityAddress(LANDMARK_TRACKING_ID), (byte)newQuantity);
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

            if (!isVerifying)
            {
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
                    for (int i = 0; i < quantityToGive; i++)
                    {
                        ItemQueue.AddItem(APSaveIDToItemKey[item.Key]);
                    }
                }
            }
            
            isVerifying = false;
        }
    }
}
