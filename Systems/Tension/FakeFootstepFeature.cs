using System.Collections;
using Dread.Config;
using UnityEngine;

namespace Dread.Systems.Tension
{
    internal class FakeFootstepFeature : MonoBehaviour
    {
        private AudioClip? _footstepClip;

        private void Start() => StartCoroutine(Init());

        private IEnumerator Init()
        {
            yield return AudioLoader.Load("footsteps.ogg", clip => _footstepClip = clip);
            StartCoroutine(FakeFootstepLoop());
        }

        private IEnumerator FakeFootstepLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(Random.Range(120f, 240f));

                if (!DreadConfig.FakeFootstepsEnabled.Value || SemiFunc.MenuLevel() || _footstepClip == null)
                    continue;

                if (Random.value > 0.35f) continue;

                var cam = Camera.main;
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
