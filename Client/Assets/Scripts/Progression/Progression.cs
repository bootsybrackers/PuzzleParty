namespace PuzzleParty.Progressions
{
    [System.Serializable]
    public class Progression
    {
        public int lastBeatenLevel;
        public int coins;
        public int streak; // Tracks consecutive level wins (0-3), unlocks powerups

        //could also contain booster array
    }
}