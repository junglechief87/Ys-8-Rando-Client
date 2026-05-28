
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Ys8AP
{
    internal static class Options
    {
        // Populated from slot data on connect.
        public static int FinalBossAccess = 0;
        public static int FormerSanctuaryCrypt = 0;
        public static int HelperText = 0;
        public static int ShuffleDamageTypes = 0;
        public static bool DeathLinkEnabled = false;
        public static string StartingCharacter = "";
        // Keys are character names: "Adol", "Sahad", "Laxia", "Ricotta", "Hummel", "Dana"
        public static Dictionary<string, List<int>> StartingSkills = new();
        // damage_mapping from slot data: e.g. { "Slash": ["Hummel","Ricotta"], "Pierce": [...] }
        public static Dictionary<string, List<string>> DamageMapping = new();

        internal static void ParseOptions(Dictionary<string, object> options)
        {
            if (options.TryGetValue("final_boss_access", out var goal) && goal != null)
            {
                if (int.TryParse(goal.ToString(), out int result))
                    FinalBossAccess = result;
            }

            if (options.TryGetValue("former_sanctuary_crypt", out var fsc) && fsc is JsonElement jsonElementFsc)
                FormerSanctuaryCrypt = jsonElementFsc.GetInt32() != 0 ? 1 : 0;
            
            if (options.TryGetValue("helper_text", out var cht) && cht is JsonElement jsonElementCht)
                HelperText = jsonElementCht.GetInt32() != 0 ? 1 : 0;

            if (options.TryGetValue("death_link", out var deathValue) && deathValue is JsonElement jsonElement)
                DeathLinkEnabled = jsonElement.GetInt32() != 0;
            
            if (options.TryGetValue("shuffle_damage_types", out var shuffleValue) && shuffleValue is JsonElement jsonElementShuffle)
                ShuffleDamageTypes = jsonElementShuffle.GetInt32() != 0 ? 1 : 0;
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

            DamageMapping.Clear();
            if (ShuffleDamageTypes == 1)
            {
                if (slotData.TryGetValue("damage_mapping", out var dmRaw) && dmRaw is JObject dmObj)
                {
                    foreach (var entry in dmObj.Properties())
                    {
                        // each entry is an array of strings
                        DamageMapping[entry.Name] = entry.Value.Select(x => x.Value<string>()).ToList();
                    }
                }
            }
        }
    }
}
