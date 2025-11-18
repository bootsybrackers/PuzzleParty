using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PuzzleParty.Service
{
    public class SceneLoader : ISceneLoader
    {
    public void LoadScene(string sceneName)
    {
        LoadSceneWithTransition(sceneName);
    }

    public void LoadSceneAsync(string sceneName, System.Action onComplete = null)
    {
        // Use SceneManager's async loading with callback
        AsyncOperation asyncOp = SceneManager.LoadSceneAsync(sceneName);
        if (asyncOp != null && onComplete != null)
        {
            asyncOp.completed += (op) => onComplete();
        }
    }

    public void LoadMainMenu()
    {
        LoadSceneWithTransition("MainMenuScene");
    }

    public void LoadGame()
    {
        LoadSceneWithTransition("GameScene");
    }

    public void LoadLoading()
    {
        LoadSceneWithTransition("LoadingScene");
    }

    private void LoadSceneWithTransition(string sceneName)
    {
        ITransitionService transitionService = ServiceLocator.GetInstance().Get<TransitionService>();

        // Fade out, then load scene, then fade in
        transitionService.FadeOut(() =>
        {
            SceneManager.LoadScene(sceneName);
            // Fade in will be called by the new scene's controller
        });
    }
    }
}
