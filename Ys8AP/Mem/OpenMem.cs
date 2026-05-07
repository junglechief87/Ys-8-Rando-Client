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
        private static bool hasEverFailed = false;  // Track if we've ever logged an error

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

            bool isProcessRunning = App.IsYs8ProcessRunning();
            uint roomSeed = Contexts.FlagEnumContext.GetAPSeed();
            bool result = compressed == roomSeed;
            
            if (result)
            {
                // Log success message only if we previously had a failure
                if (!lastSeedWasGood && hasEverFailed && isProcessRunning && Contexts.GameContext.InventoryAddress != 0)
                {
                    Log.Logger.Information("Room seed verified successfully.");
                    lastSeedWasGood = true;
                    hasEverFailed = false;  // Reset failure flag so we won't log success again until next failure
                    errorLogCount = 0; // Reset error count on success
                    lastErrorLogTime = DateTime.MinValue;
                }
                return true;
            }
            
            lastSeedWasGood = false;
            
            // Only log errors if the Ys8 process is running and the game is actually loaded; otherwise silently retry
            if (isProcessRunning && Contexts.GameContext.InventoryAddress != 0)
            {
                DateTime now = DateTime.UtcNow;
                
                // Calculate backoff interval based on error count: skip first, then 10s, 10s, 20s, then cap at 30s
                int backoffInterval = errorLogCount switch
                {
                    0 => int.MaxValue,  // First error: suppress (don't log on initial file load)
                    1 => 10,  // Second error: wait 10s
                    2 => 10,  // Third error: wait another 10s
                    3 => 20,  // Fourth error: wait 20s
                    _ => MAX_BACKOFF_SECONDS  // Fifth+ errors: wait 30s (capped)
                };
                
                // Check if enough time has passed to log the error again
                if (now.Subtract(lastErrorLogTime).TotalSeconds >= backoffInterval)
                {
                    Log.Logger.Error("Room seed mismatch. Expected " + compressed + ", found " + roomSeed + ".");
                    lastErrorLogTime = now;
                    errorLogCount++;
                    hasEverFailed = true;  // Mark that we've had at least one logged error
                }
            }
            return false;
        }
    }
}
