using Dread.Config;

namespace Dread.Systems
{
    /// <summary>
    /// Runtime gate for error report enqueue and send (ERR-2 first-run prompt).
    /// </summary>
    internal static class ErrorReportingConsent
    {
        public static bool IsReportingAllowed()
        {
            if (!DreadConfig.ErrorReportingEnabled.Value)
                return false;

            if (!DreadConfig.ErrorReportingPromptShown.Value)
                return false;

            return true;
        }
    }
}
