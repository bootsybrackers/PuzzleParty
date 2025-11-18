namespace PuzzleParty.Service
{
    public interface ISceneLoader
    {
        void LoadScene(string sceneName);
        void LoadSceneAsync(string sceneName, System.Action onComplete = null);
        void LoadMainMenu();
        void LoadGame();
        void LoadLoading();
    }
}
