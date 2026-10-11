using UnityEngine;

namespace MoonPostman
{
    [DisallowMultipleComponent]
    public class MusicManager : MonoBehaviour
    {
        public static MusicManager Instance { get; private set; }

        [Header("BGM")]
        [Tooltip("Music that plays on loop after startup.")]
        [SerializeField] private AudioClip bgmClip;

        [Range(0f, 1f)]
        [Tooltip("Music volume.")]
        [SerializeField] private float volume = 1f;

        private AudioSource _source;
        private bool _warnedNoClip;

        public bool IsPlaying => _source != null && _source.isPlaying;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            DontDestroyOnLoad(gameObject);

            _source = GetComponent<AudioSource>();
            if (_source == null)
            {
                _source = gameObject.AddComponent<AudioSource>();
                _source.playOnAwake = false;
            }
        }

        private void Start()
        {
            PlayMusic(bgmClip);
        }

        public void PlayMusic(AudioClip clip)
        {
            if (clip == null)
            {
                if (!_warnedNoClip)
                {
                    _warnedNoClip = true;
                    Debug.LogWarning(
                        $"[MusicManager] '{name}' has no BGM Clip assigned, so no music plays. Assign an audio file in the Inspector.", this);
                }
                return;
            }

            if (_source.clip == clip && _source.isPlaying) return;

            bgmClip = clip;
            _source.clip = clip;
            _source.loop = true;
            _source.volume = volume;
            _source.Play();
        }

        public void StopMusic()
        {
            if (_source != null && _source.isPlaying)
            {
                _source.Stop();
            }
        }
    }
}