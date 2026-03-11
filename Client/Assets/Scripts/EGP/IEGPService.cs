using PuzzleParty.Progressions;

namespace PuzzleParty.EGP
{
    public interface IEGPService
    {
        EGPRound GetCurrentOffer();
        EGPContents Purchase(IProgressionService progressionService);
        bool CanAfford(IProgressionService progressionService);
        void ResetRounds();
    }
}
