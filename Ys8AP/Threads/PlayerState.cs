using Ys8AP.GlobalAddresses;
using Ys8AP.Mem;
using System;
using System.Threading;
using Serilog;

namespace Ys8AP.Threads
{
    /// Class for accessing player state memory values through a background monitoring thread
    public static class PlayerState
    {
        private static Thread? _stateMonitorThread;
        private static bool _threadRunning = false;
        private static bool _lastBasicConditionsMet = false;
        private static DateTime _lastStateChangeTime = DateTime.MinValue;
        private const int STATE_CHECK_INTERVAL_MS = 50;  // Check state 20 times per second
        private const int SEED_VALIDATION_DEBOUNCE_MS = 200;  // Wait 200ms after state change before validating seed

        /// Public read-only property for player ready state
        public static bool IsPlayerReady { get; private set; } = false;

        /// Start the background state monitor thread
        public static void StartMonitoring()
        {
            if (_threadRunning)
                return;

            _threadRunning = true;
            _stateMonitorThread = new Thread(StateMonitorLoop)
            {
                IsBackground = true,
                Name = "PlayerStateMonitor"
            };
            _stateMonitorThread.Start();
        }

        /// Stop the background state monitor thread
        public static void StopMonitoring()
        {
            _threadRunning = false;
            if (_stateMonitorThread != null)
            {
                _stateMonitorThread.Join(TimeSpan.FromSeconds(5));
            }
        }

        /// Background thread loop that constantly monitors and updates player state
        private static void StateMonitorLoop()
        {
            while (_threadRunning)
            {
                try
                {
                    // If we connected before loading a save, InventoryAddress will be 0 because the game
                    // hadn't loaded the save yet when InitializeAddresses() was called. Re-read the addresses
                    // from memory once the process is running and they become available.
                    if (Contexts.GameContext != null && Contexts.GameContext.InventoryAddress == 0 && App.IsYs8ProcessRunning())
                    {
                        try
                        {
                            AddressInit.InitializeAddresses();
                            OpenMem.ResetState();  // Suppress first seed check — seed may not be written yet
                        }
                        catch { /* process not available yet */ }
                    }

                    // Check basic conditions (file loaded, not in menu, etc)
                    bool basicConditionsMet = Contexts.GameContext != null && 
                        Contexts.FlagEnumContext != null &&
                        Contexts.GameContext.InventoryAddress != 0 &&
                        !Contexts.FlagEnumContext.GetSaveMenuFlag() &&
                        !Contexts.FlagEnumContext.GetCustomGameOverFlag() &&
                        Contexts.FlagEnumContext.GetEventStartFlag();  
                        
                    // Detect state transition (entering or exiting valid state)
                    if (basicConditionsMet != _lastBasicConditionsMet)
                    {
                        _lastBasicConditionsMet = basicConditionsMet;
                        _lastStateChangeTime = DateTime.UtcNow;
                        OpenMem.lastSeedWasGood = false;  // Force seed re-check on transition
                    }

                    // Update player ready state
                    if (basicConditionsMet)
                    {
                        // Wait for debounce period after state change before validating seed
                        if (DateTime.UtcNow.Subtract(_lastStateChangeTime).TotalMilliseconds >= SEED_VALIDATION_DEBOUNCE_MS)
                        {
                            IsPlayerReady = OpenMem.TestRoomSeed() && App.Client?.IsConnected == true;
                        }
                        else
                        {
                            IsPlayerReady = false;  // Not ready yet, waiting for debounce
                        }
                    }
                    else
                    {
                        IsPlayerReady = false;
                    }

                    Thread.Sleep(STATE_CHECK_INTERVAL_MS);
                }
                catch (Exception ex)
                {
                    Log.Logger.Error($"Error in PlayerState monitor thread: {ex}");
                    Thread.Sleep(STATE_CHECK_INTERVAL_MS);
                }
            }
        }

        public static bool PostRetry()
        {
            return Contexts.FlagEnumContext.GetRetryFlag();
        }

        public static bool GameOver()
        {
            return Contexts.FlagEnumContext.GetCustomGameOverFlag();
        }

        public static bool NotInTown()
        {
            return !Contexts.FlagEnumContext.GetInTownFlag();
        }
    }
}
