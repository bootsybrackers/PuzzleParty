using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace PuzzleParty.Board.Effects
{
    public class ConfettiEffect : MonoBehaviour
    {
        [Header("Spawn Settings")]
        [SerializeField] private float confettiSpawnRate = 0.05f;
        [SerializeField] private int confettiPerSpawn = 3;
        [SerializeField] private Vector2 confettiSize = new Vector2(12f, 20f);
        [SerializeField] private float confettiFallDuration = 2.5f;
        [SerializeField] private float confettiSpinSpeed = 360f;
        [SerializeField] private float confettiHorizontalDrift = 80f;
        [SerializeField] private int confettiBurstCount = 100;
        [SerializeField] private int maxActiveConfetti = 120;
        [SerializeField] private Color[] confettiColors = new Color[]
        {
            new Color(1f, 0.2f, 0.3f),
            new Color(1f, 0.8f, 0.2f),
            new Color(0.2f, 0.8f, 0.4f),
            new Color(0.3f, 0.6f, 1f),
            new Color(0.9f, 0.4f, 0.9f),
            new Color(1f, 0.5f, 0.2f),
        };

        private Coroutine confettiCoroutine;
        private List<GameObject> activeConfetti = new List<GameObject>();
        private Queue<GameObject> pool = new Queue<GameObject>();
        private Sprite confettiSprite;
        private Transform canvasParent;

        public void Play(Transform canvasParent)
        {
            this.canvasParent = canvasParent;
            confettiSprite = Resources.Load<Sprite>("Images/Effects/confetti_piece");
            if (confettiSprite == null)
            {
                Debug.LogWarning("ConfettiEffect: confetti_piece sprite not found, generating fallback");
                confettiSprite = CreateFallbackConfettiSprite();
            }

            InitPool();
            StartCoroutine(SpawnConfettiBurst());
            confettiCoroutine = StartCoroutine(SpawnConfettiContinuously());
        }

        private void InitPool()
        {
            for (int i = 0; i < maxActiveConfetti; i++)
            {
                GameObject obj = new GameObject("Confetti");
                obj.transform.SetParent(canvasParent, false);
                Image img = obj.AddComponent<Image>();
                img.sprite = confettiSprite;
                img.raycastTarget = false;
                obj.SetActive(false);
                pool.Enqueue(obj);
            }
        }

        private GameObject GetFromPool()
        {
            if (pool.Count == 0) return null;
            GameObject obj = pool.Dequeue();
            Image img = obj.GetComponent<Image>();
            if (img != null)
            {
                img.sprite = confettiSprite;
                Color c = img.color;
                c.a = 1f;
                img.color = c;
            }
            obj.SetActive(true);
            return obj;
        }

        private void ReturnToPool(GameObject obj)
        {
            if (obj == null) return;
            activeConfetti.Remove(obj);
            obj.transform.DOKill();
            obj.SetActive(false);
            pool.Enqueue(obj);
        }

        public void Stop()
        {
            if (confettiCoroutine != null)
            {
                StopCoroutine(confettiCoroutine);
                confettiCoroutine = null;
            }

            foreach (var confetti in activeConfetti)
            {
                if (confetti != null)
                {
                    confetti.transform.DOKill();
                    confetti.SetActive(false);
                    pool.Enqueue(confetti);
                }
            }
            activeConfetti.Clear();
        }

        private void OnDestroy()
        {
            Stop();
            while (pool.Count > 0)
            {
                var obj = pool.Dequeue();
                if (obj != null) Destroy(obj);
            }
        }

        private IEnumerator SpawnConfettiBurst()
        {
            if (canvasParent == null)
            {
                Debug.LogWarning("ConfettiEffect: Cannot spawn confetti burst - missing canvas parent");
                yield break;
            }

            float screenWidth = Screen.width;
            float screenHeight = Screen.height;

            for (int i = 0; i < confettiBurstCount; i++)
            {
                SpawnBurstConfetti(screenWidth, screenHeight);
            }

            yield return null;
        }

        private void SpawnBurstConfetti(float screenWidth, float screenHeight)
        {
            GameObject confettiObj = GetFromPool();
            if (confettiObj == null) return;

            Image img = confettiObj.GetComponent<Image>();
            img.color = confettiColors[Random.Range(0, confettiColors.Length)];

            RectTransform rect = confettiObj.GetComponent<RectTransform>();

            float size = Random.Range(confettiSize.x, confettiSize.y) * 1.8f;
            rect.sizeDelta = new Vector2(size * 0.6f, size);

            float spawnX = screenWidth * 0.5f + Random.Range(-screenWidth * 0.15f, screenWidth * 0.15f);
            float spawnY = screenHeight * 0.65f + Random.Range(-30f, 60f);

            rect.position = new Vector3(spawnX, spawnY, 0f);

            float startRotation = Random.Range(0f, 360f);
            rect.rotation = Quaternion.Euler(0f, 0f, startRotation);

            activeConfetti.Add(confettiObj);

            float targetY = -100f;
            float driftX = Random.Range(-screenWidth * 0.8f, screenWidth * 0.8f);
            float targetX = spawnX + driftX;

            float upwardBoost = Random.Range(200f, 450f);
            float duration = confettiFallDuration * Random.Range(1.0f, 1.5f);

            float spinDirection = Random.value > 0.5f ? 1f : -1f;
            float totalRotation = confettiSpinSpeed * 2.5f * duration * spinDirection;

            DOVirtual.Float(0f, 1f, duration, t =>
                {
                    if (rect == null) return;
                    float xPos = Mathf.Lerp(spawnX, targetX, t);
                    float arcHeight = upwardBoost * 1.3f;
                    float yOffset = arcHeight * (4f * t * (1f - t));
                    float yBase = Mathf.Lerp(spawnY, targetY, t * t);
                    float flutter = Mathf.Sin(t * Mathf.PI * 5f) * 35f;
                    rect.position = new Vector3(xPos + flutter, yBase + yOffset, 0f);
                })
                .OnComplete(() => ReturnToPool(confettiObj));

            rect.DORotate(new Vector3(0f, 0f, startRotation + totalRotation), duration, RotateMode.FastBeyond360)
                .SetEase(Ease.Linear);
            img.DOFade(0f, duration * 0.3f).SetDelay(duration * 0.7f);
        }

        private IEnumerator SpawnConfettiContinuously()
        {
            if (canvasParent == null)
            {
                Debug.LogWarning("ConfettiEffect: Cannot spawn confetti - missing canvas parent");
                yield break;
            }

            float screenWidth = Screen.width;
            float screenHeight = Screen.height;

            while (true)
            {
                if (activeConfetti.Count < maxActiveConfetti)
                {
                    for (int i = 0; i < confettiPerSpawn; i++)
                    {
                        if (activeConfetti.Count >= maxActiveConfetti) break;
                        SpawnSingleConfetti(screenWidth, screenHeight);
                    }
                }
                yield return new WaitForSeconds(confettiSpawnRate);
            }
        }

        private void SpawnSingleConfetti(float screenWidth, float screenHeight)
        {
            GameObject confettiObj = GetFromPool();
            if (confettiObj == null) return;

            Image img = confettiObj.GetComponent<Image>();
            img.color = confettiColors[Random.Range(0, confettiColors.Length)];

            RectTransform rect = confettiObj.GetComponent<RectTransform>();

            float size = Random.Range(confettiSize.x, confettiSize.y);
            rect.sizeDelta = new Vector2(size * 0.6f, size);

            float spawnX = Random.value > 0.5f
                ? Random.Range(0f, screenWidth * 0.35f)
                : Random.Range(screenWidth * 0.65f, screenWidth);
            float spawnY = screenHeight + 50f;

            rect.position = new Vector3(spawnX, spawnY, 0f);

            float startRotation = Random.Range(0f, 360f);
            rect.rotation = Quaternion.Euler(0f, 0f, startRotation);

            activeConfetti.Add(confettiObj);

            float targetY = -100f;
            float driftX = Random.Range(-confettiHorizontalDrift, confettiHorizontalDrift);
            float targetX = spawnX + driftX;

            float duration = confettiFallDuration * Random.Range(0.8f, 1.2f);

            float spinDirection = Random.value > 0.5f ? 1f : -1f;
            float totalRotation = confettiSpinSpeed * duration * spinDirection;

            DOVirtual.Float(0f, 1f, duration, t =>
                {
                    if (rect == null) return;
                    float wave = Mathf.Sin(t * Mathf.PI * 3f) * 30f;
                    float xPos = Mathf.Lerp(spawnX, targetX, t) + wave;
                    float yPos = Mathf.Lerp(spawnY, targetY, t * t);
                    rect.position = new Vector3(xPos, yPos, 0f);
                })
                .OnComplete(() => ReturnToPool(confettiObj));

            rect.DORotate(new Vector3(0f, 0f, startRotation + totalRotation), duration, RotateMode.FastBeyond360)
                .SetEase(Ease.Linear);
            img.DOFade(0f, duration * 0.3f).SetDelay(duration * 0.7f);
        }

        private static Sprite CreateFallbackConfettiSprite()
        {
            int width = 8;
            int height = 12;
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    tex.SetPixel(x, y, Color.white);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}
