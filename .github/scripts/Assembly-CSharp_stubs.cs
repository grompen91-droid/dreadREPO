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
    public float Health { get; }
    public float stamina { get; }
}
public static class SemiFunc
{
    public static bool MenuLevel() => false;
    public static bool IsMasterClient() => false;
    public static void OnLevelGenDone() { }
    public static bool RunIsLobbyMenu() => false;
    public static bool RunIsShop() => false;
    public static bool IsMainMenu() => false;
    public static bool IsSplashScreen() => false;
    public static bool TruckLevel() => false;
    public static bool RunLevel() => false;
}
public static class SharedSceneData
{
    public static bool IsInShop;
    public static bool IsInLobby;
    public static bool IsInTruckLobby;
    public static bool IsInGame;
    public static bool IsInMainMenu;
}
