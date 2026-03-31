using Archipelago.Core.Util;
using Ys8AP.Constants;

namespace Ys8AP.Mem
{
    /// Class for accessing player state memory values
    public static class PlayerState
    {
        public static bool ValidGameState = false;
        /// Game is loaded and ready to connect with.
        public static bool PlayerReady()
        {
            // File is loaded, player not in load menu, player can recieve items.
            return Memory.ReadByte(GlobalAddresses.HealAreaFlag) == 1 && Memory.ReadByte(GlobalAddresses.SaveMenuFlag) != 1 && Memory.ReadByte(GlobalAddresses.TimeAttackFlag) != 1;
        }

    }
}
