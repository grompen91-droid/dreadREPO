using System.Collections;
using System.Collections.Generic;
using Dread.Config;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Dread.Systems
{
    public class VisualCorruptionSystem : MonoBehaviour
    {
        private readonly List<Light> _sceneLights = new();
        private Image? _vignetteImage;
        private bool _inLevel;
        private Camera? _mainCam;

        private void Start()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            BuildVignetteOverlay();
            StartCoroutine(LightFlickerLoop());
            StartCoroutine(VignettePulseLoop());
            StartCoroutine(ShadowGlitchLoop());
            StartCoroutine(MonsterProximityLoop());
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            _inLevel = !scene.name.Contains("Menu") && !scene.name.Contains("Main");
            _mainCam = null;
            RefreshLights();
        }

        private void RefreshLights()
        {
            _sceneLights.Clear();
            foreach (var light in FindObjectsOfType<Light>())
            {
                if (light.type == LightType.Point || light.type == LightType.Spot)
                    _sceneLights.Add(light);
            }
        }

        // ── Light Flicker ─────────────────────────────────────────────────────

        private IEnumerator LightFlickerLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(Random.Range(8f, 25f));

                if (!DreadConfig.LightFlickerEnabled.Value || !_inLevel || _sceneLights.Count == 0)
                    continue;

                _sceneLights.RemoveAll(l => l == null);
                if (_sceneLights.Count == 0) continue;

                var target = _sceneLights[Random.Range(0, _sceneLights.Count)];
                StartCoroutine(FlickerLight(target));
            }
        }

        private IEnumerator FlickerLight(Light light)
        {
            var originalIntensity = light.intensity;
            var count = Random.Range(2, 5);
            for (var i = 0; i < count; i++)
            {
                light.intensity = 0f;
                yield return new WaitForSeconds(Random.Range(0.05f, 0.15f));
                light.intensity = originalIntensity;
                yield return new WaitForSeconds(Random.Range(0.05f, 0.12f));
            }
        }

        // ── Vignette Pulse ────────────────────────────────────────────────────

        private void BuildVignetteOverlay()
        {
            var canvasGo = new GameObject("DreadVignette");
            DontDestroyOnLoad(canvasGo);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999;
            canvasGo.AddComponent<CanvasScaler>();

            var imgGo = new GameObject("VignetteImage");
            imgGo.transform.SetParent(canvasGo.transform, false);
            _vignetteImage = imgGo.AddComponent<Image>();
            _vignetteImage.color = new Color(0f, 0f, 0f, 0f);
            _vignetteImage.raycastTarget = false;

            var rect = imgGo.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            _vignetteImage.sprite = CreateVignetteSprite(256);
        }

        private static Sprite CreateVignetteSprite(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var center = new Vector2(size / 2f, size / 2f);
            var maxDist = size / 2f;

            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                var dist = Vector2.Distance(new Vector2(x, y), center) / maxDist;
                var alpha = Mathf.Clamp01((dist - 0.4f) / 0.6f);
                tex.SetPixel(x, y, new Color(0f, 0f, 0f, alpha));
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        private IEnumerator VignettePulseLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(Random.Range(15f, 45f));

                if (!DreadConfig.VignetteEnabled.Value || !_inLevel || _vignetteImage == null)
                    continue;

                StartCoroutine(PulseVignette());
            }
        }

        private IEnumerator PulseVignette()
        {
            const float peakAlpha = 0.65f;
            const float fadeIn = 0.4f;
            const float hold = 0.2f;
            const float fadeOut = 1.2f;

            var t = 0f;
            while (t < fadeIn)
            {
                _vignetteImage!.color = new Color(0f, 0f, 0f, Mathf.Lerp(0f, peakAlpha, t / fadeIn));
                t += Time.deltaTime;
                yield return null;
            }

            yield return new WaitForSeconds(hold);

            t = 0f;
            while (t < fadeOut)
            {
                _vignetteImage!.color = new Color(0f, 0f, 0f, Mathf.Lerp(peakAlpha, 0f, t / fadeOut));
                t += Time.deltaTime;
                yield return null;
            }

            _vignetteImage!.color = new Color(0f, 0f, 0f, 0f);
        }

        // ── Shadow Glitch ─────────────────────────────────────────────────────

        private IEnumerator ShadowGlitchLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(Random.Range(20f, 60f));

                if (!DreadConfig.ShadowGlitchEnabled.Value || !_inLevel)
                    continue;

                if (_mainCam == null) _mainCam = Camera.main;
                if (_mainCam == null) continue;

                StartCoroutine(SpawnShadow());
            }
        }

        private IEnumerator SpawnShadow()
        {
            var cam = _mainCam!;
            var right = cam.transform.right;
            var side = Random.value > 0.5f ? 1f : -1f;
            var pos = cam.transform.position
                + cam.transform.forward * Random.Range(8f, 12f)
                + right * (side * 3f);

            var shadow = GameObject.CreatePrimitive(PrimitiveType.Quad);
            shadow.name = "DreadShadow";
            shadow.transform.position = pos;
            shadow.transform.localScale = new Vector3(0.4f, 2.2f, 1f);
            shadow.transform.LookAt(cam.transform);

            var rend = shadow.GetComponent<Renderer>();
            var mat = new Material(Shader.Find("Sprites/Default"));
            mat.color = new Color(0f, 0f, 0f, 0.85f);
            rend.material = mat;

            Destroy(shadow.GetComponent<Collider>());

            yield return new WaitForSeconds(Random.Range(0.08f, 0.25f));
            Destroy(shadow);
        }

        // ── Monster Proximity ─────────────────────────────────────────────────
        // Scans for EnemyHealth components every 3s and adds a proximity tracker.
        // Tag-agnostic — works with Mimic, WesleysEnemies, and any future modded enemies.

        private IEnumerator MonsterProximityLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(3f);

                if (!DreadConfig.MonsterVisualEnabled.Value || !_inLevel)
                    continue;

                var enemies = FindObjectsOfType<EnemyHealth>();
                foreach (var e in enemies)
                {
                    if (e.GetComponent<MonsterProximityEffect>() == null)
                        e.gameObject.AddComponent<MonsterProximityEffect>();
                }
            }
        }
    }

    // Attached to every enemy GameObject at runtime. Drives the red-edge distortion overlay.
    public class MonsterProximityEffect : MonoBehaviour
    {
        private static Image? _distortionOverlay;
        private static int _activeCount;

        private const float TriggerRange = 8f;
        private const float MaxAlpha = 0.35f;

        private Camera? _cam;

        private void Start()
        {
            _cam = Camera.main;
            BuildOverlayIfNeeded();
            _activeCount++;
        }

        private void OnDestroy()
        {
            _activeCount--;
        }

        private void Update()
        {
            if (_cam == null || _distortionOverlay == null) return;

            var dist = Vector3.Distance(transform.position, _cam.transform.position);
            var t = 1f - Mathf.Clamp01(dist / TriggerRange);
            var alpha = t * MaxAlpha;

            var current = _distortionOverlay.color.a;
            if (alpha > current)
            {
                _distortionOverlay.color = new Color(0.1f, 0f, 0f, alpha);
            }
            else if (_activeCount <= 1)
            {
                _distortionOverlay.color = new Color(
                    0.1f, 0f, 0f,
                    Mathf.MoveTowards(current, 0f, Time.deltaTime * 2f));
            }
        }

        private static void BuildOverlayIfNeeded()
        {
            if (_distortionOverlay != null) return;

            var go = new GameObject("DreadMonsterOverlay");
            DontDestroyOnLoad(go);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 998;
            go.AddComponent<CanvasScaler>();

            var imgGo = new GameObject("DistortionImage");
            imgGo.transform.SetParent(go.transform, false);
            _distortionOverlay = imgGo.AddComponent<Image>();
            _distortionOverlay.color = new Color(0.1f, 0f, 0f, 0f);
            _distortionOverlay.raycastTarget = false;

            var rect = imgGo.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
