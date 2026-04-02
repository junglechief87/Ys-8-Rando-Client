
using Ys8AP.GlobalAddresses;
using System;
using System.Collections.Generic;

namespace Ys8AP
{
    internal static class Options
    {
        private static int FinalBossAccess = 0;

        internal static void ParseOptions(Dictionary<string, object> options)
        {
#pragma warning disable CS8604 // Possible null reference argument.
#pragma warning disable CS8601 // Possible null reference.
            FinalBossAccess = Int32.Parse(options["final_boss_access"].ToString());
#pragma warning restore CS8601 // Possible null reference.
#pragma warning restore CS8604 // Possible null reference argument.
        }
    }
}
