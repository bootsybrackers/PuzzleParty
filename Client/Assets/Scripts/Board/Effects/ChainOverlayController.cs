using UnityEngine;

namespace PuzzleParty.Board.Effects
{
    public class ChainOverlayController : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer darkOverlayRenderer;
        [SerializeField] private SpriteRenderer chainRenderer;

        public SpriteRenderer DarkOverlayRenderer => darkOverlayRenderer;
        public SpriteRenderer ChainRenderer => chainRenderer;

        /// <summary>
        /// Sets up the chain overlay to fit a tile of the given size.
        /// </summary>
        public void Setup(float tileWidth, float tileHeight, Sprite chainSprite)
        {
            // Setup dark overlay
            if (darkOverlayRenderer != null)
            {
                Sprite whiteSquare = Resources.Load<Sprite>("Images/Effects/white_square");
                if (whiteSquare == null)
                {
                    whiteSquare = CreateFallbackWhiteSquare();
                }
                darkOverlayRenderer.sprite = whiteSquare;
                darkOverlayRenderer.sortingOrder = 19;
                darkOverlayRenderer.color = new Color(0f, 0f, 0f, 0.5f);

                float darkSpriteSize = darkOverlayRenderer.sprite.bounds.size.x;
                float darkScaleX = tileWidth / darkSpriteSize;
                float darkScaleY = tileHeight / darkSpriteSize;
                darkOverlayRenderer.transform.localScale = new Vector3(darkScaleX, darkScaleY, 1f);
            }

            // Setup chain sprite
            if (chainRenderer != null && chainSprite != null)
            {
                chainRenderer.sprite = chainSprite;
                chainRenderer.sortingOrder = 20;

                float chainWidth = chainSprite.bounds.size.x;
                float chainHeight = chainSprite.bounds.size.y;

                float scaleX = tileWidth / chainWidth;
                float scaleY = tileHeight / chainHeight;
                float uniformScale = Mathf.Min(scaleX, scaleY) * 0.85f;

                chainRenderer.transform.localScale = new Vector3(uniformScale, uniformScale, 1f);
            }
        }

        /// <summary>
        /// Updates the chain sprite (for progression animation).
        /// </summary>
        public void SetChainSprite(Sprite newSprite)
        {
            if (chainRenderer != null)
            {
                chainRenderer.sprite = newSprite;
            }
        }

        private static Sprite CreateFallbackWhiteSquare()
        {
            int size = 32;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    tex.SetPixel(x, y, Color.white);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}
