using System.Collections;
using UnityEngine;

namespace MoonPostman
{
    [DisallowMultipleComponent]
    public class CloudClickRise : MonoBehaviour
    {
        [Header("Sound")]
        [Tooltip("Sound clip played on click.")]
        [SerializeField] private AudioClip clickSound;

        [Range(0f, 1f)]
        [Tooltip("Sound volume.")]
        [SerializeField] private float volume = 1f;

        [Header("Rise")]
        [Tooltip("Rise distance in world units. The cloud in Scene3 sits near y=4 and the camera top is about y=5, so 3 units clears the screen.")]
        [SerializeField] private float riseHeight = 3f;

        [Tooltip("Rise animation duration in seconds.")]
        [SerializeField] private float riseDuration = 1.5f;

        [Tooltip("Rise curve: x is time (0~1), y is progress (0~1). Default eases out.")]
        [SerializeField] private AnimationCurve riseCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Tooltip("When enabled, rises only once; otherwise every click rises another riseHeight and replays sound.")]
        [SerializeField] private bool clickOnlyOnce = true;

        private AudioSource _source;
        private bool _rising;
        private bool _hasRisen;
        private Coroutine _riseRoutine;
        private bool _warnedNoClip;

        public bool IsRising => _rising;
        public bool HasRisen => _hasRisen;

        private void Awake()
        {
            EnsureAudioSource();
            EnsureCollider();
        }

        private void OnMouseDown()
        {
            if (_rising) return;
            if (clickOnlyOnce && _hasRisen) return;

            _hasRisen = true;

            PlaySound();
            StartRise();
        }

        private void PlaySound()
        {
            if (clickSound == null)
            {
                if (!_warnedNoClip)
                {
                    _warnedNoClip = true;
                    Debug.LogWarning(
                        $"[CloudClickRise] '{name}' has no Click Sound assigned, so no sound will play. Assign an audio file in the Inspector.", this);
                }
                return;
            }

            _source.clip = clickSound;
            _source.volume = volume;
            _source.Stop();
            _source.Play();
        }

        private void StartRise()
        {
            if (Mathf.Abs(riseHeight) < 0.01f || riseDuration <= 0f) return;

            if (_riseRoutine != null) StopCoroutine(_riseRoutine);
            _riseRoutine = StartCoroutine(RiseRoutine());
        }

        private IEnumerator RiseRoutine()
        {
            _rising = true;

            Vector3 start = transform.position;
            Vector3 end = start + new Vector3(0f, riseHeight, 0f);

            float elapsed = 0f;
            while (elapsed < riseDuration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / riseDuration;

                float t = riseCurve != null && riseCurve.length >= 2
                    ? riseCurve.Evaluate(progress)
                    : Mathf.SmoothStep(0f, 1f, progress);

                transform.position = Vector3.Lerp(start, end, Mathf.Clamp01(t));
                yield return null;
            }

            transform.position = end;

            _rising = false;
            _riseRoutine = null;
        }

        private void EnsureAudioSource()
        {
            _source = GetComponent<AudioSource>();
            if (_source == null)
            {
                _source = gameObject.AddComponent<AudioSource>();
                _source.playOnAwake = false;
            }
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
                "[CloudClickRise] Auto-added a BoxCollider2D (required for click detection). " +
                "Adjust its Size in the Inspector to change the click area.", this);
        }
    }
}