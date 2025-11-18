using System;

namespace PuzzleParty.Maps
{
    [Serializable]
    public class Map
    {
        public int id;
        public string name;
        public int startLevel;
        public int endLevel;
        public string theme;

        public int TotalLevels => endLevel - startLevel + 1;

        public bool ContainsLevel(int levelId)
        {
            return levelId >= startLevel && levelId <= endLevel;
        }

        public int GetLevelProgress(int lastBeatenLevel)
        {
            if (lastBeatenLevel < startLevel)
                return 0;

            if (lastBeatenLevel >= endLevel)
                return TotalLevels;

            return (lastBeatenLevel - startLevel) + 1;
        }
    }
}
