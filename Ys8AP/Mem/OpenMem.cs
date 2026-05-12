using Archipelago.Core.Util;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        /// Reset error tracking so the first mismatch after a re-init is suppressed (seed may not be written yet)
        internal static void ResetState()
        {
            errorLogCount = 0;
            lastErrorLogTime = DateTime.MinValue;
            lastSeedWasGood = false;
            hasEverFailed = false;
        }

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

            // If contexts aren't initialized, can't validate
            if (Contexts.FlagEnumContext == null || Contexts.GameContext == null)
                return false;

            // Always do a fresh process check here to avoid stale cache causing seed mismatch on process death
            bool isProcessRunning = Process.GetProcessesByName("ys8").Any();
            if (!isProcessRunning) return false;  // Process gone — don't log seed mismatch, let Reconnect loop handle the warning
            uint roomSeed = Contexts.FlagEnumContext.GetAPSeed();

            if (Contexts.GameContext.InventoryAddress == 0) return false;
            bool result = compressed == roomSeed;
            
            if (result)
            {
                // Always reset backoff so the next mismatch (e.g. on process death) is treated as a fresh first occurrence
                errorLogCount = 0;
                lastErrorLogTime = DateTime.MinValue;
                // Log success message only if we previously had a logged failure
                if (!lastSeedWasGood && hasEverFailed && isProcessRunning && Contexts.GameContext.InventoryAddress != 0)
                {
                    Log.Logger.Information("Room seed verified successfully.");
                    lastSeedWasGood = true;
                    hasEverFailed = false;
                }
                return true;
            }
            
            lastSeedWasGood = false;
            
            // Only log errors if the Ys8 process is running and the game is actually loaded; otherwise silently retry
            if (isProcessRunning && Contexts.GameContext.InventoryAddress != 0)
            {
                DateTime now = DateTime.UtcNow;
                
                if (errorLogCount == 0)
                {
                    // First mismatch after a (re)init: seed may not be written to memory yet, suppress and start the clock
                    errorLogCount++;
                    lastErrorLogTime = now;
                }
                else
                {
                    // Subsequent mismatches: apply backoff before logging
                    int backoffInterval = errorLogCount switch
                    {
                        1 => 2,
                        2 => 10,
                        3 => 20,
                        _ => MAX_BACKOFF_SECONDS
                    };
                    
                    if (now.Subtract(lastErrorLogTime).TotalSeconds >= backoffInterval)
                    {
                        if (roomSeed == 0)
                        {
                            Log.Logger.Error("Room seed is 0. Not an AP Save File.");
                        }
                        else
                        {
                            Log.Logger.Error("Room seed mismatch. Expected " + compressed + ", found " + roomSeed + ".");
                        }
                        
                        lastErrorLogTime = now;
                        errorLogCount++;
                        hasEverFailed = true;
                    }
                }
            }
            return false;
        }
    }
}
