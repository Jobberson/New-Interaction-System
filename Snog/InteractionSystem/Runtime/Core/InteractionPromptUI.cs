using UnityEngine;
using UnityEngine.UI;

namespace Snog.InteractionSystem.Runtime.Core
{
    [RequireComponent(typeof(CanvasGroup))]
    public class InteractionPromptUI : MonoBehaviour
    {
        [Header("Prompt")]
        [SerializeField] private Text promptText;
        [SerializeField] private float fadeSpeed = 12f;

        [Header("Hold")]
        [SerializeField] private Image holdFillImage;

        [Header("Crosshair")]
        [SerializeField] private Image crosshairImage;
        [SerializeField] private Sprite defaultCrosshairSprite;
        [SerializeField] private Color defaultCrosshairColor = new Color(1f, 1f, 1f, 0.75f);
        [SerializeField] private Color availableColor = new Color(1f, 1f, 1f, 0.95f);
        [SerializeField] private Color unavailableColor = new Color(1f, 0.35f, 0.25f, 0.95f);

        private CanvasGroup canvasGroup;
        private float targetAlpha;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            targetAlpha = 0f;

            if (crosshairImage != null && defaultCrosshairSprite == null)
            {
                defaultCrosshairSprite = crosshairImage.sprite;
            }

            SetHoldProgress(0f);
            ResetCrosshair();
        }

        private void Update()
        {
            canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, targetAlpha, Time.deltaTime * fadeSpeed);
        }

        public void Show(string message)
        {
            if (promptText != null)
            {
                promptText.text = message;
            }

            targetAlpha = 1f;
        }

        public void Hide()
        {
            targetAlpha = 0f;
            SetHoldProgress(0f);

            // Important: do NOT hide the crosshair.
            ResetCrosshair();
        }

        public void SetImmediate(bool visible)
        {
            targetAlpha = visible ? 1f : 0f;
            canvasGroup.alpha = targetAlpha;
        }

        public void SetHoldProgress(float progress01)
        {
            if (holdFillImage == null)
            {
                return;
            }

            holdFillImage.fillAmount = Mathf.Clamp01(progress01);
        }

        public void SetCrosshair(Sprite sprite, bool isAvailable)
        {
            if (crosshairImage == null)
            {
                return;
            }

            Sprite chosen = sprite != null ? sprite : defaultCrosshairSprite;

            crosshairImage.sprite = chosen;
            crosshairImage.color = isAvailable ? availableColor : unavailableColor;
            crosshairImage.enabled = chosen != null;
        }

        public void ResetCrosshair()
        {
            if (crosshairImage == null)
            {
                return;
            }

            crosshairImage.sprite = defaultCrosshairSprite;
            crosshairImage.color = defaultCrosshairColor;
            crosshairImage.enabled = defaultCrosshairSprite != null;
        }
    }
}