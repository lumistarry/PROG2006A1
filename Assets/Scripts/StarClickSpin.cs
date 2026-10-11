using System.Collections;
using UnityEngine;

namespace MoonPostman
{
    [DisallowMultipleComponent]
    public class StarClickSpin : MonoBehaviour
    {
        [Header("Click")]
        [Tooltip("Minimum interval between two valid clicks (seconds).")]
        [SerializeField] private float minClickInterval = 0.5f;

        [Header("Sound")]
        [Tooltip("Sound clip played on click. Clicking again restarts it from the beginning.")]
        [SerializeField] private AudioClip clickSound;

        [Range(0f, 1f)]
        [Tooltip("Sound volume.")]
        [SerializeField] private float volume = 1f;

        [Header("Spin")]
        [Tooltip("Degrees the star rotates around the Z axis per click.")]
        [SerializeField] private float spinDegrees = 360f;

        [Tooltip("Spin animation duration (seconds).")]
        [SerializeField] private float spinDuration = 0.5f;

        private AudioSource _source;
        private float _lastClickTime = float.NegativeInfinity;
        private Coroutine _spinRoutine;
        private bool _warnedNoClip;

        private void Awake()
        {
            EnsureAudioSource();
            EnsureCollider();
        }

        private void OnMouseDown()
        {
            if (Time.time - _lastClickTime < minClickInterval) return;

            _lastClickTime = Time.time;

            PlaySound();
            StartSpin();
        }

        private void PlaySound()
        {
            if (clickSound == null)
            {
                if (!_warnedNoClip)
                {
                    _warnedNoClip = true;
                    Debug.LogWarning(
                        $"[StarClickSpin] '{name}' has no Click Sound assigned, so no sound plays. Assign an audio file in the Inspector.", this);
                }
                return;
            }

            _source.clip = clickSound;
            _source.volume = volume;
            _source.Stop();
            _source.Play();
        }

        private void StartSpin()
        {
            if (Mathf.Abs(spinDegrees) < 0.01f || spinDuration <= 0f) return;

            if (_spinRoutine != null) StopCoroutine(_spinRoutine);
            _spinRoutine = StartCoroutine(SpinRoutine());
        }

        private IEnumerator SpinRoutine()
        {
            float startZ = transform.eulerAngles.z;
            float endZ = startZ + spinDegrees;

            float elapsed = 0f;
            while (elapsed < spinDuration)
            {
                elapsed += Time.deltaTime;
                float angle = Mathf.Lerp(startZ, endZ, Mathf.Clamp01(elapsed / spinDuration));
                transform.rotation = Quaternion.Euler(0f, 0f, angle);
                yield return null;
            }

            transform.rotation = Quaternion.Euler(0f, 0f, endZ);
            _spinRoutine = null;
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
                "[StarClickSpin] Auto-added a BoxCollider2D (required for click detection). " +
                "Adjust its Size in the Inspector to change the click area.", this);
        }
    }
}