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
    /// arrives, then the timer resets. Works in solo (the lone player counts).
    ///
    /// Silent in normal play; surfaces a toast and overlay state only when the
    /// debug overlay is enabled.
    /// </summary>
    public class CampLureSystem : MonoBehaviour
    {
        private const float TickInterval = 1.0f;   // re-evaluate camp state each second
        private const float PullInterval = 4.0f;   // re-issue the lure this often while active
        private const float EscalateEvery = 30f;   // grow the pull every N seconds past threshold
        private const float BasePullRadius = 25f;  // investigate radius at step 1
        private const float RadiusPerStep = 15f;   // added reach per escalation step
        private const float MaxPullRadius = 90f;

        private readonly Dictionary<string, float> _campTimers = new();

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

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => ResetState();

        private void Update()
        {
            if (!DreadConfig.MonsterLureEnabled.Value || DreadConfig.CompatibilityMode.Value)
                return;
            if (!GameplayContext.IsRun())
                return;
            if (!HarmonyPatchCompat.IsMasterClient())
                return;

            float now = Time.time;
            if (now >= _nextTick)
            {
                _nextTick = now + TickInterval;
                Evaluate();
            }

            MaybePull(now);
        }

        private void Evaluate()
        {
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
            float bestDist = -1f; // most isolated = largest nearest-enemy distance

            var seen = new HashSet<string>();
            foreach (var p in players)
            {
                seen.Add(p.Label);
                float nearest = ProximityScan.NearestDistance(p.Position);

                float timer = _campTimers.TryGetValue(p.Label, out var prev) ? prev : 0f;
                timer = nearest > safe ? timer + TickInterval : 0f;
                _campTimers[p.Label] = timer;

                if (timer >= threshold && nearest > bestDist)
                {
                    bestDist = nearest;
                    bestLabel = p.Label;
                    bestPos = p.Position;
                    bestCamp = timer;
                }
            }

            PruneTimers(seen);

            if (bestLabel.Length == 0)
            {
                ClearTarget();
                return;
            }

            bool newTarget = bestLabel != _targetLabel;
            _targetLabel = bestLabel;
            _targetPos = bestPos;
            _targetCamp = bestCamp;
            _pullStep = 1 + (int)((bestCamp - threshold) / EscalateEvery);

            if (newTarget)
            {
                _nextPull = 0f; // pull immediately on a fresh target
                NotifyDebug($"armed on {bestLabel}");
            }

            PublishState();
        }

        private void MaybePull(float now)
        {
            if (_targetLabel.Length == 0 || now < _nextPull)
                return;

            _nextPull = now + PullInterval;
            float radius = Mathf.Min(MaxPullRadius, BasePullRadius + (_pullStep - 1) * RadiusPerStep);

            if (EnemyLureCompat.Pull(_targetPos, radius))
                NotifyDebug($"pulling {_targetLabel} (step {_pullStep}, r{radius:F0})");
        }

        private void ClearTarget()
        {
            if (_targetLabel.Length == 0)
                return;

            _targetLabel = "";
            _targetCamp = 0f;
            _pullStep = 0;
            PublishState();
        }

        private void ResetState()
        {
            _campTimers.Clear();
            _targetLabel = "";
            _targetCamp = 0f;
            _pullStep = 0;
            _nextTick = 0f;
            _nextPull = 0f;
            PublishState();
        }

        private void PruneTimers(HashSet<string> seen)
        {
            if (_campTimers.Count == seen.Count)
                return;

            var dead = new List<string>();
            foreach (var key in _campTimers.Keys)
            {
                if (!seen.Contains(key))
                    dead.Add(key);
            }

            foreach (var key in dead)
                _campTimers.Remove(key);
        }

        private void PublishState()
        {
            DreadRuntimeState.LureTarget = _targetLabel;
            DreadRuntimeState.LureCampTimer = _targetCamp;
            DreadRuntimeState.LurePullStep = _pullStep;
        }

        // Visible only with the debug overlay enabled: a log line plus a toast.
        private static void NotifyDebug(string message)
        {
            if (!DreadConfig.DebugOverlayEnabled.Value)
                return;

            LoggingService.LogInfo("[CampLure] " + message);
            DreadNotificationSystem.Warn("Camp Lure", message);
        }
    }
}
