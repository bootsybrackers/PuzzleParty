using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MainMenuController : MonoBehaviour
{
    [SerializeField]
    private Button playButton;

    [SerializeField]
    private TextMeshProUGUI titleText;

    private ISceneLoader sceneLoader;
    private IProgressionService progressionService;

    void Start()
    {
        // Get services
        sceneLoader = ServiceLocator.GetInstance().Get<SceneLoader>();
        progressionService = ServiceLocator.GetInstance().Get<ProgressionService>();

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

        // Display player stats (optional)
        DisplayPlayerStats();
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

        // You can add UI elements to display these stats
        // For example: coinsText.text = $"Coins: {progression.coins}";
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
