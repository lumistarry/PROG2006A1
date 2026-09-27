using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MoonPostman
{
    public enum BoundaryMode
    {
        Disable = 0,

        Hide = 1,

        Wrap = 2
    }

    [DisallowMultipleComponent]
    public class StoryPageManager : MonoBehaviour
    {
        public static readonly string[] DefaultSceneNames =
        {
            "HomePage", "Scene1", "Scene2", "Scene3", "Scene4", "Credits"
        };

        private static StoryPageManager _instance;
        private static bool _buildCheckDone;

        public static StoryPageManager Instance
        {
            get
            {
                if (_instance == null) _instance = FindObjectOfType<StoryPageManager>();
                return _instance;
            }
        }

        [SerializeField] private List<string> sceneNames = new List<string>();

        [SerializeField] private BoundaryMode boundaryMode = BoundaryMode.Disable;

        [SerializeField] private float fadeDuration = 0.3f;

        [SerializeField] private Color fadeColor = new Color(0.02f, 0.02f, 0.06f, 1f);

        [SerializeField] private bool fadeInOnStart = true;

        [SerializeField] private bool enableKeyboardShortcuts = true;

        [SerializeField] private bool autoFixUiSetup = true;

        [SerializeField] private bool logDiagnostics = true;

        private readonly List<string> _scenes = new List<string>();
        private int _currentIndex = -1;
        private StorySceneFader _fader;

        public int PageCount => _scenes.Count;

        public int CurrentIndex => _currentIndex;

        public BoundaryMode BoundaryRule => boundaryMode;

        public string CurrentSceneName => gameObject.scene.name;

        private void Awake()
        {
            if (_instance != null && _instance != this && _instance.gameObject.scene == gameObject.scene)
            {
                Debug.LogWarning("[StoryPageManager] Multiple StoryPageManager components found in the same scene. Keep only one.", this);
            }

            _instance = this;

            BuildSceneList();
            ResolveCurrentIndex();

            if (autoFixUiSetup) { EnsureEventSystem(); EnsureGraphicRaycaster(); }
            if (!_buildCheckDone) { _buildCheckDone = true; CheckBuildSettings(); }

            _fader = StorySceneFader.GetOrCreate(fadeColor);
        }

        private void Start()
        {
            RefreshNavButtons();

            if (logDiagnostics) LogDiagnostics();

            if (_fader == null) return;

            if (fadeInOnStart && !StorySceneFader.HasPlayedOpeningFade)
            {
                StorySceneFader.HasPlayedOpeningFade = true;
                _fader.PlayOpeningFade(fadeDuration);
            }
            else
            {
                _fader.FadeInIfCovered(fadeDuration);
            }
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        private void Update()
        {
            if (!enableKeyboardShortcuts || _scenes.Count == 0) return;

            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Home)) GoHome();
            else if (Input.GetKeyDown(KeyCode.LeftArrow)) GoBack();
            else if (Input.GetKeyDown(KeyCode.RightArrow)) GoNext();
        }

        public void GoHome() => GoTo(0);

        public void GoBack()
        {
            if (_currentIndex < 0 || _scenes.Count == 0) return;

            int target = _currentIndex - 1;
            if (target < 0)
            {
                if (boundaryMode != BoundaryMode.Wrap) return;
                target = _scenes.Count - 1;
            }
            GoTo(target);
        }

        public void GoNext()
        {
            if (_currentIndex < 0 || _scenes.Count == 0) return;

            int target = _currentIndex + 1;
            if (target >= _scenes.Count)
            {
                if (boundaryMode != BoundaryMode.Wrap) return;
                target = 0;
            }
            GoTo(target);
        }

        public void Restart() => GoTo(0);

        public void GoTo(int index)
        {
            if (_scenes.Count == 0) return;

            index = Mathf.Clamp(index, 0, _scenes.Count - 1);
            if (index == _currentIndex) return;

            GoToScene(_scenes[index]);
        }

        public void GoToScene(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName)) return;

            if (_fader == null) _fader = StorySceneFader.GetOrCreate(fadeColor);
            if (_fader == null) return;

            _fader.FadeOutAndLoad(sceneName, fadeDuration);
        }

        public void RefreshNavButtons()
        {
            var buttons = FindObjectsOfType<StoryNavButton>(true);
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] != null) buttons[i].RefreshState();
            }
        }

        public bool CanPerform(NavAction action, int targetIndex = -1)
        {
            if (_scenes.Count == 0) return false;
            if (_currentIndex < 0) return false;

            switch (action)
            {
                case NavAction.Home:
                    return _currentIndex > 0;

                case NavAction.Back:
                    return boundaryMode == BoundaryMode.Wrap || _currentIndex > 0;

                case NavAction.Next:
                    return boundaryMode == BoundaryMode.Wrap || _currentIndex < _scenes.Count - 1;

                case NavAction.Restart:
                    return true;

                case NavAction.GoToIndex:
                    return Mathf.Clamp(targetIndex, 0, _scenes.Count - 1) != _currentIndex;

                default:
                    return false;
            }
        }

        private void LogDiagnostics()
        {
            var canvas = GetComponentInParent<Canvas>();
            var canvasRect = canvas != null ? canvas.GetComponent<RectTransform>() : null;

            float canvasW = canvasRect != null ? canvasRect.rect.width : -1f;
            float canvasH = canvasRect != null ? canvasRect.rect.height : -1f;
            float screenAspect = (float)Screen.width / Mathf.Max(1, Screen.height);

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"[MoonPostman Diagnostic] Scene = {CurrentSceneName}, page index = {_currentIndex} / {_scenes.Count - 1}");
            sb.AppendLine($"    Screen {Screen.width} x {Screen.height} (aspect {screenAspect:F3})");
            sb.AppendLine($"    Canvas {canvasW:F0} x {canvasH:F0} (aspect {(canvasH > 0f ? canvasW / canvasH : 0f):F3})");
            sb.AppendLine($"    BoundaryMode {boundaryMode}, fade {fadeDuration}s");

            var buttons = FindObjectsOfType<StoryNavButton>(true);
            if (buttons.Length == 0)
            {
                sb.AppendLine("    Warning: no StoryNavButton found in this scene.");
            }

            for (int i = 0; i < buttons.Length; i++)
            {
                var b = buttons[i];
                var rt = b.transform as RectTransform;
                var btn = b.GetComponent<Button>();

                if (rt == null)
                {
                    sb.AppendLine($"    [{b.name}] missing RectTransform");
                    continue;
                }

                var corners = new Vector3[4];
                rt.GetWorldCorners(corners);

                Vector3 center = (corners[0] + corners[2]) * 0.5f;
                float w = Vector2.Distance(corners[0], corners[3]);
                float h = Vector2.Distance(corners[0], corners[1]);

                bool onScreen = center.x >= 0f && center.x <= Screen.width &&
                                center.y >= 0f && center.y <= Screen.height;

                sb.AppendLine(
                    $"    [{b.name}] Action={b.Action} active={b.gameObject.activeSelf} " +
                    $"interactable={(btn != null ? btn.interactable.ToString() : "no Button")} " +
                    $"center=({center.x:F0}, {center.y:F0}) size={w:F0}x{h:F0} " +
                    (onScreen ? "on screen" : "OFF SCREEN"));
            }

            Debug.Log(sb.ToString(), this);
        }

        private void BuildSceneList()
        {
            _scenes.Clear();

            if (sceneNames != null)
            {
                for (int i = 0; i < sceneNames.Count; i++)
                {
                    if (!string.IsNullOrWhiteSpace(sceneNames[i])) _scenes.Add(sceneNames[i].Trim());
                }
            }

            if (_scenes.Count == 0) _scenes.AddRange(DefaultSceneNames);
        }

        private void ResolveCurrentIndex()
        {
            _currentIndex = IndexOfScene(CurrentSceneName);

            if (_currentIndex < 0)
            {
                Debug.LogWarning(
                    $"[StoryPageManager] Current scene '{CurrentSceneName}' is not in the page list, all nav buttons will be disabled.\n" +
                    $"List: {string.Join(", ", _scenes)}", this);
            }
        }

        private int IndexOfScene(string sceneName)
        {
            for (int i = 0; i < _scenes.Count; i++)
            {
                if (string.Equals(_scenes[i], sceneName, StringComparison.OrdinalIgnoreCase)) return i;
            }
            return -1;
        }

        private void CheckBuildSettings()
        {
            var missing = new List<string>();
            for (int i = 0; i < _scenes.Count; i++)
            {
                if (!Application.CanStreamedLevelBeLoaded(_scenes[i])) missing.Add(_scenes[i]);
            }

            if (missing.Count > 0)
            {
                Debug.LogWarning(
                    "[StoryPageManager] These scenes are not in Build Settings and cannot be loaded: " +
                    string.Join(", ", missing) +
                    "\nRun menu 'Moon Postman > Add Story Scenes To Build Settings' or add them manually in File > Build Settings.", this);
            }
        }

        private void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;
            if (FindObjectOfType<EventSystem>() != null) return;

            var go = new GameObject("EventSystem (auto)", typeof(EventSystem), typeof(StandaloneInputModule));
            Debug.Log("[StoryPageManager] No EventSystem found, created one automatically. Consider adding it manually and saving the scene.", go);
        }

        private void EnsureGraphicRaycaster()
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null) return;
            if (canvas.GetComponent<GraphicRaycaster>() != null) return;

            canvas.gameObject.AddComponent<GraphicRaycaster>();
            Debug.LogWarning(
                "[StoryPageManager] This Canvas has no GraphicRaycaster; added one automatically (otherwise buttons cannot be clicked).\n" +
                "Consider adding it manually via Add Component > Graphic Raycaster and saving the scene.", this);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (fadeDuration < 0f) fadeDuration = 0f;
        }
#endif
    }
}
