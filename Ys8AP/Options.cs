
using Ys8AP.GlobalAddresses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Ys8AP
{
    internal static class Options
    {
        public static int FinalBossAccess = 0;
        public static bool DeathLinkEnabled = false;

        // Populated from slot data on connect
        public static int StartingCharacter = 0;
        // Keys are character names: "Adol", "Sahad", "Laxia", "Ricotta", "Hummel", "Dana"
        public static Dictionary<string, List<int>> StartingSkills = new();

        internal static void ParseOptions(Dictionary<string, object> options)
        {
            if (options.TryGetValue("final_boss_access", out var value) && value != null)
            {
                if (int.TryParse(value.ToString(), out int result))
                    FinalBossAccess = result;
            }

            if (options.TryGetValue("death_link", out var deathValue) && deathValue is JsonElement jsonElement)
                DeathLinkEnabled = jsonElement.GetInt32() != 0;
        }

        internal static void ParseSlotData(Dictionary<string, object> slotData)
        {
            if (slotData.TryGetValue("starting_character", out var sc) && sc is JsonElement scElem)
                StartingCharacter = scElem.GetInt32();

            StartingSkills.Clear();
            if (slotData.TryGetValue("starting_skills", out var raw) && raw is JsonElement skillsElem)
            {
                foreach (var entry in skillsElem.EnumerateObject())
                    StartingSkills[entry.Name] = entry.Value.EnumerateArray().Select(x => x.GetInt32()).ToList();
            }
        }
    }
}
