using Archipelago.Core.Util;
using Ys8AP.Constants;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Ys8AP.Mem
{
    public static class FlagEnum
    {
        public static uint GetFlag(int flagID, uint GroupOffset)
        {
            return Memory.ReadUInt(GlobalAddresses.FlagEnumOffset + GroupOffset + (uint)(flagID * 4));
        }

        public static byte GetFlagByte(int flagID, uint GroupOffset)
        {
            return Memory.ReadByte(GlobalAddresses.FlagEnumOffset + GroupOffset + (uint)(flagID * 4));
        }

        public static void SetFlag(int flagID, uint GroupOffset, byte[] value)
        {
            Memory.WriteByteArray(GlobalAddresses.FlagEnumOffset + GroupOffset + (uint)(flagID * 4), value);
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
