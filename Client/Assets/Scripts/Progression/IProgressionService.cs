namespace PuzzleParty.Progressions
{
    public interface IProgressionService
    {
        Progression GetProgression();
        void SaveProgression(Progression progression);

        void WipeProgression();
    }
}