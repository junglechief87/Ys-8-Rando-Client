using Archipelago.Core.Util;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Numerics;
using Ys8AP.GlobalAddresses;

namespace Ys8AP.Mem
{
    /// <summary>
    /// Empty(?) block of memory on the mem card to use as we see fit.
    /// </summary>
    internal class OpenMem
    {
        /// <summary>
        /// Compute the compressed seed to match Python's int.from_bytes(float32(...).tobytes(), 'little')
        /// </summary>
        private static uint GetCompressedRoomSeed()
        {
            float seedVal = float.Parse(App.Client.CurrentSession.RoomState.Seed);
            byte[] bytes = BitConverter.GetBytes(seedVal);
            if (!BitConverter.IsLittleEndian) Array.Reverse(bytes);
            return BitConverter.ToUInt32(bytes, 0); // matches Python's unsigned int.from_bytes(..., 'little')
        }

        /// <summary>
        /// Compares the compressed seed stored in the flag context with the computed compressed seed.
        /// Only logs errors if the game is actually loaded; silently returns false if file not loaded yet.
        /// </summary>
        internal static bool TestRoomSeed()
        {
            uint compressed = GetCompressedRoomSeed();
            uint roomSeed = Contexts.FlagEnumContext.APSeed;
            bool result = compressed == roomSeed;
            
            // Only log errors if the game is actually loaded; otherwise silently retry
            if (!result && Contexts.GameContext.InventoryAddress != 0)
            {
                Log.Logger.Error("Room seed mismatch. Expected " + roomSeed + ", found " + compressed + ".");
            }
            return result;
        }
    }
}
