using System;
using System.Collections;
using Dread.Config;
using Dread.Systems.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

namespace Dread.Systems
{
    public partial class PsychoticBreakSystem : MonoBehaviour
    {
        private bool _enabled;
        private float _episodeDuration;
        private bool _oncePerMatch;

        private float _checkIntervalSeconds = 8f;
        private float _perRollProbability = 0.003f;
        private float _losLostDelaySeconds = 3f;
        private PsychoticBreakTriggerTuning _tuning;

        private bool _episodeActive;
        private bool _hasTriggeredThisMatch;
        private float _nextTriggerCheck;
        private float _episodeTimer;
        private Camera? _mainCam;

        private float _threatMemoryUntil;
        private const float ThreatMemoryDuration = 30f;
        internal const float ThreatRange = 15f;
        internal const float SoloRange = 30f;

        private AudioClip? _peakScreamClip;
        private AudioClip? _distantScreamClip;
        private AudioClip? _threatScreamClip;
        private AudioClip? _footstepClip;

        private GameObject? _overlayRoot;
        private Component? _darknessImage;
        private Component? _vignetteImage;
        private Texture2D? _vignetteTexture;

        private EnemyHealth[]? _cachedEnemies;
        private float _tumbleMaintainTimer;
        private bool _sceneLoaded;
        private bool _typeInitFailed;
        private string _lastLoggedBlockReason = "";
        private bool _cachedAnyEnemyVisible;
        private float _nextVisibilityRefresh;

        internal const int VisionBlockMask = -1;
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
            DreadConfig.PsychoticBreakChancePercent.SettingChanged += OnConfigChanged;
            DreadConfig.PsychoticBreakDuration.SettingChanged += OnConfigChanged;
            DreadConfig.PsychoticBreakOncePerMatch.SettingChanged += OnConfigChanged;
            DreadConfig.PsychoticBreakAccentEnabled.SettingChanged += OnConfigChanged;

            StartCoroutine(LoadAudioClips());
        }

        private void RefreshConfig()
        {
            _enabled = DreadConfig.PsychoticBreakEnabled.Value;
            _episodeDuration = DreadConfig.PsychoticBreakDuration.Value;
            _oncePerMatch = DreadConfig.PsychoticBreakOncePerMatch.Value;
            _tuning = PsychoticBreakTriggerTuning.Compute(DreadConfig.PsychoticBreakChancePercent.Value);
            _checkIntervalSeconds = _tuning.CheckIntervalSeconds;
            _perRollProbability = _tuning.PerRollProbability;
            _losLostDelaySeconds = _tuning.LosLostDelaySeconds;
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
            DreadConfig.PsychoticBreakChancePercent.SettingChanged -= OnConfigChanged;
            DreadConfig.PsychoticBreakDuration.SettingChanged -= OnConfigChanged;
            DreadConfig.PsychoticBreakOncePerMatch.SettingChanged -= OnConfigChanged;
            DreadConfig.PsychoticBreakAccentEnabled.SettingChanged -= OnConfigChanged;
            DreadConfig.CompatibilityMode.SettingChanged -= OnConfigChanged;

            _peakScreamClip = null;
            _distantScreamClip = null;
            _threatScreamClip = null;
            _footstepClip = null;

            DestroyActiveHallucination();
            CleanupOverlay();
            CleanupFootstepSource();
            CleanupDistantScreamSource();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            _sceneLoaded = true;
            if (_episodeActive)
                RestorePlayerControl(PlayerController.instance);

            PsychoticBreakEpisodeProtection.SetActive(false);
            _episodeActive = false;
            _threatMemoryUntil = 0f;
            _sawEnemyWhileThreatActive = false;
            _losLostEligibleAt = 0f;
            _wasEnemyVisibleLastRefresh = false;
            _hidingSince = -1f;
            _cachedEnemies = null;
            _mainCam = Camera.main;
            ProximityScan.Invalidate();
            DestroyActiveHallucination();
            CleanupOverlay();
            CleanupFootstepSource();
            CleanupDistantScreamSource();
            if (GameplayContext.IsMenuLevel())
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
                if (DreadFeaturePolicy.CompatibilityMode)
                {
                    EndEpisode();
                    return;
                }

                UpdateEpisode();
                PublishRuntimeState();
                return;
            }

            if (!_enabled || !DreadFeaturePolicy.PsychoticBreakEnabled || GameplayContext.IsMenuLevel())
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
            UpdateLosLostTracking();
            UpdateHidingTimestamp();

            if (Time.time < _nextTriggerCheck)
            {
                PublishRuntimeState();
                return;
            }
            _nextTriggerCheck = Time.time + _checkIntervalSeconds;

            if (!CanTrigger())
            {
                PublishRuntimeState();
                return;
            }

            if (Random.value < _perRollProbability)
                StartEpisode();

            PublishRuntimeState();
        }

        private void PublishRuntimeState()
        {
            DreadRuntimeState.PsychoticBreakEnabled = _enabled && DreadFeaturePolicy.PsychoticBreakEnabled;
            DreadRuntimeState.PsychoticBreakEpisodeActive = _episodeActive;
            DreadRuntimeState.PsychoticBreakEpisodeTimer = _episodeTimer;
            DreadRuntimeState.PsychoticBreakEpisodeDuration = _episodeDuration;
            var nextCheck = _nextTriggerCheck - Time.time;
            DreadRuntimeState.PsychoticBreakNextCheckIn = nextCheck < 0f ? 0f : nextCheck;
            DreadRuntimeState.PsychoticBreakThreatCount = GetThreatMemorySecondsRemaining();
            DreadRuntimeState.PsychoticBreakEnemyCount = ProximityScan.Count;
            DreadRuntimeState.PsychoticBreakClipsLoaded = AreClipsLoaded();
            DreadRuntimeState.PsychoticBreakLosLostIn = GetLosLostEligibleInSeconds();
            DreadRuntimeState.PsychoticBreakThreatEngaged = _sawEnemyWhileThreatActive;
            DreadRuntimeState.PsychoticBreakEnemyVisible = IsAnyEnemyVisibleCached();
            DreadRuntimeState.PsychoticBreakHallucinationStatus = GetHallucinationStatusForDebug();
            DreadRuntimeState.PsychoticBreakEpisodeProtected = PsychoticBreakEpisodeProtection.IsActive;
            DreadRuntimeState.PsychoticBreakCheckInterval = _checkIntervalSeconds;
            DreadRuntimeState.PsychoticBreakPerRollChance = _perRollProbability;
            DreadRuntimeState.PsychoticBreakEstimatedWindowChance = _tuning.EstimatedWindowChance;

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

        public void ForceEpisodeForDebug()
        {
            if (_episodeActive)
                return;
            if (GameplayContext.IsMenuLevel())
            {
                LoggingService.LogWarning("[PsychoticBreak] ForceEpisode ignored on menu level");
                return;
            }

            StartEpisode(countAsMatchTrigger: false, debugDamageProtection: true);
        }
    }
}
