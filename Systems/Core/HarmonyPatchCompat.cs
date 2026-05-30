using System;
using System.Collections.Generic;
using System.Reflection;
using Dread;
using Dread.Config;
using Dread.Systems;
using HarmonyLib;

namespace Dread.Systems.Core
{
    /// <summary>
    /// Shared helpers for host-only patch gates and optional skip when other mods already patch a method.
    /// </summary>
    internal static class HarmonyPatchCompat
    {
        private static readonly HashSet<string> SkipWarningsLogged = new(StringComparer.Ordinal);
        private static readonly MethodInfo? IsMasterClientMethod =
            AccessTools.Method(typeof(SemiFunc), "IsMasterClient");

        internal static bool IsMasterClient()
        {
            try
            {
                if (IsMasterClientMethod == null)
                    return true;

                return IsMasterClientMethod.Invoke(null, null) is bool isMaster && isMaster;
            }
            catch
            {
                return true;
            }
        }

        internal static bool ShouldSkipDueToForeignPatches(MethodBase method, string patchLabel)
        {
            if (!DreadConfig.CompatibilitySkipConflictingPatches.Value)
                return false;

            var info = Harmony.GetPatchInfo(method);
            if (info == null)
                return false;

            if (!HasForeignOwners(info))
                return false;

            if (SkipWarningsLogged.Add(patchLabel))
            {
                LoggingService.LogWarning(
                    $"[Dread] Skipping {patchLabel}: target already patched by another mod "
                        + "(CompatibilitySkipConflictingPatches=true)");
            }

            return true;
        }

        private static bool HasForeignOwners(Patches info)
        {
            foreach (var patch in info.Prefixes)
            {
                if (!IsDreadOwner(patch.owner))
                    return true;
            }

            foreach (var patch in info.Postfixes)
            {
                if (!IsDreadOwner(patch.owner))
                    return true;
            }

            foreach (var patch in info.Transpilers)
            {
                if (!IsDreadOwner(patch.owner))
                    return true;
            }

            foreach (var patch in info.Finalizers)
            {
                if (!IsDreadOwner(patch.owner))
                    return true;
            }

            return false;
        }

        private static bool IsDreadOwner(string owner)
        {
            return string.Equals(owner, Plugin.GUID, StringComparison.Ordinal);
        }
    }
}
