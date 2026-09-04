using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using PuzzleParty.Maps;
using PuzzleParty.Service;
using PuzzleParty.Progressions;
using PuzzleParty.Levels;

namespace PuzzleParty.UI
{
    public class MainMenuController : MonoBehaviour
    {
    [Header("View")]
    [SerializeField]
    private MainMenuView mainMenuView;

    [Header("UI Components")]
    [SerializeField]
    private Button playButton;

    [SerializeField]
    private TextMeshProUGUI playButtonText;

    [SerializeField]
    private TextMeshProUGUI titleText;

    [SerializeField]
    private TextMeshProUGUI mapProgressText;

    [SerializeField]
    private TextMeshProUGUI coinsText;

    [Header("Overlays")]
    [SerializeField]
    private MapCompletionOverlayController mapCompletionOverlay;

    private ISceneLoader sceneLoader;
    private IProgressionService progressionService;
    private IMapService mapService;
    private ITransitionService transitionService;
    private ILevelService levelService;

    void Start()
    {
        // Get view if not assigned
        if (mainMenuView == null)
        {
            mainMenuView = GetComponent<MainMenuView>();
        }

        // Get services
        sceneLoader = ServiceLocator.GetInstance().Get<SceneLoader>();
        progressionService = ServiceLocator.GetInstance().Get<ProgressionService>();
        mapService = ServiceLocator.GetInstance().Get<MapService>();
        transitionService = ServiceLocator.GetInstance().Get<TransitionService>();
        levelService = ServiceLocator.GetInstance().Get<LevelService>();

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

        // Start play button pulse animation
        StartPlayButtonPulseAnimation();

        bool outOfContent = mapService.IsOutOfContent(progressionService.GetProgression().lastBeatenLevel);

        // Display player stats and map progress
        DisplayPlayerStats();

        // Update play button text with next level name
        UpdatePlayButtonText();

        // Setup atlas visualization
        SetupAtlas();

        // Check if we just completed a level (and optionally a whole map)
        bool justCompletedLevel = PlayerPrefs.GetInt("JustCompletedLevel", 0) == 1;
        bool justCompletedMap   = PlayerPrefs.GetInt("JustCompletedMap",   0) == 1;
        int  completedMapId     = PlayerPrefs.GetInt("CompletedMapId",      0);

        if (justCompletedLevel)
        {
            PlayerPrefs.SetInt("JustCompletedLevel", 0);
            PlayerPrefs.Save();
        }
        if (justCompletedMap)
        {
            PlayerPrefs.SetInt("JustCompletedMap", 0);
            PlayerPrefs.SetInt("CompletedMapId",   0);
            PlayerPrefs.Save();
        }

        // Wire up hidden debug overlay (3 taps in top-left corner)
        DebugOverlayController debug = gameObject.AddComponent<DebugOverlayController>();
        debug.OnClose = () =>
        {
            DisplayPlayerStats();
            UpdatePlayButtonText();
            SetupAtlas();

            // See the comment in ShowMapCompletionAfterDelay: SetupAtlas() rebuilds every
            // marker from scratch, and freshly created ones start invisible until animated
            // in. The atlas is already open by the time the debug overlay can be closed, so
            // snap the rebuilt markers straight to visible.
            if (mainMenuView != null)
                mainMenuView.SetOpenImmediate();
        };

        // Wire up the "out of content" overlay (shown when there's no next level/map left).
        EOCOverlayController eocOverlay = gameObject.AddComponent<EOCOverlayController>();

        // Fade in from black, then animate atlas
        transitionService.FadeIn(() =>
        {
            if (mainMenuView == null) return;

            if (outOfContent)
            {
                // Beating the last level of the last map - or simply returning later with
                // still no new content available - should go straight to the out-of-content
                // overlay: skip the atlas-open animation and the map-completion overlay
                // entirely (the atlas itself is already left blank, see SetupAtlas above).
                mainMenuView.SetOpenImmediate();
                eocOverlay.Show();
            }
            else if (justCompletedMap)
            {
                // Finishing the last level of a map should feel like it leads directly
                // into the map completion screen, not through the menu underneath it.
                // The atlas has already switched to the new map (see SetupAtlas above),
                // so snap it straight to its open state and show the overlay immediately
                // instead of playing the book-opening animation first.
                mainMenuView.SetOpenImmediate();
                StartCoroutine(ShowMapCompletionAfterDelay(completedMapId, 0.1f));
            }
            else
            {
                mainMenuView.AnimateOpen(() =>
                {
                    if (justCompletedLevel)
                    {
                        AnimateNewLevelCompletion();
                    }
                });
            }
        });
    }

    private void OnPlayButtonClicked()
    {
        Debug.Log("Play button clicked - Loading game scene");
        sceneLoader.LoadGame();
    }

    private void UpdatePlayButtonText()
    {
        Level nextLevel = levelService.GetNextLevel();
        bool hasNextLevel = nextLevel != null;

        if (playButtonText != null)
        {
            playButtonText.text = hasNextLevel ? $"Level {nextLevel.Id}" : "Play";
        }

        // No level left to load - avoid sending the player into a Play button that has
        // nothing to play (GetNextLevel() returning null would otherwise crash BoardController).
        if (playButton != null)
        {
            playButton.interactable = hasNextLevel;
        }
    }

    private void DisplayPlayerStats()
    {
        Progression progression = progressionService.GetProgression();
        Debug.Log($"Player stats - Last beaten level: {progression.lastBeatenLevel}, Coins: {progression.coins}");

        // Display coins
        if (coinsText != null)
        {
            coinsText.text = $"{progression.coins}";
        }

        if (mapService.IsOutOfContent(progression.lastBeatenLevel))
        {
            // No map left to show - leave the title/progress blank instead of re-showing the
            // already-finished last map (see EOCOverlayController / the out-of-content overlay).
            if (mainMenuView != null) mainMenuView.SetMapName("");
            if (mapProgressText != null) mapProgressText.text = "";
            return;
        }

        // Get and display current map info
        Map currentMap = mapService.GetCurrentMap(progression.lastBeatenLevel);
        if (currentMap != null)
        {
            // Set map name through the view so it can be animated
            if (mainMenuView != null)
            {
                mainMenuView.SetMapName(currentMap.name);
            }

            if (mapProgressText != null)
            {
                int currentMapNumber = currentMap.id;
                int totalMaps = mapService.GetAllMaps().Length;
                mapProgressText.text = $"{currentMapNumber}/{totalMaps}";

                Debug.Log($"Current map: {currentMap.name} - Map {currentMapNumber}/{totalMaps}");
            }
        }
        else
        {
            Debug.LogWarning("No current map found for player progression");
        }
    }

    private void SetupAtlas()
    {
        if (mainMenuView == null) return;

        Progression progression = progressionService.GetProgression();

        // Clear existing markers up front - out of content or not, we're about to redraw.
        mainMenuView.ClearLevelMarkers();

        if (mapService.IsOutOfContent(progression.lastBeatenLevel))
        {
            // No content left - leave the book's pages blank (see EOCOverlayController).
            Debug.Log("Out of content - leaving the atlas blank");
            return;
        }

        Map currentMap = mapService.GetCurrentMap(progression.lastBeatenLevel);

        if (currentMap == null)
        {
            Debug.LogWarning("No current map found for atlas setup");
            return;
        }

        // TODO: Load map sprite from resources or StreamingAssets
        // For now, you'll need to set this manually in the Unity Editor
        // mainMenuView.SetMapSprite(mapSprite);

        // Create level markers for all levels in the current map, split across left and right pages
        int halfPoint = currentMap.TotalLevels / 2;
        for (int levelId = currentMap.startLevel; levelId <= currentMap.endLevel; levelId++)
        {
            Level level = levelService.GetLevel(levelId);
            bool isCompleted = levelId <= progression.lastBeatenLevel;

            if (level != null)
            {
                int indexInMap = levelId - currentMap.startLevel;
                int page = indexInMap < halfPoint ? 0 : 1;
                mainMenuView.AddLevelMarker(levelId, level.Name, isCompleted, page);
            }
        }

        Debug.Log($"Atlas setup complete - Map: {currentMap.name}, Levels: {currentMap.startLevel}-{currentMap.endLevel}");
    }

    private void AnimateNewLevelCompletion()
    {
        if (mainMenuView == null)
        {
            Debug.LogWarning("mainMenuView is null in AnimateNewLevelCompletion");
            return;
        }

        Progression progression = progressionService.GetProgression();
        Debug.Log($"AnimateNewLevelCompletion - lastBeatenLevel: {progression.lastBeatenLevel}, coins: {progression.coins}");

        // Get the map that contains the just-completed level
        Map completedLevelMap = mapService.GetCurrentMap(progression.lastBeatenLevel);

        if (completedLevelMap == null)
        {
            Debug.LogWarning("completedLevelMap is null in AnimateNewLevelCompletion");
            return;
        }

        // Calculate the index of the just-completed level within that map
        int completedLevelIndex = progression.lastBeatenLevel - completedLevelMap.startLevel;

        Debug.Log($"Animating completion of level {progression.lastBeatenLevel} (index {completedLevelIndex} in map {completedLevelMap.name})");
        Debug.Log($"Map range: {completedLevelMap.startLevel}-{completedLevelMap.endLevel}");

        // Only animate if the index is valid
        if (completedLevelIndex >= 0 && completedLevelIndex < completedLevelMap.TotalLevels)
        {
            mainMenuView.AnimateNewLevelCompletion(completedLevelIndex);
        }
        else
        {
            Debug.LogWarning($"Completed level index {completedLevelIndex} is out of bounds for map {completedLevelMap.name} (0-{completedLevelMap.TotalLevels - 1})");
        }
    }

    private IEnumerator ShowMapCompletionAfterDelay(int completedMapId, float delay)
    {
        yield return new WaitForSeconds(delay);

        Map completedMap = mapService.GetMapById(completedMapId);
        if (completedMap == null) yield break;

        // Find the next map (the one after the completed one)
        Map[] allMaps = mapService.GetAllMaps();
        string nextMapName = null;
        for (int i = 0; i < allMaps.Length - 1; i++)
        {
            if (allMaps[i].id == completedMapId)
            {
                nextMapName = allMaps[i + 1].name;
                break;
            }
        }

        if (mapCompletionOverlay == null)
        {
            Debug.LogError("mapCompletionOverlay is not assigned in the Inspector on MainMenuController.");
            yield break;
        }

        mapCompletionOverlay.Show(completedMap.name, nextMapName, () =>
        {
            // Refresh UI to reflect new map and updated coins
            DisplayPlayerStats();
            UpdatePlayButtonText();
            SetupAtlas();

            // SetupAtlas() destroys and recreates every level marker. Freshly created
            // markers start scaled to zero and only reveal themselves via the staggered
            // entrance animation fired from AnimateOpen() - which already ran before the
            // overlay appeared, so it never touches these new instances. The atlas is
            // already open at this point, so just snap the new markers straight to visible
            // instead of leaving them invisible.
            if (mainMenuView != null)
                mainMenuView.SetOpenImmediate();
        });
    }

    private void StartPlayButtonPulseAnimation()
    {
        if (playButton == null) return;

        // Subtle pulse animation - scale from 1.0 to 1.05 and back
        playButton.transform.DOScale(1.05f, 0.8f)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);
    }

    void OnDestroy()
    {
        // Clean up button listener
        if (playButton != null)
        {
            playButton.onClick.RemoveListener(OnPlayButtonClicked);
        }

        // Kill any DOTween animations on the play button
        if (playButton != null)
        {
            playButton.transform.DOKill();
        }
    }
    }
}
