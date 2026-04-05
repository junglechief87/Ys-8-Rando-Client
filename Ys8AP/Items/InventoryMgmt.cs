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

            if (currentQuantity > receivedItem.QuantityMax)
            {
                newQuantity = receivedItem.QuantityMax;
            }
            else if (currentQuantity > receivedItem.QuantityMaxInferno && Contexts.FlagEnumContext.InfernoFlag)
            {
                newQuantity = receivedItem.QuantityMaxInferno;
            }
            
            Contexts.InventoryContext.CheckIfObtainedAndSet(receivedItem.ItemID);
            Memory.WriteByte(Contexts.InventoryContext.GetItemQuantityAddress(receivedItem.ItemID), (byte)newQuantity);
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
