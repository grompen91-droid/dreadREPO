using System.Collections;
using System.Collections.Generic;
using Dread.Config;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Dread.Systems
{
    public class TensionSystem : MonoBehaviour
    {
        private AudioSource? _breathSource;
        private readonly List<AudioClip> _breathClips = new();
        private AudioClip? _footstepClip;

        // Proximity scan shared by adrenaline and panic sprint
        private float _nextScan;
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
            if (Time.time >= _nextScan)
            {
                _nextScan = Time.time + 0.5f;
                _nearestDist = SemiFunc.MenuLevel() ? float.MaxValue : FindNearestEnemyDist();
            }

            UpdateAdrenaline();
            UpdateLowStamina();
            UpdatePanicSprint();
        }

        // ── Adrenaline ────────────────────────────────────────────────────────

        private void UpdateAdrenaline()
        {
            LoggingService.LogVerbose("[Tension] Checking adrenaline...");
            if (!DreadConfig.AdrenalineEnabled.Value || SemiFunc.MenuLevel())
            {
                RestoreDrain();
                return;
            }

            var pc = PlayerController.instance;
            if ((object)pc == null) return;

            if (_originalDrain < 0f)
                _originalDrain = pc.EnergySprintDrain;

            float targetDrain = _nearestDist < ProximityRange
                ? _originalDrain * Mathf.Lerp(0.30f, 1f, _nearestDist / ProximityRange)
                : _originalDrain;

            pc.EnergySprintDrain = Mathf.MoveTowards(pc.EnergySprintDrain, targetDrain, 0.5f * Time.deltaTime);
        }

        private void RestoreDrain()
        {
            if (_originalDrain >= 0f && (object)PlayerController.instance != null)
                PlayerController.instance.EnergySprintDrain = _originalDrain;
        }

        private void RestoreSprintMultiplier()
        {
            if (_originalSprintMultiplier >= 0f && (object)PlayerController.instance != null)
            {
                var sprintField = Traverse.Create(PlayerController.instance).Field<float>("SprintSpeedMultiplier");
                sprintField.Value = _originalSprintMultiplier;
                _originalSprintMultiplier = -1f;
            }
        }

        // ── Low Stamina ───────────────────────────────────────────────────────

        private void UpdateLowStamina()
        {
            LoggingService.LogVerbose("[Tension] Checking low stamina...");
            if (!DreadConfig.LowStaminaSoundEnabled.Value || SemiFunc.MenuLevel())
            {
                _breathCooldown = 0f;
                return;
            }

            _breathCooldown -= Time.deltaTime;

            if (_breathSource == null || _breathClips.Count == 0) return;

            var pc = PlayerController.instance;
            if ((object)pc == null || pc.EnergyStart <= 0f) return;

            bool currentlySprinting = pc.sprinting;

            // Trigger each time player stops sprinting because energy ran out
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
            LoggingService.LogVerbose("[Tension] Checking panic sprint...");
            if (!DreadConfig.PanicSprintEnabled.Value || SemiFunc.MenuLevel())
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
                        Traverse.Create(pc).Field<float>("SprintSpeedMultiplier").Value = _originalSprintMultiplier;
                        _originalSprintMultiplier = -1f;
                    }
                }
            }
            else if (!_wasSprinting && currentlySprinting && _nearestDist < ProximityRange && _panicCooldown <= 0f)
            {
                var tpc = Traverse.Create(pc);
                _originalSprintMultiplier = tpc.Field<float>("SprintSpeedMultiplier").Value;
                tpc.Field<float>("SprintSpeedMultiplier").Value = _originalSprintMultiplier * 1.25f;
                _panicTimer = 2f;
            }

            _wasSprinting = currentlySprinting;
        }

        // ── Shared ────────────────────────────────────────────────────────────

        private float FindNearestEnemyDist()
        {
            if (_mainCam == null) _mainCam = Camera.main;
            var cam = _mainCam;
            if (cam == null) return float.MaxValue;

            float nearest = float.MaxValue;
            foreach (var e in FindObjectsOfType<EnemyHealth>())
            {
                if (e == null) continue;
                float d = Vector3.Distance(cam.transform.position, e.transform.position);
                if (d < nearest) nearest = d;
            }
            return nearest;
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
            while (true)
            {
                if (!DreadConfig.FakeFootstepsEnabled.Value || SemiFunc.MenuLevel() || _footstepClip == null)
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

            var host = new GameObject("DreadFakeStep");
            host.transform.position = pos;
            var src = host.AddComponent<AudioSource>();
            src.clip = _footstepClip;
            src.pitch = Random.Range(0.5f, 1.5f);
            src.spatialBlend = 1f;
            src.volume = 0.55f;
            src.rolloffMode = AudioRolloffMode.Linear;
            src.minDistance = 0.5f;
            src.maxDistance = 8f;
            src.Play();

            if (_footstepClip != null)
                Destroy(host, _footstepClip.length + 0.5f);
            else
                Destroy(host, 0.5f);
        }
    }
}
