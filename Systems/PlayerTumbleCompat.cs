using System;
using System.Reflection;
using HarmonyLib;

namespace Dread.Systems
{
    /// <summary>
    /// REPO tumble (ragdoll/fallen) via PlayerAvatar.tumble — TumbleSet / TumbleRequest.
    /// </summary>
    internal static class PlayerTumbleCompat
    {
        private static bool _forcedActive;

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

            foreach (var field in tumble.GetType().GetFields(
                         BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (field.FieldType != typeof(bool))
                    continue;

                var name = field.Name;
                if (name.IndexOf("tumble", StringComparison.OrdinalIgnoreCase) < 0
                    && name.IndexOf("fall", StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

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
            if (!_forcedActive || pc == null)
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

            foreach (var field in avatar.GetType().GetFields(
                         BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (!field.Name.Equals("tumble", StringComparison.OrdinalIgnoreCase))
                    continue;

                try
                {
                    return field.GetValue(avatar);
                }
                catch { }
            }

            return null;
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
            foreach (var name in new[] { "TumbleSet", "TumbleRequest" })
            {
                var method = AccessTools.Method(type, name, new[] { typeof(bool), typeof(bool) });
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
    }
}
