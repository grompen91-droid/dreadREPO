using System.Collections;
using System;
using System.Reflection;
using Dread.Config;
using HarmonyLib;
using UnityEngine;
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
        private float _nextTriggerCheck;
        private float _episodeTimer;
        private Camera? _mainCam;

        private float _threatMemoryUntil;
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

        private GameObject? _overlayRoot;
        private Component? _darknessImage;
        private Component? _vignetteImage;
        private Texture2D? _vignetteTexture;

        private EnemyHealth[]? _cachedEnemies;
        private float _phantomSoundAccumulator;
        private float _tumbleMaintainTimer;
        private bool _sceneLoaded;
        private bool _typeInitFailed;
        private string _lastLoggedBlockReason = "";
        private bool _cachedAnyEnemyVisible;
        private float _nextVisibilityRefresh;

        // -1 = all layers; avoids stub-only Physics.DefaultRaycastLayers at type init
        private const int VisionBlockMask = -1;
        private const float VisibilityRefreshInterval = 0.25f;

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
            _sceneLoaded = true;
            if (_episodeActive)
                RestorePlayerControl(PlayerController.instance);

            _episodeActive = false;
            _threatMemoryUntil = 0f;
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
                PublishRuntimeState();
                return;
            }

            if (!_enabled || DreadConfig.CompatibilityMode.Value || SemiFunc.MenuLevel())
            {
                PublishRuntimeState();
                return;
            }
            if (_oncePerMatch && _hasTriggeredThisMatch)
            {
                PublishRuntimeState();
                return;
            }

            UpdateThreatTimestamps();

            if (Time.time < _nextTriggerCheck)
            {
                PublishRuntimeState();
                return;
            }
            _nextTriggerCheck = Time.time + 2f;

            if (!CanTrigger())
            {
                PublishRuntimeState();
                return;
            }

            if (Random.value < _triggerChance)
                StartEpisode();

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
            DreadRuntimeState.PsychoticBreakThreatCount = GetThreatMemorySecondsRemaining();
            DreadRuntimeState.PsychoticBreakEnemyCount = EnemyScanCache.Count;
            DreadRuntimeState.PsychoticBreakClipsLoaded = AreClipsLoaded();

            if (_episodeActive)
            {
                DreadRuntimeState.PsychoticBreakCanTrigger = false;
                DreadRuntimeState.PsychoticBreakBlockReason = "episode active";
                return;
            }

            var blockReason = GetTriggerBlockReason();
            DreadRuntimeState.PsychoticBreakBlockReason = blockReason ?? "";
            DreadRuntimeState.PsychoticBreakCanTrigger = blockReason == null;

            var reasonKey = blockReason ?? "ready";
            if (reasonKey != _lastLoggedBlockReason)
            {
                _lastLoggedBlockReason = reasonKey;
                if (blockReason != null)
                    LoggingService.LogInfo($"[PsychoticBreak] Blocked: {blockReason}");
            }
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
            if (!HasRecentThreat())
                return "no recent threat";
            if (IsAnyEnemyVisibleCached())
                return "visible enemy";
            if (!IsHidingVulnerable(pc))
                return "not hiding";

            return null;
        }

        private void UpdateThreatTimestamps()
        {
            if (_mainCam == null)
                _mainCam = Camera.main;

            var origin = GetThreatScanOrigin();
            if (!origin.HasValue)
                return;

            _cachedEnemies = EnemyScanCache.GetEnemies();
            foreach (var e in _cachedEnemies)
            {
                if (!EnemyHealthCompat.IsValid(e))
                    continue;

                float d = Vector3.Distance(origin.Value, EnemyScanCache.GetFocusPosition(e));
                if (d < ThreatRange)
                {
                    _threatMemoryUntil = Time.time + ThreatMemoryDuration;
                    return;
                }
            }
        }

        private bool HasRecentThreat() => Time.time < _threatMemoryUntil;

        private int GetThreatMemorySecondsRemaining()
        {
            if (!HasRecentThreat())
                return 0;
            return (int)Math.Ceiling(_threatMemoryUntil - Time.time);
        }

        private Vector3? GetThreatScanOrigin()
        {
            var pc = PlayerController.instance;
            if ((object)pc != null)
            {
                try
                {
                    return pc.transform.position;
                }
                catch { }
            }

            if (_mainCam == null)
                _mainCam = Camera.main;
            return _mainCam != null ? _mainCam.transform.position : null;
        }

        private bool CanTrigger()
        {
            LoggingService.LogVerbose("[PsychoticBreak] Checking trigger condition...");
            var pc = PlayerController.instance;
            if ((object)pc == null) return false;

            if (!IsSolo(pc)) return false;
            if (!HasRecentThreat()) return false;
            if (IsAnyEnemyVisibleCached()) return false;
            if (!IsHidingVulnerable(pc)) return false;
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

        private bool IsAnyEnemyVisibleCached()
        {
            if (Time.time < _nextVisibilityRefresh)
                return _cachedAnyEnemyVisible;

            _nextVisibilityRefresh = Time.time + VisibilityRefreshInterval;
            _cachedAnyEnemyVisible = IsAnyEnemyVisible(_cachedEnemies);
            return _cachedAnyEnemyVisible;
        }

        private bool IsAnyEnemyVisible(EnemyHealth[]? enemies)
        {
            var cam = _mainCam;
            if (cam == null || enemies == null) return false;

            Vector3 origin;
            try
            {
                origin = cam.transform.position;
            }
            catch
            {
                return false;
            }

            for (int i = 0; i < enemies.Length; i++)
            {
                var e = enemies[i];
                if (!EnemyHealthCompat.IsValid(e))
                    continue;

                try
                {
                    if (!EnemyHealthCompat.IsAliveForVisibility(e))
                        continue;

                    var target = EnemyScanCache.GetFocusPosition(e);

                    if (Vector3.Distance(origin, target) < 0.75f)
                        return true;

                    if (Physics.Linecast(origin, target, out var hit, VisionBlockMask, QueryTriggerInteraction.Ignore))
                    {
                        if (hit.collider != null && hit.collider.transform.IsChildOf(e.transform))
                            return true;
                        continue;
                    }

                    return true;
                }
                catch
                {
                    continue;
                }
            }

            return false;
        }

        private static bool IsHidingVulnerable(PlayerController pc) =>
            PlayerControllerCompat.IsHidingVulnerable(pc);

        private void CleanupDistantScreamSource()
        {
            if (_distantScreamSource != null)
            {
                Destroy(_distantScreamSource.gameObject);
                _distantScreamSource = null;
            }
        }

        /// <summary>
        /// Debug server / MCP only (loopback, <c>DebugServerEnabled</c>).
        /// Skips trigger guards; does not consume once-per-match.
        /// </summary>
        public void ForceEpisodeForDebug()
        {
            if (_episodeActive)
                return;
            if (SemiFunc.MenuLevel())
            {
                LoggingService.LogWarning("[PsychoticBreak] ForceEpisode ignored on menu level");
                return;
            }

            StartEpisode(countAsMatchTrigger: false);
        }

        private void StartEpisode(bool countAsMatchTrigger = true)
        {
            _episodeActive = true;
            if (countAsMatchTrigger)
                _hasTriggeredThisMatch = true;
            _episodeTimer = 0f;
            _phantomSoundAccumulator = 0f;
            _tumbleMaintainTimer = 0f;
            _nextTriggerCheck = Time.time + 10f;
            _hasPlayedPeakScream = false;

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

            if (raw >= _episodeDuration)
            {
                EndEpisode();
                return;
            }

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
            LoggingService.LogInfo("[Dread] Psychotic Break ended.");
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
            if (_overlayRoot != null) return;

            var rawImageType = ResolveRawImageType();
            if (rawImageType == null)
            {
                LoggingService.LogError("[PsychoticBreak] UnityEngine.UI.RawImage not available");
                return;
            }

            var canvasType = ResolveCanvasType();
            if (canvasType == null)
            {
                LoggingService.LogError("[PsychoticBreak] UnityEngine.Canvas not available");
                return;
            }

            var go = new GameObject("DreadPsychoticBreakOverlay");
            DontDestroyOnLoad(go);
            _overlayRoot = go;

            var canvas = AddRuntimeComponent(go, canvasType);
            ConfigureCanvas(canvas);

            var darkGo = new GameObject("Darkness");
            darkGo.transform.SetParent(go.transform, false);
            _darknessImage = AddRuntimeComponent(darkGo, rawImageType);
            SetRawImageColor(_darknessImage, new Color(0, 0, 0, 0));
            StretchToParent(_darknessImage);

            var vigGo = new GameObject("Vignette");
            vigGo.transform.SetParent(go.transform, false);
            _vignetteImage = AddRuntimeComponent(vigGo, rawImageType);
            SetRawImageColor(_vignetteImage, new Color(0, 0, 0, 0));
            StretchToParent(_vignetteImage);

            _vignetteTexture = OverlayTextureUtil.CreateVignette(256);
            if (_vignetteTexture == null)
            {
                LoggingService.LogWarning("[PsychoticBreak] Vignette texture unavailable; overlay may be incomplete");
                return;
            }

            SetRawImageTexture(_vignetteImage, _vignetteTexture);
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

        private static Type? ResolveCanvasType()
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                var name = asm.GetName().Name;
                if (name is not ("UnityEngine.UIModule" or "UnityEngine"))
                    continue;
                var canvas = asm.GetType("UnityEngine.Canvas");
                if (canvas != null)
                    return canvas;
            }

            return Type.GetType("UnityEngine.Canvas, UnityEngine.UIModule")
                ?? Type.GetType("UnityEngine.Canvas, UnityEngine");
        }

        private static void ConfigureCanvas(Component canvas)
        {
            var renderModeProp = canvas.GetType().GetProperty("renderMode");
            renderModeProp?.SetValue(canvas, RenderMode.ScreenSpaceOverlay, null);

            var sortingOrderProp = canvas.GetType().GetProperty("sortingOrder");
            sortingOrderProp?.SetValue(canvas, 999, null);
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
            if (_overlayRoot != null)
            {
                Destroy(_overlayRoot);
                _overlayRoot = null;
            }
            _darknessImage = null;
            _vignetteImage = null;

            if (_vignetteTexture != null)
            {
                Destroy(_vignetteTexture);
                _vignetteTexture = null;
            }
        }

        private void SetDarknessAlpha(float alpha)
        {
            if (_darknessImage != null)
                SetRawImageColor(_darknessImage, new Color(0, 0, 0, Mathf.Clamp01(alpha)));
        }

        private void SetVignetteAlpha(float alpha)
        {
            if (_vignetteImage != null)
                SetRawImageColor(_vignetteImage, new Color(0, 0, 0, Mathf.Clamp01(alpha)));
        }

        private static void LockPlayerForEpisode(PlayerController? pc)
        {
            if ((object)pc == null)
                return;

            DisableFlashlight(pc);
            LockInput(pc);
            if (!PlayerTumbleCompat.ApplyForcedTumble(pc))
                LoggingService.LogVerbose("[PsychoticBreak] Tumble lock unavailable; input lock only");
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
            PlayerTumbleCompat.ReleaseForcedTumble(pc);
            if ((object)pc == null)
                return;

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

            var files = new[]
            {
                "scream_peak.ogg",
                "scream_distant.ogg",
                "scream_threat.ogg",
                "footsteps.ogg",
            };

            yield return AudioClipLoader.LoadClips(files, (name, clip) =>
            {
                switch (name)
                {
                    case "scream_peak.ogg":
                        _peakScreamClip = clip;
                        break;
                    case "scream_distant.ogg":
                        _distantScreamClip = clip;
                        break;
                    case "scream_threat.ogg":
                        _threatScreamClip = clip;
                        break;
                    case "footsteps.ogg":
                        _footstepClip = clip;
                        break;
                }

                if (clip != null)
                    LoggingService.LogInfo($"[PsychoticBreak] Loaded {name}");
                else
                    LoggingService.LogWarning($"[PsychoticBreak] Missing or failed: {name}");
            });
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
