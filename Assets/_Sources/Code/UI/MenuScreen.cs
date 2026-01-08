using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace Sources.Code.UI
{
    public class MenuScreen : BaseScreen
    {
        [Header("Main Menu")]
        [SerializeField] private CanvasGroup _mainGroup;
        [SerializeField] private Button _playButton;

        [Header("Fade To Black")]
        [SerializeField] private CanvasGroup _fadeGroup;
        [SerializeField] private float fadeDuration = 0.6f;
        [SerializeField] private float blackPause = 0.3f;

        private IMain _main;
        private Sequence _sequence;

        public void Init(IMain main)
        {
            _main = main;

            _fadeGroup.alpha = 0f;
            _fadeGroup.blocksRaycasts = false;

            ShowMain();
        }

        private void OnEnable()
        {
            _playButton.onClick.AddListener(OnPlayClicked);
        }

        private void OnDisable()
        {
            _playButton.onClick.RemoveListener(OnPlayClicked);
            _sequence?.Kill();
        }

        private void OnPlayClicked()
        {
            DisableAllInput();

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            _fadeGroup.blocksRaycasts = true;

            _sequence?.Kill();
            _sequence = DOTween.Sequence()
                .Append(_fadeGroup.DOFade(1f, fadeDuration).SetEase(Ease.InQuad))
                .AppendInterval(blackPause)
                .OnComplete(() =>
                {
                    _main.StartGame();
                });
        }

        private void ShowMain()
        {
            _mainGroup.alpha = 1f;
            SetGroupState(_mainGroup, true);
        }

        private void DisableAllInput()
        {
            SetGroupState(_mainGroup, false);
        }

        private static void SetGroupState(CanvasGroup group, bool enabled)
        {
            if (group == null) return;
            group.interactable = enabled;
            group.blocksRaycasts = enabled;
        }
    }
}
