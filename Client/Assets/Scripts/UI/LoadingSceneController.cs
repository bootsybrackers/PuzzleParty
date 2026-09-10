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

    [SerializeField]
    private float backendReadyTimeoutSeconds = 6f; // Max time to wait on the backend before continuing anyway

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

        // Wait for backend login/install to finish so server progression is applied before MainMenu
        // reads it - but only up to a point. With no internet connection, or a slow/unreachable
        // server, this would otherwise block here forever with no feedback that anything's wrong.
        // Continuing without it just means the player sees local/stale progression for this
        // session instead of a frozen loading screen; BackendSyncService keeps trying in the
        // background regardless and IsReady/progression catch up once it does complete.
        float backendWaitStart = Time.time;
        yield return new WaitUntil(() =>
            backendSync.IsReady || Time.time - backendWaitStart > backendReadyTimeoutSeconds);

        if (!backendSync.IsReady)
        {
            Debug.LogWarning("[LoadingScene] Backend not ready after timeout - continuing with local progression.");
        }

        // Loading complete, go to main menu
        sceneLoader.LoadMainMenu();
    }
    }
}
