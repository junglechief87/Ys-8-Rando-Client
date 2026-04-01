using Archipelago.Core.Util;
using Ys8AP.Constants;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.IO;

namespace Ys8AP.Mem
{
    public static class FlagEnum
    {
        public static uint GetFlag(uint flagID, ulong GroupOffset)
        {
            ulong flagOffset = Memory.GlobalOffset - GlobalAddresses.FlagEnumOffset + GroupOffset + (flagID * 4);
            return Memory.ReadUInt(flagOffset);
        }

        public static byte GetFlagByte(uint flagID, ulong GroupOffset)
        {
            Memory.GlobalOffset = GlobalAddresses.FlagEnumAddress;
            ulong flagOffset = GroupOffset + (flagID * 4);
            //Memory.Write(0x83BD4F86, [0x01, 0xFF, 0x12, 0x05]);
            //Memory.WriteByteArray(flagOffset, [0xFF, 0x15, 0x16, 0x17]); // Test write to make sure we have the right offset; if this causes a crash, the offset is likely wrong.
            return Memory.ReadByte(flagOffset);
        }

        public static void SetFlag(uint flagID, ulong GroupOffset, byte[] value)
        {
            Memory.WriteByteArray(Memory.GlobalOffset - GlobalAddresses.FlagEnumOffset + GroupOffset + (flagID * 4), value);
        }

        public static uint GetSpecificOffset(uint offset)
        {
            return Memory.ReadUInt(Memory.GlobalOffset - GlobalAddresses.FlagEnumOffset + offset);
        }

        public static void SetSpecificOffset(uint offset, byte[] value)
        {
            Memory.WriteByteArray(Memory.GlobalOffset - GlobalAddresses.FlagEnumOffset + offset, value);
        }
    }
}
