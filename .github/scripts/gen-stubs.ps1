param(
    [string]$OutDir = "$PSScriptRoot/../_ci"
)

$ErrorActionPreference = "Stop"

$stubsDir = New-Item -ItemType Directory -Force "$OutDir/refs" | Select-Object -ExpandProperty FullName

$stubs = @'
using System;
using System.Collections;
using System.Reflection;
using UnityEngine;

namespace UnityEngine
{
    public abstract class MonoBehaviour : Behaviour
    {
        public GameObject gameObject { get; }
        public Transform transform { get; }
        public Coroutine StartCoroutine(IEnumerator routine) => null;
    }
    public class Behaviour : Component { }
    public class Component : Object
    {
        public T GetComponent<T>() where T : class => null;
        public GameObject gameObject { get; }
        public Transform transform { get; }
    }
    public class GameObject : Object
    {
        public GameObject() { }
        public GameObject(string name) { }
        public T AddComponent<T>() where T : Component => null;
        public Transform transform { get; }
    }
    public class Transform : Component
    {
        public Vector3 position { get; set; }
        public Vector3 forward { get; }
        public Vector3 right { get; }
    }
    public class Object
    {
        public static T FindObjectOfType<T>() where T : Object => null;
        public static T[] FindObjectsOfType<T>() where T : Object => null;
        public static void Destroy(Object obj, float t = 0f) { }
        public static void DontDestroyOnLoad(Object obj) { }
        public static implicit operator bool(Object exists) => exists != null;
    }
    public struct Vector3
    {
        public float x, y, z;
        public Vector3 normalized => this;
        public static Vector3 forward => new Vector3();
        public static Vector3 right => new Vector3();
        public static Vector3 zero => new Vector3();
        public static float Distance(Vector3 a, Vector3 b) => 0f;
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public static Vector3 operator *(Vector3 v, float s) => v;
        public static Vector3 operator +(Vector3 a, Vector3 b) => a;
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
        public void Play() { }
    }
    public class AudioClip : Object
    {
        public float length { get; }
        public string name { get; set; }
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
    }
    public static class Random
    {
        public static float Range(float min, float max) => min;
        public static int Range(int min, int max) => min;
        public static float value { get; }
    }
    public static class Mathf
    {
        public static float Lerp(float a, float b, float t) => a;
        public static float Clamp(float value, float min, float max) => value;
    }
}
namespace UnityEngine.SceneManagement
{
    public static class SceneManager
    {
        public static event Action<Scene, LoadSceneMode> sceneLoaded;
        public static void LoadScene(string name) { }
    }
    public struct Scene
    {
        public string name { get; }
    }
    public enum LoadSceneMode { Single, Additive }
}
namespace UnityEngine.Networking
{
    public class UnityWebRequest : IDisposable
    {
        public Result result { get; }
        public string error { get; }
        public AsyncOperation SendWebRequest() => null;
        public void Dispose() { }
        public enum Result { Success }
    }
    public class UnityWebRequestMultimedia
    {
        public static UnityWebRequest GetAudioClip(string url, AudioType audioType) => null;
    }
    public class DownloadHandlerAudioClip
    {
        public static AudioClip GetContent(UnityWebRequest req) => null;
    }
}
namespace UnityEngine.AI
{
    public class NavMeshAgent : Behaviour
    {
        public float speed { get; set; }
        public float acceleration { get; set; }
    }
}

public class EnemyHealth : MonoBehaviour { }
public class EnemyParent : MonoBehaviour { }
public class EnemyNavMeshAgent : MonoBehaviour
{
    public NavMeshAgent Agent;
    public float DefaultSpeed;
    public float DefaultAcceleration;
}
public class EnemyDirector : MonoBehaviour
{
    public void SetInvestigate(ref float radius) { }
}
public class PlayerController : MonoBehaviour
{
    public static PlayerController instance;
    public float CrouchSpeed;
    public float EnergySprintDrain;
    public bool sprinting;
    public float EnergyCurrent;
    public float EnergyStart;
    public float SprintSpeedMultiplier;
}
public static class SemiFunc
{
    public static bool MenuLevel() => false;
    public static bool IsMasterClient() => false;
}
'@

Add-Type -TypeDefinition $stubs -OutputAssembly "$stubsDir/CIStubs.dll" -WarningAction SilentlyContinue
Write-Host "[gen-stubs] Created CIStubs.dll ($((Get-Item "$stubsDir/CIStubs.dll").Length / 1KB) KB)"

$bepinVersion = "5.4.21"
$bepinUrl = "https://github.com/BepInEx/BepInEx/releases/download/v$bepinVersion/BepInEx_win_x64_$bepinVersion.0.zip"
$bepinZip = "$OutDir/bepinex.zip"
$bepinDir = "$OutDir/bepinex"

if (!(Test-Path "$bepinDir/BepInEx/core/BepInEx.dll")) {
    Write-Host "[gen-stubs] Downloading BepInEx $bepinVersion..."
    try {
        Invoke-WebRequest -Uri $bepinUrl -OutFile $bepinZip -UseBasicParsing -ErrorAction Stop
        Expand-Archive -Path $bepinZip -DestinationPath $bepinDir -Force
        Remove-Item $bepinZip -Force
    } catch {
        Write-Warning "[gen-stubs] BepInEx download failed: $_"
    }
}

$coreDir = "$stubsDir/core"
New-Item -ItemType Directory -Force $coreDir | Out-Null

if (Test-Path "$bepinDir/BepInEx/core/BepInEx.dll") {
    Copy-Item "$bepinDir/BepInEx/core/BepInEx.dll" "$coreDir/BepInEx.dll"
    Copy-Item "$bepinDir/BepInEx/core/0Harmony.dll" "$coreDir/0Harmony.dll"
    Write-Host "[gen-stubs] Copied BepInEx.dll ($((Get-Item "$coreDir/BepInEx.dll").Length / 1KB) KB)"
    Write-Host "[gen-stubs] Copied 0Harmony.dll ($((Get-Item "$coreDir/0Harmony.dll").Length / 1KB) KB)"
} else {
    Write-Warning "[gen-stubs] BepInEx not available -- stubs only, build will fail"
}

$targets = @'
<Project>
  <Target Name="ReplaceGameRefsWithCIStubs" BeforeTargets="ResolveAssemblyReferences">
    <ItemGroup>
      <_CIRefs Include="@(Reference)" Condition="
        '%(Identity)' != 'BepInEx' And
        '%(Identity)' != '0Harmony'" />
      <Reference Remove="@(_CIRefs)" />
      <Reference Include="CIStubs">
        <HintPath>$(MSBuildProjectDirectory)/.github/_ci/refs/CIStubs.dll</HintPath>
      </Reference>
    </ItemGroup>
  </Target>
</Project>
'@

$targets | Out-File -FilePath "$OutDir/Directory.Build.targets" -Encoding utf8
Write-Host "[gen-stubs] Created Directory.Build.targets"
Write-Host "[gen-stubs] Done"