using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace NinjaVillage.EditorTools.Build
{
    /// <summary>
    /// Android builds from the menu or headlessly:
    /// <c>unity run . -- -executeMethod NinjaVillage.EditorTools.Build.AndroidBuilder.BuildDevApkBatch</c>.
    /// Enforces the settings the game depends on before every build: IL2CPP + ARM64, portrait only, the
    /// Firebase Gradle templates (the External Dependency Manager writes Firebase's libraries into them, and
    /// Unity ignores them unless these flags are on), and the debug keystore for development builds.
    /// </summary>
    public static class AndroidBuilder
    {
        public const string ApplicationId = "com.thun.ninjavillagedefense";
        public const string ProductName = "Ninja Village Defense";
        public const string DevApkPath = "Builds/Android/NinjaVillage-dev.apk";

        [MenuItem("Ninja Village/Build/Android Development APK", priority = 30)]
        public static void BuildDevApkMenu() => BuildDevApk();

        public static void BuildDevApkBatch() => EditorApplication.Exit(BuildDevApk() ? 0 : 1);

        public static bool BuildDevApk()
        {
            ApplyAndroidSettings();
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android &&
                !EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android))
            {
                Debug.LogError("[AndroidBuilder] Could not switch to Android (is Android Build Support installed?).");
                return false;
            }
            EditorUserBuildSettings.buildAppBundle = false; // APK for direct install; use an .aab for the Play Store

            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            if (scenes.Length == 0)
            {
                Debug.LogError("[AndroidBuilder] No scenes in the build list — run Ninja Village → Setup Everything first.");
                return false;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(DevApkPath));
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = DevApkPath,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.Development,
            });

            var summary = report.summary;
            Debug.Log($"[AndroidBuilder] {summary.result}: {summary.outputPath} " +
                      $"({summary.totalSize / (1024f * 1024f):0.0} MB, {summary.totalTime.TotalMinutes:0.0} min, " +
                      $"{summary.totalErrors} errors, {summary.totalWarnings} warnings)");
            if (summary.result != BuildResult.Succeeded)
            {
                foreach (var step in report.steps)
                    foreach (var message in step.messages)
                        if (message.type == LogType.Error || message.type == LogType.Exception)
                            Debug.LogError($"[AndroidBuilder] {step.name}: {message.content}");
            }
            return summary.result == BuildResult.Succeeded;
        }

        /// <summary>Settings the game relies on; safe to apply repeatedly.</summary>
        public static void ApplyAndroidSettings()
        {
            var android = NamedBuildTarget.Android;
            PlayerSettings.SetApplicationIdentifier(android, ApplicationId);
            PlayerSettings.productName = ProductName;
            PlayerSettings.SetScriptingBackend(android, ScriptingImplementation.IL2CPP); // required for ARM64
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.useCustomKeystore = false; // debug key for development builds

            // The whole UI and the joystick are laid out for portrait (1080×1920 reference).
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;

            // Firebase: the External Dependency Manager patched these templates with every Firebase library,
            // Maven repo and the AndroidX properties — they're only used when these flags are on.
            var playerSettings = new SerializedObject(Unsupported.GetSerializedAssetInterfaceSingleton("PlayerSettings"));
            foreach (var flag in new[] { "useCustomMainGradleTemplate", "useCustomGradlePropertiesTemplate", "useCustomGradleSettingsTemplate" })
            {
                var property = playerSettings.FindProperty(flag);
                if (property != null) property.boolValue = true;
                else Debug.LogWarning($"[AndroidBuilder] PlayerSettings has no '{flag}'.");
            }
            playerSettings.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
        }
    }
}
