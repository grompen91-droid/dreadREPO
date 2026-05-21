using System.Collections;
using System.Collections.Generic;
using Dread.Config;
using UnityEngine;

namespace Dread.Systems.Tension
{
    internal class LowStaminaFeature : MonoBehaviour
    {
        private AudioSource _breathSource = null!;
        private readonly List<AudioClip> _breathClips = new();
        private bool _wasSprintingForBreath;
        private float _breathCooldown;

        private static readonly string[] BreathCandidates = { "breathing.ogg", "breath2.ogg", "breath3.ogg" };

        private void Start()
        {
            _breathSource = gameObject.AddComponent<AudioSource>();
            _breathSource.spatialBlend = 0f;
            _breathSource.loop = false;
            _breathSource.playOnAwake = false;
            StartCoroutine(LoadBreathClips());
        }

        private IEnumerator LoadBreathClips()
        {
            foreach (var name in BreathCandidates)
            {
                yield return AudioLoader.Load(name, clip =>
                {
                    if (clip != null)
                    {
                        _breathClips.Add(clip);
                        Plugin.Logger.LogInfo($"[Dread] Breath clip loaded: {name}");
                    }
                });
            }
        }

        private void Update()
        {
            _breathCooldown -= Time.deltaTime;

            if (!DreadConfig.LowStaminaSoundEnabled.Value || SemiFunc.MenuLevel() || _breathClips.Count == 0) return;

            var pc = PlayerController.instance;
            if ((object)pc == null || pc.EnergyStart <= 0f) return;

            bool currentlySprinting = pc.sprinting;

            if (_wasSprintingForBreath && !currentlySprinting && pc.EnergyCurrent <= 5f && _breathCooldown <= 0f)
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
    }
}
