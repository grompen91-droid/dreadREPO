using System;
using System.Collections;
using Dread.Config;
using Dread.Systems.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Dread.Systems
{
    /// <summary>
    /// Arms one random item per run as the "snitch". The first player to
    /// pick it up triggers a loud 3D bang and marks the position as a
    /// persistent enemy POI for SnitchPOIDurationSeconds. Host only.
    /// Silent in normal play; surfaces overlay state and a toast when the
    /// debug overlay is enabled.
    /// </summary>
    public class SnitchSystem : MonoBehaviour
    {
        private static SnitchSystem? _instance;

        private const float PoiReissueInterval = 30f;
        private const float PoiRadius = 60f;
        private const float ArmDelaySeconds = 5f;
        private const float ArmRetrySeconds = 5f;
        private const int MaxArmRetries = 6;

        private AudioClip? _bangClip;

        private bool _armed;
        private bool _armFailed;
        private float _armCountdown = ArmDelaySeconds;
        private int _armRetries;

        private bool _triggered;
        private Vector3 _triggerPos;
        private float _poiRemaining;
        private float _nextReissue;
        private SnitchItemMarker? _activeMarker;

        private string _lastLoggedBlockReason = "";

        private void OnEnable() => _instance = this;

        private void OnDisable()
        {
            if (_instance == this)
                _instance = null;
        }

        internal static void NotifyLevelGenDone() => _instance?.OnLevelGenComplete();

        private void Start()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            DreadConfig.SnitchEnabled.SettingChanged += OnConfigChanged;
            DreadConfig.CompatibilityMode.SettingChanged += OnConfigChanged;

            StartCoroutine(AudioClipLoader.LoadClip("snitch_bang.ogg", clip =>
            {
                _bangClip = clip;
                if (clip == null)
                    LoggingService.LogWarning("[Snitch] snitch_bang.ogg not found — bang audio will be silent");
            }));
        }

        private void OnDestroy()
        {
            StopAllCoroutines();
            SceneManager.sceneLoaded -= OnSceneLoaded;
            DreadConfig.SnitchEnabled.SettingChanged -= OnConfigChanged;
            DreadConfig.CompatibilityMode.SettingChanged -= OnConfigChanged;
        }

        private void OnConfigChanged(object? sender, EventArgs e) => _lastLoggedBlockReason = "";

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mode != LoadSceneMode.Single)
                return;

            GameplayPhaseCompat.ResetForSceneLoad();
            ResetState();
        }

        private void OnLevelGenComplete()
        {
            if (_armed || _triggered || _armFailed)
                return;

            _armCountdown = 0f;
            LoggingService.LogVerbose("[Snitch] Level gen done; arm attempt scheduled");
        }

        private void Update()
        {
            PublishRuntimeState();

            if (!DreadFeaturePolicy.SnitchEnabled)
                return;
            if (!GameplayContext.AllowsHostMonsterFeatures)
                return;
            if (!HarmonyPatchCompat.IsMasterClient())
                return;

            DreadRuntimeState.SnitchNextCheckIn = _armed || _armFailed ? -1f : _armCountdown;

            if (!_armed && !_armFailed)
            {
                _armCountdown -= Time.deltaTime;
                if (_armCountdown <= 0f)
                    TryArm();
                return;
            }

            if (_armed && !_triggered && _activeMarker != null)
            {
                try
                {
                    var cam = Camera.main;
                    if ((object)cam != null)
                    {
                        DreadRuntimeState.SnitchItemDistance = Vector3.Distance(
                            cam.transform.position,
                            _activeMarker.transform.position);
                    }
                }
                catch { }
            }

            if (_triggered && _poiRemaining > 0f)
            {
                _poiRemaining = Mathf.Max(0f, _poiRemaining - Time.deltaTime);
                DreadRuntimeState.SnitchPoiRemaining = _poiRemaining;

                if (_poiRemaining > 0f && Time.time >= _nextReissue)
                {
                    _nextReissue = Time.time + PoiReissueInterval;
                    LoggingService.LogVerbose("[Snitch] POI pull");
                    EnemyLureCompat.Pull(_triggerPos, PoiRadius);
                }
            }
        }

        private void PublishRuntimeState()
        {
            DreadRuntimeState.GameplayPhase = GameplayContext.PhaseLabel;
            DreadRuntimeState.SnitchEnabled = DreadFeaturePolicy.SnitchEnabled;

            if (_triggered)
            {
                DreadRuntimeState.SnitchState = "triggered";
                DreadRuntimeState.SnitchBlockReason = "";
                return;
            }

            if (_armed && _activeMarker != null)
            {
                DreadRuntimeState.SnitchState = "armed";
                DreadRuntimeState.SnitchBlockReason = "";
                return;
            }

            if (_armFailed)
            {
                DreadRuntimeState.SnitchState = "failed";
                DreadRuntimeState.SnitchBlockReason = "no items this run";
                return;
            }

            DreadRuntimeState.SnitchState = "disarmed";

            var blockReason = GetBlockReason();
            DreadRuntimeState.SnitchBlockReason = blockReason ?? "";

            if (blockReason == null && !_armed)
                DreadRuntimeState.SnitchBlockReason = $"arming in {_armCountdown:F0}s";

            var reasonKey = blockReason ?? (_armed ? "armed" : "active");
            if (reasonKey != _lastLoggedBlockReason)
            {
                _lastLoggedBlockReason = reasonKey;
                if (blockReason != null)
                    LoggingService.LogInfo($"[Snitch] Blocked: {blockReason}");
            }
        }

        private static string? GetBlockReason()
        {
            if (!DreadConfig.SnitchEnabled.Value)
                return "disabled in config";
            if (DreadConfig.CompatibilityMode.Value)
                return "compatibility mode";
            if (GameplayContext.IsMenuLevel())
                return "menu level";
            if (!GameplayContext.AllowsHostMonsterFeatures)
                return GameplayContext.PhaseLabel;
            if (!HarmonyPatchCompat.IsMasterClient())
                return "not host";
            return null;
        }

        private void TryArm()
        {
            var items = ItemRosterCompat.GetItemGameObjects();
            LoggingService.LogVerbose(
                $"[Snitch] Arm attempt {_armRetries + 1}: {items.Count} item(s) found");
            if (items.Count == 0)
            {
                _armRetries++;
                if (_armRetries > MaxArmRetries)
                {
                    _armFailed = true;
                    LoggingService.LogWarning("[Snitch] No items found after max retries; snitch disabled this run");
                    return;
                }

                _armCountdown = ArmRetrySeconds;
                LoggingService.LogVerbose(
                    $"[Snitch] No items yet (attempt {_armRetries}); retrying in {ArmRetrySeconds}s");
                return;
            }

            _armed = true;
            var chosen = items[UnityEngine.Random.Range(0, items.Count)];
            var marker = chosen.AddComponent<SnitchItemMarker>();
            marker.System = this;
            _activeMarker = marker;

            LoggingService.LogInfo($"[Snitch] Armed on {chosen.name} (id {chosen.GetInstanceID()})");
        }

        internal void OnSnitchTriggered(Vector3 position)
        {
            if (_triggered)
                return;

            _triggered = true;
            _triggerPos = position;
            _poiRemaining = DreadConfig.SnitchPOIDurationSeconds.Value;
            _nextReissue = Time.time;

            if (_bangClip != null)
            {
                SpatialAudio3D.PlayAt(position, _bangClip, new SpatialAudio3D.PlayOptions
                {
                    Volume = 1f,
                    MinDistance = 5f,
                    MaxDistance = 80f,
                    Pitch = 1f,
                    PaddingSeconds = 0.5f,
                    HostName = "DreadSnitchBang",
                });
            }

            if (DreadConfig.DebugOverlayEnabled.Value)
            {
                LoggingService.LogInfo("[Snitch] Triggered");
                DreadNotificationSystem.Bad("Snitch", "item betrayed a player");
            }
        }

        private void ResetState()
        {
            if (_activeMarker != null)
            {
                Destroy(_activeMarker);
                _activeMarker = null;
            }

            ItemRosterCompat.ResetForNewRun();

            _armed = false;
            _armFailed = false;
            _armCountdown = ArmDelaySeconds;
            _armRetries = 0;
            _lastLoggedBlockReason = "";
            _triggered = false;
            _poiRemaining = 0f;
            _nextReissue = 0f;
            DreadRuntimeState.SnitchState = "disarmed";
            DreadRuntimeState.SnitchPoiRemaining = 0f;
            DreadRuntimeState.SnitchItemDistance = -1f;
            DreadRuntimeState.SnitchNextCheckIn = -1f;
            DreadRuntimeState.SnitchBlockReason = "";
        }
    }

    /// <summary>
    /// Polls after a grace period to detect when its item was picked up, then
    /// calls back to <see cref="SnitchSystem.OnSnitchTriggered"/>.
    /// </summary>
    internal class SnitchItemMarker : MonoBehaviour
    {
        private const float PickupGraceSeconds = 2f;
        private const float PollIntervalSeconds = 0.25f;

        internal SnitchSystem? System;

        private Vector3 _spawnPos;
        private Transform? _baselineParent;
        private bool _baselineKinematic;
        private bool _baselineCaptured;
        private Rigidbody? _rb;
        private bool _triggered;

        private void Start()
        {
            _rb = GetComponent<Rigidbody>();
            StartCoroutine(PollPickup());
        }

        private void OnDestroy()
        {
            StopAllCoroutines();
        }

        private IEnumerator PollPickup()
        {
            yield return new WaitForSeconds(PickupGraceSeconds);
            CaptureBaseline();

            var wait = new WaitForSeconds(PollIntervalSeconds);
            while (!_triggered)
            {
                yield return wait;
                if (TryGetPickupReason(out _))
                    Trigger();
            }
        }

        private void CaptureBaseline()
        {
            _spawnPos = transform.position;
            _baselineParent = transform.parent;
            _baselineKinematic = _rb != null && _rb.isKinematic;
            _baselineCaptured = true;
        }

        private bool TryGetPickupReason(out string reason)
        {
            reason = "";
            if (!_baselineCaptured)
                return false;

            try
            {
                if (_rb != null && _rb.isKinematic && !_baselineKinematic)
                {
                    reason = "kinematic";
                    return true;
                }

                if ((transform.position - _spawnPos).sqrMagnitude > 0.25f)
                {
                    reason = "moved";
                    return true;
                }

                if (transform.parent != _baselineParent)
                {
                    reason = "reparented";
                    return true;
                }
            }
            catch
            {
                // treat as not picked up
            }

            return false;
        }

        private void Trigger()
        {
            _triggered = true;
            StopAllCoroutines();
            System?.OnSnitchTriggered(transform.position);
        }
    }
}
