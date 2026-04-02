using Archipelago.Core.Util;
using Ys8AP.GlobalAddresses;
using Ys8AP.Items;
using Ys8AP.Mem;
using Serilog;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;

namespace Ys8AP.Threads
{
    internal class ItemQueue
    {
        private const int DisplayTime = 350; // cs, 3.5 seconds
        //private static int MsToCs = 10;  // Convert 1000ths of a second to 100ths

        private static ConcurrentQueue<long> inventoryQueue = new();
        private static ConcurrentQueue<string> msgQueue = new();

        private static int oldKeyCount = 0;
        private static int oldInvCount = 0;
        private static int oldAttachCount = 0;

        internal static bool checkItems = false;

        internal static bool runThread = true;

        
        internal static void AddItem(long apId)
        {
            if (PlayerState.PlayerReady())
                inventoryQueue.Enqueue(apId);
        }
        

        internal static void AddMsg(string msg)
        {
            if (PlayerState.PlayerReady())
            {
                Log.Logger.Information(msg);
                msgQueue.Enqueue(msg);
            }
        }

        internal static void ThreadLoop(object? parameters)
        {
            
            runThread = true;
            bool result = true;
            bool itemReceived = false;

            // Clean out the queues before stopping
            while (runThread)
            {
                Thread.Sleep(100);

                if (PlayerState.PlayerReady())
                {
                    itemReceived = false;
                    result = true;

                    while (result && inventoryQueue.TryDequeue(out long apId))
                    {
                        InventoryMgmt.GiveItem(apId);
                        itemReceived = true;
                    }


                    if (checkItems)
                    {
                        //InventoryMgmt.VerifyItems();
                        checkItems = false;
                    }
                }
                // Player hasn't started the game, or has reset so clear the queues.
                else
                {
                    //ClearQueues();
                }
            }
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
