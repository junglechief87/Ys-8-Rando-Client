using Archipelago.Core.Util;
using Archipelago.MultiClient.Net.Models;
using Ys8AP.GlobalAddresses;
using Ys8AP.Mem;
using Ys8AP.Threads;
using Ys8AP.Utils;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;


namespace Ys8AP.Items
{
    internal class InventoryMgmt
    {
        private static ConcurrentDictionary<long, InvItem>? ItemData = Resources.Embedded.Items;
        private static readonly HashSet<string> FlagsSetTo2 = new() { "0x002C8B70", "0x002C8B94", "0x002C7D24" }; // when these flags are set, they need to be set to 2 instead of 1 to properly trigger events.
        private static readonly HashSet<long> TMemos = new() { 760, 761, 762, 763 };

        /// <summary>
        /// Searches for an empty inventory slot and gives the player the item supplied.  Returns true if successful, false if inventory is full.
        /// </summary>
        /// <param name="itemId"></param>
        /// <returns></returns>
        internal static void GiveItem(long itemId)
        {   
            InvItem receivedItem = ItemData[itemId];
            int currentQuantity = Memory.ReadInt(Contexts.InventoryContext.GetItemQuantityAddress(receivedItem.ItemID));
            int newQuantity = currentQuantity + receivedItem.ItemQuantity;
            
            // handle special items
            if (receivedItem.ItemID == 139 && currentQuantity >= 7) // Progressive shop rank, if we have 7 we give essences stone instead.
            {
                receivedItem = ItemData[32800]; // Essence Stone
                currentQuantity = Memory.ReadInt(Contexts.InventoryContext.GetItemQuantityAddress(receivedItem.ItemID));
                newQuantity = currentQuantity + 5;
            }
            else if (receivedItem.ItemID == 139 && currentQuantity == 0) // Progressive shop rank, if it's the first one we give Kathleen.
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
            else if (TMemos.Contains(receivedItem.ItemID)) // TMemo Intercept unlocks
            {
                if (!Contexts.FlagEnumContext.TMemo1)
                {
                    Memory.WriteByte(Contexts.GameContext.FlagEnumAddress + 0x002CA578, 1); // Intercept List 1
                }
                else if (!Contexts.FlagEnumContext.TMemo2)
                {
                    Memory.WriteByte(Contexts.GameContext.FlagEnumAddress + 0x002CA57C, 1); // Intercept List 2
                    Memory.WriteByte(Contexts.GameContext.FlagEnumAddress + 0x002CA560, 1); // Dogi Control Option Unlocked
                }
                else if (!Contexts.FlagEnumContext.TMemo3)
                {
                    Memory.WriteByte(Contexts.GameContext.FlagEnumAddress + 0x002CA580, 1); // Intercept List 3
                }
                else if (!Contexts.FlagEnumContext.TMemo4)
                {
                    Memory.WriteByte(Contexts.GameContext.FlagEnumAddress + 0x002CA584, 1); // Intercept List 4
                }
            }
            else if (receivedItem.ItemID == 629) // Fishing rod
            {
                Contexts.InventoryContext.CheckIfObtainedAndSet(628);
                Memory.WriteByte(Contexts.InventoryContext.GetItemQuantityAddress(628), 0x1E); // Give 30 bait
            }
            else if (receivedItem.ItemID == 218) // Slash Medal
            {
                Contexts.InventoryContext.CheckIfObtainedAndSet(219);
                Memory.WriteByte(Contexts.InventoryContext.GetItemQuantityAddress(219), 1); // Pierce Medal
                Contexts.InventoryContext.CheckIfObtainedAndSet(220);
                Memory.WriteByte(Contexts.InventoryContext.GetItemQuantityAddress(220), 1); // Strike Medal
            }

            // handle event flags
            if (receivedItem.Flags != null)
            {
                foreach (string flag in receivedItem.Flags)
                {

                    if (FlagsSetTo2.Contains(flag))
                    {
                        Memory.WriteByte(Contexts.GameContext.FlagEnumAddress + Convert.ToUInt64(flag, 16), 2);
                    }
                    else if (flag == "0x002C7D0C")
                    {
                        Memory.WriteByte(Contexts.GameContext.FlagEnumAddress + Convert.ToUInt64(flag, 16), 7); // GF_CAMP_SHIPYARD_LV
                    }
                    else
                    {
                        Memory.WriteByte(Contexts.GameContext.FlagEnumAddress + Convert.ToUInt64(flag, 16), 1);
                    }
                }
            }

            // handle item quantity limits
            if (currentQuantity > receivedItem.QuantityMax)
            {
                newQuantity = receivedItem.QuantityMax;
            }
            else if (currentQuantity > receivedItem.QuantityMaxInferno && Contexts.FlagEnumContext.InfernoFlag)
            {
                newQuantity = receivedItem.QuantityMaxInferno;
            }


            if (receivedItem.CrewMember)
            {
                Contexts.InventoryContext.CheckIfObtainedAndSet(143); // Castaway item for tracking crew member obtained for work totals.
                Memory.WriteByte(Contexts.InventoryContext.GetItemQuantityAddress(143), (byte)newQuantity);

                if (receivedItem.CrewJoinID != null)
                {
                    Contexts.FlagEnumContext.SetNPCJoinState((int)receivedItem.CrewJoinID);
                }
            }
            else if (receivedItem.Landmark)
            {
                Contexts.InventoryContext.CheckIfObtainedAndSet(148); // Landmark item for tracking totals.
                Memory.WriteByte(Contexts.InventoryContext.GetItemQuantityAddress(148), (byte)newQuantity);
            }
            else
            {
                Contexts.InventoryContext.CheckIfObtainedAndSet(receivedItem.ItemID);
                Memory.WriteByte(Contexts.InventoryContext.GetItemQuantityAddress(receivedItem.ItemID), (byte)newQuantity);
            }
        }

        // Now unused, holding onto in case we find a reason to use it.
        //internal static bool RemoveAttchItem(short itemId)
        //{
        //    uint addr;
        //    bool result = false;

        //    for (int ii = 0; ii < attachChanges.Count; ii++)
        //    {
        //        (short, short) item = attachChanges[ii];
        //        if (item.Item2 == itemId)
        //        {
        //            addr = (uint)(FirstAttchAddr + (item.Item1 * AttachmentSize));
        //            Memory.Write(addr, 0x0000FFFF);
        //            for (int x = 4; x < AttachmentSize; x += 4)
        //                Memory.Write((ulong)(addr + x), 0);
        //            result = true;
        //            break;
        //        }
        //    }

        //    return result;
        //}

        /// <summary>
        /// Compares the GameState item counts to how many of each item are saved to memory, giving the player the difference.
        /// </summary>
        /*
        internal static void VerifyItems()
        {
            // Clear current values, check what the server thinks first, then compare that against the save file.
            ItemQueue.ClearItemQueues();
            itemCounts.Clear();
            attachCounts.Clear();

            foreach (ItemInfo itemInfo in App.Client.CurrentSession.Items.AllItemsReceived)
            {
                long apId = itemInfo.ItemId;
                if (apId > MiscConstants.AttachIdBase)
                    IncAttachCount(apId);
                else if (apId > MiscConstants.ItemIdBase)
                    IncItemCount(apId);
            }

            foreach (long itemId in itemCounts.Keys)
            {
                //if (itemId == MiscConstants.DarkGenieApId) continue;

                byte value = OpenMem.ReadItemCountValue(itemId);
                if (itemCounts[itemId] > value)
                {
                    for (int ii = value; ii < itemCounts[itemId]; ii++)
                    {
                        if (MiscConstants.KeyItemApIds.Contains(itemId))
                            ItemQueue.AddKeyItem(itemId);
                        else
                            ItemQueue.AddItem(itemId);
                    }
                }
            }

            foreach (long attachId in attachCounts.Keys)
            {
                //if (attachId == MiscConstants.DarkGenieApId) continue;

                byte value = OpenMem.ReadItemCountValue(attachId);
                if (attachCounts[attachId] > value)
                {
                    for (int ii = value; ii < attachCounts[attachId]; ii++)
                        ItemQueue.AddAttachment(attachId);
                }
            }
        }
        */

        // Now unused, holding onto in case we find a reason to use it.
        /// <summary>
        /// Checks the player's current attachment inventory and optionally compares it against the most recent inventory so when we remove 
        /// an attachment, we only remove the new one.  This is due to atk/spd/mg/end attachments all having the same ID but different values.
        /// </summary>
        /// <param name="firstInit">Don't compare against the existing data, this is an initialization call.</param>
        //internal static void CheckAttachments(bool firstInit)
        //{
        //    uint addr = FirstAttchAddr;
        //    List<short> newAttachmentInv = new(MaxAttachCount);

        //    for (int ii = 0; ii < MaxAttachCount; ii++)
        //    {
        //        newAttachmentInv.Add(Memory.ReadShort(addr));
        //        addr += AttachmentSize;
        //    }

        //    if (!firstInit)
        //    {
        //        attachChanges.Clear();
        //        for (short ii = 0; ii < attachmentInv.Count; ii++)
        //        {
        //            if (attachmentInv[ii] != newAttachmentInv[ii])
        //            {
        //                attachChanges.Add((ii, newAttachmentInv[ii]));
        //            }
        //        }
        //        attachmentInv = newAttachmentInv;
        //    }
        //}
    }
}
