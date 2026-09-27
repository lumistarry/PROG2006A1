using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MoonPostman
{
    [DisallowMultipleComponent]
    public class StorySceneFader : MonoBehaviour
    {
        public static bool HasPlayedOpeningFade;

        public static bool IsBusy { get; private set; }

        private static StorySceneFader _instance;
        private static readonly Color FallbackColor = new Color(0.02f, 0.02f, 0.06f, 1f);

        private Canvas _canvas;
        private Image _image;
        private Coroutine _routine;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(this);
                return;
            }

            _instance = this;
            EnsureVisuals();
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
                IsBusy = false;
            }
        }

        public static StorySceneFader GetOrCreate(Color fadeColor)
        {
            if (Application.isPlaying == false) return null;

            if (_instance == null)
            {
                var go = new GameObject("~StorySceneFader");
                go.AddComponent<StorySceneFader>();
            }

            _instance.SetColor(fadeColor);
            return _instance;
        }

        public void FadeOutAndLoad(string sceneName, float duration)
        {
            if (string.IsNullOrEmpty(sceneName)) return;
            if (IsBusy)
            {
                Debug.Log("[StorySceneFader] Previous transition is still in progress, ignoring this click.");
                return;
            }

            Restart(FadeOutAndLoadRoutine(sceneName, duration));
        }

        public void PlayOpeningFade(float duration)
        {
            if (IsBusy) return;
            Restart(OpeningFadeRoutine(duration));
        }

        public void FadeInIfCovered(float duration)
        {
            if (IsBusy) return;
            if (_image == null || _image.color.a <= 0.01f) return;

            Restart(FadeRoutine(_image.color.a, 0f, duration));
        }

        private void Restart(IEnumerator routine)
        {
            if (_routine != null) StopCoroutine(_routine);
            _routine = StartCoroutine(routine);
        }

        private void EnsureVisuals()
        {
            if (_canvas != null) return;

            _canvas = gameObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 30000;

            var layer = new GameObject("Fade", typeof(RectTransform));
            layer.transform.SetParent(transform, false);

            var rt = (RectTransform)layer.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.localScale = Vector3.one;

            _image = layer.AddComponent<Image>();
            _image.raycastTarget = false;
            _image.color = new Color(FallbackColor.r, FallbackColor.g, FallbackColor.b, 0f);
        }

        private void SetColor(Color color)
        {
            if (_image == null) return;

            var c = _image.color;
            _image.color = new Color(color.r, color.g, color.b, c.a);
        }

        private IEnumerator FadeRoutine(float from, float to, float duration)
        {
            if (_image == null) yield break;

            Color c = _image.color;
            c.a = from;
            _image.color = c;

            duration = Mathf.Max(0f, duration);

            if (duration > 0f)
            {
                float elapsed = 0f;
                while (elapsed < duration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    c.a = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
                    _image.color = c;
                    yield return null;
                }
            }

            c.a = to;
            _image.color = c;
        }

        private IEnumerator OpeningFadeRoutine(float duration)
        {
            IsBusy = true;
            yield return FadeRoutine(1f, 0f, duration);
            IsBusy = false;
            _routine = null;
        }

        private IEnumerator FadeOutAndLoadRoutine(string sceneName, float duration)
        {
            IsBusy = true;

            yield return FadeRoutine(_image.color.a, 1f, duration);

            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError(
                    $"[StorySceneFader] Scene '{sceneName}' is not in Build Settings and cannot be loaded.\n" +
                    "Run menu 'Moon Postman > Add Story Scenes To Build Settings' or add all 6 scenes manually in File > Build Settings.");

                yield return FadeRoutine(1f, 0f, duration);
                IsBusy = false;
                _routine = null;
                yield break;
            }

            var op = SceneManager.LoadSceneAsync(sceneName);
            while (op != null && !op.isDone) yield return null;

            yield return null;

            yield return FadeRoutine(1f, 0f, duration);

            IsBusy = false;
            _routine = null;
        }
    }
}
