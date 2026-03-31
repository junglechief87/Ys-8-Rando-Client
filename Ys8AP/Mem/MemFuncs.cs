using Archipelago.Core.Util;
using Ys8AP.Constants;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.IO;

namespace Ys8AP.Mem
{
    public static class FlagEnum
    {
        public static uint GetFlag(uint flagID, uint GroupOffset)
        {
            ulong flagOffset = GlobalAddresses.FlagEnumOffset + GroupOffset + (flagID * 4);
            return Memory.ReadUInt(flagOffset);
        }

        public static byte GetFlagByte(uint flagID, uint GroupOffset)
        {
            uint flagOffset = GlobalAddresses.FlagEnumOffset + GroupOffset + (flagID * 4);
            return Memory.ReadByte(flagOffset);
        }

        public static void SetFlag(uint flagID, uint GroupOffset, byte[] value)
        {
            Memory.WriteByteArray(GlobalAddresses.FlagEnumOffset + GroupOffset + (flagID * 4), value);
        }

        public static uint GetSpecificOffset(uint offset)
        {
            return Memory.ReadUInt(GlobalAddresses.FlagEnumOffset + offset);
        }

        public static void SetSpecificOffset(uint offset, byte[] value)
        {
            Memory.WriteByteArray(GlobalAddresses.FlagEnumOffset + offset, value);
        }
    }
}
