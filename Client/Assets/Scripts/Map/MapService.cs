using System.IO;
using System.Linq;
using UnityEngine;

namespace PuzzleParty.Maps
{
    public class MapService : IMapService
    {
        private MapsConfig mapsConfig;
        private static string ConfigPath => Path.Combine(Application.streamingAssetsPath, "config", "maps.json");

        public MapService()
        {
            LoadMapsConfig();
        }

        private void LoadMapsConfig()
        {
            if (!File.Exists(ConfigPath))
            {
                Debug.LogError($"Maps config not found at: {ConfigPath}");
                mapsConfig = new MapsConfig { maps = new Map[0] };
                return;
            }

            string json = File.ReadAllText(ConfigPath);
            mapsConfig = JsonUtility.FromJson<MapsConfig>(json);
            Debug.Log($"Loaded {mapsConfig.maps.Length} maps from config");
        }

        public MapsConfig GetMapsConfig()
        {
            return mapsConfig;
        }

        public Map GetCurrentMap(int lastBeatenLevel)
        {
            // Find the map that contains the next level to play
            int nextLevel = lastBeatenLevel + 1;

            foreach (Map map in mapsConfig.maps)
            {
                if (map.ContainsLevel(nextLevel))
                {
                    return map;
                }
            }

            // If no map contains the next level, return the last map
            if (mapsConfig.maps.Length > 0)
            {
                return mapsConfig.maps[mapsConfig.maps.Length - 1];
            }

            return null;
        }

        public Map GetMapById(int mapId)
        {
            return mapsConfig.maps.FirstOrDefault(m => m.id == mapId);
        }

        public Map[] GetAllMaps()
        {
            return mapsConfig.maps;
        }

        public bool IsMapUnlocked(int mapId, int lastBeatenLevel)
        {
            Map map = GetMapById(mapId);
            if (map == null)
                return false;

            // Map is unlocked if player has reached or passed its start level
            return lastBeatenLevel >= map.startLevel - 1;
        }
    }
}
