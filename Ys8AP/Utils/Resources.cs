using Ys8AP.Locations;
using Ys8AP.Items;
using System.Collections.Concurrent;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace Ys8AP.Utils
{
    internal class Resources
    {
        public static class Embedded
        {
            static JsonSerializerOptions jOptions = new(JsonSerializerDefaults.Web)
            {
                AllowOutOfOrderMetadataProperties = true,
                IncludeFields = true
            };

            public static ConcurrentDictionary<int, ChestLocation> ChestLocations
            {
                get
                {
                    var info = Assembly.GetExecutingAssembly().GetName();
                    var name = info.Name;
                    using var stream = Assembly
                        .GetExecutingAssembly()
                        .GetManifestResourceStream($"{name}.Locations.ChestLocations.json")!;
                    using var streamReader = new StreamReader(stream, Encoding.UTF8);
                    return JsonSerializer.Deserialize<ConcurrentDictionary<int, ChestLocation>>(streamReader.ReadToEnd(), jOptions);
                }
            }

            public static ConcurrentDictionary<long, InvItem> Items
            {
                get
                {
                    var info = Assembly.GetExecutingAssembly().GetName();
                    var name = info.Name;
                    using var stream = Assembly
                        .GetExecutingAssembly()
                        .GetManifestResourceStream($"{name}.Items.Items.json")!;
                    using var streamReader = new StreamReader(stream, Encoding.UTF8);
                    return JsonSerializer.Deserialize<ConcurrentDictionary<long, InvItem>>(streamReader.ReadToEnd(), jOptions);
                }
            }
        }
    }
}
