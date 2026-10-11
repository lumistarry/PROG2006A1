using System;
using UnityEngine;
using UnityEngine.UI;

namespace MoonPostman
{
    [RequireComponent(typeof(Button))]
    [DisallowMultipleComponent]
    public class VolumeToggleButton : MonoBehaviour
    {
        public static bool Muted { get; private set; }

        public static event Action<bool> OnMutedChanged;

        [Header("Icons")]
        [Tooltip("Icon shown when sound is on.")]
        [SerializeField] private Sprite soundOnIcon;

        [Tooltip("Icon shown when muted.")]
        [SerializeField] private Sprite soundOffIcon;

        [Tooltip("Image that shows the icon. Leave empty to use the button's own Image component.")]
        [SerializeField] private Image iconImage;

        private Button _button;
        private bool _warnedNoIcon;

        private void Awake()
        {
            _button = GetComponent<Button>();
            _button.onClick.AddListener(OnClick);

            if (iconImage == null) iconImage = GetComponent<Image>();
        }

        private void OnEnable()
        {
            OnMutedChanged += HandleMutedChanged;

            ApplyVolume();
            RefreshIcon();
        }

        private void OnDisable()
        {
            OnMutedChanged -= HandleMutedChanged;
        }

        private void OnDestroy()
        {
            if (_button != null) _button.onClick.RemoveListener(OnClick);
        }

        private void OnClick()
        {
            SetMuted(!Muted);
        }

        public static void SetMuted(bool muted)
        {
            if (Muted == muted) return;

            Muted = muted;
            AudioListener.volume = muted ? 0f : 1f;
            OnMutedChanged?.Invoke(muted);
        }

        private void HandleMutedChanged(bool muted)
        {
            AudioListener.volume = muted ? 0f : 1f;
            RefreshIcon();
        }

        private void ApplyVolume()
        {
            AudioListener.volume = Muted ? 0f : 1f;
        }

        private void RefreshIcon()
        {
            if (iconImage == null) return;

            Sprite target = Muted ? soundOffIcon : soundOnIcon;

            if (target == null)
            {
                if (!_warnedNoIcon)
                {
                    _warnedNoIcon = true;
                    Debug.LogWarning(
                        $"[VolumeToggleButton] '{name}' is missing an icon: assign " +
                        (Muted ? "the Sound Off Icon" : "the Sound On Icon") +
                        " in the Inspector.", this);
                }
                return;
            }

            iconImage.sprite = target;
        }
    }
}