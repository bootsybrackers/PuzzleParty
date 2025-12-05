namespace PuzzleParty.Levels
{
    [System.Serializable]
    public class LockedTile
    {
        public int row;
        public int column;
    }

    public class LevelConf
    {
        public int id;

        public int rows;
        public int columns;
        public int moves;
        public int holes;
        public string name;
        public LockedTile[] locked_tiles;
    }
}