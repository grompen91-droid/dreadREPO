using UnityEngine;

namespace Dread.Systems.Core
{
    /// <summary>
    /// Spawns a temporary 3D AudioSource at a world position with linear rolloff and scheduled destroy.
    /// </summary>
    internal static class SpatialAudio3D
    {
        internal struct PlayOptions
        {
            public float Volume;
            public float MinDistance;
            public float MaxDistance;
            public float Pitch;
            public float PaddingSeconds;
            public string HostName;

            public static PlayOptions Default => new PlayOptions
            {
                Volume = 1f,
                MinDistance = 1f,
                MaxDistance = 25f,
                Pitch = 1f,
                PaddingSeconds = 0.5f,
                HostName = "DreadSpatialAudio",
            };
        }

        public static GameObject? PlayAt(Vector3 position, AudioClip clip, PlayOptions options)
        {
            if (clip == null)
                return null;

            var hostName = string.IsNullOrEmpty(options.HostName) ? "DreadSpatialAudio" : options.HostName;
            var host = new GameObject(hostName);
            host.transform.position = position;
            var src = host.AddComponent<AudioSource>();
            src.clip = clip;
            src.pitch = options.Pitch;
            src.spatialBlend = 1f;
            src.volume = options.Volume;
            src.rolloffMode = AudioRolloffMode.Linear;
            src.minDistance = options.MinDistance;
            src.maxDistance = options.MaxDistance;
            src.Play();

            Object.Destroy(
                host,
                AudioPlayUtil.PlayLifetimeSeconds(clip, options.Pitch, options.PaddingSeconds));
            return host;
        }
    }
}
