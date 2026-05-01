
using Ys8AP.GlobalAddresses;
using System;
using System.Collections.Generic;

namespace Ys8AP
{
    internal static class Options
    {
        public static int FinalBossAccess = 0;
        public static bool DeathLinkEnabled = false;

        internal static void ParseOptions(Dictionary<string, object> options)
        {
            if (options.TryGetValue("final_boss_access", out var value) && value != null)
            {
                if (int.TryParse(value.ToString(), out int result))
                    FinalBossAccess = result;
            }

            if (options.TryGetValue("death_link", out var deathValue) && deathValue is System.Text.Json.JsonElement jsonElement)
                DeathLinkEnabled = jsonElement.GetInt32() != 0;
        }
    }
}
