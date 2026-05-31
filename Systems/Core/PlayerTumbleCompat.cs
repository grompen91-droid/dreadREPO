using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace Dread.Systems.Core
{
    /// <summary>
    /// REPO tumble (ragdoll/fallen) via PlayerAvatar.tumble — TumbleSet / TumbleRequest.
    /// </summary>
    internal static class PlayerTumbleCompat
    {
        private static bool _forcedActive;
        private static readonly Dictionary<Type, FieldInfo[]> TumbleBoolFieldsByType = new();
        private static readonly Dictionary<Type, FieldInfo?> AvatarTumbleFieldByType = new();
        private static readonly Dictionary<Type, MethodInfo?> TumbleSetMethodByType = new();
        private static readonly Dictionary<Type, MethodInfo?> TumbleRequestMethodByType = new();

        private static readonly string[] TumbleActiveMemberNames =
        {
            "tumbling", "Tumbling", "isTumbling", "IsTumbling",
            "fallen", "Fallen", "isFallen", "IsFallen",
            "active", "Active", "isActive", "IsActive"
        };

        /// <summary>
        /// True when the local avatar is in a tumble/fallen hide state (counts as crouching for triggers).
        /// </summary>
        public static bool IsInTumble(PlayerController pc)
        {
            var tumble = ResolveTumble(pc);
            if (tumble == null)
                return false;

            foreach (var name in TumbleActiveMemberNames)
            {
                try
                {
                    if (Traverse.Create(tumble).Field<bool>(name).Value)
                        return true;
                }
                catch { }

                try
                {
                    if (Traverse.Create(tumble).Property<bool>(name).Value)
                        return true;
                }
                catch { }
            }

            foreach (var field in GetTumbleBoolFields(tumble.GetType()))
            {
                try
                {
                    if ((bool)field.GetValue(tumble)!)
                        return true;
                }
                catch { }
            }

            return false;
        }

        public static bool ApplyForcedTumble(PlayerController pc)
        {
            var tumble = ResolveTumble(pc);
            if (tumble == null)
                return false;

            if (!InvokeTumble(tumble, active: true))
                return false;

            _forcedActive = true;
            return true;
        }

        public static void MaintainForcedTumble(PlayerController pc)
        {
            EnsureForcedTumbleHeld(pc);
        }

        /// <summary>
        /// Re-applies forced tumble only if the fallen state was lost (avoids replaying tumble SFX).
        /// </summary>
        public static void EnsureForcedTumbleHeld(PlayerController? pc)
        {
            if (!_forcedActive || pc == null)
                return;

            if (IsInTumble(pc))
                return;

            var tumble = ResolveTumble(pc);
            if (tumble == null)
                return;

            InvokeTumble(tumble, active: true);
        }

        public static void ReleaseForcedTumble(PlayerController? pc)
        {
            if (!_forcedActive)
                return;

            _forcedActive = false;
            if (pc == null)
                return;

            var tumble = ResolveTumble(pc);
            if (tumble == null)
                return;

            InvokeTumble(tumble, active: false);
        }

        private static object? ResolveTumble(PlayerController pc)
        {
            var avatar = GetLocalAvatar(pc);
            if (avatar == null)
                return null;

            try
            {
                return Traverse.Create(avatar).Field("tumble").GetValue();
            }
            catch { }

            var tumbleField = GetAvatarTumbleField(avatar.GetType());
            if (tumbleField == null)
                return null;

            try
            {
                return tumbleField.GetValue(avatar);
            }
            catch
            {
                return null;
            }
        }

        private static object? GetLocalAvatar(PlayerController pc)
        {
            if (pc == null)
                return null;

            try
            {
                var fromPc = Traverse.Create(pc).Field("playerAvatarScript").GetValue();
                if (fromPc != null)
                    return fromPc;
            }
            catch { }

            try
            {
                var avatarType = AccessTools.TypeByName("PlayerAvatar");
                if (avatarType == null)
                    return null;

                try
                {
                    var instanceProp = Traverse.Create(avatarType).Property("instance").GetValue();
                    if (instanceProp != null)
                        return instanceProp;
                }
                catch { }

                var instanceField = AccessTools.Field(avatarType, "instance");
                if (instanceField != null)
                    return instanceField.GetValue(null);
            }
            catch { }

            return null;
        }

        private static bool InvokeTumble(object tumble, bool active)
        {
            var type = tumble.GetType();
            foreach (var method in new[] { GetTumbleSetMethod(type), GetTumbleRequestMethod(type) })
            {
                if (method == null)
                    continue;

                try
                {
                    method.Invoke(tumble, new object[] { active, false });
                    return true;
                }
                catch { }
            }

            return false;
        }

        private static FieldInfo[] GetTumbleBoolFields(Type type)
        {
            lock (TumbleBoolFieldsByType)
            {
                if (TumbleBoolFieldsByType.TryGetValue(type, out var cached))
                    return cached;

                var matches = new List<FieldInfo>();
                foreach (var field in type.GetFields(
                             BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    if (field.FieldType != typeof(bool))
                        continue;

                    var name = field.Name;
                    if (name.IndexOf("tumble", StringComparison.OrdinalIgnoreCase) < 0
                        && name.IndexOf("fall", StringComparison.OrdinalIgnoreCase) < 0)
                        continue;

                    matches.Add(field);
                }

                var result = matches.ToArray();
                TumbleBoolFieldsByType[type] = result;
                return result;
            }
        }

        private static FieldInfo? GetAvatarTumbleField(Type avatarType)
        {
            lock (AvatarTumbleFieldByType)
            {
                if (AvatarTumbleFieldByType.TryGetValue(avatarType, out var cached))
                    return cached;

                FieldInfo? match = null;
                foreach (var field in avatarType.GetFields(
                             BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    if (!field.Name.Equals("tumble", StringComparison.OrdinalIgnoreCase))
                        continue;

                    match = field;
                    break;
                }

                AvatarTumbleFieldByType[avatarType] = match;
                return match;
            }
        }

        private static MethodInfo? GetTumbleSetMethod(Type tumbleType)
        {
            lock (TumbleSetMethodByType)
            {
                if (TumbleSetMethodByType.TryGetValue(tumbleType, out var cached))
                    return cached;

                var method = AccessTools.Method(tumbleType, "TumbleSet", new[] { typeof(bool), typeof(bool) });
                TumbleSetMethodByType[tumbleType] = method;
                return method;
            }
        }

        private static MethodInfo? GetTumbleRequestMethod(Type tumbleType)
        {
            lock (TumbleRequestMethodByType)
            {
                if (TumbleRequestMethodByType.TryGetValue(tumbleType, out var cached))
                    return cached;

                var method = AccessTools.Method(tumbleType, "TumbleRequest", new[] { typeof(bool), typeof(bool) });
                TumbleRequestMethodByType[tumbleType] = method;
                return method;
            }
        }
    }
}
