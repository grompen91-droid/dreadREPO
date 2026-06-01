using System.Collections.Generic;
using Dread.Config;
using Dread.Systems.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Dread.Systems
{
    /// <summary>
    /// Anti-camping attraction. On the host, tracks how long each player stays
    /// isolated (far from any enemy). Once a player has camped past the configured
    /// threshold, enemies are drawn toward them with escalating reach until danger
    /// arrives, then a per-player cooldown applies before they can be targeted again.
    /// Works in solo (the lone player counts).
    ///
    /// Silent in normal play; surfaces a toast and overlay state only when the
    /// debug overlay is enabled.
    /// </summary>
    public class CampLureSystem : MonoBehaviour
    {
        private const float TickInterval = 1.0f;
        private const float PullInterval = 4.0f;
        private const float BasePullRadius = 25f;
        private const float RadiusPerStep = 15f;
        private const float MaxPullRadius = 90f;

        private struct PlayerCampState
        {
            public float CampTimer;
            public float CooldownUntil;
        }

        private readonly Dictionary<string, PlayerCampState> _playerStates = new();

        private float _nextTick;
        private float _nextPull;

        private string _targetLabel = "";
        private Vector3 _targetPos;
        private float _targetCamp;
        private int _pullStep;

        private void Start()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mode != LoadSceneMode.Single)
                return;

            GameplayPhaseCompat.ResetForSceneLoad();
            ResetState();
        }

        private void Update()
        {
            DreadRuntimeState.GameplayPhase = GameplayContext.PhaseLabel;

            if (!DreadFeaturePolicy.MonsterLureEnabled)
            {
                DreadRuntimeState.LureBlockReason = "disabled in config";
                return;
            }

            if (DreadConfig.CompatibilityMode.Value)
            {
                DreadRuntimeState.LureBlockReason = "compatibility mode";
                return;
            }

            if (!HarmonyPatchCompat.IsMasterClient())
            {
                DreadRuntimeState.LureBlockReason = "not host";
                return;
            }

            if (!GameplayContext.AllowsHostMonsterFeatures)
            {
                DreadRuntimeState.LureBlockReason = GameplayContext.PhaseLabel;
                ClearTarget();
                return;
            }

            DreadRuntimeState.LureBlockReason = "";

            float now = Time.time;
            if (now >= _nextTick)
            {
                _nextTick = now + TickInterval;
                Evaluate(now);
            }

            MaybePull(now);
        }

        private void Evaluate(float now)
        {
            if (!ProximityScan.HasEnemies())
            {
                ClearTarget();
                return;
            }

            var players = PlayerRosterCompat.GetPlayers();
            if (players.Count == 0)
            {
                ClearTarget();
                return;
            }

            float safe = DreadConfig.LureSafeDistance.Value;
            float threshold = DreadConfig.LureCampSeconds.Value;

            string bestLabel = "";
            Vector3 bestPos = Vector3.zero;
            float bestCamp = 0f;
            float bestDist = -1f;

            var seen = new HashSet<string>();
            foreach (var p in players)
            {
                seen.Add(p.Label);
                float nearest = ProximityScan.NearestDistance(p.Position);

                var state = _playerStates.TryGetValue(p.Label, out var prev)
                    ? prev
                    : default;

                if (now < state.CooldownUntil)
                {
                    state.CampTimer = 0f;
                    _playerStates[p.Label] = state;
                    continue;
                }

                state.CampTimer = nearest > safe ? state.CampTimer + TickInterval : 0f;
                _playerStates[p.Label] = state;

                if (state.CampTimer >= threshold && nearest > bestDist)
                {
                    bestDist = nearest;
                    bestLabel = p.Label;
                    bestPos = p.Position;
                    bestCamp = state.CampTimer;
                }
            }

            PruneStates(seen);

            if (bestLabel.Length == 0)
            {
                ClearTarget();
                return;
            }

            bool newTarget = bestLabel != _targetLabel;
            _targetLabel = bestLabel;
            _targetPos = bestPos;
            _targetCamp = bestCamp;
            float escalate = Mathf.Max(1f, DreadConfig.LureEscalateSeconds.Value);
            _pullStep = 1 + (int)((bestCamp - threshold) / escalate);

            if (newTarget)
            {
                _nextPull = 0f;
                NotifyDebug($"armed on {bestLabel}");
            }

            PublishState(now);
        }

        private void MaybePull(float now)
        {
            if (_targetLabel.Length == 0 || now < _nextPull)
                return;

            if (!ProximityScan.HasEnemies())
            {
                ClearTarget();
                return;
            }

            float safe = DreadConfig.LureSafeDistance.Value;
            float nearest = ProximityScan.NearestDistance(_targetPos);
            if (nearest <= safe)
            {
                ApplyContactCooldown(_targetLabel, now);
                ClearTarget();
                return;
            }

            _nextPull = now + PullInterval;
            float radius = Mathf.Min(MaxPullRadius, BasePullRadius + (_pullStep - 1) * RadiusPerStep);

            LoggingService.LogVerbose($"[CampLure] pull step {_pullStep} radius {radius:F0}");
            if (EnemyLureCompat.Pull(_targetPos, radius))
                NotifyDebug($"pulling {_targetLabel} (step {_pullStep}, r{radius:F0})");
        }

        private void ApplyContactCooldown(string label, float now)
        {
            if (label.Length == 0)
                return;

            var state = _playerStates.TryGetValue(label, out var prev) ? prev : default;
            state.CampTimer = 0f;
            state.CooldownUntil = now + DreadConfig.LureCooldownSeconds.Value;
            _playerStates[label] = state;
        }

        private void ClearTarget()
        {
            if (_targetLabel.Length == 0)
            {
                PublishState(Time.time);
                return;
            }

            _targetLabel = "";
            _targetCamp = 0f;
            _pullStep = 0;
            PublishState(Time.time);
        }

        private void ResetState()
        {
            _playerStates.Clear();
            _targetLabel = "";
            _targetCamp = 0f;
            _pullStep = 0;
            _nextTick = 0f;
            _nextPull = 0f;
            DreadRuntimeState.LureBlockReason = "";
            PublishState(Time.time);
        }

        private void PruneStates(HashSet<string> seen)
        {
            if (_playerStates.Count == seen.Count)
                return;

            var dead = new List<string>();
            foreach (var key in _playerStates.Keys)
            {
                if (!seen.Contains(key))
                    dead.Add(key);
            }

            foreach (var key in dead)
                _playerStates.Remove(key);
        }

        private void PublishState(float now)
        {
            DreadRuntimeState.LureTarget = _targetLabel;
            DreadRuntimeState.LureCampTimer = _targetCamp;
            DreadRuntimeState.LurePullStep = _pullStep;

            float cooldown = 0f;
            if (_targetLabel.Length > 0
                && _playerStates.TryGetValue(_targetLabel, out var state)
                && now < state.CooldownUntil)
            {
                cooldown = state.CooldownUntil - now;
            }

            DreadRuntimeState.LureCooldownRemaining = cooldown;
        }

        private static void NotifyDebug(string message)
        {
            if (!DreadConfig.DebugOverlayEnabled.Value)
                return;

            LoggingService.LogInfo("[CampLure] " + message);
            DreadNotificationSystem.Warn("Camp Lure", message);
        }
    }
}
