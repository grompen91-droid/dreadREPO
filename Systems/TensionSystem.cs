using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Dread.Config;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Networking;
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
        private bool _panicActive;
        private float _panicTimer;
        private float _panicCooldown;
        private float _originalSprintMultiplier = -1f;

        private const float ProximityRange = 15f;

        private void Start()
        {
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
            _mainCam = Camera.main;
            _originalDrain = -1f;
            _panicActive = false;
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
                Traverse.Create(PlayerController.instance).Field<float>("SprintSpeedMultiplier").Value = _originalSprintMultiplier;
                _originalSprintMultiplier = -1f;
                _panicActive = false;
            }
        }

        // ── Low Stamina ───────────────────────────────────────────────────────

        private void UpdateLowStamina()
        {
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
            if (_wasSprintingForBreath && !currentlySprinting && pc.EnergyCurrent <= pc.EnergyStart * 0.1f && _breathCooldown <= 0f)
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
            if (!DreadConfig.PanicSprintEnabled.Value || SemiFunc.MenuLevel())
            {
                if (_panicActive)
                    RestoreSprintMultiplier();
                return;
            }

            var pc = PlayerController.instance;
            if ((object)pc == null) return;

            _panicCooldown -= Time.deltaTime;

            bool currentlySprinting = pc.sprinting;

            if (_panicActive)
            {
                _panicTimer -= Time.deltaTime;
                if (_panicTimer <= 0f)
                {
                    _panicActive = false;
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
                _panicActive = true;
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
            var audioDir = Path.Combine(
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!,
                "audio");

            foreach (var name in BreathCandidates)
            {
                var path = Path.Combine(audioDir, name);
                if (!File.Exists(path)) continue;

                using var req = UnityWebRequestMultimedia.GetAudioClip(
                    "file:///" + path.Replace('\\', '/'), AudioType.OGGVORBIS);
                yield return req.SendWebRequest();

                if (req.result == UnityWebRequest.Result.Success)
                {
                    _breathClips.Add(DownloadHandlerAudioClip.GetContent(req));
                    Plugin.Logger.LogInfo($"[Dread] Breath clip loaded: {name}");
                }
                else
                {
                    Plugin.Logger.LogWarning($"[Dread] Breath clip failed {name}: {req.error}");
                }
            }
        }

        // ── Fake Footsteps ────────────────────────────────────────────────────

        private IEnumerator LoadFootstepClip()
        {
            var audioDir = Path.Combine(
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!,
                "audio");
            var path = Path.Combine(audioDir, "footsteps.ogg");
            if (!File.Exists(path)) yield break;

            using var req = UnityWebRequestMultimedia.GetAudioClip(
                "file:///" + path.Replace('\\', '/'), AudioType.OGGVORBIS);
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
                _footstepClip = DownloadHandlerAudioClip.GetContent(req);
        }

        private IEnumerator FakeFootstepLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(Random.Range(180f, 360f));

                if (!DreadConfig.FakeFootstepsEnabled.Value || SemiFunc.MenuLevel() || _footstepClip == null)
                    continue;

                if (Random.value > 0.2f) continue;

                var cam = _mainCam;
                if (cam == null) continue;

                SpawnFakeFootstep(cam);
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
