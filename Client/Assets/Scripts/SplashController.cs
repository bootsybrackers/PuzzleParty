using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using PuzzleParty.Service;

namespace PuzzleParty.UI
{
    public class SplashController : MonoBehaviour
    {
        [SerializeField]
        private Image logoImage;

        [SerializeField]
        private float fadeInDuration = 1f;

        [SerializeField]
        private float displayDuration = 2f;

        [SerializeField]
        private float fadeOutDuration = 1f;

        private ISceneLoader sceneLoader;
        private ITransitionService transitionService;

        void Start()
        {
            // Get services
            sceneLoader = ServiceLocator.GetInstance().Get<SceneLoader>();
            transitionService = ServiceLocator.GetInstance().Get<TransitionService>();

        // Start with logo invisible
        if (logoImage != null)
        {
            logoImage.color = new Color(1, 1, 1, 0);
        }

        // Fade in from black when scene starts
        transitionService.FadeIn(() =>
        {
            // Start the splash sequence after fade in
            StartCoroutine(PlaySplashSequence());
        });
    }

    private IEnumerator PlaySplashSequence()
    {
        // Fade in
        if (logoImage != null)
        {
            logoImage.DOFade(1f, fadeInDuration).SetEase(Ease.InOutQuad);
        }
        yield return new WaitForSeconds(fadeInDuration);

        // Display
        yield return new WaitForSeconds(displayDuration);

        // Fade out
        if (logoImage != null)
        {
            logoImage.DOFade(0f, fadeOutDuration).SetEase(Ease.InOutQuad);
        }
        yield return new WaitForSeconds(fadeOutDuration);

        // Load next scene
        sceneLoader.LoadLoading();
    }
    }
}
