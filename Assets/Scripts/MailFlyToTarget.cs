using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace MoonPostman
{
    [DisallowMultipleComponent]
    public class MailFlyToTarget : MonoBehaviour
    {
        [Header("Path (Start / End)")]
        [Tooltip("Start anchor; drag in a scene object, otherwise use startPosition below.")]
        [SerializeField] private Transform startPoint;

        [Tooltip("End anchor; drag in a scene object (e.g. mailbox), otherwise use endPosition below.")]
        [SerializeField] private Transform endPoint;

        [Tooltip("Start world position (used when no anchor is assigned).")]
        [SerializeField] private Vector3 startPosition = new Vector3(4.68f, -1.09f, -0.87f);

        [Tooltip("End world position (used when no anchor is assigned).")]
        [SerializeField] private Vector3 endPosition = new Vector3(4.61f, -1.52f, 0f);

        [Header("Flight")]
        [Tooltip("Seconds to fly from start to end.")]
        [SerializeField] private float flightDuration = 1.2f;

        [Tooltip("Flight curve: x is time (0~1), y is progress (0~1). Default eases in and out.")]
        [SerializeField] private AnimationCurve flightCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Tooltip("When enabled, flies only once; otherwise every click flies again from start to end.")]
        [SerializeField] private bool flyOnlyOnce = true;

        [Header("Sound")]
        [Tooltip("Sound played when flight begins (e.g. whoosh); leave empty for no sound.")]
        [SerializeField] private AudioClip flySound;

        [Tooltip("Flight sound volume (0~1).")]
        [SerializeField, Range(0f, 1f)] private float flySoundVolume = 1f;

        [Header("Events (Optional)")]
        [Tooltip("Invoked when flight begins.")]
        public UnityEvent onFlightStart;

        [Tooltip("Invoked when the target is reached.")]
        public UnityEvent onArrived;

        private bool _flying;
        private bool _arrived;
        private Coroutine _routine;
        private AudioSource _audioSource;

        public bool IsFlying => _flying;
        public bool HasArrived => _arrived;

        public Vector3 StartPosition => startPoint != null ? startPoint.position : startPosition;
        public Vector3 EndPosition => endPoint != null ? endPoint.position : endPosition;

        private void Awake()
        {
            EnsureCollider();
            EnsureAudioSource();
        }

        private void Start()
        {
            transform.position = StartPosition;
        }

        private void OnMouseDown()
        {
            Fly();
        }

        public void Fly()
        {
            if (_flying) return;
            if (flyOnlyOnce && _arrived) return;

            if (_routine != null) StopCoroutine(_routine);
            _routine = StartCoroutine(FlyRoutine());
        }

        private IEnumerator FlyRoutine()
        {
            _flying = true;

            Vector3 from = StartPosition;
            Vector3 to = EndPosition;
            float duration = Mathf.Max(0.01f, flightDuration);

            onFlightStart?.Invoke();

            if (flySound != null)
            {
                _audioSource.PlayOneShot(flySound, flySoundVolume);
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;

                float t = flightCurve != null && flightCurve.length >= 2
                    ? flightCurve.Evaluate(progress)
                    : Mathf.SmoothStep(0f, 1f, progress);

                transform.position = Vector3.Lerp(from, to, Mathf.Clamp01(t));
                yield return null;
            }

            transform.position = to;

            _flying = false;
            _arrived = true;
            _routine = null;

            onArrived?.Invoke();
        }

        private void EnsureAudioSource()
        {
            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
            }
            _audioSource.playOnAwake = false;
            _audioSource.spatialBlend = 0f;
        }

        private void EnsureCollider()
        {
            if (GetComponent<Collider2D>() != null) return;

            var col = gameObject.AddComponent<BoxCollider2D>();

            var sr = GetComponent<SpriteRenderer>();
            if (sr != null && sr.sprite != null)
            {
                col.size = sr.sprite.bounds.size;
            }

            Debug.Log(
                "[MailFlyToTarget] Auto-added a BoxCollider2D (required for click detection). " +
                "Adjust its Size in the Inspector to change the click area.", this);
        }

#if UNITY_EDITOR
        [ContextMenu("Record Current Position As START")]
        private void RecordStart()
        {
            startPosition = transform.position;
            UnityEditor.EditorUtility.SetDirty(this);
            Debug.Log($"[MailFlyToTarget] START recorded as {startPosition}", this);
        }

        [ContextMenu("Record Current Position As END")]
        private void RecordEnd()
        {
            endPosition = transform.position;
            UnityEditor.EditorUtility.SetDirty(this);
            Debug.Log($"[MailFlyToTarget] END recorded as {endPosition}", this);
        }
#endif
    }
}