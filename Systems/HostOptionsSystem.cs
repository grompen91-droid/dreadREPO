using System.Reflection;
using Dread.Config;
using ExitGames.Client.Photon;
using HarmonyLib;
using Photon.Pun;
using REPOLib.Modules;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Dread.Systems
{
    public class HostOptionsSystem : MonoBehaviour
    {
        private static NetworkedEvent? _gammaEvent;
        private static NetworkedEvent? _pixelationEvent;

        private void Start()
        {
            _gammaEvent = new NetworkedEvent("Dread.GammaForce", OnGammaReceived);
            _pixelationEvent = new NetworkedEvent("Dread.PixelationForce", OnPixelationReceived);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            bool isMenu = scene.name.Contains("Menu") || scene.name.Contains("Main");
            if (isMenu) return;
            if (!IsHost()) return;

            if (DreadConfig.GammaForceEnabled.Value)
                _gammaEvent?.RaiseEvent(
                    DreadConfig.GammaValue.Value,
                    NetworkingEvents.RaiseAll,
                    SendOptions.SendReliable);

            if (DreadConfig.PixelationForceEnabled.Value)
                _pixelationEvent?.RaiseEvent(
                    DreadConfig.PixelationValue.Value,
                    NetworkingEvents.RaiseAll,
                    SendOptions.SendReliable);
        }

        private static void OnGammaReceived(EventData data)
        {
            int gamma = (int)data.CustomData;
            ApplyGamma(gamma);
        }

        private static void ApplyGamma(int gamma)
        {
            gamma = Mathf.Clamp(gamma, 0, 100);

            var gm = FindObjectOfType<GraphicsManager>();
            if (gm == null)
            {
                Plugin.Logger.LogWarning("[Dread] GraphicsManager not found — storing gamma for later.");
                PlayerPrefs.SetInt("Gamma", gamma);
                PlayerPrefs.Save();
                return;
            }

            var t = Traverse.Create(gm);
            t.Field<int>("gamma").Value = gamma;

            var method = typeof(GraphicsManager).GetMethod(
                "UpdateGamma",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (method != null)
                method.Invoke(gm, null);
            else
                Plugin.Logger.LogWarning("[Dread] GraphicsManager.UpdateGamma not found.");
        }

        private static void OnPixelationReceived(EventData data)
        {
            int value = (int)data.CustomData;
            ApplyPixelation(value);
        }

        private static void ApplyPixelation(int percent)
        {
            percent = Mathf.Clamp(percent, 1, 100);
            float multiplier = percent / 100f;

            var gm = FindObjectOfType<GraphicsManager>();
            if (gm == null)
            {
                Plugin.Logger.LogWarning("[Dread] GraphicsManager not found — storing render size for later.");
                PlayerPrefs.SetFloat("RenderSize", multiplier);
                PlayerPrefs.Save();
                return;
            }

            var t = Traverse.Create(gm);
            t.Field<float>("_resolutionMultiplier").Value = multiplier;

            var method = typeof(GraphicsManager).GetMethod(
                "UpdateRenderSize",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (method != null)
                method.Invoke(gm, null);
            else
                Plugin.Logger.LogWarning("[Dread] GraphicsManager.UpdateRenderSize not found.");
        }

        private static bool IsHost()
        {
            try { return PhotonNetwork.IsMasterClient; }
            catch { return true; }
        }
    }
}
