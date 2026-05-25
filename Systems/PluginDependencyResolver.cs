using System;
using System.IO;
using System.Reflection;

namespace Dread.Systems
{
    internal static class PluginDependencyResolver
    {
        private static bool _registered;

        public static void Register()
        {
            if (_registered) return;
            _registered = true;

            var pluginDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
            AppDomain.CurrentDomain.AssemblyResolve += (_, args) =>
            {
                var name = new AssemblyName(args.Name).Name;
                if (string.IsNullOrEmpty(name)) return null;

                var path = Path.Combine(pluginDir, name + ".dll");
                if (!File.Exists(path)) return null;

                try
                {
                    return Assembly.LoadFrom(path);
                }
                catch
                {
                    return null;
                }
            };
        }
    }
}
