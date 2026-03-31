using Archipelago.Core.Util;
using Ys8AP.Constants;
using Ys8AP.Mem;
using Ys8AP;
using System.Collections.Generic;
using System.Threading;

namespace Ys8AP.Threads
{
    /// <summary>
    /// More complex monitoring of memory/game state than Memory.Monitor methods
    /// </summary>
    internal class HelperThread
    {
        internal static void DoLoop()
        {
            new Thread(() =>
            {
                while (true)
                {
                    if (PlayerState.PlayerReady() && App.Client.IsConnected )
                    {
                        CheckChests();
                    }
                    Thread.Sleep(1000);
                }
            }).Start();
        }


        private static void CheckChests()
        {
            int ChestID;
            uint ChestOpenFlag;
            for (uint i = 0; i < 100; i++)
            {
                ChestOpenFlag=FlagEnum.GetFlag(i, GlobalAddresses.ChestFlagStart + 2);
                if (ChestOpenFlag == 0x30)
                {
                    ChestID = (int)i;
                    Ys8AP.App.SendLocation(ChestID);
                }
            }

        }

    }
}
