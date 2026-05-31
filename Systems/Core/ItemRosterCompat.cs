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
        private static readonly string[] ItemTypeNames = { "PhysGrabObject", "ValuableObject", "ItemPickup" };

        private static Type? _itemType;
        private static bool _resolved;
        private static bool _loggedError;

        public static List<GameObject> GetItemGameObjects()
        {
            var result = new List<GameObject>();
            try
            {
                ResolveItemType();
                if (_itemType == null)
                    return result;

                foreach (var o in UnityEngine.Object.FindObjectsOfType(_itemType))
                {
                    if (o is Component c && (object)c != null)
                        result.Add(c.gameObject);
                }
            }
            catch (Exception ex)
            {
                LogErrorOnce("GetItemGameObjects failed", ex);
            }
            return result;
        }

        private static void ResolveItemType()
        {
            if (_resolved)
                return;

            _resolved = true;
            foreach (var name in ItemTypeNames)
            {
                _itemType = AccessTools.TypeByName(name);
                if (_itemType != null)
                {
                    LoggingService.LogVerbose($"[Dread] ItemRosterCompat: resolved item type as '{name}'");
                    return;
                }
            }

            LoggingService.LogWarning("[Dread] ItemRosterCompat: no item type found (tried PhysGrabObject, ValuableObject, ItemPickup); snitch will be disabled");
        }

        private static void LogErrorOnce(string context, Exception ex)
        {
            if (_loggedError)
                return;
            _loggedError = true;
            LoggingService.LogWarning($"[Dread] ItemRosterCompat: {context}: {ex.GetType().Name}: {ex.Message}");
        }
    }
}
