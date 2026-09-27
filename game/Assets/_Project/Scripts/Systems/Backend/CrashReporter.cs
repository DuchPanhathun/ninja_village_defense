using UnityEngine;

namespace NinjaVillage.Systems.Backend
{
    /// <summary>
    /// Crashlytics bridge (EPIC 22 "Crashlytics"): forwards Unity errors and exceptions as Crashlytics
    /// logs/non-fatal exceptions so crash reports carry context. Rate-limited so an error spamming every
    /// frame can't flood the report. (Uncaught exceptions are reported as fatal by the provider.)
    /// </summary>
    public static class CrashReporter
    {
        private const int MaxReportsPerMinute = 20;
        private static float _windowStart;
        private static int _count;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            Application.logMessageReceived -= OnLog;
            Application.logMessageReceived += OnLog;
        }

        private static void OnLog(string condition, string stackTrace, LogType type)
        {
            if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
            var provider = BackendService.Provider;
            if (provider == null || !provider.IsOnline) return;

            float now = Time.realtimeSinceStartup;
            if (now - _windowStart > 60f)
            {
                _windowStart = now;
                _count = 0;
            }
            if (++_count > MaxReportsPerMinute) return;

            provider.CrashLog($"[{type}] {condition}");
            if (type == LogType.Exception)
                provider.RecordException(new System.Exception($"{condition}\n{stackTrace}"));
        }
    }
}
