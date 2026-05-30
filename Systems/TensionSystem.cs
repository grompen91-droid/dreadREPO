using System.Collections;
using System.Collections.Generic;
using Dread.Config;
using Dread.Systems.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Dread.Systems
{
    public class TensionSystem : MonoBehaviour
    {
        private AudioSource? _breathSource;
        private readonly List<AudioClip> _breathClips = new();
        private AudioClip? _footstepClip;

        private float _nearestDist = float.MaxValue;
        private Camera? _mainCam;

        // Adrenaline state
        private float _originalDrain = -1f;

        // Low stamina state
        private bool _wasSprintingForBreath;
        private float _breathCooldown;

        // Panic sprint state
        private bool _wasSprinting;
        private float _panicTimer;
        private float _panicCooldown;
        private float _originalSprintMultiplier = -1f;

        private const float ProximityRange = 15f;

        private void Start()
        {
            LoggingService.LogVerbose("[Tension] Awake starting...");
            SceneManager.sceneLoaded += OnSceneLoaded;
            _mainCam = Camera.main;

            _breathSource = gameObject.AddComponent<AudioSource>();
            _breathSource.spatialBlend = 0f;
            _breathSource.loop = false;
            _breathSource.playOnAwake = false;

            StartCoroutine(LoadBreathClips());
            StartCoroutine(LoadFootstepClip());
            StartCoroutine(FakeFootstepLoop());
        }

        private void OnDestroy()
        {
            StopAllCoroutines();
            SceneManager.sceneLoaded -= OnSceneLoaded;
            RestoreDrain();
            RestoreSprintMultiplier();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            RestoreDrain();
            RestoreSprintMultiplier();
            AudioClipLoader.ClearCache();
            _mainCam = Camera.main;
            _originalDrain = -1f;
            _panicTimer = 0f;
            _panicCooldown = 0f;
            _originalSprintMultiplier = -1f;
            _wasSprinting = false;
            _wasSprintingForBreath = false;
            _breathCooldown = 0f;
        }

        private void Update()
        {
            _nearestDist = GameplayContext.IsMenuLevel()
                ? float.MaxValue
                : ProximityScan.NearestDistance(GetScanOrigin());

            UpdateAdrenaline();
            UpdateLowStamina();
            UpdatePanicSprint();
            PublishRuntimeState();
        }

        private void PublishRuntimeState()
        {
            DreadRuntimeState.NearestEnemyDist = _nearestDist;

            var pc = PlayerController.instance;
            bool adrenaline = false;
            if ((object)pc != null && _originalDrain >= 0f
                && DreadFeaturePolicy.AdrenalineEnabled
                && !GameplayContext.IsMenuLevel()
                && _nearestDist < ProximityRange
                && PlayerControllerCompat.TryReadEnergySprintDrain(pc, out var currentDrain))
            {
                adrenaline = currentDrain < _originalDrain * 0.95f;
            }

            DreadRuntimeState.AdrenalineActive = adrenaline;
            DreadRuntimeState.PanicSprintActive = _panicTimer > 0f;
            DreadRuntimeState.PanicSprintCooldown = _panicCooldown < 0f ? 0f : _panicCooldown;
        }

        // ── Adrenaline ────────────────────────────────────────────────────────

        private void UpdateAdrenaline()
        {
            if (!DreadFeaturePolicy.AdrenalineEnabled || GameplayContext.IsMenuLevel())
            {
                RestoreDrain();
                return;
            }

            var pc = PlayerController.instance;
            if ((object)pc == null) return;

            if (_originalDrain < 0f && !PlayerControllerCompat.TryReadEnergySprintDrain(pc, out _originalDrain))
                return;

            float targetDrain = _nearestDist < ProximityRange
                ? _originalDrain * Mathf.Lerp(0.30f, 1f, _nearestDist / ProximityRange)
                : _originalDrain;

            if (PlayerControllerCompat.TryReadEnergySprintDrain(pc, out var current))
            {
                var next = Mathf.MoveTowards(current, targetDrain, 0.5f * Time.deltaTime);
                PlayerControllerCompat.TrySetEnergySprintDrain(pc, next);
            }
        }

        private void RestoreDrain()
        {
            if (_originalDrain >= 0f && (object)PlayerController.instance != null)
                PlayerControllerCompat.TrySetEnergySprintDrain(PlayerController.instance, _originalDrain);
        }

        private void RestoreSprintMultiplier()
        {
            if (_originalSprintMultiplier >= 0f && (object)PlayerController.instance != null)
            {
                PlayerControllerCompat.TrySetSprintMultiplier(PlayerController.instance, _originalSprintMultiplier);
                _originalSprintMultiplier = -1f;
            }
        }

        // ── Low Stamina ───────────────────────────────────────────────────────

        private void UpdateLowStamina()
        {
            if (!DreadFeaturePolicy.LowStaminaSoundEnabled || GameplayContext.IsMenuLevel())
            {
                _breathCooldown = 0f;
                return;
            }

            _breathCooldown -= Time.deltaTime;

            if (_breathSource == null || _breathClips.Count == 0) return;

            var pc = PlayerController.instance;
            if ((object)pc == null || pc.EnergyStart <= 0f) return;

            bool currentlySprinting = pc.sprinting;

            var lowEnergy = pc.EnergyCurrent <= pc.EnergyStart * 0.1f;
            if (_wasSprintingForBreath && !currentlySprinting && lowEnergy && _breathCooldown <= 0f)
            {
                _breathCooldown = 60f;

                var clip = _breathClips[Random.Range(0, _breathClips.Count)];
                _breathSource.clip = clip;
                _breathSource.pitch = Random.Range(0.88f, 1.15f);
                _breathSource.volume = 1.0f;
                _breathSource.Play();
            }

            _wasSprintingForBreath = currentlySprinting;
        }

        // ── Panic Sprint ──────────────────────────────────────────────────────

        private void UpdatePanicSprint()
        {
            if (!DreadFeaturePolicy.PanicSprintEnabled || GameplayContext.IsMenuLevel())
            {
                if (_originalSprintMultiplier >= 0f)
                    RestoreSprintMultiplier();
                return;
            }

            var pc = PlayerController.instance;
            if ((object)pc == null) return;

            _panicCooldown -= Time.deltaTime;

            bool currentlySprinting = pc.sprinting;

            if (_originalSprintMultiplier >= 0f)
            {
                _panicTimer -= Time.deltaTime;
                if (_panicTimer <= 0f)
                {
                    _panicCooldown = 20f;
                    if (_originalSprintMultiplier >= 0f)
                    {
                        PlayerControllerCompat.TrySetSprintMultiplier(pc, _originalSprintMultiplier);
                        _originalSprintMultiplier = -1f;
                    }
                }
            }
            else if (!_wasSprinting && currentlySprinting && _nearestDist < ProximityRange && _panicCooldown <= 0f
                     && PlayerControllerCompat.TryReadSprintMultiplier(pc, out var currentMult))
            {
                _originalSprintMultiplier = currentMult;
                PlayerControllerCompat.TrySetSprintMultiplier(pc, _originalSprintMultiplier * 1.25f);
                _panicTimer = 2f;
            }

            _wasSprinting = currentlySprinting;
        }

        // ── Shared ────────────────────────────────────────────────────────────

        private Vector3 GetScanOrigin()
        {
            if (_mainCam == null)
                _mainCam = Camera.main;
            return _mainCam != null ? _mainCam.transform.position : Vector3.zero;
        }

        // ── Breath Clips ──────────────────────────────────────────────────────

        private static readonly string[] BreathCandidates = { "breathing.ogg", "breath2.ogg", "breath3.ogg" };

        private IEnumerator LoadBreathClips()
        {
            yield return AudioClipLoader.LoadClips(BreathCandidates, (name, clip) =>
            {
                if (clip != null) _breathClips.Add(clip);
            });
        }

        // ── Fake Footsteps ────────────────────────────────────────────────────

        private IEnumerator LoadFootstepClip()
        {
            AudioClip? clip = null;
            yield return AudioClipLoader.LoadClip("footsteps.ogg", c => clip = c);
            if (clip != null) _footstepClip = clip;
        }

        private IEnumerator FakeFootstepLoop()
        {
            yield return new WaitForSeconds(45f);

            while (true)
            {
                if (!DreadFeaturePolicy.FakeFootstepsEnabled || GameplayContext.IsMenuLevel()
                    || _footstepClip == null || (object)PlayerController.instance == null)
                {
                    yield return new WaitForSeconds(1f);
                    continue;
                }

                var cam = _mainCam;
                if (cam == null)
                {
                    yield return new WaitForSeconds(1f);
                    continue;
                }

                LoggingService.LogVerbose("[Tension] Checking fake footsteps...");
                if (Random.value <= 0.35f)
                    SpawnFakeFootstep(cam);

                yield return new WaitForSeconds(Random.Range(60f, 90f));
            }
        }

        private void SpawnFakeFootstep(Camera cam)
        {
            var behind = -cam.transform.forward;
            var side = cam.transform.right * Random.Range(-0.8f, 0.8f);
            var pos = cam.transform.position + (behind + side).normalized * Random.Range(2.5f, 5f);
            pos.y -= 1.5f;

            var pitch = Random.Range(0.5f, 1.5f);
            SpatialAudio3D.PlayAt(
                pos,
                _footstepClip!,
                new SpatialAudio3D.PlayOptions
                {
                    Volume = 0.55f,
                    MinDistance = 0.5f,
                    MaxDistance = 8f,
                    Pitch = pitch,
                    HostName = "DreadFakeStep",
                });
        }
    }
}
