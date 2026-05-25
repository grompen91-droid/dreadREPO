using System;
using System.Collections;
using UnityEngine;

namespace UnityEngine
{
    public abstract class MonoBehaviour : Behaviour
    {
        public Coroutine StartCoroutine(IEnumerator routine) => null;
        public void StopAllCoroutines() { }
    }
    public class Behaviour : Component
    {
        public bool enabled { get; set; }
    }
    public class Component : Object
    {
        public T GetComponent<T>() where T : class => null;
        public Component GetComponent(Type type) => null;
        public T GetComponentInChildren<T>() where T : class => null;
        public GameObject gameObject { get; }
        public Transform transform { get; }
    }
    public class GameObject : Object
    {
        public GameObject() { }
        public GameObject(string name) { }
        public bool activeInHierarchy { get; }
        public T AddComponent<T>() where T : Component => null;
        public Component AddComponent(Type componentType) => null;
        public T[] GetComponentsInChildren<T>() where T : class => null;
        public Transform transform { get; }
    }
    public class Transform : Component
    {
        public Vector3 position { get; set; }
        public Vector3 forward { get; }
        public Vector3 right { get; }
        public Vector3 localPosition { get; set; }
        public Vector3 localEulerAngles { get; set; }
        public Transform parent { get; set; }
        public void SetParent(Transform parent) { }
        public void SetParent(Transform parent, bool worldPositionStays) { }
        public bool IsChildOf(Transform potentialParent)
        {
            var t = this;
            while (t != null)
            {
                if (t == potentialParent) return true;
                t = t.parent;
            }
            return false;
        }
    }
    public class Object
    {
        public static T FindObjectOfType<T>() where T : Object => null;
        public static T[] FindObjectsOfType<T>() where T : Object => null;
        public static void Destroy(Object obj, float t = 0f) { }
        public static void DontDestroyOnLoad(Object obj) { }
        public static implicit operator bool(Object exists) { return exists != null; }
    }
    public struct Vector2
    {
        public float x, y;
        public static Vector2 zero => new Vector2();
        public static Vector2 one => new Vector2();
        public Vector2(float x, float y) { this.x = x; this.y = y; }
    }
    public struct Color
    {
        public float r, g, b, a;
        public static Color white => new Color();
        public static Color black => new Color();
        public static Color clear => new Color();
        public Color(float r, float g, float b, float a) { this.r = r; this.g = g; this.b = b; this.a = a; }
        public Color(float r, float g, float b) { this.r = r; this.g = g; this.b = b; this.a = 1f; }
    }
    public struct Vector3
    {
        public float x, y, z;
        public Vector3 normalized => this;
        public float magnitude => 0f;
        public static Vector3 forward => new Vector3();
        public static Vector3 right => new Vector3();
        public static Vector3 zero => new Vector3();
        public static float Distance(Vector3 a, Vector3 b) => 0f;
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public static Vector3 operator *(Vector3 v, float s) => v;
        public static Vector3 operator -(Vector3 v) => v;
        public static Vector3 operator +(Vector3 a, Vector3 b) => a;
        public static Vector3 operator -(Vector3 a, Vector3 b) => a;
    }
    public class Camera : Behaviour
    {
        public static Camera main { get; }
        public new Transform transform { get; }
    }
    public class AudioSource : Behaviour
    {
        public AudioClip clip { get; set; }
        public float volume { get; set; }
        public float pitch { get; set; }
        public float spatialBlend { get; set; }
        public bool loop { get; set; }
        public bool playOnAwake { get; set; }
        public AudioRolloffMode rolloffMode { get; set; }
        public float minDistance { get; set; }
        public float maxDistance { get; set; }
        public float reverbZoneMix { get; set; }
        public float panStereo { get; set; }
        public bool isPlaying { get; set; }
        public void Play() { }
        public void Stop() { }
    }
    public class AudioClip : Object
    {
        public delegate void PCMReaderCallback(float[] data);
        public delegate void PCMSetPositionCallback(int position);

        public float length { get; }
        public string name { get; set; }
        public static AudioClip Create(string name, int lengthSamples, int channels, int frequency, bool stream) => null!;
        public static AudioClip Create(string name, int lengthSamples, int channels, int frequency, bool stream, PCMReaderCallback pcmreadercallback) => null!;
        public static AudioClip Create(string name, int lengthSamples, int channels, int frequency, bool stream, PCMReaderCallback pcmreadercallback, PCMSetPositionCallback pcmsetpositioncallback) => null!;
    }
    public enum AudioRolloffMode { Linear }
    public enum AudioType { OGGVORBIS }
    public class AsyncOperation : YieldInstruction { }
    public sealed class WaitForSeconds : YieldInstruction
    {
        public WaitForSeconds(float seconds) { }
    }
    public class YieldInstruction { }
    public class Coroutine : YieldInstruction { }
    public static class Time
    {
        public static float deltaTime { get; }
        public static float time { get; }
        public static float realtimeSinceStartup { get; }
        public static int frameCount { get; }
    }
    public static class Random
    {
        public static float Range(float min, float max) => min;
        public static int Range(int min, int max) => min;
        public static float value { get; }
        public static Vector3 insideUnitSphere => new Vector3();
    }
    public static class Mathf
    {
        public static float Lerp(float a, float b, float t) => a;
        public static float MoveTowards(float current, float target, float maxDelta) => current;
        public static float Clamp(float value, float min, float max) => value;
        public static float Sin(float f) => 0f;
        public static float Sqrt(float f) => 0f;
        public static float Clamp01(float value) => value;
    }

    public static class Physics
    {
        public static int DefaultRaycastLayers => -1;

        public static bool Linecast(
            Vector3 start, Vector3 end,
            out RaycastHit hitInfo,
            int layerMask,
            QueryTriggerInteraction queryTriggerInteraction)
        {
            hitInfo = default;
            return false;
        }
    }

    public struct RaycastHit
    {
        public Collider collider { get; }
        public Transform transform { get; }
        public Vector3 point { get; }
    }

    public class Collider : Component { }

    public enum QueryTriggerInteraction
    {
        UseGlobal = 0,
        Ignore = 1,
        Collide = 2
    }

    public class Texture2D : Object
    {
        public Texture2D(int width, int height) { }
        public Texture2D(int width, int height, TextureFormat format, bool mipChain) { }
        public void SetPixel(int x, int y, Color color) { }
        public void SetPixels(Color[] colors) { }
        public void Apply() { }
    }

    public class Font : Object
    {
        public static Font CreateDynamicFontFromOSFont(string fontname, int size) => new Font();
    }

    public enum FontStyle
    {
        Normal,
        Bold,
        Italic,
        BoldAndItalic
    }

    public enum TextAnchor
    {
        UpperLeft,
        UpperCenter,
        UpperRight,
        MiddleLeft,
        MiddleCenter,
        MiddleRight,
        LowerLeft,
        LowerCenter,
        LowerRight
    }

    public class RectOffset
    {
        public RectOffset() { }
        public RectOffset(int left, int right, int top, int bottom) { }
        public int left { get; set; }
        public int right { get; set; }
        public int top { get; set; }
        public int bottom { get; set; }
    }

    public static class Resources
    {
        public static T GetBuiltinResource<T>(string path) where T : Object => null;
    }
    public enum TextureFormat { RGBA32 }
    public enum RenderMode { ScreenSpaceOverlay }
    public struct LayerMask
    {
        public static int GetMask(params string[] layerNames) => 0;
    }
    public class Light : Behaviour { }
    public class Canvas : Behaviour
    {
        public RenderMode renderMode { get; set; }
        public int sortingOrder { get; set; }
    }
    public static class Debug
    {
        public static void LogError(object message) { }
        public static void LogException(System.Exception exception) { }
    }
    public enum LogType { Error, Assert, Warning, Log, Exception }
    public static class Application
    {
        public static string dataPath { get; }
        public static string version { get; }
        public static string unityVersion { get; }
        public static RuntimePlatform platform { get; }
        public delegate void LogCallback(string logString, string stackTrace, LogType type);
        public static event LogCallback logMessageReceived;
    }
    public enum RuntimePlatform { WindowsPlayer, OSXPlayer, LinuxPlayer }
    public static class JsonUtility
    {
        public static string ToJson(object obj) => "";
        public static T FromJson<T>(string json) => default!;
    }
    public static class SystemInfo
    {
        public static string operatingSystem => "";
        public static string processorType => "";
        public static int processorCount => 0;
        public static int systemMemorySize => 0;
        public static string graphicsDeviceName => "";
        public static int graphicsMemorySize => 0;
        public static string graphicsDeviceVendor => "";
        public static string deviceModel => "";
        public static string deviceName => "";
        public static string operatingSystemFamily => "";
        public static int processorFrequency => 0;
        public static string graphicsDeviceVersion => "";
        public static int graphicsShaderLevel => 0;
        public static string deviceType => "";
    }
    public struct Resolution
    {
        public int width;
        public int height;
        public int refreshRate;
    }
    public enum FullScreenMode { FullScreenWindow, ExclusiveFullScreen, Windowed, MaximizedWindow }
    public static class Screen
    {
        public static int width => 0;
        public static int height => 0;
        public static Resolution currentResolution => new Resolution();
        public static float dpi => 0f;
        public static FullScreenMode fullScreenMode => FullScreenMode.FullScreenWindow;
    }

    public struct Rect
    {
        public float x, y, width, height;
        public float yMin { get; }
        public float xMax => x + width;
        public Rect(float x, float y, float width, float height)
        {
            this.x = x;
            this.y = y;
            this.width = width;
            this.height = height;
            yMin = y;
        }
    }

    public class GUIContent
    {
        public static GUIContent none { get; } = new GUIContent("");
        public GUIContent() { }
        public GUIContent(string text) { }
    }

    public class GUIStyleState
    {
        public Texture2D? background { get; set; }
        public Color textColor { get; set; }
    }

    public class GUIStyle
    {
        public GUIStyleState normal { get; } = new GUIStyleState();
        public Font? font { get; set; }
        public int fontSize { get; set; }
        public FontStyle fontStyle { get; set; }
        public TextAnchor alignment { get; set; }
        public bool wordWrap { get; set; }
        public bool richText { get; set; }
        public RectOffset border { get; set; } = new RectOffset();
        public RectOffset padding { get; set; } = new RectOffset();
        public GUIStyle() { }
        public GUIStyle(GUIStyle other) { }
        public Vector2 CalcSize(GUIContent content) => Vector2.zero;
    }

    public class GUISkin
    {
        public GUIStyle box { get; } = new GUIStyle();
        public GUIStyle label { get; } = new GUIStyle();
        public Font? font { get; }
    }

    public class Event
    {
        public static Event? current { get; set; }
        public EventType type { get; set; }
        public KeyCode keyCode { get; set; }
    }

    public enum EventType
    {
        KeyDown
    }

    public static class GUI
    {
        public static GUISkin skin { get; } = new GUISkin();
        public static Color color { get; set; }
        public static void Box(Rect position, GUIContent content, GUIStyle style) { }
        public static void DrawTexture(Rect position, Texture2D texture) { }
        public static void Label(Rect position, string text) { }
        public static void Label(Rect position, string text, GUIStyle style) { }
    }

    public static class Input
    {
        public static bool GetKeyDown(KeyCode key) => false;
    }

    public enum KeyCode
    {
        F10 = 290
    }
}
namespace UnityEngine.Events
{
    public delegate void UnityAction<T0, T1>(T0 arg0, T1 arg1);
}
namespace UnityEngine.SceneManagement
{
    public static class SceneManager
    {
#pragma warning disable 0067
        public static event UnityEngine.Events.UnityAction<Scene, LoadSceneMode> sceneLoaded;
#pragma warning restore 0067
        public static Scene GetActiveScene() => new Scene();
        public static void LoadScene(string name) { }
    }
    public struct Scene
    {
        public string name { get; }
    }
    public enum LoadSceneMode { Single, Additive }
}
namespace UnityEngine.AI
{
    public class NavMeshAgent : Behaviour
    {
        public float speed { get; set; }
        public float acceleration { get; set; }
    }
}


