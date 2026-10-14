using System;
using System.IO;
using EchoFactory.Tests;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace EchoFactory.Editor
{
    public static class PrototypeTools
    {
        private const string ScenePath = "Assets/EchoFactory/Scenes/Factory.unity";
        private const string LayoutPath = "Assets/EchoFactory/Resources/UI/Game.uxml";
        [MenuItem("Echo Factory/Otwórz halę")]
        public static void OpenHall()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(ScenePath);
        }
        [MenuItem("Echo Factory/Edytuj wygląd gry (UI Builder)")]
        public static void OpenLayout()
        {
            var layout = AssetDatabase.LoadAssetAtPath<UnityEngine.UIElements.VisualTreeAsset>(LayoutPath);
            if (layout == null) { Debug.LogError("Nie znaleziono " + LayoutPath); return; }
            AssetDatabase.OpenAsset(layout);
        }
        [MenuItem("Echo Factory/Uruchom testy logiki")]
        public static void RunChecks() { Debug.Log(CoreChecks.Run()); Debug.Log("PASS: " + StrategyChecks.Run() + " strategic assertions."); }
        // Optional command-line gate: -batchmode -nographics -projectPath ... -executeMethod EchoFactory.Editor.PrototypeTools.CheckBatch -logFile ...
        public static void CheckBatch()
        {
            try { RunChecks(); EditorApplication.Exit(0); }
            catch (Exception error) { Debug.LogException(error); EditorApplication.Exit(1); }
        }
        [MenuItem("Echo Factory/Zbuduj prototyp Windows")]
        public static void BuildWindows()
        {
            string folder = EditorUtility.OpenFolderPanel("Folder na build", "", "");
            if (string.IsNullOrEmpty(folder)) return;
            Build(folder);
        }
        // ---------------------------------------------------------------- Android

        private const string AndroidPackage = "com.lemonadaq.echofactory";

        [MenuItem("Echo Factory/Zbuduj APK Android")]
        public static void BuildAndroidMenu()
        {
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
            {
                EditorUtility.DisplayDialog("Brak modułu Android",
                    "Zainstaluj w Unity Hub: Installs → 6000.3.18f1 → ⚙ → Add modules → Android Build Support (z OpenJDK oraz Android SDK & NDK Tools), potem uruchom Unity ponownie.", "OK");
                return;
            }
            string folder = EditorUtility.OpenFolderPanel("Folder na APK", "", "");
            if (string.IsNullOrEmpty(folder)) return;
            string apk = BuildAndroid(folder);
            EditorUtility.RevealInFinder(apk);
        }

        // Command line: -batchmode -quit -projectPath ... -executeMethod EchoFactory.Editor.PrototypeTools.BuildAndroidBatch -buildFolder <dir>
        public static void BuildAndroidBatch()
        {
            string folder = "Builds/Android";
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++) if (args[i] == "-buildFolder") folder = args[i + 1];
            try { BuildAndroid(folder); EditorApplication.Exit(0); }
            catch (Exception error) { Debug.LogException(error); EditorApplication.Exit(1); }
        }

        private static string BuildAndroid(string folder)
        {
            var android = UnityEditor.Build.NamedBuildTarget.Android;
            PlayerSettings.SetApplicationIdentifier(android, AndroidPackage);
            // Modern phones are 64-bit only, which requires IL2CPP.
            PlayerSettings.SetScriptingBackend(android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            // The interface is designed for landscape.
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            EditorUserBuildSettings.buildAppBundle = false; // plain APK to install directly on a phone

            Directory.CreateDirectory(folder);
            string apk = Path.Combine(folder, "EchoFactory.apk");
            var result = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = new[] { ScenePath }, locationPathName = apk, target = BuildTarget.Android, options = BuildOptions.None });
            if (result.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded) throw new Exception("Build Android nie powiódł się. Sprawdź Console.");
            Debug.Log("APK gotowe: " + Path.GetFullPath(apk));
            return apk;
        }

        private static void Build(string folder)
        {
            var result = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = new[] { ScenePath }, locationPathName = Path.Combine(folder, "EchoFactory.exe"), target = BuildTarget.StandaloneWindows64, options = BuildOptions.Development });
            if (result.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded) throw new Exception("Build nie powiódł się. Sprawdź Console.");
        }
    }
}
