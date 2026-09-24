using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace NinjaVillage.EditorTools
{
    /// <summary>
    /// Installs packages through the Package Manager API (not by hand-editing manifest.json), usable from
    /// the menu or headlessly: <c>unity run -- -executeMethod NinjaVillage.EditorTools.PackageSetup.InstallPurchasingBatch</c>.
    /// </summary>
    public static class PackageSetup
    {
        public const string PurchasingPackage = "com.unity.purchasing";

        private static AddRequest _request;
        private static bool _exitWhenDone;

        [MenuItem("Ninja Village/Packages/Install Unity IAP", priority = 40)]
        public static void InstallPurchasing() => Begin(PurchasingPackage, exitWhenDone: false);

        /// <summary>
        /// Batch mode passes -quit, so the Editor would exit as soon as this returns — block until the
        /// Package Manager finishes instead of polling from EditorApplication.update.
        /// </summary>
        public static void InstallPurchasingBatch()
        {
            Debug.Log($"[PackageSetup] Adding {PurchasingPackage} (blocking)...");
            var request = Client.Add(PurchasingPackage);
            var timeout = System.DateTime.UtcNow.AddMinutes(10);
            while (!request.IsCompleted && System.DateTime.UtcNow < timeout)
                System.Threading.Thread.Sleep(200);

            if (request.IsCompleted && request.Status == StatusCode.Success)
            {
                Debug.Log($"[PackageSetup] Installed {request.Result.packageId} (version {request.Result.version}).");
                EditorApplication.Exit(0);
            }
            else
            {
                Debug.LogError($"[PackageSetup] Failed: {(request.IsCompleted ? request.Error?.message : "timed out")}");
                EditorApplication.Exit(1);
            }
        }

        private static void Begin(string packageId, bool exitWhenDone)
        {
            _exitWhenDone = exitWhenDone;
            Debug.Log($"[PackageSetup] Adding {packageId}...");
            _request = Client.Add(packageId);
            EditorApplication.update += Poll;
        }

        private static void Poll()
        {
            if (_request == null || !_request.IsCompleted) return;
            EditorApplication.update -= Poll;

            int exitCode = 0;
            if (_request.Status == StatusCode.Success)
            {
                Debug.Log($"[PackageSetup] Installed {_request.Result.packageId} (version {_request.Result.version}).");
            }
            else
            {
                Debug.LogError($"[PackageSetup] Failed: {_request.Error?.message}");
                exitCode = 1;
            }
            _request = null;
            if (_exitWhenDone) EditorApplication.Exit(exitCode);
        }
    }
}
