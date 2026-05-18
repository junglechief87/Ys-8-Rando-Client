using System.IO;
using Newtonsoft.Json;

namespace Ys8AP
{
    public class UserSettings
    {
        public string Host { get; set; } = "archipelago.gg:";
        public string Slot { get; set; } = "Player1";

        private static string SettingsPath => Path.Combine(
            System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData),
            "Ys8AP", "user_settings.json");

        public static UserSettings Load()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    var json = File.ReadAllText(SettingsPath);
                    return JsonConvert.DeserializeObject<UserSettings>(json) ?? new UserSettings();
                }
                else
                {
                    // Create file with defaults on first run
                    var defaults = new UserSettings { Host = "archipelago.gg:", Slot = "Player1" };
                    defaults.Save();
                    return defaults;
                }
            }
            catch { }
            return new UserSettings { Host = "archipelago.gg:", Slot = "Player1" };
        }

        public void Save()
        {
            try
            {
                var dir = Path.GetDirectoryName(SettingsPath);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                var json = JsonConvert.SerializeObject(this, Formatting.Indented);
                File.WriteAllText(SettingsPath, json);
            }
            catch { }
        }
    }
}
