using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PuzzleParty.Service;
using PuzzleParty.Progressions;

namespace PuzzleParty.UI
{
    /// <summary>
    /// Hidden debug panel. Tap the top-left corner 3 times to open it.
    /// Lets you adjust lastBeatenLevel and coins without touching save files.
    /// </summary>
    public class DebugOverlayController : MonoBehaviour
    {
        private const float TAP_ZONE_WIDTH  = 0.15f; // fraction of screen width
        private const float TAP_ZONE_HEIGHT = 0.15f; // fraction of screen height (from top)
        private const float TAP_RESET_TIME  = 1.5f;  // seconds before tap count resets

        public System.Action OnClose;

        private int   tapCount   = 0;
        private float lastTapTime = 0f;
        private bool  isOpen     = false;

        private GameObject overlayRoot;
        private TextMeshProUGUI levelValueText;
        private TextMeshProUGUI coinsValueText;

        private IProgressionService progressionService;

        void Start()
        {
            progressionService = ServiceLocator.GetInstance().Get<ProgressionService>();
        }

        void Update()
        {
            if (!isOpen)
                DetectCornerTaps();
        }

        // ─── Tap detection ────────────────────────────────────────────────────────

        private void DetectCornerTaps()
        {
            bool tapped = false;
            Vector2 tapPos = Vector2.zero;

            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Began)
                {
                    tapped = true;
                    tapPos = touch.position;
                }
            }
            else if (Input.GetMouseButtonDown(0))
            {
                tapped = true;
                tapPos = Input.mousePosition;
            }

            if (!tapped) return;

            // Screen coords: x=0 left, y=0 bottom — top-left = x small, y large
            bool inZone = tapPos.x < Screen.width  * TAP_ZONE_WIDTH &&
                          tapPos.y > Screen.height * (1f - TAP_ZONE_HEIGHT);

            if (!inZone)
            {
                tapCount = 0;
                return;
            }

            if (Time.unscaledTime - lastTapTime > TAP_RESET_TIME)
                tapCount = 0;

            tapCount++;
            lastTapTime = Time.unscaledTime;

            if (tapCount >= 3)
            {
                tapCount = 0;
                Open();
            }
        }

        // ─── Open / Close ─────────────────────────────────────────────────────────

        private void Open()
        {
            if (isOpen) return;
            isOpen = true;

            Progression prog = progressionService.GetProgression();

            // Root — full-screen overlay canvas
            overlayRoot = new GameObject("DebugOverlay");
            DontDestroyOnLoad(overlayRoot); // survives scene transitions if needed
            Canvas canvas = overlayRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9999;

            CanvasScaler scaler = overlayRoot.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;

            overlayRoot.AddComponent<GraphicRaycaster>();

            // Dim background — blocks all touches behind it
            GameObject dimBg = MakePanel(overlayRoot.transform, new Color(0f, 0f, 0f, 0.75f));
            StretchFull(dimBg.GetComponent<RectTransform>());

            // Card
            GameObject card = MakePanel(dimBg.transform, new Color(0.08f, 0.10f, 0.16f, 1f));
            RectTransform cardRt = card.GetComponent<RectTransform>();
            cardRt.anchorMin = new Vector2(0.08f, 0.28f);
            cardRt.anchorMax = new Vector2(0.92f, 0.72f);
            cardRt.sizeDelta = Vector2.zero;
            AddRoundedOutline(card);

            // ── Title ──
            MakeLabel(card.transform, "DEBUG PANEL", 54, new Color(1f, 0.85f, 0.2f),
                new Vector2(0f, 0.84f), new Vector2(1f, 0.98f));

            // ── Level section ──
            MakeLabel(card.transform, "Last Beaten Level", 30, new Color(0.7f, 0.7f, 0.7f),
                new Vector2(0.05f, 0.68f), new Vector2(0.95f, 0.80f));

            GameObject levelRow = MakeRow(card.transform,
                new Vector2(0.05f, 0.52f), new Vector2(0.95f, 0.68f));

            MakeButton(levelRow.transform, "−", new Vector2(0f, 0f), new Vector2(0.25f, 1f),
                new Color(0.8f, 0.2f, 0.2f), () => ChangeLevel(-1));
            levelValueText = MakeLabel(levelRow.transform, $"{prog.lastBeatenLevel}", 52, Color.white,
                new Vector2(0.25f, 0f), new Vector2(0.75f, 1f));
            MakeButton(levelRow.transform, "+", new Vector2(0.75f, 0f), new Vector2(1f, 1f),
                new Color(0.2f, 0.65f, 0.2f), () => ChangeLevel(1));

            // ── Coins section ──
            MakeLabel(card.transform, "Coins", 30, new Color(0.7f, 0.7f, 0.7f),
                new Vector2(0.05f, 0.36f), new Vector2(0.95f, 0.48f));

            coinsValueText = MakeLabel(card.transform, $"{prog.coins}", 52, Color.white,
                new Vector2(0.05f, 0.24f), new Vector2(0.95f, 0.36f));

            MakeButton(card.transform, "+100 Coins", new Vector2(0.1f, 0.10f), new Vector2(0.9f, 0.23f),
                new Color(0.15f, 0.45f, 0.85f), AddCoins);

            // ── Close ──
            MakeButton(card.transform, "✕", new Vector2(0.82f, 0.86f), new Vector2(0.97f, 0.98f),
                new Color(0.35f, 0.10f, 0.10f), Close);
        }

        private void Close()
        {
            if (!isOpen) return;
            isOpen = false;

            if (overlayRoot != null)
            {
                Destroy(overlayRoot);
                overlayRoot = null;
            }

            OnClose?.Invoke();
        }

        // ─── Actions ──────────────────────────────────────────────────────────────

        private void ChangeLevel(int delta)
        {
            Progression prog = progressionService.GetProgression();
            prog.lastBeatenLevel = Mathf.Max(0, prog.lastBeatenLevel + delta);
            progressionService.SaveProgression(prog);
            if (levelValueText != null)
                levelValueText.text = $"{prog.lastBeatenLevel}";
        }

        private void AddCoins()
        {
            Progression prog = progressionService.GetProgression();
            prog.coins += 100;
            progressionService.SaveProgression(prog);
            if (coinsValueText != null)
                coinsValueText.text = $"{prog.coins}";
        }

        // ─── UI helpers ───────────────────────────────────────────────────────────

        private static GameObject MakePanel(Transform parent, Color color)
        {
            var go = new GameObject("Panel");
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            var img = go.AddComponent<Image>();
            img.color = color;
            return go;
        }

        private static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
        }

        private static void AddRoundedOutline(GameObject card)
        {
            // Simple 2px white border via a child panel behind the card content
            var border = new GameObject("Border");
            border.transform.SetParent(card.transform, false);
            border.transform.SetAsFirstSibling();
            var rt = border.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = new Vector2(4f, 4f); // 2px each side
            rt.anchoredPosition = Vector2.zero;
            var img = border.AddComponent<Image>();
            img.color = new Color(0.4f, 0.45f, 0.6f, 0.8f);
        }

        private static TextMeshProUGUI MakeLabel(Transform parent, string text, int fontSize,
            Color color, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject("Label");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontStyle = FontStyles.Bold;
            return tmp;
        }

        private static GameObject MakeRow(Transform parent, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject("Row");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
            return go;
        }

        private static Button MakeButton(Transform parent, string label,
            Vector2 anchorMin, Vector2 anchorMax, Color bgColor, System.Action onClick)
        {
            var go = new GameObject($"Btn_{label}");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;

            var img = go.AddComponent<Image>();
            img.color = bgColor;

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            var colors = btn.colors;
            colors.highlightedColor = bgColor * 1.3f;
            colors.pressedColor     = bgColor * 0.7f;
            btn.colors = colors;
            btn.onClick.AddListener(() => onClick());

            MakeLabel(go.transform, label, 40, Color.white, Vector2.zero, Vector2.one);
            return btn;
        }
    }
}
