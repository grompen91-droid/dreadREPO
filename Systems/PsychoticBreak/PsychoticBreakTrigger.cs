using System;
using Dread.Config;
using Dread.Systems.Core;
using UnityEngine;

namespace Dread.Systems
{
    public partial class PsychoticBreakSystem
    {
        private string? GetTriggerBlockReason()
        {
            if (!_enabled)
                return "disabled";
            if (DreadConfig.CompatibilityMode.Value)
                return "compatibility mode";
            if (SemiFunc.MenuLevel())
                return "menu level";
            if (_oncePerMatch && _hasTriggeredThisMatch)
                return "once per match";
            if (!AreClipsLoaded())
                return "clips not loaded";

            var pc = PlayerController.instance;
            if ((object)pc == null)
                return "no player";
            if (!IsSolo(pc))
                return "not solo";
            if (!HasRecentThreat())
                return "no recent threat";
            if (IsAnyEnemyVisibleCached())
                return "visible enemy";
            if (!IsHidingVulnerable(pc))
                return "not hiding";

            return null;
        }

        private void UpdateThreatTimestamps()
        {
            if (_mainCam == null)
                _mainCam = Camera.main;

            var origin = GetThreatScanOrigin();
            if (!origin.HasValue)
                return;

            _cachedEnemies = EnemyScanCache.GetEnemies();
            foreach (var e in _cachedEnemies)
            {
                if (!EnemyHealthCompat.IsValid(e))
                    continue;

                float d = Vector3.Distance(origin.Value, EnemyScanCache.GetFocusPosition(e));
                if (d < ThreatRange)
                {
                    _threatMemoryUntil = Time.time + ThreatMemoryDuration;
                    return;
                }
            }
        }

        private bool HasRecentThreat() => Time.time < _threatMemoryUntil;

        private int GetThreatMemorySecondsRemaining()
        {
            if (!HasRecentThreat())
                return 0;
            return (int)Math.Ceiling(_threatMemoryUntil - Time.time);
        }

        private Vector3? GetThreatScanOrigin()
        {
            var pc = PlayerController.instance;
            if ((object)pc != null)
            {
                try
                {
                    return pc.transform.position;
                }
                catch { }
            }

            if (_mainCam == null)
                _mainCam = Camera.main;
            return _mainCam != null ? _mainCam.transform.position : null;
        }

        private bool CanTrigger()
        {
            LoggingService.LogVerbose("[PsychoticBreak] Checking trigger condition...");
            var pc = PlayerController.instance;
            if ((object)pc == null) return false;

            if (!IsSolo(pc)) return false;
            if (!HasRecentThreat()) return false;
            if (IsAnyEnemyVisibleCached()) return false;
            if (!IsHidingVulnerable(pc)) return false;
            if (!AreClipsLoaded()) return false;

            return true;
        }

        private static PlayerController[]? _cachedPlayers;
        private static float _nextPlayerRefresh;

        private static bool IsSolo(PlayerController pc)
        {
            if (Time.time >= _nextPlayerRefresh)
            {
                _nextPlayerRefresh = Time.time + 2f;
                _cachedPlayers = FindObjectsOfType<PlayerController>();
            }

            var players = _cachedPlayers;
            if (players == null)
                return true;

            foreach (var other in players)
            {
                if ((object)other == null || (object)other == (object)pc) continue;
                if (!IsPlayerAlive(other)) continue;
                if (Vector3.Distance(pc.transform.position, other.transform.position) < SoloRange)
                    return false;
            }
            return true;
        }

        private static bool IsPlayerAlive(PlayerController pc) =>
            PlayerControllerCompat.IsAlive(pc);

        private bool IsAnyEnemyVisibleCached()
        {
            if (Time.time < _nextVisibilityRefresh)
                return _cachedAnyEnemyVisible;

            _nextVisibilityRefresh = Time.time + VisibilityRefreshInterval;
            _cachedAnyEnemyVisible = IsAnyEnemyVisible(_cachedEnemies);
            return _cachedAnyEnemyVisible;
        }

        private bool IsAnyEnemyVisible(EnemyHealth[]? enemies)
        {
            var cam = _mainCam;
            if (cam == null || enemies == null) return false;

            Vector3 origin;
            try
            {
                origin = cam.transform.position;
            }
            catch
            {
                return false;
            }

            for (int i = 0; i < enemies.Length; i++)
            {
                var e = enemies[i];
                if (!EnemyHealthCompat.IsValid(e))
                    continue;

                try
                {
                    if (!EnemyHealthCompat.IsAliveForVisibility(e))
                        continue;

                    var target = EnemyScanCache.GetFocusPosition(e);

                    if (Vector3.Distance(origin, target) < 0.75f)
                        return true;

                    if (Physics.Linecast(origin, target, out var hit, VisionBlockMask, QueryTriggerInteraction.Ignore))
                    {
                        if (hit.collider != null && hit.collider.transform.IsChildOf(e.transform))
                            return true;
                        continue;
                    }

                    return true;
                }
                catch
                {
                    continue;
                }
            }

            return false;
        }

        private static bool IsHidingVulnerable(PlayerController pc) =>
            PlayerControllerCompat.IsHidingVulnerable(pc);
    }
}
