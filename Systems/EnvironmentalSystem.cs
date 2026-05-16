using System.Collections;
using System.Collections.Generic;
using Dread.Config;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Dread.Systems
{
    public class EnvironmentalSystem : MonoBehaviour
    {
        private readonly List<Rigidbody> _props = new();
        private readonly List<Light> _toggleableLights = new();
        private bool _inLevel;

        private void Start()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            StartCoroutine(NudgeLoop());
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            _inLevel = !scene.name.Contains("Menu") && !scene.name.Contains("Main");
            _props.Clear();
            _toggleableLights.Clear();

            if (!_inLevel || !DreadConfig.EnvironmentalEnabled.Value) return;

            foreach (var rb in FindObjectsOfType<Rigidbody>())
            {
                if (rb.mass < 5f && rb.gameObject.layer != LayerMask.NameToLayer("Player"))
                    _props.Add(rb);
            }

            foreach (var light in FindObjectsOfType<Light>())
            {
                if (light.type == LightType.Point && light.intensity > 0.1f)
                    _toggleableLights.Add(light);
            }
        }

        private IEnumerator NudgeLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(Random.Range(12f, 30f));

                if (!DreadConfig.EnvironmentalEnabled.Value || !_inLevel)
                    continue;

                _props.RemoveAll(rb => rb == null);
                if (_props.Count > 0)
                {
                    var target = _props[Random.Range(0, _props.Count)];
                    NudgeProp(target);
                }

                if (Random.value < DreadConfig.RareEventChance.Value)
                    StartCoroutine(RareEvent());
            }
        }

        private static void NudgeProp(Rigidbody rb)
        {
            var nudge = new Vector3(
                Random.Range(-0.12f, 0.12f),
                0f,
                Random.Range(-0.12f, 0.12f));
            rb.MovePosition(rb.position + nudge);
        }

        private IEnumerator RareEvent()
        {
            _toggleableLights.RemoveAll(l => l == null);
            if (_toggleableLights.Count == 0) yield break;

            var light = _toggleableLights[Random.Range(0, _toggleableLights.Count)];
            var originalIntensity = light.intensity;

            light.intensity = 0f;
            yield return new WaitForSeconds(Random.Range(2f, 4f));
            if (light != null)
                light.intensity = originalIntensity;
        }
    }
}
