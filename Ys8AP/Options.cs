
using Ys8AP.GlobalAddresses;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Ys8AP
{
    internal static class Options
    {
        public static int FinalBossAccess = 0;
        public static int FormerSanctuaryCrypt = 0;
        public static int HelperText = 0;
        public static bool DeathLinkEnabled = false;

        // Populated from slot data on connect
        public static string StartingCharacter = "";
        // Keys are character names: "Adol", "Sahad", "Laxia", "Ricotta", "Hummel", "Dana"
        public static Dictionary<string, List<int>> StartingSkills = new();

        internal static void ParseOptions(Dictionary<string, object> options)
        {
            if (options.TryGetValue("final_boss_access", out var goal) && goal != null)
            {
                if (int.TryParse(goal.ToString(), out int result))
                    FinalBossAccess = result;
            }

            if (options.TryGetValue("former_sanctuary_crypt", out var fsc) && fsc is JsonElement jsonElementFsc)
            {
                FormerSanctuaryCrypt = jsonElementFsc.GetInt32() != 0 ? 1 : 0;
            }

            if (options.TryGetValue("helper_text", out var cht) && cht is JsonElement jsonElementCht)
            {
                HelperText = jsonElementCht.GetInt32() != 0 ? 1 : 0;
            }

            if (options.TryGetValue("death_link", out var deathValue) && deathValue is JsonElement jsonElement)
                DeathLinkEnabled = jsonElement.GetInt32() != 0;
        }

        internal static void ParseSlotData(Dictionary<string, object> slotData)
        {
            if (slotData.TryGetValue("starting_character", out var sc) && sc != null)
                StartingCharacter = sc.ToString();

            StartingSkills.Clear();
            if (slotData.TryGetValue("starting_skills", out var raw) && raw is JObject skillsObj)
            {
                foreach (var entry in skillsObj.Properties())
                    StartingSkills[entry.Name] = entry.Value.Select(x => x.Value<int>()).ToList();
            }
        }
    }
}
