using System.Collections.Generic;
using System.IO;
using System.Linq;
using PuzzleParty.Maps;
using UnityEditor;
using UnityEngine;

namespace PuzzleParty.EditorTools
{
    /// <summary>
    /// Editor-only read/write for config/maps.json. Reuses the game's
    /// <see cref="Map"/>/<see cref="MapsConfig"/> types so the editor and the
    /// runtime share one schema. Maps are kept as a gap-free, contiguous chain
    /// of level ranges (map N starts right after map N-1 ends).
    /// </summary>
    public static class MapFileService
    {
        private static string MapsPath => Path.Combine(Application.streamingAssetsPath, "config", "maps.json");

        public static List<Map> LoadMaps()
        {
            if (!File.Exists(MapsPath))
                return new List<Map>();
            MapsConfig conf = JsonUtility.FromJson<MapsConfig>(File.ReadAllText(MapsPath));
            if (conf?.maps == null)
                return new List<Map>();
            return conf.maps.OrderBy(m => m.startLevel).ToList();
        }

        public static void SaveMaps(List<Map> maps)
        {
            Normalize(maps);
            var conf = new MapsConfig { maps = maps.ToArray() };
            Directory.CreateDirectory(Path.GetDirectoryName(MapsPath));
            File.WriteAllText(MapsPath, JsonUtility.ToJson(conf, true));
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// Forces the map list into a contiguous chain: the first map starts at
        /// level 1, each following map starts one level after the previous ends,
        /// and every range holds at least one level.
        /// </summary>
        public static void Normalize(List<Map> maps)
        {
            for (int i = 0; i < maps.Count; i++)
            {
                int start = i == 0 ? 1 : maps[i - 1].endLevel + 1;
                maps[i].startLevel = start;
                if (maps[i].endLevel < start)
                    maps[i].endLevel = start;
            }
        }

        public static int NextMapId(List<Map> maps) => maps.Count == 0 ? 1 : maps.Max(m => m.id) + 1;

        /// <summary>Returns the map a level id belongs to, or null if unassigned.</summary>
        public static Map MapForLevel(List<Map> maps, int levelId) =>
            maps.FirstOrDefault(m => levelId >= m.startLevel && levelId <= m.endLevel);
    }
}
