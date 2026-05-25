using System.Reflection;
using System.Runtime.InteropServices;

var stubsDir = args.Length > 0 ? args[0] : ".github/stubs/refs";
var gameTypes = new[] { "EnemyNavMeshAgent", "EnemyHealth", "EnemyDirector", "EnemyParent", "PlayerController", "SemiFunc" };
var failed = false;

var runtimeDir = RuntimeEnvironment.GetRuntimeDirectory();
var resolver = new PathAssemblyResolver(
    Directory.GetFiles(stubsDir, "*.dll")
        .Concat(Directory.GetFiles(runtimeDir, "*.dll"))
);
var mlc = new MetadataLoadContext(resolver);

void CheckAssembly(string label, string dllPath, string[] shouldContain, string[] shouldNotContain)
{
    Console.Write($"[verify] Checking {label}... ");
    var asm = mlc.LoadFromAssemblyPath(dllPath);
    var typeNames = asm.GetExportedTypes().Select(t => t.FullName!).ToHashSet();

    foreach (var name in shouldContain)
    {
        if (!typeNames.Any(t => t.EndsWith($".{name}") || t == name))
        {
            Console.WriteLine($"FAIL (missing {name})");
            failed = true;
            return;
        }
    }

    foreach (var name in shouldNotContain)
    {
        if (typeNames.Any(t => t.EndsWith($".{name}") || t == name))
        {
            Console.WriteLine($"FAIL (found {name} but should not)");
            failed = true;
            return;
        }
    }

    Console.WriteLine("OK");
}

var unityDll = Path.Combine(stubsDir, "UnityEngine.dll");
var acsDll = Path.Combine(stubsDir, "Assembly-CSharp.dll");
var uwrDll = Path.Combine(stubsDir, "UnityEngine.UnityWebRequestModule.dll");
var uwraDll = Path.Combine(stubsDir, "UnityEngine.UnityWebRequestAudioModule.dll");

var missing = new[] {
    ("UnityEngine.dll", unityDll),
    ("Assembly-CSharp.dll", acsDll),
    ("UnityEngine.UnityWebRequestModule.dll", uwrDll),
    ("UnityEngine.UnityWebRequestAudioModule.dll", uwraDll),
}.Where(p => !File.Exists(p.Item2)).Select(p => p.Item1).ToList();

if (missing.Count > 0)
{
    foreach (var name in missing) Console.WriteLine($"::error::{name} not found at expected path");
    failed = true;
}
else
{
    CheckAssembly("UnityEngine.dll", unityDll, shouldContain: Array.Empty<string>(), shouldNotContain: gameTypes.Concat(new[] { "RawImage", "RectTransform" }).ToArray());

    var uiDll = Path.Combine(stubsDir, "UnityEngine.UI.dll");
    if (!File.Exists(uiDll))
    {
        Console.WriteLine("::error::UnityEngine.UI.dll not found at expected path");
        failed = true;
    }
    else
    {
        CheckAssembly("UnityEngine.UI.dll", uiDll, shouldContain: new[] { "RawImage", "RectTransform" }, shouldNotContain: gameTypes);
    }
    CheckAssembly("Assembly-CSharp.dll", acsDll, shouldContain: gameTypes, shouldNotContain: Array.Empty<string>());
    CheckAssembly("UnityEngine.UnityWebRequestModule.dll", uwrDll, shouldContain: new[] { "UnityWebRequestAsyncOperation", "UnityWebRequest" }, shouldNotContain: new[] { "UnityWebRequestMultimedia", "DownloadHandlerAudioClip" }.Concat(gameTypes).ToArray());
    CheckAssembly("UnityEngine.UnityWebRequestAudioModule.dll", uwraDll, shouldContain: new[] { "UnityWebRequestMultimedia", "DownloadHandlerAudioClip" }, shouldNotContain: gameTypes);

    var acsAsm = mlc.LoadFromAssemblyPath(acsDll);
    var refs = acsAsm.GetReferencedAssemblies().Select(r => r.Name).ToHashSet();
    if (!refs.Contains("UnityEngine"))
    {
        Console.WriteLine("::error::Assembly-CSharp.dll does not reference UnityEngine.dll");
        failed = true;
    }
    else
    {
        Console.WriteLine("[verify] Assembly-CSharp references UnityEngine: OK");
    }

    foreach (var (label, dll, expectedRef) in new[] {
        ("UnityWebRequestModule", uwrDll, "UnityEngine"),
        ("UnityWebRequestAudioModule", uwraDll, "UnityEngine"),
        ("UnityWebRequestAudioModule", uwraDll, "UnityEngine.UnityWebRequestModule"),
    })
    {
        var asm = mlc.LoadFromAssemblyPath(dll);
        var asmRefs = asm.GetReferencedAssemblies().Select(r => r.Name).ToHashSet();
        if (!asmRefs.Contains(expectedRef))
        {
            Console.WriteLine($"::error::{label}.dll does not reference {expectedRef}.dll");
            failed = true;
        }
        else
        {
            Console.WriteLine($"[verify] {label} references {expectedRef}: OK");
        }
    }
}

if (failed)
{
    Console.WriteLine("::error::Stub verification failed -- a game type is in the wrong stub assembly");
    Environment.Exit(1);
}

Console.WriteLine("[verify] All stub verification checks passed");
