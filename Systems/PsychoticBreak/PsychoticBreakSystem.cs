using System;
using System.Collections;
using Dread.Config;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

namespace Dread.Systems
{
    public partial class PsychoticBreakSystem : MonoBehaviour
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
        internal const float ThreatRange = 15f;
        internal const float SoloRange = 30f;

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
    }
}
