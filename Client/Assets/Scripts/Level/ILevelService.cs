namespace PuzzleParty.Levels
{
    public interface ILevelService
    {
        Level GetNextLevel();
        Level GetLevel(int levelId);
    }
}