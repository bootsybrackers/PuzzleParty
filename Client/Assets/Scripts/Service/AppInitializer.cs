using UnityEngine;

namespace PuzzleParty.Service
{
    public class AppInitializer : MonoBehaviour
    {
        private static bool isInitialized = false;

        void Awake()
        {
            if (!isInitialized)
            {
                // Configure all services once at app startup
                ServiceLocator.GetInstance().Configure();

                isInitialized = true;

                // Make sure we're on a root GameObject before calling DontDestroyOnLoad
                if (transform.parent == null)
                {
                    DontDestroyOnLoad(gameObject);
                }
                else
                {
                    Debug.LogWarning("AppInitializer should be on a root GameObject for DontDestroyOnLoad to work properly");
                }

                Debug.Log("App initialized - Services configured");
            }
            else
            {
                // If another AppInitializer exists, destroy this one
                Destroy(gameObject);
            }
        }
    }
}
