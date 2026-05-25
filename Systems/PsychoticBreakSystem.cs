using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using System.Reflection;
using Dread.Config;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

namespace Dread.Systems
{
    public class PsychoticBreakSystem : MonoBehaviour
    {
        private bool _enabled;
        private float _triggerChance;
        private float _episodeDuration;
        private bool _oncePerMatch;

        private bool _episodeActive;
        private bool _hasTriggeredThisMatch;
        private bool _episodeLocationCaptured;
        private Vector3 _episodeWorldPosition;
        private float _nextTriggerCheck;
        private float _episodeTimer;
        private Camera? _mainCam;

        private readonly List<float> _recentThreatTimestamps = new();
        private const float ThreatMemoryDuration = 30f;
        private const float ThreatRange = 15f;
        private const float SoloRange = 30f;

        private AudioClip? _peakScreamClip;
        private AudioClip? _distantScreamClip;
        private AudioClip? _threatScreamClip;
        private AudioClip? _footstepClip;
        private AudioSource? _footstepSource;
        private AudioSource? _distantScreamSource;
        private bool _hasPlayedPeakScream;

        private Canvas? _overlayCanvas;
        private Component? _darknessImage;
        private Component? _vignetteImage;
        private Texture2D? _vignetteTexture;
        private bool _useImguiOverlay;
        private float _darknessAlpha;
        private float _vignetteAlpha;
        private Texture2D? _imguiSolidTexture;
        private Texture2D? _imguiVignetteTexture;
        private GUIStyle? _imguiBoxStyle;

        private EnemyHealth[]? _cachedEnemies;
        private float _phantomSoundAccumulator;
        private float _tumbleMaintainTimer;
        private bool _sceneLoaded;
        private float _nextStatePublish;
        private string _cachedBlockReason = "";
        private bool _typeInitFailed;

        // -1 = all layers; avoids stub-only Physics.DefaultRaycastLayers at type init
        private const int VisionBlockMask = -1;

        private void Start()
        {
            LoggingService.LogVerbose("[PsychoticBreak] Awake starting...");
            SceneManager.sceneLoaded += OnSceneLoaded;
            _mainCam = Camera.main;

            RefreshConfig();
            OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);

            DreadConfig.PsychoticBreakEnabled.SettingChanged += OnConfigChanged;
            DreadConfig.CompatibilityMode.SettingChanged += OnConfigChanged;
            DreadConfig.PsychoticBreakTriggerChance.SettingChanged += OnConfigChanged;
            DreadConfig.PsychoticBreakDuration.SettingChanged += OnConfigChanged;
            DreadConfig.PsychoticBreakOncePerMatch.SettingChanged += OnConfigChanged;

            StartCoroutine(LoadAudioClips());
        }

        private void RefreshConfig()
        {
            _enabled = DreadConfig.PsychoticBreakEnabled.Value;
            _triggerChance = DreadConfig.PsychoticBreakTriggerChance.Value;
            _episodeDuration = DreadConfig.PsychoticBreakDuration.Value;
            _oncePerMatch = DreadConfig.PsychoticBreakOncePerMatch.Value;
        }

        private void OnConfigChanged(object? sender, EventArgs e)
        {
            RefreshConfig();
        }

        private void OnApplicationQuit()
        {
            CleanupOverlay();
            if (_episodeActive)
                RestorePlayerControl(PlayerController.instance);

            _episodeActive = false;
        }

        private void OnDestroy()
        {
            StopAllCoroutines();
            SceneManager.sceneLoaded -= OnSceneLoaded;

            DreadConfig.PsychoticBreakEnabled.SettingChanged -= OnConfigChanged;
            DreadConfig.PsychoticBreakTriggerChance.SettingChanged -= OnConfigChanged;
            DreadConfig.PsychoticBreakDuration.SettingChanged -= OnConfigChanged;
            DreadConfig.PsychoticBreakOncePerMatch.SettingChanged -= OnConfigChanged;
            DreadConfig.CompatibilityMode.SettingChanged -= OnConfigChanged;

            if (_peakScreamClip != null) Destroy(_peakScreamClip);
            if (_distantScreamClip != null) Destroy(_distantScreamClip);
            if (_threatScreamClip != null) Destroy(_threatScreamClip);
            if (_footstepClip != null) Destroy(_footstepClip);

            CleanupOverlay();
            CleanupFootstepSource();
        }

        private void CleanupFootstepSource()
        {
            if (_footstepSource != null)
            {
                Destroy(_footstepSource.gameObject);
                _footstepSource = null;
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            CleanupOverlay();
            _sceneLoaded = true;
            if (_episodeActive)
                RestorePlayerControl(PlayerController.instance);

            _episodeActive = false;
            _recentThreatTimestamps.Clear();
            _cachedEnemies = null;
            _mainCam = Camera.main;
            EnemyScanCache.Invalidate();
            CleanupOverlay();
            CleanupFootstepSource();
            CleanupDistantScreamSource();
            if (SemiFunc.MenuLevel())
                _hasTriggeredThisMatch = false;
        }

        private void Update()
        {
            if (_typeInitFailed) return;

            try
            {
                UpdateInternal();
            }
            catch (TypeInitializationException ex)
            {
                _typeInitFailed = true;
                enabled = false;
                var detail = ex.InnerException?.Message ?? ex.Message;
                LoggingService.LogError($"[PsychoticBreak] Disabled after init failure: {detail}");
            }
        }

        private void UpdateInternal()
        {
            if (_episodeActive)
            {
                if (DreadConfig.CompatibilityMode.Value)
                {
                    EndEpisode();
                    return;
                }

                UpdateEpisode();
                PublishRuntimeStateThrottled(force: true);
                return;
            }

            if (SemiFunc.MenuLevel())
            {
                if (_episodeActive)
                    EndEpisode();
                else
                    CleanupOverlay();
                PublishRuntimeStateThrottled(force: true);
                return;
            }

            if (!_enabled || DreadConfig.CompatibilityMode.Value)
            {
                PublishRuntimeStateThrottled();
                return;
            }
            if (_oncePerMatch && _hasTriggeredThisMatch)
            {
                PublishRuntimeStateThrottled();
                return;
            }

            if (Time.time < _nextTriggerCheck)
            {
                PublishRuntimeStateThrottled();
                return;
            }
            _nextTriggerCheck = Time.time + 2f;

            UpdateThreatTimestamps();
            _cachedBlockReason = GetTriggerBlockReason() ?? "";

            if (!CanTrigger())
            {
                PublishRuntimeStateThrottled(force: true);
                return;
            }

            if (Random.value < _triggerChance)
                StartEpisode();

            PublishRuntimeStateThrottled(force: true);
        }

        private void PublishRuntimeStateThrottled(bool force = false)
        {
            if (!force && !_episodeActive && Time.time < _nextStatePublish)
                return;

            _nextStatePublish = Time.time + 0.5f;
            PublishRuntimeState();
        }

        private void PublishRuntimeState()
        {
            DreadRuntimeState.PsychoticBreakEnabled = _enabled && !DreadConfig.CompatibilityMode.Value;
            DreadRuntimeState.PsychoticBreakEpisodeActive = _episodeActive;
            DreadRuntimeState.PsychoticBreakEpisodeTimer = _episodeTimer;
            DreadRuntimeState.PsychoticBreakEpisodeDuration = _episodeDuration;
            var nextCheck = _nextTriggerCheck - Time.time;
            DreadRuntimeState.PsychoticBreakNextCheckIn = nextCheck < 0f ? 0f : nextCheck;
            DreadRuntimeState.PsychoticBreakThreatCount = _recentThreatTimestamps.Count;
            DreadRuntimeState.PsychoticBreakClipsLoaded = AreClipsLoaded();

            var blockReason = _episodeActive ? "episode active" : _cachedBlockReason;
            if (_episodeActive)
            {
                DreadRuntimeState.PsychoticBreakCanTrigger = false;
                DreadRuntimeState.PsychoticBreakBlockReason = blockReason;
                return;
            }

            DreadRuntimeState.PsychoticBreakBlockReason = blockReason;
            DreadRuntimeState.PsychoticBreakCanTrigger = string.IsNullOrEmpty(blockReason);
        }

        private bool AreClipsLoaded()
        {
            return _peakScreamClip != null && _distantScreamClip != null
                && _threatScreamClip != null && _footstepClip != null;
        }

        private string? GetTriggerBlockReason()
        {
            if (!_enabled)
                return "disabled";
            if (DreadConfig.CompatibilityMode.Value)
                return "compatibility mode";
            if (SemiFunc.MenuLevel())
                return "menu level";
            if (_oncePerMatch && _hasTriggeredThisMatch)
                return "once per match";
            if (!AreClipsLoaded())
                return "clips not loaded";

            var pc = PlayerController.instance;
            if ((object)pc == null)
                return "no player";
            if (!IsSolo(pc))
                return "not solo";
            if (_recentThreatTimestamps.Count == 0)
                return "no recent threat";
            if (IsAnyEnemyVisible(_cachedEnemies))
                return "visible enemy";
            if (!IsCrouching(pc))
                return "not crouching";

            return null;
        }

        private void UpdateThreatTimestamps()
        {
            var cam = _mainCam;
            if (cam == null) return;

            _recentThreatTimestamps.RemoveAll(t => Time.time - t > ThreatMemoryDuration);

            _cachedEnemies = EnemyScanCache.GetEnemies();
            foreach (var e in _cachedEnemies)
            {
                if (e == null) continue;
                float d = Vector3.Distance(cam.transform.position, e.transform.position);
                if (d < ThreatRange)
                    _recentThreatTimestamps.Add(Time.time);
            }
        }

        private bool CanTrigger()
        {
            var pc = PlayerController.instance;
            if ((object)pc == null) return false;

            if (!IsSolo(pc)) return false;
            if (_recentThreatTimestamps.Count == 0) return false;
            if (IsAnyEnemyVisible(_cachedEnemies)) return false;
            if (!IsCrouching(pc)) return false;
            if (!AreClipsLoaded()) return false;

            return true;
        }

        private static PlayerController[]? _cachedPlayers;
        private static float _nextPlayerRefresh;

        private static bool IsSolo(PlayerController pc)
        {
            if (Time.time >= _nextPlayerRefresh)
            {
                _nextPlayerRefresh = Time.time + 2f;
                _cachedPlayers = FindObjectsOfType<PlayerController>();
            }

            var players = _cachedPlayers;
            if (players == null)
                return true;

            foreach (var other in players)
            {
                if ((object)other == null || (object)other == (object)pc) continue;
                if (!IsPlayerAlive(other)) continue;
                if (Vector3.Distance(pc.transform.position, other.transform.position) < SoloRange)
                    return false;
            }
            return true;
        }

        private static bool IsPlayerAlive(PlayerController pc) =>
            PlayerControllerCompat.IsAlive(pc);

        private bool IsAnyEnemyVisible(EnemyHealth[]? enemies)
        {
            var cam = _mainCam;
            if (cam == null || enemies == null) return false;

            var origin = cam.transform.position;
            foreach (var e in enemies)
            {
                if (e == null) continue;
                var target = e.transform.position;
                if (Vector3.Distance(origin, target) < 1f) return true;

                if (Physics.Linecast(origin, target, out var hit, VisionBlockMask, QueryTriggerInteraction.Ignore))
                {
                    if (hit.collider != null && hit.collider.transform.IsChildOf(e.transform))
                        return true;
                    continue;
                }
                return true;
            }
            return false;
        }

        private static bool IsCrouching(PlayerController pc) =>
            PlayerControllerCompat.IsCrouching(pc);

        private void CleanupDistantScreamSource()
        {
            if (_distantScreamSource != null)
            {
                Destroy(_distantScreamSource.gameObject);
                _distantScreamSource = null;
            }
        }

        /// <summary>Debug server / MCP entry point to force-start an episode.</summary>
        public void ForceEpisodeForDebug()
        {
            if (_episodeActive)
                return;

            StartEpisode();
        }

        private void StartEpisode()
        {
            _episodeActive = true;
            _hasTriggeredThisMatch = true;
            _episodeTimer = 0f;
            _phantomSoundAccumulator = 0f;
            _tumbleMaintainTimer = 0f;
            _nextTriggerCheck = Time.time + 10f;
            _hasPlayedPeakScream = false;

            CaptureEpisodeLocation(PlayerController.instance);
            LockPlayerForEpisode(PlayerController.instance);

            CreateOverlay();
            PlayCirclingFootsteps();
            PlayDistantScream();

            LoggingService.LogInfo("[Dread] Psychotic Break triggered!");
        }

        private void UpdateEpisode()
        {
            _episodeTimer += Time.deltaTime;
            float raw = _episodeTimer;

            _tumbleMaintainTimer -= Time.deltaTime;
            if (_tumbleMaintainTimer <= 0f)
            {
                _tumbleMaintainTimer = 0.4f;
                MaintainPlayerFallenState(PlayerController.instance);
            }

            float p1 = _episodeDuration * 0.15f;
            float p2 = _episodeDuration * 0.50f;
            float p3 = _episodeDuration * 0.80f;

            if (raw < p1)
            {
                float alpha = Mathf.Lerp(0f, 0.85f, raw / p1);
                SetDarknessAlpha(alpha);
                SetVignetteAlpha(0f);
            }
            else if (raw < p2)
            {
                float progress = (raw - p1) / (p2 - p1);
                float flicker = Mathf.Sin(Time.time * Mathf.Lerp(10f, 30f, progress)) * 0.5f + 0.5f;
                float vignetteBase = Mathf.Lerp(0.1f, 0.6f, progress);
                SetVignetteAlpha(vignetteBase * flicker);
                SetDarknessAlpha(0.85f);

                if (_footstepSource != null)
                    _footstepSource.panStereo = Mathf.Lerp(-1f, 1f, progress);
            }
            else if (raw < p3)
            {
                if (!_hasPlayedPeakScream)
                {
                    _hasPlayedPeakScream = true;
                    PlayPeakScream();
                }

                float progress = (raw - p2) / (p3 - p2);
                float vignetteIntensity = Mathf.Lerp(0.5f, 0.9f, progress);
                float flicker = Mathf.Sin(Time.time * 35f) * 0.3f + 0.7f;
                SetVignetteAlpha(vignetteIntensity * flicker);
                SetDarknessAlpha(0.85f);

                if (_footstepSource != null)
                {
                    _footstepSource.panStereo = Mathf.Sin(Time.time * 4f) * 0.8f;
                    _footstepSource.volume = Mathf.Lerp(0.5f, 1f, progress);
                }

                _phantomSoundAccumulator += Time.deltaTime * 0.3f;
                if (_phantomSoundAccumulator >= 1f)
                {
                    _phantomSoundAccumulator = 0f;
                    SpawnPhantomSound();
                }
            }
            else
            {
                float progress = (raw - p3) / (_episodeDuration - p3);
                if (progress < 0.5f)
                {
                    float peak = Mathf.Lerp(0.9f, 1f, progress * 2f);
                    SetVignetteAlpha(0.95f * peak);
                    SetDarknessAlpha(0.9f * peak);
                }
                else
                {
                    SetDarknessAlpha(0f);
                    SetVignetteAlpha(0f);
                }

                if (_footstepSource != null)
                {
                    if (progress < 0.5f)
                    {
                        _footstepSource.volume = Mathf.Lerp(1f, 0f, progress * 2f);
                        _footstepSource.panStereo = Mathf.Sin(Time.time * 8f) * 0.9f;
                    }
                    else
                    {
                        _footstepSource.Stop();
                    }
                }

                if (raw >= _episodeDuration - 0.5f)
                    EndEpisode();
            }
        }

        private void EndEpisode()
        {
            LoggingService.LogVerbose("[PsychoticBreak] Episode ending...");
            _episodeActive = false;
            SetDarknessAlpha(0f);
            SetVignetteAlpha(0f);

            var pc = PlayerController.instance;
            RestorePlayerControl(pc);
            if ((object)pc != null)
                StartCoroutine(DoStumble());

            CleanupFootstepSource();
            CleanupDistantScreamSource();
            CleanupOverlay();
            AlertMonstersToEpisodeLocation();
            LoggingService.LogInfo("[Dread] Psychotic Break ended.");
        }

        private void CaptureEpisodeLocation(PlayerController? pc)
        {
            _episodeLocationCaptured = false;
            if ((object)pc == null)
                return;

            _episodeWorldPosition = pc.transform.position;
            _episodeLocationCaptured = true;
        }

        private void AlertMonstersToEpisodeLocation()
        {
            if (!_episodeLocationCaptured || DreadConfig.CompatibilityMode.Value)
                return;

            EnemyDirectorCompat.AlertAllEnemiesToPoint(_episodeWorldPosition);
            _episodeLocationCaptured = false;
        }

        private IEnumerator DoStumble()
        {
            var cam = _mainCam;
            if (cam == null) yield break;

            var originalRot = cam.transform.localEulerAngles;
            var originalPos = cam.transform.localPosition;
            float duration = 0.5f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                float t = elapsed / duration;
                float roll = Mathf.Lerp(15f, 0f, t);
                float dip = Mathf.Lerp(-0.3f, 0f, t);
                cam.transform.localEulerAngles = new Vector3(originalRot.x, originalRot.y, originalRot.z + roll);
                cam.transform.localPosition = originalPos + new Vector3(0f, dip, 0f);
                elapsed += Time.deltaTime;
                yield return null;
            }

            cam.transform.localEulerAngles = originalRot;
            cam.transform.localPosition = originalPos;
        }

        private void CreateOverlay()
        {
            if (_overlayCanvas != null)
                return;

            _useImguiOverlay = false;
            _darknessAlpha = 0f;
            _vignetteAlpha = 0f;

            var rawImageType = ResolveRawImageType();
            if (rawImageType == null)
            {
                EnableImguiOverlayFallback("UnityEngine.UI.RawImage not available");
                return;
            }

            try
            {
                var go = new GameObject("DreadPsychoticBreakOverlay");
                DontDestroyOnLoad(go);

                _overlayCanvas = go.AddComponent<Canvas>();
                _overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                _overlayCanvas.sortingOrder = 999;

                var darkGo = new GameObject("Darkness");
                darkGo.transform.SetParent(go.transform, false);
                _darknessImage = AddRuntimeComponent(darkGo, rawImageType);
                if (_darknessImage == null)
                    throw new InvalidOperationException("Failed to add darkness RawImage");
                SetRawImageColor(_darknessImage, new Color(0, 0, 0, 0));
                StretchToParent(_darknessImage);

                var vigGo = new GameObject("Vignette");
                vigGo.transform.SetParent(go.transform, false);
                _vignetteImage = AddRuntimeComponent(vigGo, rawImageType);
                if (_vignetteImage == null)
                    throw new InvalidOperationException("Failed to add vignette RawImage");
                SetRawImageColor(_vignetteImage, new Color(0, 0, 0, 0));
                StretchToParent(_vignetteImage);

                _vignetteTexture = OverlayTextureUtil.CreateVignette(256);
                if (_vignetteTexture != null)
                    SetRawImageTexture(_vignetteImage, _vignetteTexture);
                else
                    LoggingService.LogWarning("[PsychoticBreak] Vignette texture skipped (no supported format on this GPU)");

                LoggingService.LogInfo("[PsychoticBreak] Canvas overlay created");
            }
            catch (Exception ex)
            {
                CleanupOverlay();
                EnableImguiOverlayFallback(ex.Message);
            }
        }

        private void EnableImguiOverlayFallback(string reason)
        {
            _useImguiOverlay = true;
            _overlayCanvas = null;
            _darknessImage = null;
            _vignetteImage = null;
            EnsureImguiOverlayTextures();
            LoggingService.LogWarning($"[PsychoticBreak] Using IMGUI overlay fallback: {reason}");
        }

        private void EnsureImguiOverlayTextures()
        {
            _imguiSolidTexture ??= OverlayTextureUtil.CreateSolid(Color.black);
            _imguiVignetteTexture ??= OverlayTextureUtil.CreateVignette(256);
            if (_imguiSolidTexture != null && _imguiBoxStyle == null)
            {
                _imguiBoxStyle = new GUIStyle(GUI.skin.box)
                {
                    border = new RectOffset(0, 0, 0, 0),
                    padding = new RectOffset(0, 0, 0, 0)
                };
                _imguiBoxStyle.normal.background = _imguiSolidTexture;
            }
        }

        private void OnGUI()
        {
            if (!_episodeActive || !_useImguiOverlay)
                return;

            if (ConfigUiDetector.IsConfigurationManagerOpen())
                return;

            try
            {
                DrawImguiOverlay();
            }
            catch (Exception ex)
            {
                LoggingService.LogVerbose($"[PsychoticBreak] IMGUI overlay frame skipped: {ex.Message}");
            }
        }

        private void DrawImguiOverlay()
        {
            if (_imguiSolidTexture == null)
                EnsureImguiOverlayTextures();
            if (_imguiBoxStyle == null)
                return;

            var screen = new Rect(0f, 0f, Screen.width, Screen.height);

            if (_darknessAlpha > 0.001f)
            {
                var prev = GUI.color;
                GUI.color = new Color(1f, 1f, 1f, _darknessAlpha);
                GUI.Box(screen, ImGuiContentCache.Empty, _imguiBoxStyle);
                GUI.color = prev;
            }

            if (_vignetteAlpha > 0.001f && _imguiVignetteTexture != null)
            {
                var prev = GUI.color;
                GUI.color = new Color(1f, 1f, 1f, _vignetteAlpha);
                GUI.DrawTexture(screen, _imguiVignetteTexture);
                GUI.color = prev;
            }
        }

        private static Type? ResolveRawImageType()
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (!string.Equals(asm.GetName().Name, "UnityEngine.UI", StringComparison.Ordinal))
                    continue;
                return asm.GetType("UnityEngine.UI.RawImage");
            }

            return Type.GetType("UnityEngine.UI.RawImage, UnityEngine.UI");
        }

        private static Component AddRuntimeComponent(GameObject go, Type componentType)
        {
            return go.AddComponent(componentType);
        }

        private static void StretchToParent(Component rawImage)
        {
            var rectTransformType = rawImage.GetType().Assembly.GetType("UnityEngine.RectTransform")
                ?? Type.GetType("UnityEngine.RectTransform, UnityEngine.CoreModule");
            if (rectTransformType == null) return;

            var rectTransform = rawImage.GetComponent(rectTransformType);
            if (rectTransform == null) return;

            var anchorMin = rectTransformType.GetProperty("anchorMin");
            var anchorMax = rectTransformType.GetProperty("anchorMax");
            var sizeDelta = rectTransformType.GetProperty("sizeDelta");
            anchorMin?.SetValue(rectTransform, Vector2.zero, null);
            anchorMax?.SetValue(rectTransform, Vector2.one, null);
            sizeDelta?.SetValue(rectTransform, Vector2.zero, null);

            var offsetMin = rectTransformType.GetProperty("offsetMin");
            var offsetMax = rectTransformType.GetProperty("offsetMax");
            offsetMin?.SetValue(rectTransform, Vector2.zero, null);
            offsetMax?.SetValue(rectTransform, Vector2.zero, null);
        }

        private static void SetRawImageColor(Component rawImage, Color color)
        {
            var colorProp = rawImage.GetType().GetProperty("color");
            colorProp?.SetValue(rawImage, color, null);
        }

        private static void SetRawImageTexture(Component rawImage, Texture2D texture)
        {
            var textureProp = rawImage.GetType().GetProperty("texture");
            textureProp?.SetValue(rawImage, texture, null);
        }

        private void CleanupOverlay()
        {
            _useImguiOverlay = false;
            _darknessAlpha = 0f;
            _vignetteAlpha = 0f;

            if (_overlayCanvas != null)
            {
                Destroy(_overlayCanvas.gameObject);
                _overlayCanvas = null;
            }
            _darknessImage = null;
            _vignetteImage = null;

            if (_vignetteTexture != null)
            {
                Destroy(_vignetteTexture);
                _vignetteTexture = null;
            }

            if (_imguiSolidTexture != null)
            {
                Destroy(_imguiSolidTexture);
                _imguiSolidTexture = null;
            }

            if (_imguiVignetteTexture != null)
            {
                Destroy(_imguiVignetteTexture);
                _imguiVignetteTexture = null;
            }

            _imguiBoxStyle = null;
            DestroyOrphanOverlayCanvas();
        }

        private static void DestroyOrphanOverlayCanvas()
        {
            var canvases = UnityEngine.Object.FindObjectsOfType<Canvas>();
            if (canvases == null)
                return;

            foreach (var canvas in canvases)
            {
                if (canvas == null || canvas.sortingOrder != 999)
                    continue;

                var go = canvas.gameObject;
                if (go == null)
                    continue;

                try
                {
                    var name = Traverse.Create(go).Property<string>("name").Value;
                    if (name == "DreadPsychoticBreakOverlay")
                        UnityEngine.Object.Destroy(go);
                }
                catch { }
            }
        }

        private void SetDarknessAlpha(float alpha)
        {
            _darknessAlpha = Mathf.Clamp01(alpha);
            if (_darknessImage != null)
                SetRawImageColor(_darknessImage, new Color(0, 0, 0, _darknessAlpha));
        }

        private void SetVignetteAlpha(float alpha)
        {
            _vignetteAlpha = Mathf.Clamp01(alpha);
            if (_vignetteImage != null)
                SetRawImageColor(_vignetteImage, new Color(0, 0, 0, _vignetteAlpha));
        }

        private static void LockPlayerForEpisode(PlayerController? pc)
        {
            if ((object)pc == null)
                return;

            DisableFlashlight(pc);
            LockInput(pc);
            if (!PlayerTumbleCompat.ApplyForcedTumble(pc))
                LoggingService.LogWarning("[PsychoticBreak] Could not apply tumble; input lock only");
        }

        private static void MaintainPlayerFallenState(PlayerController? pc)
        {
            if ((object)pc == null)
                return;

            PlayerTumbleCompat.MaintainForcedTumble(pc);
            LockInput(pc);
        }

        private static void RestorePlayerControl(PlayerController? pc)
        {
            if ((object)pc == null)
            {
                PlayerTumbleCompat.ReleaseForcedTumble(null);
                return;
            }

            PlayerTumbleCompat.ReleaseForcedTumble(pc);
            UnlockInput(pc);
            RestoreFlashlight(pc);
        }

        private static void DisableFlashlight(PlayerController pc)
        {
            var light = pc.GetComponentInChildren<Light>();
            if (light != null && light.enabled)
            {
                light.enabled = false;
                var tracker = new GameObject("DreadFlashlightTracker");
                tracker.transform.SetParent(pc.transform);
                var stored = tracker.AddComponent<FlashlightStateTracker>();
                stored.Flashlight = light;
            }
        }

        private static void RestoreFlashlight(PlayerController pc)
        {
            var tracker = pc.GetComponentInChildren<FlashlightStateTracker>();
            if (tracker != null && tracker.Flashlight != null)
                tracker.Flashlight.enabled = true;
            Destroy(tracker?.gameObject);
        }

        private static void LockInput(PlayerController pc)
        {
            bool any = false;
            try { Traverse.Create(pc).Field<bool>("inputLocked").Value = true; any = true; }
            catch { }
            try { Traverse.Create(pc).Field<bool>("interactDisabled").Value = true; any = true; }
            catch { }
            try { Traverse.Create(pc).Field<bool>("InputLocked").Value = true; any = true; }
            catch { }
            try { Traverse.Create(pc).Field<bool>("InteractDisabled").Value = true; any = true; }
            catch { }
            if (!any)
                LoggingService.LogWarning("[Dread] LockInput: no matching field found on PlayerController");
        }

        private static void UnlockInput(PlayerController pc)
        {
            bool any = false;
            try { Traverse.Create(pc).Field<bool>("inputLocked").Value = false; any = true; }
            catch { }
            try { Traverse.Create(pc).Field<bool>("interactDisabled").Value = false; any = true; }
            catch { }
            try { Traverse.Create(pc).Field<bool>("InputLocked").Value = false; any = true; }
            catch { }
            try { Traverse.Create(pc).Field<bool>("InteractDisabled").Value = false; any = true; }
            catch { }
            if (!any)
                LoggingService.LogWarning("[Dread] UnlockInput: no matching field found on PlayerController");
        }

        private IEnumerator LoadAudioClips()
        {
            while (!_sceneLoaded || SemiFunc.MenuLevel()) yield return null;
            var audioDir = Path.Combine(
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!,
                "audio");

            var clipDefs = new (string file, string label, System.Action<AudioClip?> setter)[]
            {
                ("scream_peak.ogg",     "PeakScream",     c => _peakScreamClip = c),
                ("scream_distant.ogg",  "DistantScream",   c => _distantScreamClip = c),
                ("scream_threat.ogg",   "ThreatScream",    c => _threatScreamClip = c),
                ("footsteps.ogg", "Footsteps",     c => _footstepClip = c),
            };

            foreach (var (file, label, setter) in clipDefs)
            {
                var path = Path.GetFullPath(Path.Combine(audioDir, file));
                if (!File.Exists(path))
                {
                    LoggingService.LogWarning($"[PsychoticBreak] Missing audio: {path}");
                    continue;
                }

                if (AudioClipLoader.TryLoadWithNvorbis(path, file, out var clip))
                {
                    setter(clip);
                    LoggingService.LogInfo($"[PsychoticBreak] Loaded {label}: {file}");
                    continue;
                }

                using var req = UnityWebRequestMultimedia.GetAudioClip(
                    AudioClipLoader.ToFileUri(path), AudioType.OGGVORBIS);
                yield return req.SendWebRequest();

                if (req.result == UnityWebRequest.Result.Success)
                {
                    var webClip = DownloadHandlerAudioClip.GetContent(req);
                    webClip.name = file;
                    setter(webClip);
                    LoggingService.LogInfo($"[PsychoticBreak] Loaded {label}: {file}");
                }
                else
                {
                    var handlerError = AudioClipLoader.GetRequestHandlerError(req);
                    var errorMsg = !string.IsNullOrEmpty(req.error)
                        ? req.error
                        : !string.IsNullOrEmpty(handlerError)
                            ? handlerError
                            : "no error details";
                    LoggingService.LogWarning($"[PsychoticBreak] Failed {label}: {errorMsg}");
                }
            }
        }

        private void PlayCirclingFootsteps()
        {
            if (_footstepClip == null) return;

            var go = new GameObject("DreadPsychoticFootsteps");
            DontDestroyOnLoad(go);
            _footstepSource = go.AddComponent<AudioSource>();
            _footstepSource.clip = _footstepClip;
            _footstepSource.loop = true;
            _footstepSource.spatialBlend = 0f;
            _footstepSource.volume = 0.3f;
            _footstepSource.panStereo = -1f;
            _footstepSource.Play();
        }

        private void PlayDistantScream()
        {
            if (_distantScreamClip == null) return;

            var go = new GameObject("DreadDistantScream");
            DontDestroyOnLoad(go);
            _distantScreamSource = go.AddComponent<AudioSource>();
            _distantScreamSource.clip = _distantScreamClip;
            _distantScreamSource.loop = false;
            _distantScreamSource.spatialBlend = 0f;
            _distantScreamSource.volume = 0.35f;
            _distantScreamSource.Play();
        }

        private void PlayPeakScream()
        {
            if (_peakScreamClip == null) return;

            var cam = _mainCam;
            if (cam == null) return;

            var go = new GameObject("DreadPeakScream");
            DontDestroyOnLoad(go);
            var src = go.AddComponent<AudioSource>();
            src.clip = _peakScreamClip;
            src.spatialBlend = 0f;
            src.volume = 0.9f;
            src.Play();
            Destroy(go, _peakScreamClip.length + 1f);
        }

        private void SpawnPhantomSound()
        {
            if (_threatScreamClip == null) return;

            var cam = _mainCam;
            if (cam == null) return;

            var clip = _threatScreamClip;

            var offset = Random.insideUnitSphere * Random.Range(5f, 15f);
            var pos = cam.transform.position + offset;

            var host = new GameObject("DreadPhantomSound");
            host.transform.position = pos;
            var src = host.AddComponent<AudioSource>();
            src.clip = clip;
            src.pitch = Random.Range(0.5f, 1.5f);
            src.spatialBlend = 1f;
            src.volume = Random.Range(0.4f, 0.8f);
            src.rolloffMode = AudioRolloffMode.Linear;
            src.minDistance = 1f;
            src.maxDistance = 25f;
            src.Play();

            Destroy(host, clip.length + 1f);
        }
    }

}
