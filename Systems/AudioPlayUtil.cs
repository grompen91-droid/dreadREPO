using System;
using UnityEngine;

namespace Dread.Systems
{
    internal static class AudioPlayUtil
    {
        /// <summary>Wall-clock seconds until an AudioSource at <paramref name="pitch"/> finishes the clip.</summary>
        internal static float PlayLifetimeSeconds(AudioClip clip, float pitch, float paddingSeconds = 0.5f) =>
            clip.length / Math.Max(pitch, 0.01f) + paddingSeconds;
    }
}
