using UnityEngine;

namespace MoonPostman
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-100)]
    public class BreatheOnIdle : MonoBehaviour
    {
        [Header("Breathe")]
        [Tooltip("Vertical float height in world units.")]
        [SerializeField] private float floatHeight = 0.15f;

        [Tooltip("Duration of one full breathe cycle (up then down) in seconds.")]
        [SerializeField] private float breathePeriod = 2.5f;

        [Tooltip("Scale amplitude relative to original size. 0 means float only.")]
        [Range(0f, 0.3f)]
        [SerializeField] private float scaleAmount = 0.05f;

        [Tooltip("Phase offset (0~1). Multiple stars can desync their breathe motion.")]
        [Range(0f, 1f)]
        [SerializeField] private float phaseOffset = 0f;

        [Tooltip("Seconds after stopping before breathing resumes (0 = never resume).\n" +
                 "For stars use 0.6s; for the rabbit use its jump duration (0.6); keep 0 for one-shot objects.")]
        [SerializeField] private float resumeAfterSeconds = 0f;

        private Vector3 _basePos;
        private Vector3 _baseScale;
        private bool _initialized;
        private bool _stopped;
        private float _resumeTimer;

        public bool IsBreathing => _initialized && !_stopped;

        private void Update()
        {
            if (!_initialized)
            {
                _basePos = transform.position;
                _baseScale = transform.localScale;
                _initialized = true;
            }

            if (_stopped)
            {
                if (resumeAfterSeconds > 0f)
                {
                    _resumeTimer += Time.deltaTime;
                    if (_resumeTimer >= resumeAfterSeconds)
                    {
                        _stopped = false;
                    }
                }
                return;
            }

            float t = Mathf.Sin((Time.time / Mathf.Max(0.01f, breathePeriod) + phaseOffset) * Mathf.PI * 2f);

            transform.position = _basePos + new Vector3(0f, t * floatHeight, 0f);

            if (scaleAmount > 0f)
            {
                transform.localScale = _baseScale * (1f + t * scaleAmount);
            }
        }

        private void OnMouseDown()
        {
            StopBreathe();
        }

        public void StopBreathe()
        {
            _stopped = true;
            _resumeTimer = 0f;

            if (_initialized)
            {
                transform.position = _basePos;
                transform.localScale = _baseScale;
            }
        }

        public void ResumeBreathe()
        {
            _stopped = false;
            _resumeTimer = 0f;
        }
    }
}