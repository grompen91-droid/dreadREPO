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
        private const float PoiReissueInterval = 30f;
        private const float PoiRadius = 60f;
        private const float ArmDelaySeconds = 2f;

        private AudioClip? _bangClip;

        private bool _armed;
        private float _armCountdown = ArmDelaySeconds;

        private bool _triggered;
        private Vector3 _triggerPos;
        private float _poiRemaining;
        private float _nextReissue;
        private SnitchItemMarker? _activeMarker;

        private void Start()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
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
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => ResetState();

        private void Update()
        {
            if (!DreadConfig.SnitchEnabled.Value || DreadConfig.CompatibilityMode.Value)
                return;
            if (!GameplayContext.IsRun())
                return;
            if (!HarmonyPatchCompat.IsMasterClient())
                return;

            if (!_armed)
            {
                _armCountdown -= Time.deltaTime;
                if (_armCountdown <= 0f)
                {
                    _armed = true;
                    ArmSnitch();
                }
                return;
            }

            if (_triggered && _poiRemaining > 0f)
            {
                _poiRemaining = Mathf.Max(0f, _poiRemaining - Time.deltaTime);
                DreadRuntimeState.SnitchPoiRemaining = _poiRemaining;

                if (_poiRemaining > 0f && Time.time >= _nextReissue)
                {
                    _nextReissue = Time.time + PoiReissueInterval;
                    EnemyLureCompat.Pull(_triggerPos, PoiRadius);
                }
            }
        }

        private void ArmSnitch()
        {
            var items = ItemRosterCompat.GetItemGameObjects();
            if (items.Count == 0)
            {
                LoggingService.LogVerbose("[Snitch] No items found this run; skipping");
                DreadRuntimeState.SnitchState = "disarmed";
                return;
            }

            var chosen = items[UnityEngine.Random.Range(0, items.Count)];
            var marker = chosen.AddComponent<SnitchItemMarker>();
            marker.System = this;
            _activeMarker = marker;

            DreadRuntimeState.SnitchState = "armed";
            LoggingService.LogVerbose($"[Snitch] Armed on {chosen.name} (id {chosen.GetInstanceID()})");
        }

        internal void OnSnitchTriggered(Vector3 position)
        {
            if (_triggered)
                return;

            _triggered = true;
            _triggerPos = position;
            _poiRemaining = DreadConfig.SnitchPOIDurationSeconds.Value;
            _nextReissue = Time.time; // pull immediately on trigger

            DreadRuntimeState.SnitchState = "triggered";
            DreadRuntimeState.SnitchPoiRemaining = _poiRemaining;

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

            _armed = false;
            _armCountdown = ArmDelaySeconds;
            _triggered = false;
            _poiRemaining = 0f;
            _nextReissue = 0f;
            DreadRuntimeState.SnitchState = "disarmed";
            DreadRuntimeState.SnitchPoiRemaining = 0f;
        }
    }

    /// <summary>
    /// Polls every 0.25 s to detect when its item was picked up, then
    /// calls back to <see cref="SnitchSystem.OnSnitchTriggered"/>.
    /// Three signals: Rigidbody.isKinematic, transform.parent != null,
    /// or position delta > 0.5 m from spawn.
    /// </summary>
    internal class SnitchItemMarker : MonoBehaviour
    {
        internal SnitchSystem? System;

        private Vector3 _spawnPos;
        private Rigidbody? _rb;
        private bool _triggered;

        private void Start()
        {
            _spawnPos = transform.position;
            _rb = GetComponent<Rigidbody>();
            StartCoroutine(PollPickup());
        }

        private void OnDestroy()
        {
            StopAllCoroutines();
        }

        private IEnumerator PollPickup()
        {
            var wait = new WaitForSeconds(0.25f);
            while (!_triggered)
            {
                yield return wait;
                if (IsPickedUp())
                    Trigger();
            }
        }

        private bool IsPickedUp()
        {
            try
            {
                if (_rb != null && _rb.isKinematic)
                    return true;
                if (transform.parent != null)
                    return true;
                if ((transform.position - _spawnPos).sqrMagnitude > 0.25f) // (0.5m)^2
                    return true;
            }
            catch
            {
                // reflection failure or Unity stub quirk — treat as not picked up
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
