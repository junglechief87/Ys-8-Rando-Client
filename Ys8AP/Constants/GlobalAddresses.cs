using System;
using Archipelago.Core.Util;

namespace Ys8AP.Constants
{
    public static class GlobalAddresses
    {

        public const ulong FlagEnumPointer  = 0x006B7138;
        public static ulong FlagEnumAddress{ get; set; }
        public static ulong FlagEnumOffset{ get; set; }
        public const ulong TimeAttackFlag   = 0x002C7130;
        public const ulong SaveMenuFlag     = 0x002C705C;
        public const ulong HealAreaFlag     = 0x002C7078; // Name is misleading, in reality it's used anytime your character can sheath their weapons


        public const ulong ChestFlagStart   = 0x002C9934;
    }
}