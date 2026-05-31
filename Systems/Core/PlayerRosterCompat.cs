using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace Dread.Systems.Core
{
    /// <summary>
    /// Defensive enumeration of all players (local and remote) with world
    /// positions. REPO's networked player type is resolved by name so stub
    /// builds stay clean and the lookup degrades gracefully (to the local
    /// player) if the game's type names change. Never throws into callers.
    /// </summary>
    internal static class PlayerRosterCompat
    {
        internal readonly struct PlayerRef
        {
            public PlayerRef(string label, Vector3 position)
            {
                Label = label;
                Position = position;
            }

            public string Label { get; }
            public Vector3 Position { get; }
        }

        private static readonly string[] AvatarTypeNames = { "PlayerAvatar", "PlayerController" };
        private static readonly string[] NameFields = { "playerName", "PlayerName", "nickName" };

        private static Type? _avatarType;
        private static bool _resolved;
        private static bool _loggedError;

        public static List<PlayerRef> GetPlayers()
        {
            var result = new List<PlayerRef>();

            try
            {
                ResolveAvatarType();
                if (_avatarType != null)
                    CollectAvatars(result);

                if (result.Count == 0)
                    AddLocalPlayer(result);
            }
            catch (Exception ex)
            {
                LogErrorOnce("GetPlayers failed", ex);
            }

            return result;
        }

        private static void CollectAvatars(List<PlayerRef> result)
        {
            try
            {
                foreach (var o in UnityEngine.Object.FindObjectsOfType(_avatarType!))
                {
                    if (o is Component c && (object)c != null)
                        result.Add(new PlayerRef(ReadName(c), c.transform.position));
                }
            }
            catch (Exception ex)
            {
                LogErrorOnce("avatar scan failed", ex);
            }
        }

        private static void AddLocalPlayer(List<PlayerRef> result)
        {
            try
            {
                var pc = PlayerController.instance;
                if ((object)pc != null)
                    result.Add(new PlayerRef("local", pc.transform.position));
            }
            catch (Exception ex)
            {
                LogErrorOnce("local player read failed", ex);
            }
        }

        private static void ResolveAvatarType()
        {
            if (_resolved)
                return;

            _resolved = true;
            try
            {
                foreach (var name in AvatarTypeNames)
                {
                    var t = AccessTools.TypeByName(name);
                    if (t != null && typeof(Component).IsAssignableFrom(t))
                    {
                        _avatarType = t;
                        LoggingService.LogInfo($"[Dread] PlayerRosterCompat using player type '{name}'");
                        return;
                    }
                }

                LoggingService.LogWarning(
                    "[Dread] PlayerRosterCompat: no player type resolved; using local player only");
            }
            catch (Exception ex)
            {
                LogErrorOnce("type resolution failed", ex);
            }
        }

        private static string ReadName(Component c)
        {
            foreach (var name in NameFields)
            {
                try
                {
                    var v = Traverse.Create(c).Field<string>(name).Value;
                    if (!string.IsNullOrEmpty(v))
                        return v;
                }
                catch
                {
                    // field absent on this type; try the next candidate
                }
            }

            try
            {
                return c.name;
            }
            catch
            {
                return "player";
            }
        }

        private static void LogErrorOnce(string what, Exception ex)
        {
            if (_loggedError)
            {
                LoggingService.LogVerbose($"[Dread] PlayerRosterCompat {what}: {ex.Message}");
                return;
            }

            _loggedError = true;
            LoggingService.LogWarning(
                $"[Dread] PlayerRosterCompat {what}: {ex.GetType().Name}: {ex.Message}");
        }
    }
}
