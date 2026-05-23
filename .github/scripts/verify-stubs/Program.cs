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

if (!File.Exists(unityDll)) { Console.WriteLine($"::error::UnityEngine.dll not found at {unityDll}"); failed = true; }
else if (!File.Exists(acsDll)) { Console.WriteLine($"::error::Assembly-CSharp.dll not found at {acsDll}"); failed = true; }
else if (!File.Exists(uwrDll)) { Console.WriteLine($"::error::UnityEngine.UnityWebRequestModule.dll not found at {uwrDll}"); failed = true; }
else
{
    CheckAssembly("UnityEngine.dll", unityDll, shouldContain: Array.Empty<string>(), shouldNotContain: gameTypes);
    CheckAssembly("Assembly-CSharp.dll", acsDll, shouldContain: gameTypes, shouldNotContain: Array.Empty<string>());
    CheckAssembly("UnityEngine.UnityWebRequestModule.dll", uwrDll, shouldContain: new[] { "UnityWebRequestAsyncOperation", "UnityWebRequest" }, shouldNotContain: gameTypes);

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

    var uwrAsm = mlc.LoadFromAssemblyPath(uwrDll);
    var uwrRefs = uwrAsm.GetReferencedAssemblies().Select(r => r.Name).ToHashSet();
    if (!uwrRefs.Contains("UnityEngine"))
    {
        Console.WriteLine("::error::UnityEngine.UnityWebRequestModule.dll does not reference UnityEngine.dll");
        failed = true;
    }
    else
    {
        Console.WriteLine("[verify] UnityWebRequestModule references UnityEngine: OK");
    }
}

if (failed)
{
    Console.WriteLine("::error::Stub verification failed -- a game type is in the wrong stub assembly");
    Environment.Exit(1);
}

Console.WriteLine("[verify] All stub verification checks passed");
