using System;
using UnityEngine;
using UnityEngine.Networking;

namespace UnityEngine.Networking
{
    public class UnityWebRequestMultimedia
    {
        public static UnityWebRequest GetAudioClip(string url, AudioType audioType) => null;
    }
    public class DownloadHandlerAudioClip
    {
        public string error { get; set; }
        public static AudioClip GetContent(UnityWebRequest req) => null;
    }
}
