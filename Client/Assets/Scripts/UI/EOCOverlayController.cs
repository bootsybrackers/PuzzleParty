using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using DG.Tweening;

namespace PuzzleParty.UI
{
    /// <summary>
    /// Controls the "Out Of Content" overlay (the EOCOverlay hierarchy already authored in
    /// MainMenuScene) shown when the player has beaten the last level of the last map.
    /// Added at runtime via MainMenuController (mirrors DebugOverlayController's pattern),
    /// and locates the existing scene hierarchy by name rather than via serialized Inspector
    /// references, so no scene/prefab wiring is required.
    /// </summary>
    public class EOCOverlayController : MonoBehaviour
    {
        private const string ROOT_NAME = "EOCOverlay";
        private const string OK_BUTTON_NAME = "OkButton";

        private GameObject overlayRoot;
        private CanvasGroup canvasGroup;
        private Button okButton;

        /// <summary>Invoked after the overlay has finished closing (OK pressed).</summary>
        public System.Action OnClosed;

        public bool IsAvailable => overlayRoot != null;

        void Awake()
        {
            // GameObject.Find() only matches objects that are active-in-hierarchy at the exact
            // moment it's called, which made this fail intermittently depending on scene/
            // transition timing. A manual search through the scene's root objects finds it
            // regardless of active state.
            overlayRoot = FindInSceneEvenIfInactive(ROOT_NAME);
            if (overlayRoot == null)
            {
                Debug.LogWarning($"EOCOverlayController: could not find '{ROOT_NAME}' in the scene.");
                return;
            }

            canvasGroup = overlayRoot.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = overlayRoot.AddComponent<CanvasGroup>();

            // Search by name rather than a fixed multi-segment path (Transform.Find requires
            // an exact match at every segment and fails silently) - same reasoning as the
            // root lookup above.
            Transform okTransform = FindRecursive(overlayRoot.transform, OK_BUTTON_NAME);
            okButton = okTransform != null ? okTransform.GetComponent<Button>() : null;
            if (okButton == null)
                Debug.LogWarning($"EOCOverlayController: could not find a Button named '{OK_BUTTON_NAME}' under '{ROOT_NAME}'.");

            // The overlay is authored active in the scene by default - hide it immediately so
            // it only ever appears when Show() is explicitly called.
            overlayRoot.SetActive(false);
        }

        public void Show()
        {
            if (overlayRoot == null) return;

            overlayRoot.transform.DOKill();
            canvasGroup.DOKill();

            overlayRoot.SetActive(true);
            overlayRoot.transform.localScale = Vector3.zero;
            canvasGroup.alpha = 0f;

            canvasGroup.DOFade(1f, 0.35f).SetLink(overlayRoot);
            overlayRoot.transform.DOScale(Vector3.one, 0.5f)
                .SetEase(Ease.OutBack)
                .SetLink(overlayRoot);

            if (okButton != null)
            {
                okButton.onClick.RemoveAllListeners();
                okButton.onClick.AddListener(Hide);
            }
        }

        private static GameObject FindInSceneEvenIfInactive(string name)
        {
            foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                Transform found = FindRecursive(root.transform, name);
                if (found != null) return found.gameObject;
            }
            return null;
        }

        private static Transform FindRecursive(Transform parent, string name)
        {
            if (parent.name == name) return parent;

            foreach (Transform child in parent)
            {
                Transform result = FindRecursive(child, name);
                if (result != null) return result;
            }

            return null;
        }

        private void Hide()
        {
            if (overlayRoot == null) return;

            overlayRoot.transform.DOKill();
            canvasGroup.DOKill();

            canvasGroup.DOFade(0f, 0.3f).SetLink(overlayRoot);
            overlayRoot.transform.DOScale(Vector3.zero, 0.3f)
                .SetEase(Ease.InBack)
                .SetLink(overlayRoot)
                .OnComplete(() =>
                {
                    if (overlayRoot != null) overlayRoot.SetActive(false);
                    OnClosed?.Invoke();
                });
        }
    }
}
