using System;
using Dread.Config;
using UnityEngine;

namespace Dread.Systems
{
    public class TestCrashSystem : MonoBehaviour
    {
        private void Update()
        {
            if (!DreadConfig.TestCrashButton.Value)
                return;

            DreadConfig.TestCrashButton.Value = false;
            throw new InvalidOperationException(
                "[Dread TestCrash] Game crashed deliberately via the 'Crash Game' config button to verify error reporting."
            );
        }
    }
}
