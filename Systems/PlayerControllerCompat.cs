using System;
using System.Reflection;
using HarmonyLib;

namespace Dread.Systems
{
    internal static class PlayerControllerCompat
    {
        // REPO v0.4.x: PlayerController.Crouching/Crawling; PlayerAvatar.isCrouching/isCrawling
        private static readonly string[] CrouchMemberNames =
        {
            "Crouching", "Crawling", "isCrouching", "isCrawling",
            "crouching", "crawling", "IsCrouching", "IsCrawling"
        };

        public static float GetHealth(PlayerController pc)
        {
            if (pc == null)
                return -1f;

            try
            {
                return Traverse.Create(pc).Property<float>("Health").Value;
            }
            catch { }

            try
            {
                return Traverse.Create(pc).Field<float>("health").Value;
            }
            catch { }

            try
            {
                return Traverse.Create(pc).Field<float>("Health").Value;
            }
            catch { }

            return -1f;
        }

        public static bool IsAlive(PlayerController pc)
        {
            var health = GetHealth(pc);
            return health < 0f || health > 0f;
        }

        public static float GetStamina(PlayerController pc)
        {
            if (pc == null)
                return -1f;

            foreach (var name in new[] { "EnergyCurrent", "stamina", "Stamina", "energy", "Energy" })
            {
                try
                {
                    return Traverse.Create(pc).Field<float>(name).Value;
                }
                catch { }

                try
                {
                    return Traverse.Create(pc).Property<float>(name).Value;
                }
                catch { }
            }

            return -1f;
        }

        public static bool IsCrouching(PlayerController pc)
        {
            if (pc == null)
                return false;

            if (TryReadCrouch(pc))
                return true;

            try
            {
                var avatar = Traverse.Create(pc).Field("playerAvatarScript").GetValue();
                if (avatar != null && TryReadCrouch(avatar))
                    return true;
            }
            catch { }

            return false;
        }

        private static bool TryReadCrouch(object target)
        {
            foreach (var name in CrouchMemberNames)
            {
                if (TryGetBoolMember(target, name, out var memberValue) && memberValue)
                    return true;
            }

            foreach (var field in target.GetType().GetFields(
                         BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (field.FieldType != typeof(bool))
                    continue;

                var name = field.Name;
                if (name.IndexOf("crouch", StringComparison.OrdinalIgnoreCase) < 0
                    && name.IndexOf("crawl", StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                try
                {
                    if ((bool)field.GetValue(target))
                        return true;
                }
                catch { }
            }

            return false;
        }

        private static bool TryGetBoolMember(object target, string name, out bool value)
        {
            value = false;
            try
            {
                value = Traverse.Create(target).Field<bool>(name).Value;
                return true;
            }
            catch { }

            try
            {
                value = Traverse.Create(target).Property<bool>(name).Value;
                return true;
            }
            catch { }

            return false;
        }
    }
}
