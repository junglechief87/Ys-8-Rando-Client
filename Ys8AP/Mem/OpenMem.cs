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
        private static DateTime lastErrorLogTime = DateTime.MinValue;
        private static int errorLogCount = 0;  // Track how many times we've logged the error
        private const int MAX_BACKOFF_SECONDS = 30;
        internal static bool lastSeedWasGood = false;

        /// <summary>
        /// Compute the compressed seed to match Python's int.from_bytes(float32(...).tobytes(), 'little')
        /// Returns null if the session isn't initialized yet (seed not available).
        /// </summary>
        private static uint? GetCompressedRoomSeed()
        {
            // Return null if session or seed not initialized yet
            if (App.Client?.CurrentSession?.RoomState?.Seed == null)
                return null;

            float seedVal = float.Parse(App.Client.CurrentSession.RoomState.Seed);
            byte[] bytes = BitConverter.GetBytes(seedVal);
            if (!BitConverter.IsLittleEndian) Array.Reverse(bytes);
            return BitConverter.ToUInt32(bytes, 0); // matches Python's unsigned int.from_bytes(..., 'little')
        }

        /// <summary>
        /// Compares the compressed seed stored in the flag context with the computed compressed seed.
        /// Returns true if seed is valid or session not initialized yet (allow connection to proceed).
        /// Only logs errors if the game is actually loaded; silently returns false if file not loaded yet.
        /// Implements exponential backoff to reduce log spam (10s, 10s, 20s, then cap at 30s).
        /// Called constantly by PlayerState background monitoring thread after state transitions.
        /// </summary>
        internal static bool TestRoomSeed()
        {
            uint? compressed = GetCompressedRoomSeed();
            // If session isn't initialized yet, pass the check and allow connection to proceed
            if (compressed == null)
                return true;

            // If contexts aren't initialized, can't validate
            if (Contexts.FlagEnumContext == null || Contexts.GameContext == null)
                return false;

            uint roomSeed = Contexts.FlagEnumContext.GetAPSeed();
            bool result = compressed == roomSeed;
            
            if (result)
            {
                // Log success message if previous state was bad
                if (!lastSeedWasGood && Contexts.GameContext.InventoryAddress != 0)
                {
                    Log.Logger.Information("Room seed verified successfully.");
                    lastSeedWasGood = true;
                    errorLogCount = 0; // Reset error count on success
                    lastErrorLogTime = DateTime.MinValue;
                }
                return true;
            }
            
            lastSeedWasGood = false;
            
            // Only log errors if the game is actually loaded; otherwise silently retry
            if (Contexts.GameContext.InventoryAddress != 0)
            {
                DateTime now = DateTime.UtcNow;
                
                // Calculate backoff interval based on error count: 10s, 10s, 20s, then cap at 30s
                int backoffInterval = errorLogCount switch
                {
                    0 => 10,  // First error: wait 10s
                    1 => 10,  // Second error: wait another 10s
                    2 => 20,  // Third error: wait 20s
                    _ => MAX_BACKOFF_SECONDS  // Fourth+ errors: wait 30s (capped)
                };
                
                // Check if enough time has passed to log the error again
                if (now.Subtract(lastErrorLogTime).TotalSeconds >= backoffInterval)
                {
                    Log.Logger.Error("Room seed mismatch. Expected " + roomSeed + ", found " + compressed + ".");
                    lastErrorLogTime = now;
                    errorLogCount++;
                }
            }
            return false;
        }
    }
}
