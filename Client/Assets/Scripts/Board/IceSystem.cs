using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using PuzzleParty.Board.Effects;

namespace PuzzleParty.Board
{
    public class IceSystem : MonoBehaviour
    {
        [SerializeField] private IceOverlayController iceOverlayPrefab;
        [SerializeField] private IceBreakEffect iceBreakEffectPrefab;

        private Dictionary<string, GameObject> tileObjects;
        private List<GameObject> holeIceContainers = new List<GameObject>();
        private Transform boardParent;
        private int gridCols;
        private int gridRows;
        private float tileWorldWidth;
        private float tileWorldHeight;

        public void Initialize()
        {
        }

        public void SetTileObjects(Dictionary<string, GameObject> tileObjects)
        {
            this.tileObjects = tileObjects;
        }

        public void SetGridInfo(Transform parent, int cols, int rows, float worldWidth, float worldHeight)
        {
            boardParent = parent;
            gridCols = cols;
            gridRows = rows;
            tileWorldWidth = worldWidth;
            tileWorldHeight = worldHeight;
        }

        public void AddIceOverlay(GameObject tileObj)
        {
            if (iceOverlayPrefab == null)
            {
                Debug.LogWarning("IceOverlayPrefab not assigned on IceSystem");
                return;
            }

            SpriteRenderer tileSr = tileObj.GetComponent<SpriteRenderer>();
            float tileWidth = tileSr != null && tileSr.sprite != null ? tileSr.sprite.bounds.size.x : tileWorldWidth;
            float tileHeight = tileSr != null && tileSr.sprite != null ? tileSr.sprite.bounds.size.y : tileWorldHeight;

            IceOverlayController overlay = Instantiate(iceOverlayPrefab, tileObj.transform);
            overlay.transform.localPosition = Vector3.zero;
            overlay.Setup(tileWidth, tileHeight);
        }

        public void AnimateIceBreak(List<(int row, int col)> icedPositions, System.Action onComplete)
        {
            StartCoroutine(IceBreakRoutine(icedPositions, onComplete));
        }

        private IEnumerator IceBreakRoutine(List<(int row, int col)> icedPositions, System.Action onComplete)
        {
            List<IceOverlayController> overlays = new List<IceOverlayController>();

            // Collect overlays from iced tiles
            foreach (var pos in icedPositions)
            {
                string key = TileFactory.GetTileKey(pos.row, pos.col);
                if (!tileObjects.TryGetValue(key, out GameObject tileObj))
                    continue;

                IceOverlayController controller = tileObj.GetComponentInChildren<IceOverlayController>();
                if (controller != null)
                    overlays.Add(controller);

                if (iceBreakEffectPrefab != null)
                {
                    IceBreakEffect effect = Instantiate(iceBreakEffectPrefab);
                    effect.Play(tileObj.transform.position);
                }
            }

            // Collect overlays from hole containers
            foreach (var holeObj in holeIceContainers)
            {
                if (holeObj == null) continue;
                IceOverlayController controller = holeObj.GetComponentInChildren<IceOverlayController>();
                if (controller != null)
                    overlays.Add(controller);

                if (iceBreakEffectPrefab != null)
                {
                    IceBreakEffect effect = Instantiate(iceBreakEffectPrefab);
                    effect.Play(holeObj.transform.position);
                }
            }

            // Shake and fade all ice overlays
            Sequence breakSeq = DOTween.Sequence();
            foreach (var overlay in overlays)
            {
                breakSeq.Join(overlay.transform.DOShakePosition(0.3f, 0.1f, 15, 90f, false, true));
                if (overlay.IceRenderer != null)
                    breakSeq.Join(overlay.IceRenderer.DOFade(0f, 0.35f));
            }

            yield return breakSeq.WaitForCompletion();

            foreach (var overlay in overlays)
                if (overlay != null)
                    Destroy(overlay.gameObject);

            // Destroy hole ice containers
            foreach (var holeObj in holeIceContainers)
                if (holeObj != null)
                    Destroy(holeObj);
            holeIceContainers.Clear();

            onComplete?.Invoke();
        }

        public void AnimateIceAppearing(BoardTile[][] scrambledBoard)
        {
            Sequence appearSeq = DOTween.Sequence();
            int index = 0;

            // Build the set of iced rows by inspecting iced tiles
            var iceRowSet = new HashSet<int>();
            for (int i = 0; i < scrambledBoard.Length; i++)
                for (int j = 0; j < scrambledBoard[i].Length; j++)
                    if (scrambledBoard[i][j] != null && scrambledBoard[i][j].IsIced)
                        iceRowSet.Add(i);

            for (int i = 0; i < scrambledBoard.Length; i++)
            {
                for (int j = 0; j < scrambledBoard[i].Length; j++)
                {
                    BoardTile tile = scrambledBoard[i][j];
                    GameObject tileObj;

                    if (tile != null && tile.IsIced)
                    {
                        // Normal iced tile — find its existing game object
                        string key = TileFactory.GetTileKey(tile.Row, tile.Column);
                        if (!tileObjects.TryGetValue(key, out tileObj))
                            continue;
                    }
                    else if (tile == null && iceRowSet.Contains(i) && boardParent != null)
                    {
                        // Hole in an iced row — create a container at the hole's position
                        Vector3 localPos = TileFactory.GetTileLocalPosition(
                            j + 1, i + 1, gridCols, gridRows,
                            tileWorldWidth, tileWorldHeight, boardParent.lossyScale);
                        tileObj = new GameObject($"IceHole_{i}_{j}");
                        tileObj.transform.SetParent(boardParent);
                        tileObj.transform.localPosition = localPos;
                        holeIceContainers.Add(tileObj);
                    }
                    else
                    {
                        continue;
                    }

                    AddIceOverlay(tileObj);

                    IceOverlayController controller = tileObj.GetComponentInChildren<IceOverlayController>();
                    if (controller != null)
                    {
                        float delay = index * 0.08f;
                        controller.transform.localScale = Vector3.zero;
                        appearSeq.Insert(delay, controller.transform.DOScale(controller.TargetScale, 0.4f).SetEase(Ease.OutBack).SetLink(controller.gameObject));

                        if (controller.IceRenderer != null)
                        {
                            Color c = controller.IceRenderer.color;
                            c.a = 0f;
                            controller.IceRenderer.color = c;
                            appearSeq.Insert(delay, controller.IceRenderer.DOFade(1f, 0.3f));
                        }

                        index++;
                    }
                }
            }
        }
    }
}
