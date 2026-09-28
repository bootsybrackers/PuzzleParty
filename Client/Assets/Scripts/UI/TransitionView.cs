using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace PuzzleParty.Service
{
    public class TransitionView : MonoBehaviour
    {
    private Canvas canvas;
    private Image fadeImage;
    private CanvasGroup canvasGroup;
    private Coroutine activeTransition;

    void Awake()
    {
        SetupCanvas();
    }

    private void SetupCanvas()
    {
        // Create canvas
        GameObject canvasGo = new GameObject("TransitionCanvas");
        canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999; // Render on top of everything
        canvasGo.AddComponent<GraphicRaycaster>();

        // Add CanvasGroup to control raycasts
        canvasGroup = canvasGo.AddComponent<CanvasGroup>();
        canvasGroup.blocksRaycasts = false; // Don't block clicks when not transitioning

        // Add CanvasScaler for proper scaling
        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        // Create fade image
        GameObject imageGo = new GameObject("FadeImage");
        imageGo.transform.SetParent(canvasGo.transform, false);
        fadeImage = imageGo.AddComponent<Image>();
        fadeImage.color = new Color(0, 0, 0, 0);

        // Stretch to fill screen
        RectTransform rt = imageGo.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;

        DontDestroyOnLoad(canvasGo);
    }

    public void FadeOut(float duration, System.Action onComplete = null)
    {
        // Interrupt whatever transition is currently running rather than silently dropping
        // this request - a caller asking to fade out always expects onComplete to eventually
        // fire (that's usually where the actual scene load happens), and a stale coroutine
        // left mid-flight from an overlapping fade would otherwise swallow it entirely.
        if (activeTransition != null) StopCoroutine(activeTransition);

        activeTransition = StartCoroutine(FadeOutCoroutine(duration, onComplete));
    }

    public void FadeIn(float duration, System.Action onComplete = null)
    {
        if (activeTransition != null) StopCoroutine(activeTransition);

        activeTransition = StartCoroutine(FadeInCoroutine(duration, onComplete));
    }

    private IEnumerator FadeOutCoroutine(float duration, System.Action onComplete)
    {
        // Block raycasts during transition
        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = true;
        }

        if (fadeImage != null)
        {
            fadeImage.DOKill();
            fadeImage.DOFade(1f, duration).SetEase(Ease.InOutQuad);
        }

        yield return new WaitForSeconds(duration);

        activeTransition = null;
        onComplete?.Invoke();
    }

    private IEnumerator FadeInCoroutine(float duration, System.Action onComplete)
    {
        if (fadeImage != null)
        {
            fadeImage.DOKill();
            fadeImage.DOFade(0f, duration).SetEase(Ease.InOutQuad);
        }

        yield return new WaitForSeconds(duration);

        // Unblock raycasts after fade in completes
        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = false;
        }

        activeTransition = null;
        onComplete?.Invoke();
    }
    }
}
