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
        [MenuItem("Echo Factory/Otwórz halę")]
        public static void OpenHall()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(ScenePath);
        }
        [MenuItem("Echo Factory/Uruchom testy logiki")]
        public static void RunChecks() { Debug.Log(CoreChecks.Run()); }
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
        private static void Build(string folder)
        {
            var result = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = new[] { ScenePath }, locationPathName = Path.Combine(folder, "EchoFactory.exe"), target = BuildTarget.StandaloneWindows64, options = BuildOptions.Development });
            if (result.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded) throw new Exception("Build nie powiódł się. Sprawdź Console.");
        }
    }
}
