using Archipelago.Core.Util;
using Ys8AP.GlobalAddresses;

namespace Ys8AP.Mem
{
    /// Class for accessing player state memory values
    public static class PlayerState
    {
        /// Game is loaded and ready to connect with.
        public static bool PlayerReady()
        {
            
            // File is loaded, player not in load menu, player can recieve items.
            return Contexts.GameContext.InventoryAddress != 0 &&
            !Contexts.FlagEnumContext.SaveMenuFlag && 
            !Contexts.FlagEnumContext.TimeAttackFlag &&
            !Contexts.FlagEnumContext.GameOverSelect;
        }

    }
}
