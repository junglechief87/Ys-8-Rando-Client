using Archipelago.Core.Util;
using Ys8AP.GlobalAddresses;
using Ys8AP.Items;
using Ys8AP.Threads;
using Ys8AP.Mem;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ys8AP.Threads
{
    internal class ItemQueue
    {
        private const int DisplayTime = 350; // cs, 3.5 seconds
        //private static int MsToCs = 10;  // Convert 1000ths of a second to 100ths
        private static ConcurrentQueue<long> inventoryQueue = new();
        private static ConcurrentQueue<string> msgQueue = new();
        internal static bool checkItems = false;
        internal static bool runThread = true;

        internal static void AddItem(long apId)
        {
            if (PlayerState.IsPlayerReady)
                inventoryQueue.Enqueue(apId);
        }
        
        internal static void AddMsg(string msg)
        {
            if (PlayerState.IsPlayerReady)
            {
                Log.Logger.Information(msg);
                msgQueue.Enqueue(msg);
            }
        }

        internal static void StopThread()
        {
            runThread = false;
        }

        internal static async Task ThreadLoop()
        {
            // Clean out the queues before stopping
            while (runThread && App.Client != null)
            {
                await Task.Delay(100);

                try
                {
                    if (PlayerState.IsPlayerReady)
                    {
                        // If player was not ready before but is now, we check items.
                        if (checkItems)
                        {
                            InventoryMgmt.VerifyItems();
                            checkItems = false;
                        }

                        while (inventoryQueue.TryDequeue(out long apId) && !checkItems)
                        {
                            InventoryMgmt.GiveItem(apId);
                        }
                    }
                    // If player enters a not ready state, we clear queues and prepare to check items, once they exit the states.
                    else if (ShouldClearQueues())
                    {
                        ClearQueues();
                        checkItems = true;
                    }
                }
                catch (Exception ex)
                {
                    Log.Logger.Error(ex, "ItemQueue ThreadLoop encountered an error");
                }
            }
        }

        private static bool ShouldClearQueues()
        {
            return (!PlayerState.IsPlayerReady || PlayerState.PostRetry()) && !checkItems;
        }

        internal static void ClearQueues()
        {
            ClearItemQueues();
            msgQueue.Clear();
        }

        internal static void ClearItemQueues()
        {
            inventoryQueue.Clear();
        }
    }
}
