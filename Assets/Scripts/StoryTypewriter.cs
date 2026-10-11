using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace MoonPostman
{
    [RequireComponent(typeof(Text))]
    [DisallowMultipleComponent]
    public class StoryTypewriter : MonoBehaviour
    {
        [Header("Typewriter")]
        [Tooltip("Delay in seconds before typing starts after the scene loads. Default 0.35s, slightly longer than the fade-in (0.3s).")]
        [SerializeField] private float startDelay = 0.35f;

        [Tooltip("Interval in seconds between each character.")]
        [SerializeField] private float charInterval = 0.06f;

        [Header("Sound")]
        [Tooltip("Sound synced with the typing (e.g. narration for this page). Can be empty.\n" +
                 "If the sound is shorter than the text it loops; it stops immediately when the text finishes.")]
        [SerializeField] private AudioClip narrationSound;

        [Range(0f, 1f)]
        [Tooltip("Sound volume.")]
        [SerializeField] private float volume = 1f;

        private Text _text;
        private string _fullText;
        private AudioSource _source;
        private Coroutine _routine;
        private bool _warnedNoClip;

        public bool IsPlaying => _routine != null;
        public string FullText => _fullText;

        private void Awake()
        {
            _text = GetComponent<Text>();
            _fullText = _text != null ? _text.text : string.Empty;
        }

        private void Start()
        {
            if (_text == null)
            {
                Debug.LogError("[StoryTypewriter] No Text component found. Attach this script to the same GameObject as the Text.", this);
                return;
            }

            _text.text = string.Empty;

            if (_routine != null) StopCoroutine(_routine);
            _routine = StartCoroutine(TypeRoutine());
        }

        private void OnDestroy()
        {
            if (_routine != null) StopCoroutine(_routine);
            StopNarration();
        }

        private void PlayNarration()
        {
            if (narrationSound == null)
            {
                if (!_warnedNoClip)
                {
                    _warnedNoClip = true;
                    Debug.LogWarning(
                        $"[StoryTypewriter] '{name}' has no Narration Sound assigned, so no sound plays while typing.", this);
                }
                return;
            }

            if (_source == null)
            {
                _source = gameObject.AddComponent<AudioSource>();
                _source.playOnAwake = false;
            }

            _source.clip = narrationSound;
            _source.volume = volume;
            _source.loop = true;
            _source.Stop();
            _source.Play();
        }

        private void StopNarration()
        {
            if (_source != null && _source.isPlaying)
            {
                _source.Stop();
            }
        }

        private IEnumerator TypeRoutine()
        {
            if (startDelay > 0f) yield return new WaitForSeconds(startDelay);

            PlayNarration();

            var sb = new StringBuilder();
            for (int i = 0; i < _fullText.Length; i++)
            {
                sb.Append(_fullText[i]);
                _text.text = sb.ToString();

                if (charInterval > 0f && i < _fullText.Length - 1)
                {
                    yield return new WaitForSeconds(charInterval);
                }
            }

            _routine = null;

            StopNarration();
        }

        public void RevealInstantly()
        {
            if (_routine != null) StopCoroutine(_routine);
            _routine = null;

            if (_text != null) _text.text = _fullText;

            StopNarration();
        }
    }
}