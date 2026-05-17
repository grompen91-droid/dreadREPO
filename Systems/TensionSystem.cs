using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Dread.Config;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

namespace Dread.Systems
{
    public class TensionSystem : MonoBehaviour
    {
        private bool _inLevel;
        private Camera? _mainCam;
        private AudioSource? _breathSource;
        private AudioClip? _footstepClip;
        private readonly List<AudioClip> _breathClips = new();

        // Proximity scan shared by adrenaline
        private float _nextScan;
        private float _nearestDist = float.MaxValue;

        // Adrenaline state
        private float _originalDrain = -1f;

        // Low stamina state
        private bool _lowStaminaTriggered;
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

            _breathSource = gameObject.AddComponent<AudioSource>();
            _breathSource.spatialBlend = 0f;
            _breathSource.loop = false;
            _breathSource.playOnAwake = false;

            StartCoroutine(LoadFootstepClip());
            StartCoroutine(LoadBreathClips());
            StartCoroutine(FakeFootstepLoop());
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            RestoreDrain();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            _inLevel = !scene.name.Contains("Menu") && !scene.name.Contains("Main");
            _mainCam = null;
            _originalDrain = -1f;
            _panicActive = false;
            _panicTimer = 0f;
            _panicCooldown = 0f;
            _originalSprintMultiplier = -1f;
            _wasSprinting = false;
        }

        private void Update()
        {
            if (Time.time >= _nextScan)
            {
                _nextScan = Time.time + 0.5f;
                _nearestDist = _inLevel ? FindNearestEnemyDist() : float.MaxValue;
            }

            _breathCooldown -= Time.deltaTime;
            UpdateAdrenaline();
            UpdateLowStamina();
            UpdatePanicSprint();
        }

        // ── Adrenaline ────────────────────────────────────────────────────────

        private void UpdateAdrenaline()
        {
            if (!DreadConfig.AdrenalineEnabled.Value) return;

            var pc = PlayerController.instance;
            if (pc == null) return;

            if (_originalDrain < 0f)
                _originalDrain = pc.EnergySprintDrain;

            float targetDrain = _nearestDist < ProximityRange
                ? _originalDrain * Mathf.Lerp(0.30f, 1f, _nearestDist / ProximityRange)
                : _originalDrain;

            pc.EnergySprintDrain = Mathf.Lerp(pc.EnergySprintDrain, targetDrain, Time.deltaTime * 1.2f);
        }

        private void RestoreDrain()
        {
            if (_originalDrain >= 0f && PlayerController.instance != null)
                PlayerController.instance.EnergySprintDrain = _originalDrain;
        }

        // ── Low Stamina ───────────────────────────────────────────────────────

        private void UpdateLowStamina()
        {
            if (!DreadConfig.LowStaminaSoundEnabled.Value || !_inLevel || _breathSource == null || _breathClips.Count == 0) return;

            var pc = PlayerController.instance;
            if (pc == null || pc.EnergyStart <= 0f) return;

            float pct = pc.EnergyCurrent / pc.EnergyStart;

            if (!_lowStaminaTriggered && pct < 0.10f && _breathCooldown <= 0f)
            {
                _lowStaminaTriggered = true;
                _breathCooldown = 60f;

                var clip = _breathClips[Random.Range(0, _breathClips.Count)];
                _breathSource.clip = clip;
                _breathSource.pitch = Random.Range(0.88f, 1.15f);
                _breathSource.volume = 0.6f;
                _breathSource.Play();
            }
            else if (_lowStaminaTriggered && pct > 0.25f)
            {
                _lowStaminaTriggered = false;
            }
        }

        // ── Panic Sprint ──────────────────────────────────────────────────────

        private void UpdatePanicSprint()
        {
            if (!DreadConfig.PanicSprintEnabled.Value) return;

            var pc = PlayerController.instance;
            if (pc == null) return;

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
                        pc.SprintSpeedMultiplier = _originalSprintMultiplier;
                        _originalSprintMultiplier = -1f;
                    }
                }
            }
            else if (!_wasSprinting && currentlySprinting && _nearestDist < ProximityRange && _panicCooldown <= 0f)
            {
                _originalSprintMultiplier = pc.SprintSpeedMultiplier;
                pc.SprintSpeedMultiplier *= 1.25f;
                _panicActive = true;
                _panicTimer = 2f;
            }

            _wasSprinting = currentlySprinting;
        }

        // ── Shared ────────────────────────────────────────────────────────────

        private float FindNearestEnemyDist()
        {
            if (_mainCam == null) _mainCam = Camera.main;
            if (_mainCam == null) return float.MaxValue;

            var enemies = FindObjectsOfType<EnemyHealth>();
            float nearest = float.MaxValue;
            foreach (var e in enemies)
            {
                if (e == null) continue;
                float d = Vector3.Distance(_mainCam.transform.position, e.transform.position);
                if (d < nearest) nearest = d;
            }
            return nearest;
        }

        // ── Fake Footsteps ────────────────────────────────────────────────────

        // Loads breathing.ogg, breath2.ogg, breath3.ogg — skips any that don't exist.
        // Drop extra files into the audio/ folder to add more variety.
        private IEnumerator LoadBreathClips()
        {
            var audioDir = Path.Combine(
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!,
                "audio");

            var path = Path.Combine(audioDir, "breathing.ogg");
            if (!File.Exists(path)) yield break;

            using var req = UnityWebRequestMultimedia.GetAudioClip(
                "file:///" + path.Replace('\\', '/'), AudioType.OGGVORBIS);
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
                _breathClips.Add(DownloadHandlerAudioClip.GetContent(req));
        }

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
                yield return new WaitForSeconds(Random.Range(120f, 240f));

                if (!DreadConfig.FakeFootstepsEnabled.Value || !_inLevel || _footstepClip == null)
                    continue;

                if (Random.value > 0.35f) continue;

                if (_mainCam == null) _mainCam = Camera.main;
                if (_mainCam == null) continue;

                SpawnFakeFootstep();
            }
        }

        private void SpawnFakeFootstep()
        {
            var cam = _mainCam!;
            var behind = -cam.transform.forward;
            var side = cam.transform.right * Random.Range(-0.8f, 0.8f);
            var pos = cam.transform.position + (behind + side).normalized * Random.Range(2.5f, 5f);
            pos.y -= 1.5f;

            var host = new GameObject("DreadFakeStep");
            host.transform.position = pos;
            var src = host.AddComponent<AudioSource>();
            src.clip = _footstepClip;
            src.spatialBlend = 1f;
            src.volume = 0.55f;
            src.rolloffMode = AudioRolloffMode.Linear;
            src.minDistance = 0.5f;
            src.maxDistance = 8f;
            src.Play();

            Destroy(host, _footstepClip!.length + 0.5f);
        }
    }
}
