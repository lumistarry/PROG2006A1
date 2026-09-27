using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace MoonPostman
{
    public enum NavAction
    {
        Home = 0,

        Back = 1,

        Next = 2,

        Restart = 3,

        GoToIndex = 4
    }

    [RequireComponent(typeof(Button))]
    [DisallowMultipleComponent]
    public class StoryNavButton : MonoBehaviour
    {
        [SerializeField] private NavAction action = NavAction.Next;

        [SerializeField] private int targetIndex = 0;

        [SerializeField] private bool useBoundaryRule = true;

        [SerializeField] private bool pressFeedback = true;

        [SerializeField] private float pressScale = 0.92f;

        [SerializeField] private float pressDuration = 0.07f;

        private Button _button;
        private Vector3 _baseScale;
        private bool _initialized;
        private bool _listening;
        private Coroutine _punchRoutine;

        public NavAction Action => action;

        private void Awake()
        {
            EnsureInit();
            StartListening();
            transform.localScale = _baseScale;
        }

        private void OnEnable()
        {
            EnsureInit();
            StartListening();
            RefreshState();
        }

        private void OnDisable()
        {
            _punchRoutine = null;
            if (_initialized) transform.localScale = _baseScale;
        }

        private void OnDestroy()
        {
            StopListening();
        }

        public void RefreshState()
        {
            EnsureInit();

            if (_button == null) return;

            var manager = StoryPageManager.Instance;
            bool hasManager = manager != null && manager.PageCount > 0;
            bool applyRule = useBoundaryRule && hasManager;

            if (!applyRule) return;

            BoundaryMode mode = manager.BoundaryRule;
            bool canUse = manager.CanPerform(action, targetIndex);

            bool shouldHide = !canUse && mode == BoundaryMode.Hide;
            if (gameObject.activeSelf == shouldHide) gameObject.SetActive(!shouldHide);

            _button.interactable = canUse;
        }

        private void EnsureInit()
        {
            if (_initialized) return;

            _button = GetComponent<Button>();
            _baseScale = transform.localScale;
            _initialized = true;
        }

        private void StartListening()
        {
            if (_listening || _button == null) return;

            _button.onClick.AddListener(OnClick);
            _listening = true;
        }

        private void StopListening()
        {
            if (!_listening || _button == null) return;

            _button.onClick.RemoveListener(OnClick);
            _listening = false;
        }

        private void OnClick()
        {
            var manager = StoryPageManager.Instance;
            if (manager == null)
            {
                Debug.LogError(
                    "[StoryNavButton] No StoryPageManager found in this scene. Add one to the Canvas of every scene.", this);
                return;
            }

            if (pressFeedback) PlayPressFeedback();

            switch (action)
            {
                case NavAction.Home: manager.GoHome(); break;
                case NavAction.Back: manager.GoBack(); break;
                case NavAction.Next: manager.GoNext(); break;
                case NavAction.Restart: manager.Restart(); break;
                case NavAction.GoToIndex: manager.GoTo(targetIndex); break;
            }
        }

        private void PlayPressFeedback()
        {
            if (!isActiveAndEnabled) return;

            if (_punchRoutine != null) StopCoroutine(_punchRoutine);
            _punchRoutine = StartCoroutine(PunchRoutine());
        }

        private IEnumerator PunchRoutine()
        {
            float half = Mathf.Max(0.01f, pressDuration);
            Vector3 small = _baseScale * pressScale;

            float t = 0f;
            while (t < half)
            {
                t += Time.unscaledDeltaTime;
                transform.localScale = Vector3.Lerp(_baseScale, small, Mathf.Clamp01(t / half));
                yield return null;
            }

            t = 0f;
            while (t < half)
            {
                t += Time.unscaledDeltaTime;
                transform.localScale = Vector3.Lerp(small, _baseScale, Mathf.Clamp01(t / half));
                yield return null;
            }

            transform.localScale = _baseScale;
            _punchRoutine = null;
        }
    }
}
