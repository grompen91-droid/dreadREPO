using UnityEngine;
using UnityEngine.AI;

public class EnemyHealth : MonoBehaviour
{
    public int CurrentHealth { get; }
}
public class EnemyParent : MonoBehaviour { }
public class EnemyNavMeshAgent : MonoBehaviour
{
    public NavMeshAgent Agent;
    public float DefaultSpeed;
    public float DefaultAcceleration;
}
public class EnemyDirector : MonoBehaviour
{
    public void SetInvestigate(ref float radius) { }
}
public class PlayerController : MonoBehaviour
{
    public static PlayerController instance;
    public float CrouchSpeed;
    public float EnergySprintDrain;
    public bool sprinting;
    public float EnergyCurrent;
    public float EnergyStart;
    public float SprintSpeedMultiplier;
    public int Health { get; }
    public float stamina { get; }
}
public static class SemiFunc
{
    public static bool MenuLevel() => false;
    public static bool IsMasterClient() => false;
}
