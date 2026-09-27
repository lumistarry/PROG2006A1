#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace MoonPostman.EditorTools
{
    public static class StoryNavLayoutTool
    {
        private const string MenuRoot = "Moon Postman/";

        [MenuItem(MenuRoot + "Fix Nav Button Anchors (Current Scene)")]
        private static void FixCurrentScene()
        {
            int ok = FixButtonsInOpenScene(out int skipped);

            if (ok > 0)
            {
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                Debug.Log($"[StoryNavLayoutTool] Fixed {ok} nav button anchors in the current scene" +
                          (skipped > 0 ? $" ({skipped} skipped, see warnings above)" : "") +
                          ". Remember to save the scene (Ctrl+S).");
            }
            else
            {
                Debug.LogWarning("[StoryNavLayoutTool] No StoryNavButton to process in the current scene." +
                                 " Make sure all three buttons have the StoryNavButton component.");
            }
        }

        [MenuItem(MenuRoot + "Fix Nav Button Anchors (All 6 Scenes)")]
        private static void FixAllScenes()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            string originPath = EditorSceneManager.GetActiveScene().path;
            int total = 0;
            int scenes = 0;

            foreach (string sceneName in StoryPageManager.DefaultSceneNames)
            {
                string path = FindScenePath(sceneName);

                if (string.IsNullOrEmpty(path))
                {
                    Debug.LogWarning($"[StoryNavLayoutTool] Scene '{sceneName}' not found, skipping.");
                    continue;
                }

                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                int ok = FixButtonsInOpenScene(out int skipped);

                if (ok > 0)
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                }

                total += ok;
                scenes++;
                Debug.Log($"[StoryNavLayoutTool] {sceneName}: fixed {ok}" +
                          (skipped > 0 ? $", skipped {skipped}" : ""));
            }

            if (!string.IsNullOrEmpty(originPath) && File.Exists(originPath))
            {
                EditorSceneManager.OpenScene(originPath, OpenSceneMode.Single);
            }

            Debug.Log($"[StoryNavLayoutTool] Done: {scenes} scenes, {total} buttons fixed.");
        }

        [MenuItem(MenuRoot + "Make Selected Object Fill Its Parent")]
        private static void MakeSelectedFillParent()
        {
            var rt = Selection.activeTransform as RectTransform;

            if (rt == null)
            {
                Debug.LogWarning("[StoryNavLayoutTool] Select a UI object in the Hierarchy first (e.g. the background image Page).");
                return;
            }

            var fitter = rt.GetComponent<AspectRatioFitter>();
            if (fitter != null)
            {
                Debug.LogWarning(
                    $"[StoryNavLayoutTool] '{rt.name}' has an Aspect Ratio Fitter, which controls its anchors and size; manual settings will be overridden.\n" +
                    "Keep it to preserve the aspect ratio, or remove it if you want it to fill the screen.", rt);
            }

            Undo.RecordObject(rt, "Make Fill Parent");
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.localScale = Vector3.one;
            EditorUtility.SetDirty(rt);

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log($"[StoryNavLayoutTool] '{rt.name}' is now set to fill its parent (offsets zeroed). Remember Ctrl+S.");
        }

        private static int FixButtonsInOpenScene(out int skipped)
        {
            int ok = 0;
            skipped = 0;

            foreach (var nav in Object.FindObjectsOfType<StoryNavButton>(true))
            {
                if (nav == null) continue;

                var rt = nav.transform as RectTransform;
                if (rt == null) { skipped++; continue; }

                Undo.RecordObject(rt, "Fix Nav Button Anchor");

                if (MakeProportional(rt))
                {
                    EditorUtility.SetDirty(rt);
                    ok++;
                }
                else
                {
                    skipped++;
                    Debug.LogWarning($"[StoryNavLayoutTool] '{nav.name}' could not be converted, skipped.", nav);
                }
            }

            return ok;
        }

        private static bool MakeProportional(RectTransform rt)
        {
            var parent = rt.parent as RectTransform;
            if (parent == null) return false;

            if (rt.localRotation != Quaternion.identity)
            {
                Debug.LogWarning($"[StoryNavLayoutTool] '{rt.name}' is rotated; cannot convert safely. Set its Rotation to zero first.", rt);
                return false;
            }

            var pc = new Vector3[4];
            parent.GetWorldCorners(pc);

            float parentW = pc[2].x - pc[0].x;
            float parentH = pc[3].y - pc[0].y;
            if (parentW <= 0.001f || parentH <= 0.001f) return false;

            var c = new Vector3[4];
            rt.GetWorldCorners(c);

            float xMin = (c[0].x - pc[0].x) / parentW;
            float yMin = (c[0].y - pc[0].y) / parentH;
            float xMax = (c[2].x - pc[0].x) / parentW;
            float yMax = (c[2].y - pc[0].y) / parentH;

            if (xMax - xMin < 0.0001f || yMax - yMin < 0.0001f) return false;

            rt.anchorMin = new Vector2(Mathf.Clamp01(xMin), Mathf.Clamp01(yMin));
            rt.anchorMax = new Vector2(Mathf.Clamp01(xMax), Mathf.Clamp01(yMax));

            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.localScale = Vector3.one;

            Debug.Log($"[StoryNavLayoutTool] '{rt.name}' anchors set to " +
                      $"Min({rt.anchorMin.x:F4}, {rt.anchorMin.y:F4}) / " +
                      $"Max({rt.anchorMax.x:F4}, {rt.anchorMax.y:F4})", rt);
            return true;
        }

        private static string FindScenePath(string sceneName)
        {
            foreach (string guid in AssetDatabase.FindAssets(sceneName + " t:Scene"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetFileNameWithoutExtension(path) == sceneName) return path;
            }

            return null;
        }
    }
}
#endif
