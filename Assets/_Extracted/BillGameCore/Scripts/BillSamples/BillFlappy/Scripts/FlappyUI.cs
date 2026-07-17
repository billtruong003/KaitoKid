using UnityEngine;
using UnityEngine.UI;
using BillGameCore;

namespace BillSamples.Flappy
{
    /// <summary>
    /// In-game HUD: score display, menu title screen.
    /// </summary>
    public class FlappyHUD : MonoBehaviour
    {
        [Header("Menu Elements")]
        public GameObject menuGroup;
        public Text titleText;
        public Text bestScoreText;
        public Text tapToStartText;

        [Header("Gameplay Elements")]
        public GameObject gameplayGroup;
        public Text scoreText;

        public void ShowMenu(int bestScore)
        {
            if (menuGroup) menuGroup.SetActive(true);
            if (gameplayGroup) gameplayGroup.SetActive(false);
            if (bestScoreText) bestScoreText.text = $"Best: {bestScore}";

            // Tap to start blink
            if (tapToStartText)
            {
                var cg = tapToStartText.GetComponent<CanvasGroup>();
                if (cg) BillTween.Fade(cg, 0.3f, 0.5f).SetEase(EaseType.InOutSine).SetLoops(-1, LoopType.Yoyo);
            }
        }

        public void ShowGameplay()
        {
            if (menuGroup) menuGroup.SetActive(false);
            if (gameplayGroup) gameplayGroup.SetActive(true);
            // Stop blink tween
            if (tapToStartText)
            {
                var cg = tapToStartText.GetComponent<CanvasGroup>();
                if (cg) BillTween.KillTarget(cg);
            }
        }

        public void UpdateScore(int score)
        {
            if (scoreText) scoreText.text = score.ToString();
        }

        public void PunchScore()
        {
            if (scoreText == null) return;
            BillTween.KillTarget(scoreText.transform);
            BillTween.ScaleY(scoreText.transform, 1.3f, 0.08f)
                .SetEase(EaseType.OutQuad)
                .OnComplete(() => BillTween.ScaleY(scoreText.transform, 1f, 0.1f));
        }
    }

    /// <summary>
    /// Game Over panel with score, best, medal, and buttons.
    /// </summary>
    public class FlappyGameOverPanel : MonoBehaviour
    {
        [Header("Panel Root")]
        public RectTransform panelRoot;
        public CanvasGroup canvasGroup;

        [Header("Score")]
        public Text finalScoreText;
        public Text bestScoreText;
        public Text newBestLabel;

        [Header("Medal")]
        public Image medalImage;
        public Sprite[] medalSprites; // 0=bronze, 1=silver, 2=gold, 3=platinum
        public GameObject medalHolder;

        [Header("Buttons — wire OnClick in Inspector or via code")]
        public Button retryButton;
        public Button menuButton;

        private FlappyGameManager _manager;

        public void Init(FlappyGameManager manager)
        {
            _manager = manager;

            if (retryButton)
                retryButton.onClick.AddListener(() => _manager.OnRetryButton());
            if (menuButton)
                menuButton.onClick.AddListener(() => _manager.OnMenuButton());

            Hide();
        }

        public void Show(int score, int best, bool isNewBest, int medal)
        {
            gameObject.SetActive(true);

            if (finalScoreText) finalScoreText.text = score.ToString();
            if (bestScoreText) bestScoreText.text = best.ToString();
            if (newBestLabel) newBestLabel.gameObject.SetActive(isNewBest);

            // Medal
            if (medalHolder) medalHolder.SetActive(medal > 0);
            if (medal > 0 && medalImage != null && medalSprites != null && medal - 1 < medalSprites.Length)
            {
                medalImage.sprite = medalSprites[medal - 1];
                // Medal stamp tween
                medalImage.transform.localScale = Vector3.zero;
                Bill.Timer.Delay(0.3f, () =>
                {
                    BillTween.Scale(medalImage.transform, 1.2f, 0.2f)
                        .SetEase(EaseType.OutBack)
                        .OnComplete(() => BillTween.Scale(medalImage.transform, 1f, 0.1f));
                    Bill.Audio.Play("sfx_medal");
                });
            }

            // New best blink
            if (isNewBest && newBestLabel)
            {
                var cg = newBestLabel.GetComponent<CanvasGroup>();
                if (cg == null) cg = newBestLabel.gameObject.AddComponent<CanvasGroup>();
                BillTween.Fade(cg, 0f, 0.3f).SetLoops(-1, LoopType.Yoyo);
            }

            // Slide in from bottom
            if (panelRoot)
            {
                panelRoot.anchoredPosition = new Vector2(0, -800);
                BillTween.Float(panelRoot.anchoredPosition.y, 0f, 0.4f, v =>
                {
                    panelRoot.anchoredPosition = new Vector2(0, v);
                }).SetEase(EaseType.OutBack);
            }

            if (canvasGroup)
            {
                canvasGroup.alpha = 0f;
                BillTween.Fade(canvasGroup, 1f, 0.3f);
            }
        }

        public void Hide()
        {
            gameObject.SetActive(false);
            BillTween.KillTarget(panelRoot);
        }
    }
}
