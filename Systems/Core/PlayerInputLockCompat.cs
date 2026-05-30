using HarmonyLib;

namespace Dread.Systems.Core
{
    /// <summary>
    /// Locks or unlocks player input via known PlayerController field name variants.
    /// </summary>
    internal static class PlayerInputLockCompat
    {
        public static void SetLocked(PlayerController pc, bool locked)
        {
            if ((object)pc == null)
                return;

            bool any = false;
            any |= TrySetBoolField(pc, "inputLocked", locked);
            any |= TrySetBoolField(pc, "interactDisabled", locked);
            any |= TrySetBoolField(pc, "InputLocked", locked);
            any |= TrySetBoolField(pc, "InteractDisabled", locked);

            if (!any)
                LoggingService.LogWarning(
                    $"[Dread] PlayerInputLockCompat: no matching field on PlayerController (locked={locked})");
        }

        private static bool TrySetBoolField(PlayerController pc, string name, bool value)
        {
            try
            {
                Traverse.Create(pc).Field<bool>(name).Value = value;
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
