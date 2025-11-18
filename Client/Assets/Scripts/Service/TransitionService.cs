using UnityEngine;

namespace PuzzleParty.Service
{
    public class TransitionService : ITransitionService
    {
        private TransitionView view;
        private float fadeDuration = 0.5f;

        public TransitionService()
        {
            // View will be created lazily on first use
        }

        private TransitionView GetOrCreateView()
        {
            if (view == null)
            {
                // Find existing view in scene (in case it was already created)
                view = Object.FindFirstObjectByType<TransitionView>();

                if (view == null)
                {
                    // Create new view
                    GameObject viewGo = new GameObject("TransitionView");
                    view = viewGo.AddComponent<TransitionView>();
                    Object.DontDestroyOnLoad(viewGo);
                }
            }

            return view;
        }

        public void FadeOut(System.Action onComplete = null)
        {
            TransitionView v = GetOrCreateView();
            v.FadeOut(fadeDuration, onComplete);
        }

        public void FadeIn(System.Action onComplete = null)
        {
            TransitionView v = GetOrCreateView();
            v.FadeIn(fadeDuration, onComplete);
        }

        public void SetFadeDuration(float duration)
        {
            fadeDuration = duration;
        }
    }
}
