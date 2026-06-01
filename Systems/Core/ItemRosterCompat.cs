using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace Dread.Systems.Core
{
    /// <summary>
    /// Defensive enumeration of all interactable item GameObjects.
    /// The item type is resolved by name so stub builds stay clean and
    /// the lookup degrades gracefully (no items returned) if the game's
    /// type names change. Never throws into callers.
    /// </summary>
    internal static class ItemRosterCompat
    {
        private static readonly string[] ItemTypeNames =
            { "ValuableObject", "PhysGrabObject", "ItemPickup", "Valuable" };

        private static Type? _itemType;
        private static bool _resolved;
        private static bool _loggedError;

        internal static void ResetForNewRun()
        {
            _resolved = false;
            _itemType = null;
            _loggedError = false;
        }

        public static List<GameObject> GetItemGameObjects()
        {
            var result = new List<GameObject>();
            try
            {
                ResolveItemType();
                if (_itemType == null)
                    return result;

                CollectInstances(result);
            }
            catch (Exception ex)
            {
                LogErrorOnce("GetItemGameObjects failed", ex);
            }

            return result;
        }

        private static void CollectInstances(List<GameObject> result)
        {
            UnityEngine.Object[] objects;
            try
            {
                objects = UnityEngine.Object.FindObjectsOfType(_itemType!, true);
            }
            catch (MissingMethodException)
            {
                objects = UnityEngine.Object.FindObjectsOfType(_itemType!);
            }

            foreach (var o in objects)
            {
                if (o is Component c && (object)c != null)
                    result.Add(c.gameObject);
            }
        }

        private static void ResolveItemType()
        {
            if (_resolved)
                return;

            _resolved = true;

            foreach (var name in ItemTypeNames)
            {
                var t = ResolveTypeByName(name);
                if (t != null)
                {
                    _itemType = t;
                    LoggingService.LogVerbose($"[Dread] ItemRosterCompat: resolved item type as '{name}'");
                    return;
                }
            }

            LoggingService.LogWarning(
                "[Dread] ItemRosterCompat: no item type found "
                + "(tried ValuableObject, PhysGrabObject, ItemPickup, Valuable); snitch will be disabled");
        }

        private static Type? ResolveTypeByName(string name)
        {
            try
            {
                var t = AccessTools.TypeByName(name);
                if (IsItemComponentType(t))
                    return t;
            }
            catch
            {
                // fall through to assembly scan
            }

            try
            {
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    var asmName = asm.GetName().Name;
                    if (asmName == null
                        || !asmName.Equals("Assembly-CSharp", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var fromAsm = asm.GetType(name, throwOnError: false, ignoreCase: false);
                    if (IsItemComponentType(fromAsm))
                        return fromAsm;
                }
            }
            catch (Exception ex)
            {
                LogErrorOnce($"type scan for '{name}' failed", ex);
            }

            return null;
        }

        private static bool IsItemComponentType(Type? t) =>
            t != null && typeof(Component).IsAssignableFrom(t);

        private static void LogErrorOnce(string context, Exception ex)
        {
            if (_loggedError)
                return;
            _loggedError = true;
            LoggingService.LogWarning(
                $"[Dread] ItemRosterCompat: {context}: {ex.GetType().Name}: {ex.Message}");
        }
    }
}
