using System.Collections.Generic;
using System.IO;
using System.Linq;
using PuzzleParty.Levels;
using UnityEditor;
using UnityEngine;

namespace PuzzleParty.EditorTools
{
    /// <summary>
    /// Editor-only disk access for level files. Reads and writes the exact
    /// <see cref="LevelConf"/> schema the game uses at runtime, so there is a
    /// single source of truth for the level format. All writes land in the repo
    /// under StreamingAssets/levels and refresh the AssetDatabase so the changes
    /// show up in git ready to commit.
    /// </summary>
    public static class LevelFileService
    {
        public static string LevelsRoot => Path.Combine(Application.streamingAssetsPath, "levels");
        private static string MapsConfigPath => Path.Combine(Application.streamingAssetsPath, "config", "maps.json");

        public static string LevelDir(int id) => Path.Combine(LevelsRoot, "level" + id);
        public static string JsonPath(int id) => Path.Combine(LevelDir(id), "level" + id + ".json");
        public static string PngPath(int id) => Path.Combine(LevelDir(id), "level" + id + ".png");

        public static bool Exists(int id) => File.Exists(JsonPath(id));

        /// <summary>Loads every level config found on disk, sorted by id.</summary>
        public static List<LevelConf> LoadAll()
        {
            var result = new List<LevelConf>();
            if (!Directory.Exists(LevelsRoot))
                return result;

            foreach (string dir in Directory.GetDirectories(LevelsRoot, "level*"))
            {
                string name = Path.GetFileName(dir);
                if (!int.TryParse(name.Substring("level".Length), out int id))
                    continue;

                string json = JsonPath(id);
                if (!File.Exists(json))
                    continue;

                LevelConf conf = JsonUtility.FromJson<LevelConf>(File.ReadAllText(json));
                if (conf != null)
                {
                    conf.id = id; // keep id authoritative to the folder
                    result.Add(conf);
                }
            }

            return result.OrderBy(c => c.id).ToList();
        }

        public static LevelConf Load(int id)
        {
            string json = JsonPath(id);
            if (!File.Exists(json))
                return null;
            LevelConf conf = JsonUtility.FromJson<LevelConf>(File.ReadAllText(json));
            if (conf != null) conf.id = id;
            return conf;
        }

        /// <summary>Writes a level's JSON to disk (id taken from conf.id) and refreshes assets.</summary>
        public static void Save(LevelConf conf)
        {
            Directory.CreateDirectory(LevelDir(conf.id));
            File.WriteAllText(JsonPath(conf.id), JsonUtility.ToJson(conf, true));
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// Swaps the design of two levels. Folder and file names stay tied to their
        /// ids (level A stays "levelA"); only the contents (image + settings) trade
        /// places, so map ranges in maps.json are unaffected.
        /// </summary>
        public static void Swap(int a, int b)
        {
            if (a == b) return;

            LevelConf confA = Load(a);
            LevelConf confB = Load(b);
            if (confA == null || confB == null)
            {
                Debug.LogError($"[LevelEditor] Cannot swap: level {a} or {b} not found.");
                return;
            }

            byte[] pngA = File.Exists(PngPath(a)) ? File.ReadAllBytes(PngPath(a)) : null;
            byte[] pngB = File.Exists(PngPath(b)) ? File.ReadAllBytes(PngPath(b)) : null;

            // Folder a now holds b's design (but keeps id a), and vice versa.
            confB.id = a;
            confA.id = b;

            File.WriteAllText(JsonPath(a), JsonUtility.ToJson(confB, true));
            File.WriteAllText(JsonPath(b), JsonUtility.ToJson(confA, true));

            if (pngB != null) File.WriteAllBytes(PngPath(a), pngB);
            if (pngA != null) File.WriteAllBytes(PngPath(b), pngA);

            AssetDatabase.Refresh();
        }

        /// <summary>Creates a new level folder from a source image and config.</summary>
        public static void CreateNew(int id, string sourceImagePath, LevelConf conf)
        {
            Directory.CreateDirectory(LevelDir(id));
            conf.id = id;

            if (!string.IsNullOrEmpty(sourceImagePath) && File.Exists(sourceImagePath))
                File.Copy(sourceImagePath, PngPath(id), overwrite: true);

            File.WriteAllText(JsonPath(id), JsonUtility.ToJson(conf, true));
            AssetDatabase.Refresh();
        }

        public static int NextFreeId()
        {
            var ids = LoadAll().Select(c => c.id).ToList();
            return ids.Count == 0 ? 1 : ids.Max() + 1;
        }

        /// <summary>Highest endLevel across all maps, or 0 if maps.json is missing.</summary>
        public static int LastMapEndLevel()
        {
            if (!File.Exists(MapsConfigPath))
                return 0;
            MapsFile file = JsonUtility.FromJson<MapsFile>(File.ReadAllText(MapsConfigPath));
            if (file?.maps == null || file.maps.Length == 0)
                return 0;
            return file.maps.Max(m => m.endLevel);
        }

        // Minimal shapes for reading maps.json without depending on the runtime Map type.
        [System.Serializable] private class MapsFile { public MapEntry[] maps; }
        [System.Serializable] private class MapEntry { public int id; public int startLevel; public int endLevel; }
    }
}
