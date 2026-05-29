using HarmonyLib;
using UnityEngine;

namespace Dread.Systems
{
    public partial class PsychoticBreakSystem
    {
        private static void LockPlayerForEpisode(PlayerController? pc)
        {
            if ((object)pc == null)
                return;

            DisableFlashlight(pc);
            LockInput(pc);
            if (!PlayerTumbleCompat.ApplyForcedTumble(pc))
                LoggingService.LogVerbose("[PsychoticBreak] Tumble lock unavailable; input lock only");
        }

        private static void MaintainPlayerFallenState(PlayerController? pc)
        {
            if ((object)pc == null)
                return;

            PlayerTumbleCompat.MaintainForcedTumble(pc);
            LockInput(pc);
        }

        private static void RestorePlayerControl(PlayerController? pc)
        {
            PlayerTumbleCompat.ReleaseForcedTumble(pc);
            if ((object)pc == null)
                return;

            UnlockInput(pc);
            RestoreFlashlight(pc);
        }

        private static void DisableFlashlight(PlayerController pc)
        {
            var light = pc.GetComponentInChildren<Light>();
            if (light != null && light.enabled)
            {
                light.enabled = false;
                var tracker = new GameObject("DreadFlashlightTracker");
                tracker.transform.SetParent(pc.transform);
                var stored = tracker.AddComponent<FlashlightStateTracker>();
                stored.Flashlight = light;
            }
        }

        private static void RestoreFlashlight(PlayerController pc)
        {
            var tracker = pc.GetComponentInChildren<FlashlightStateTracker>();
            if (tracker != null && tracker.Flashlight != null)
                tracker.Flashlight.enabled = true;
            Destroy(tracker?.gameObject);
        }

        private static void LockInput(PlayerController pc)
        {
            bool any = false;
            try { Traverse.Create(pc).Field<bool>("inputLocked").Value = true; any = true; }
            catch { }
            try { Traverse.Create(pc).Field<bool>("interactDisabled").Value = true; any = true; }
            catch { }
            try { Traverse.Create(pc).Field<bool>("InputLocked").Value = true; any = true; }
            catch { }
            try { Traverse.Create(pc).Field<bool>("InteractDisabled").Value = true; any = true; }
            catch { }
            if (!any)
                LoggingService.LogWarning("[Dread] LockInput: no matching field found on PlayerController");
        }

        private static void UnlockInput(PlayerController pc)
        {
            bool any = false;
            try { Traverse.Create(pc).Field<bool>("inputLocked").Value = false; any = true; }
            catch { }
            try { Traverse.Create(pc).Field<bool>("interactDisabled").Value = false; any = true; }
            catch { }
            try { Traverse.Create(pc).Field<bool>("InputLocked").Value = false; any = true; }
            catch { }
            try { Traverse.Create(pc).Field<bool>("InteractDisabled").Value = false; any = true; }
            catch { }
            if (!any)
                LoggingService.LogWarning("[Dread] UnlockInput: no matching field found on PlayerController");
        }
    }
}
