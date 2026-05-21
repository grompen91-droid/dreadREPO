using Dread.Config;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Dread.Systems.Tension
{
    internal class PanicSprintFeature : MonoBehaviour
    {
        private EnemyProximityScanner _scanner = null!;
        private bool _wasSprinting;
        private bool _panicActive;
        private float _panicTimer;
        private float _panicCooldown;
        private float _originalSprintMultiplier = -1f;

        private const float ProximityRange = 15f;

        private void Start()
        {
            _scanner = GetComponent<EnemyProximityScanner>();
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDestroy() => SceneManager.sceneLoaded -= OnSceneLoaded;

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            _panicActive = false;
            _panicTimer = 0f;
            _panicCooldown = 0f;
            _originalSprintMultiplier = -1f;
            _wasSprinting = false;
        }

        private void Update()
        {
            if (!DreadConfig.PanicSprintEnabled.Value || SemiFunc.MenuLevel()) return;

            var pc = PlayerController.instance;
            if ((object)pc == null) return;

            _panicCooldown -= Time.deltaTime;
            bool currentlySprinting = pc.sprinting;

            if (_panicActive)
            {
                _panicTimer -= Time.deltaTime;
                if (_panicTimer <= 0f)
                {
                    _panicActive = false;
                    _panicCooldown = 20f;
                    if (_originalSprintMultiplier >= 0f)
                    {
                        Traverse.Create(pc).Field<float>("SprintSpeedMultiplier").Value = _originalSprintMultiplier;
                        _originalSprintMultiplier = -1f;
                    }
                }
            }
            else if (!_wasSprinting && currentlySprinting && _scanner.NearestDist < ProximityRange && _panicCooldown <= 0f)
            {
                var tpc = Traverse.Create(pc);
                _originalSprintMultiplier = tpc.Field<float>("SprintSpeedMultiplier").Value;
                tpc.Field<float>("SprintSpeedMultiplier").Value = _originalSprintMultiplier * 1.25f;
                _panicActive = true;
                _panicTimer = 2f;
            }

            _wasSprinting = currentlySprinting;
        }
    }
}
