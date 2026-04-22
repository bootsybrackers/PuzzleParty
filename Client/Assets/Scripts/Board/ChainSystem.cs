using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using PuzzleParty.Board.Effects;

namespace PuzzleParty.Board
{
    public class ChainSystem : MonoBehaviour
    {
        [SerializeField] private ChainBreakEffect chainBreakEffectPrefab;
        [SerializeField] private ChainOverlayController chainOverlayPrefab;

        private Sprite chainSprite4;
        private Sprite chainSprite3;
        private Sprite chainSprite2;
        private Sprite chainSprite1;
        private int currentChainState = -1;

        private Dictionary<string, GameObject> tileObjects;

        public void Initialize()
        {
            LoadChainSprites();
            currentChainState = -1;
        }

        public void SetTileObjects(Dictionary<string, GameObject> tileObjects)
        {
            this.tileObjects = tileObjects;
        }

        public void SetInitialState(int tilesRemaining)
        {
            currentChainState = tilesRemaining;
        }

        public void UpdateChainProgress(int correctlyPlacedCount)
        {
            int tilesRemaining = Mathf.Max(0, 4 - correctlyPlacedCount);

            if (tilesRemaining == currentChainState)
                return;

            int previousState = currentChainState;
            currentChainState = tilesRemaining;

            // If unlocked (0 remaining), don't update chains - AnimateUnlock handles that
            if (tilesRemaining == 0)
                return;

            Sprite newSprite = GetChainSpriteForRemaining(tilesRemaining);
            if (newSprite == null)
            {
                Debug.LogWarning($"No chain sprite found for {tilesRemaining} remaining");
                return;
            }

            foreach (var kvp in tileObjects)
            {
                ChainOverlayController controller = kvp.Value.GetComponentInChildren<ChainOverlayController>();
                if (controller != null)
                {
                    SpriteRenderer chainSr = controller.ChainRenderer;
                    if (chainSr != null && chainSr.sprite != newSprite)
                        AnimateChainTransition(controller, chainSr, newSprite);
                }
            }
        }

        public void UpdateChainOverlay(GameObject tileObj, bool shouldBeLocked)
        {
            ChainOverlayController controller = tileObj.GetComponentInChildren<ChainOverlayController>();
            bool hasChain = controller != null;

            if (shouldBeLocked && !hasChain)
            {
                int tilesRemaining = currentChainState > 0 ? currentChainState : 3;
                AddChainOverlay(tileObj, tilesRemaining);
            }
            else if (!shouldBeLocked && hasChain)
            {
                Destroy(controller.gameObject);
            }
        }

        public void AddChainOverlay(GameObject tileObj)
        {
            AddChainOverlay(tileObj, 3);
        }

        public void AddChainOverlay(GameObject tileObj, int tilesRemaining)
        {
            Sprite chainSprite = GetChainSpriteForRemaining(tilesRemaining);

            if (chainSprite == null)
            {
                Debug.LogError($"Failed to load chain sprite for {tilesRemaining} remaining");
                return;
            }

            SpriteRenderer tileSr = tileObj.GetComponent<SpriteRenderer>();
            float tileWidth = 1f;
            float tileHeight = 1f;
            if (tileSr != null && tileSr.sprite != null)
            {
                tileWidth = tileSr.sprite.bounds.size.x;
                tileHeight = tileSr.sprite.bounds.size.y;
            }

            if (chainOverlayPrefab != null)
            {
                ChainOverlayController overlay = Instantiate(chainOverlayPrefab, tileObj.transform);
                overlay.transform.localPosition = Vector3.zero;
                overlay.Setup(tileWidth, tileHeight, chainSprite);
            }
            else
            {
                Debug.LogWarning("ChainOverlay prefab not assigned, falling back to procedural creation");
                CreateChainOverlayProcedural(tileObj, chainSprite, tileWidth, tileHeight);
            }
        }

        public void AnimateUnlock(int row, int col)
        {
            string key = $"{row}_{col}";
            if (!tileObjects.TryGetValue(key, out GameObject tileObj))
                return;

            ChainOverlayController controller = tileObj.GetComponentInChildren<ChainOverlayController>();

            if (controller != null)
            {
                SpriteRenderer chainSr = controller.ChainRenderer;
                SpriteRenderer darkSr = controller.DarkOverlayRenderer;

                if (chainSr != null)
                {
                    controller.transform.DOScale(Vector3.one * 1.5f, 0.3f).SetEase(Ease.OutBack).SetLink(controller.gameObject);
                    chainSr.DOFade(0f, 0.3f).SetLink(controller.gameObject);
                }

                if (darkSr != null)
                    darkSr.DOFade(0f, 0.3f);

                Destroy(controller.gameObject, 0.35f);
            }
            else
            {
                // Fallback: look for old-style overlays
                Transform chainOverlay = tileObj.transform.Find("ChainOverlay");
                Transform darkOverlay = tileObj.transform.Find("ChainDarkOverlay");

                if (chainOverlay != null)
                {
                    SpriteRenderer chainSr = chainOverlay.GetComponent<SpriteRenderer>();
                    if (chainSr != null)
                    {
                        chainOverlay.DOScale(Vector3.one * 1.5f, 0.3f).SetEase(Ease.OutBack).SetLink(chainOverlay.gameObject);
                        chainSr.DOFade(0f, 0.3f).OnComplete(() => Destroy(chainOverlay.gameObject)).SetLink(chainOverlay.gameObject);
                    }
                }

                if (darkOverlay != null)
                {
                    SpriteRenderer darkSr = darkOverlay.GetComponent<SpriteRenderer>();
                    if (darkSr != null)
                        darkSr.DOFade(0f, 0.3f).OnComplete(() => Destroy(darkOverlay.gameObject));
                }
            }
        }

        private void LoadChainSprites()
        {
            chainSprite4 = Resources.Load<Sprite>("Images/chains");
            chainSprite3 = Resources.Load<Sprite>("Images/chains_3");
            chainSprite2 = Resources.Load<Sprite>("Images/chains_2");
            chainSprite1 = Resources.Load<Sprite>("Images/chains_1");

            if (chainSprite4 == null || chainSprite3 == null || chainSprite2 == null || chainSprite1 == null)
                Debug.LogWarning("Some chain sprites not found. Make sure chains.png, chains_3.png, chains_2.png, chains_1.png are in Resources/Images/");
        }

        private Sprite GetChainSpriteForRemaining(int tilesRemaining)
        {
            if (tilesRemaining >= 4) return chainSprite4;
            if (tilesRemaining == 3) return chainSprite3;
            if (tilesRemaining == 2) return chainSprite2;
            if (tilesRemaining == 1) return chainSprite1;
            return null;
        }

        private void AnimateChainTransition(ChainOverlayController controller, SpriteRenderer chainSr, Sprite newSprite)
        {
            Transform chainTransform = controller.transform;
            Vector3 originalScale = chainTransform.localScale;
            Vector3 worldPos = chainTransform.position;

            if (chainBreakEffectPrefab != null)
            {
                ChainBreakEffect burst = Instantiate(chainBreakEffectPrefab);
                burst.Play(worldPos);
            }

            DG.Tweening.Sequence transitionSeq = DOTween.Sequence().SetLink(chainTransform.gameObject);
            transitionSeq.Append(chainTransform.DOScale(originalScale * 0.7f, 0.12f).SetEase(Ease.InBack));
            transitionSeq.Join(chainSr.DOFade(0.3f, 0.12f));
            transitionSeq.AppendCallback(() => { chainSr.sprite = newSprite; });
            transitionSeq.Append(chainTransform.DOScale(originalScale, 0.2f).SetEase(Ease.OutBack));
            transitionSeq.Join(chainSr.DOFade(1f, 0.15f));
            transitionSeq.Join(chainTransform.DOShakePosition(0.15f, 0.05f, 10, 90f, false, true));
        }

        private void CreateChainOverlayProcedural(GameObject tileObj, Sprite chainSprite, float tileWidth, float tileHeight)
        {
            GameObject darkOverlay = new GameObject("ChainDarkOverlay");
            darkOverlay.transform.SetParent(tileObj.transform, false);
            darkOverlay.transform.localPosition = Vector3.zero;

            SpriteRenderer darkSr = darkOverlay.AddComponent<SpriteRenderer>();
            Texture2D whiteTex = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            for (int y = 0; y < 32; y++)
                for (int x = 0; x < 32; x++)
                    whiteTex.SetPixel(x, y, Color.white);
            whiteTex.Apply();
            darkSr.sprite = Sprite.Create(whiteTex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 100f);
            darkSr.sortingOrder = 19;
            darkSr.color = new Color(0f, 0f, 0f, 0.5f);

            float darkSpriteSize = darkSr.sprite.bounds.size.x;
            darkOverlay.transform.localScale = new Vector3(tileWidth / darkSpriteSize, tileHeight / darkSpriteSize, 1f);

            GameObject chainObj = new GameObject("ChainOverlay");
            chainObj.transform.SetParent(tileObj.transform, false);

            SpriteRenderer chainSr = chainObj.AddComponent<SpriteRenderer>();
            chainSr.sprite = chainSprite;
            chainSr.sortingOrder = 20;
            chainObj.transform.localPosition = Vector3.zero;

            float scaleX = tileWidth / chainSprite.bounds.size.x;
            float scaleY = tileHeight / chainSprite.bounds.size.y;
            float uniformScale = Mathf.Min(scaleX, scaleY) * 0.85f;
            chainObj.transform.localScale = new Vector3(uniformScale, uniformScale, 1f);
        }
    }
}
