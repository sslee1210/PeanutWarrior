#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace PeanutWarrior.EditorTools
{
    public static class PeanutWarriorBuildSetup
    {
        private const string AndroidIdentifier = "com.infact.peanutwarrior";
        private const string IosIdentifier = "com.infact.peanutwarrior";

        [MenuItem("Peanut Warrior/Build/Apply Android Settings")]
        public static void ApplyAndroidSettings()
        {
            PlayerSettings.productName = "땅콩전사 키우기";
            PlayerSettings.companyName = "IN-FACT";
            PlayerSettings.bundleVersion = "0.1.0";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, AndroidIdentifier);
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.runInBackground = true;
            PlayerSettings.resizableWindow = true;
            EditorUtility.DisplayDialog("땅콩전사", "Android 가로 화면·IL2CPP·ARM64 설정을 적용했습니다.", "확인");
        }

        [MenuItem("Peanut Warrior/Build/Apply iOS Settings")]
        public static void ApplyIosSettings()
        {
            PlayerSettings.productName = "땅콩전사 키우기";
            PlayerSettings.companyName = "IN-FACT";
            PlayerSettings.bundleVersion = "0.1.0";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, IosIdentifier);
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.iOS, ScriptingImplementation.IL2CPP);
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.iOS.buildNumber = "1";
            PlayerSettings.iOS.targetDevice = iOSTargetDevice.iPhoneAndiPad;
            EditorUtility.DisplayDialog("땅콩전사", "iOS 가로 화면·IL2CPP 설정을 적용했습니다. 최종 서명은 macOS와 Xcode가 필요합니다.", "확인");
        }

        [MenuItem("Peanut Warrior/Build/Build Android Development APK")]
        public static void BuildAndroidDevelopmentApk()
        {
            ApplyAndroidSettings();
            string[] scenes = EnabledScenes();
            if (scenes.Length == 0)
            {
                EditorUtility.DisplayDialog("땅콩전사", "Build Settings에 활성화된 Scene이 없습니다.", "확인");
                return;
            }

            string directory = Path.Combine(Directory.GetCurrentDirectory(), "Builds", "Android");
            Directory.CreateDirectory(directory);
            string path = Path.Combine(directory, "PeanutWarrior-development.apk");
            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = path,
                target = BuildTarget.Android,
                options = BuildOptions.Development | BuildOptions.AllowDebugging
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            string message = report.summary.result == BuildResult.Succeeded
                ? $"개발 APK 생성 완료\n{path}\n크기 {report.summary.totalSize / (1024f * 1024f):0.0} MB"
                : $"Android 빌드 실패\n결과: {report.summary.result}\nConsole을 확인하십시오.";
            EditorUtility.DisplayDialog("땅콩전사 Android Build", message, "확인");
        }

        [MenuItem("Peanut Warrior/Validation/Run EditMode Tests")]
        public static void OpenTestRunner()
        {
            EditorApplication.ExecuteMenuItem("Window/General/Test Runner");
        }

        private static string[] EnabledScenes()
        {
            var scenes = new System.Collections.Generic.List<string>();
            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            {
                if (scene.enabled && !string.IsNullOrEmpty(scene.path)) scenes.Add(scene.path);
            }
            return scenes.ToArray();
        }
    }
}
#endif
