using UnityEngine;

namespace PuzzleParty.Board.Effects
{
    public class IceOverlayController : MonoBehaviour
    {
        public SpriteRenderer IceRenderer { get; private set; }
        public Vector3 TargetScale { get; private set; } = Vector3.one;

        public void Setup(float tileWidth, float tileHeight)
        {
            IceRenderer = GetComponentInChildren<SpriteRenderer>();
            if (IceRenderer == null || IceRenderer.sprite == null)
            {
                Debug.LogWarning("IceOverlayController: no SpriteRenderer or sprite found in children");
                return;
            }

            float spriteWidth = IceRenderer.sprite.bounds.size.x;
            float spriteHeight = IceRenderer.sprite.bounds.size.y;
            TargetScale = new Vector3(tileWidth / spriteWidth, tileHeight / spriteHeight, 1f);
            transform.localScale = TargetScale;
        }
    }
}
