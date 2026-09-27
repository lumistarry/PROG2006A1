#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace MoonPostman.EditorTools
{
    public static class StorySceneBuildSettings
    {
        [MenuItem("Moon Postman/Add Story Scenes To Build Settings")]
        private static void AddStoryScenes()
        {
            string[] names = StoryPageManager.DefaultSceneNames;

            var found = new List<string>();
            var missing = new List<string>();

            foreach (string name in names)
            {
                string path = FindScenePath(name);

                if (string.IsNullOrEmpty(path)) missing.Add(name);
                else if (!found.Contains(path)) found.Add(path);
            }

            if (found.Count == 0)
            {
                Debug.LogError(
                    "[StorySceneBuildSettings] No scenes found. Make sure the 6 .unity files are under Assets (e.g. Assets/Scenes).");
                return;
            }

            var scenes = new List<EditorBuildSettingsScene>();
            foreach (string path in found) scenes.Add(new EditorBuildSettingsScene(path, true));

            EditorBuildSettings.scenes = scenes.ToArray();

            string message = "[StorySceneBuildSettings] Build Settings updated (in order):\n  " +
                             string.Join("\n  ", found);
            if (missing.Count > 0)
            {
                message += "\nMissing scenes (check file names): " + string.Join(", ", missing);
                Debug.LogWarning(message);
            }
            else
            {
                Debug.Log(message);
            }

            EditorApplication.RepaintHierarchyWindow();
        }

        private static string FindScenePath(string sceneName)
        {
            string[] guids = AssetDatabase.FindAssets(sceneName + " t:Scene");

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetFileNameWithoutExtension(path) == sceneName) return path;
            }

            return null;
        }
    }
}
#endif
