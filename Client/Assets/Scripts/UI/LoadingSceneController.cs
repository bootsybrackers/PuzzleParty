using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PuzzleParty.Service;
using PuzzleParty.Progressions;

namespace PuzzleParty.UI
{
    public class LoadingSceneController : MonoBehaviour
    {
    [SerializeField]
    private TextMeshProUGUI loadingText;

    [SerializeField]
    private Slider progressBar;

    [SerializeField]
    private float minimumLoadingTime = 2f; // Minimum time to show loading screen

    private ISceneLoader sceneLoader;
    private IProgressionService progressionService;
    private ITransitionService transitionService;
    private IBackendSyncService backendSync;

    void Start()
    {
        // Get services
        sceneLoader = ServiceLocator.GetInstance().Get<SceneLoader>();
        progressionService = ServiceLocator.GetInstance().Get<ProgressionService>();
        transitionService = ServiceLocator.GetInstance().Get<TransitionService>();
        backendSync = ServiceLocator.GetInstance().Get<BackendSyncService>();

        // Fade in from black when scene starts
        transitionService.FadeIn(() =>
        {
            // Start loading after fade in
            StartCoroutine(LoadGameResources());
        });
    }

    private IEnumerator LoadGameResources()
    {
        float startTime = Time.time;
        float progress = 0f;

        // Simulate loading resources (you can replace this with actual resource loading)
        while (progress < 1f)
        {
            progress += Time.deltaTime / minimumLoadingTime;
            progress = Mathf.Clamp01(progress);

            // Update UI
            if (progressBar != null)
            {
                progressBar.value = progress;
            }

            if (loadingText != null)
            {
                loadingText.text = $"Loading... {Mathf.RoundToInt(progress * 100)}%";
            }

            yield return null;
        }

        // Ensure minimum loading time has passed
        float elapsedTime = Time.time - startTime;
        if (elapsedTime < minimumLoadingTime)
        {
            yield return new WaitForSeconds(minimumLoadingTime - elapsedTime);
        }

        // Wait for backend login/install to finish so server progression is applied before MainMenu reads it
        yield return new WaitUntil(() => backendSync.IsReady);

        // Loading complete, go to main menu
        sceneLoader.LoadMainMenu();
    }
    }
}
