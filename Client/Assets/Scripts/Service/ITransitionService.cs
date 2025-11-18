namespace PuzzleParty.Service
{
    public interface ITransitionService
    {
        void FadeOut(System.Action onComplete = null);
        void FadeIn(System.Action onComplete = null);
        void SetFadeDuration(float duration);
    }
}
