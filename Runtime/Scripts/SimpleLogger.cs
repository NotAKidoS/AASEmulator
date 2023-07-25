using UnityEngine;

namespace NAK.AASEmulator.Runtime.Scripts
{
    public static class SimpleLogger
    {
        private const string projectName = nameof(AASEmulator);
        private const string messageColor = "orange";

        public static void Log(string message, Object context = null)
        {
            Debug.Log($"<color={messageColor}>[{projectName}]</color> : {message}", context);
        }

        public static void LogWarning(string message, Object context = null)
        {
            Debug.LogWarning($"<color={messageColor}>[{projectName}]</color> : {message}", context);
        }

        public static void LogError(string message, Object context = null)
        {
            Debug.LogError($"<color={messageColor}>[{projectName}]</color> : {message}", context);
        }
    }
}