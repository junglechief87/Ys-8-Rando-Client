using Archipelago.Core.Util;
using Ys8AP.GlobalAddresses;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.IO;

namespace Ys8AP.Mem
{
    public static class FlagFuncs
    {
        public static uint GetFlag(uint flagID, ulong GroupOffset)
        {
            ulong flagOffset = GroupOffset + (flagID * 4);
            return Memory.ReadUInt(flagOffset);
        }

        public static byte GetFlagByte(uint flagID, ulong GroupOffset)
        {
            ulong flagOffset = GroupOffset + (flagID * 4);
            return Memory.ReadByte(flagOffset);
        }

        public static void SetFlag(uint flagID, ulong GroupOffset, byte[] value)
        {
            Memory.WriteByteArray(GroupOffset + (flagID * 4), value);
        }

        public static uint GetSpecificOffset(uint offset)
        {
            return Memory.ReadUInt(offset);
        }

        public static void SetSpecificOffset(uint offset, byte[] value)
        {
            Memory.WriteByteArray(offset, value);
        }
    }
}
