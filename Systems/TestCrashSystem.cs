using System;
using Dread.Config;
using UnityEngine;

namespace Dread.Systems
{
    public class TestCrashSystem : MonoBehaviour
    {
        private void Start()
        {
            DreadConfig.TestCrashButton.SettingChanged += OnTestCrashRequested;
        }

        private void OnDestroy()
        {
            DreadConfig.TestCrashButton.SettingChanged -= OnTestCrashRequested;
        }

        private void OnTestCrashRequested(object? sender, EventArgs e)
        {
            if (!DreadConfig.TestCrashButton.Value)
                return;

            TriggerCrash();
        }

        /// <summary>Debug server / MCP entry point for deliberate crash testing.</summary>
        public static void TriggerForDebug()
        {
            TriggerCrash();
        }

        private static void TriggerCrash()
        {
            DreadConfig.TestCrashButton.Value = false;
            throw new InvalidOperationException(
                "[Dread TestCrash] Game crashed deliberately via the "
                    + "'Crash Game' config button to verify error reporting."
            );
        }
    }
}
