using System;

namespace Ys8AP.Constants
{
    public static class GlobalAddresses
    {

        public const uint FlagEnumPointer = 0x006B7138;
        public static uint FlagEnumOffset;
        public const uint TimeAttackFlag = 0x2C7130;
        public const uint SaveMenuFlag = 0x2C705C;
        public const uint HealAreaFlag = 0x2C7078; // Name is misleading, in reality it's used anytime your character can sheath their weapons

        public const uint ChestFlagStart = 0x2C9934;
    }
}