using System.Collections;
using UnityEngine;

namespace MoonPostman
{
    [DisallowMultipleComponent]
    public class RabbitClickJump : MonoBehaviour
    {
        [Header("Sound")]
        [Tooltip("Sound clip played on click.")]
        [SerializeField] private AudioClip jumpSound;

        [Range(0f, 1f)]
        [Tooltip("Sound volume.")]
        [SerializeField] private float volume = 1f;

        [Header("Jump")]
        [Tooltip("Peak jump height in world units. Milo on HomePage sits near y=-1.2 and the camera top is about y=5.")]
        [SerializeField] private float jumpHeight = 1.5f;

        [Tooltip("Total duration of one full jump (rise, peak, land) in seconds.")]
        [SerializeField] private float jumpDuration = 0.6f;

        private AudioSource _source;
        private Coroutine _jumpRoutine;
        private bool _jumping;
        private bool _warnedNoClip;

        public bool IsJumping => _jumping;

        private void Awake()
        {
            EnsureAudioSource();
            EnsureCollider();
        }

        private void OnMouseDown()
        {
            if (_jumping) return;

            PlayJumpSound();
            StartJump();
        }

        private void PlayJumpSound()
        {
            if (jumpSound == null)
            {
                if (!_warnedNoClip)
                {
                    _warnedNoClip = true;
                    Debug.LogWarning(
                        $"[RabbitClickJump] '{name}' has no Jump Sound assigned, so no sound plays. Assign an audio file in the Inspector.", this);
                }
                return;
            }

            _source.clip = jumpSound;
            _source.volume = volume;
            _source.Stop();
            _source.Play();
        }

        private void StartJump()
        {
            if (Mathf.Abs(jumpHeight) < 0.01f || jumpDuration <= 0f) return;

            if (_jumpRoutine != null) StopCoroutine(_jumpRoutine);
            _jumpRoutine = StartCoroutine(JumpRoutine());
        }

        private IEnumerator JumpRoutine()
        {
            _jumping = true;

            Vector3 basePos = transform.position;

            float elapsed = 0f;
            while (elapsed < jumpDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / jumpDuration);

                float height = Mathf.Sin(t * Mathf.PI) * jumpHeight;
                transform.position = basePos + new Vector3(0f, height, 0f);

                yield return null;
            }

            transform.position = basePos;

            _jumping = false;
            _jumpRoutine = null;
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
                "[RabbitClickJump] Auto-added a BoxCollider2D (required for click detection). " +
                "Adjust its Size in the Inspector to change the click area.", this);
        }
    }
}