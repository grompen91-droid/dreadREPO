using Dread.Systems.Core;
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
            PlayerInputLockCompat.SetLocked(pc, locked: true);
            if (!PlayerTumbleCompat.ApplyForcedTumble(pc))
                LoggingService.LogVerbose("[PsychoticBreak] Tumble lock unavailable; input lock only");
        }

        private static void EnsurePlayerFallenHeld(PlayerController? pc)
        {
            if ((object)pc == null)
                return;

            PlayerTumbleCompat.EnsureForcedTumbleHeld(pc);
            PlayerInputLockCompat.SetLocked(pc, locked: true);
        }

        private static void RestorePlayerControl(PlayerController? pc)
        {
            PlayerTumbleCompat.ReleaseForcedTumble(pc);
            if ((object)pc == null)
                return;

            PlayerInputLockCompat.SetLocked(pc, locked: false);
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

    }
}
