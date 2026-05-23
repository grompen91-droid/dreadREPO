using System;
using UnityEngine;

namespace UnityEngine.Networking
{
    public class UnityWebRequest : IDisposable
    {
        public UnityWebRequest() { }
        public UnityWebRequest(string url, string method) { }
        public UploadHandlerRaw uploadHandler { get; set; }
        public DownloadHandlerBuffer downloadHandler { get; set; }
        public string method { get; set; }
        public Result result { get; }
        public string error { get; }
        public UnityWebRequestAsyncOperation SendWebRequest() => null;
        public void SetRequestHeader(string name, string value) { }
        public void Dispose() { }
        public enum Result { Success }
    }
    public class UploadHandlerRaw
    {
        public UploadHandlerRaw(byte[] data) { }
    }
    public class DownloadHandlerBuffer
    {
        public string text { get; }
    }
    public class UnityWebRequestAsyncOperation : AsyncOperation { }
}
