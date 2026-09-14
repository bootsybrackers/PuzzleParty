using System.Linq;
using UnityEngine;

namespace PuzzleParty.Maps
{
    public class MapService : IMapService
    {
        private MapsConfig mapsConfig;

        public MapService()
        {
            LoadMapsConfig();
        }

        private void LoadMapsConfig()
        {
            // Resources/Config/maps.json, not StreamingAssets - see LevelService for why
            // (StreamingAssets isn't readable via System.IO on Android at all).
            TextAsset configAsset = Resources.Load<TextAsset>("Config/maps");
            if (configAsset == null)
            {
                Debug.LogError("Maps config not found at Resources/Config/maps");
                mapsConfig = new MapsConfig { maps = new Map[0] };
                return;
            }

            mapsConfig = JsonUtility.FromJson<MapsConfig>(configAsset.text);
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

        /// <summary>
        /// True once the player has beaten the last level of the last configured map - i.e.
        /// there is currently no next level or next map to send them to.
        /// </summary>
        public bool IsOutOfContent(int lastBeatenLevel)
        {
            if (mapsConfig.maps.Length == 0)
                return false;

            Map lastMap = mapsConfig.maps[mapsConfig.maps.Length - 1];
            return lastBeatenLevel >= lastMap.endLevel;
        }
    }
}
