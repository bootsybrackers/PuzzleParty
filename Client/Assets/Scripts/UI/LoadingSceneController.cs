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

    void Start()
    {
        // Get services
        sceneLoader = ServiceLocator.GetInstance().Get<SceneLoader>();
        progressionService = ServiceLocator.GetInstance().Get<ProgressionService>();
        transitionService = ServiceLocator.GetInstance().Get<TransitionService>();

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

        //Remove old progression. Just for dev mode
        progressionService.WipeProgression();

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

        // Loading complete, go to main menu
        sceneLoader.LoadMainMenu();
    }
    }
}
