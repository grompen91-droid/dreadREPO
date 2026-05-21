using Dread.Systems.Tension;
using UnityEngine;

namespace Dread.Systems
{
    // Thin coordinator: adds the proximity scanner and all tension feature components.
    // Each feature is self-contained and reads NearestDist from EnemyProximityScanner.
    public class TensionSystem : MonoBehaviour
    {
        private void Start()
        {
            gameObject.AddComponent<EnemyProximityScanner>();
            gameObject.AddComponent<AdrenalineFeature>();
            gameObject.AddComponent<LowStaminaFeature>();
            gameObject.AddComponent<PanicSprintFeature>();
            gameObject.AddComponent<FakeFootstepFeature>();
        }
    }
}
