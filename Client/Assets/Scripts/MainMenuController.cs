using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PuzzleParty.Maps;
using PuzzleParty.Service;
using PuzzleParty.Progressions;

namespace PuzzleParty.UI
{
    public class MainMenuController : MonoBehaviour
    {
    [SerializeField]
    private Button playButton;

    [SerializeField]
    private TextMeshProUGUI titleText;

    [SerializeField]
    private TextMeshProUGUI mapNameText;

    [SerializeField]
    private TextMeshProUGUI mapProgressText;

    [SerializeField]
    private TextMeshProUGUI coinsText;

    private ISceneLoader sceneLoader;
    private IProgressionService progressionService;
    private IMapService mapService;
    private ITransitionService transitionService;

    void Start()
    {
        // Get services
        sceneLoader = ServiceLocator.GetInstance().Get<SceneLoader>();
        progressionService = ServiceLocator.GetInstance().Get<ProgressionService>();
        mapService = ServiceLocator.GetInstance().Get<MapService>();
        transitionService = ServiceLocator.GetInstance().Get<TransitionService>();

        // Setup UI
        if (titleText != null)
        {
            titleText.text = "Puzzle Party";
        }

        // Setup button listener
        if (playButton != null)
        {
            playButton.onClick.AddListener(OnPlayButtonClicked);
        }

        // Display player stats and map progress
        DisplayPlayerStats();

        // Fade in from black when scene starts
        transitionService.FadeIn();
    }

    private void OnPlayButtonClicked()
    {
        Debug.Log("Play button clicked - Loading game scene");
        sceneLoader.LoadGame();
    }

    private void DisplayPlayerStats()
    {
        Progression progression = progressionService.GetProgression();
        Debug.Log($"Player stats - Last beaten level: {progression.lastBeatenLevel}, Coins: {progression.coins}");

        // Display coins
        if (coinsText != null)
        {
            coinsText.text = $"Coins: {progression.coins}";
        }

        // Get and display current map info
        Map currentMap = mapService.GetCurrentMap(progression.lastBeatenLevel);
        if (currentMap != null)
        {
            if (mapNameText != null)
            {
                mapNameText.text = currentMap.name;
            }

            if (mapProgressText != null)
            {
                int levelsCompleted = currentMap.GetLevelProgress(progression.lastBeatenLevel);
                int totalLevels = currentMap.TotalLevels;
                mapProgressText.text = $"Level {levelsCompleted}/{totalLevels}";

                Debug.Log($"Current map: {currentMap.name} - Progress: {levelsCompleted}/{totalLevels}");
            }
        }
        else
        {
            Debug.LogWarning("No current map found for player progression");
        }
    }

    void OnDestroy()
    {
        // Clean up button listener
        if (playButton != null)
        {
            playButton.onClick.RemoveListener(OnPlayButtonClicked);
        }
    }
    }
}
