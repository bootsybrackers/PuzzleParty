namespace PuzzleParty.EGP
{
    [System.Serializable]
    public class EGPConfig
    {
        public EGPRound[] rounds;
    }

    [System.Serializable]
    public class EGPRound
    {
        public int round;
        public int price;
        public EGPContents contents;
    }

    [System.Serializable]
    public class EGPContents
    {
        public int extraMoves;
    }
}
