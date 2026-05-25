using HarmonyLib;

namespace Dread.Systems
{
    internal static class EnemyHealthCompat
    {
        internal static bool IsAlive(EnemyHealth enemy)
        {
            if (enemy == null)
                return false;

            try
            {
                var hp = Traverse.Create(enemy).Property<int>("CurrentHealth").Value;
                return hp > 0;
            }
            catch
            {
                try
                {
                    var hp = Traverse.Create(enemy).Field<int>("currentHealth").Value;
                    return hp > 0;
                }
                catch
                {
                    var go = Traverse.Create(enemy).Property<UnityEngine.GameObject>("gameObject").Value;
                    return go != null && go.activeInHierarchy;
                }
            }
        }
    }
}
