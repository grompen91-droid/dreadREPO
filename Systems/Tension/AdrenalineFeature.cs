using Dread.Config;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Dread.Systems.Tension
{
    internal class AdrenalineFeature : MonoBehaviour
    {
        private EnemyProximityScanner _scanner = null!;
        private float _originalDrain = -1f;

        private const float ProximityRange = 15f;

        private void Start()
        {
            _scanner = GetComponent<EnemyProximityScanner>();
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            RestoreDrain();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => _originalDrain = -1f;

        private void Update()
        {
            if (!DreadConfig.AdrenalineEnabled.Value || SemiFunc.MenuLevel()) return;

            var pc = PlayerController.instance;
            if ((object)pc == null) return;

            if (_originalDrain < 0f)
                _originalDrain = pc.EnergySprintDrain;

            float targetDrain = _scanner.NearestDist < ProximityRange
                ? _originalDrain * Mathf.Lerp(0.30f, 1f, _scanner.NearestDist / ProximityRange)
                : _originalDrain;

            pc.EnergySprintDrain = Mathf.Lerp(pc.EnergySprintDrain, targetDrain, Time.deltaTime * 1.2f);
        }

        private void RestoreDrain()
        {
            if (_originalDrain >= 0f && (object)PlayerController.instance != null)
                PlayerController.instance.EnergySprintDrain = _originalDrain;
        }
    }
}
