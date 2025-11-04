using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : ISceneLoader
{
    public void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    public void LoadSceneAsync(string sceneName, System.Action onComplete = null)
    {
        // Need to start coroutine from a MonoBehaviour
        // This will be handled by a SceneLoaderBehaviour component
        SceneLoaderBehaviour.Instance.LoadSceneAsyncCoroutine(sceneName, onComplete);
    }

    public void LoadMainMenu()
    {
        LoadScene("MainMenuScene");
    }

    public void LoadGame()
    {
        LoadScene("GameScene");
    }

    public void LoadLoading()
    {
        LoadScene("LoadingScene");
    }
}

// MonoBehaviour wrapper to handle coroutines
public class SceneLoaderBehaviour : MonoBehaviour
{
    private static SceneLoaderBehaviour instance;

    public static SceneLoaderBehaviour Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject go = new GameObject("SceneLoaderBehaviour");
                instance = go.AddComponent<SceneLoaderBehaviour>();
                DontDestroyOnLoad(go);
            }
            return instance;
        }
    }

    public void LoadSceneAsyncCoroutine(string sceneName, System.Action onComplete)
    {
        StartCoroutine(LoadSceneAsyncRoutine(sceneName, onComplete));
    }

    private IEnumerator LoadSceneAsyncRoutine(string sceneName, System.Action onComplete)
    {
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);

        // Wait until the asynchronous scene fully loads
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        onComplete?.Invoke();
    }
}
