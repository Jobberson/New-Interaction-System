using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Snog.InteractionSystem.Runtime.Interfaces;

namespace Snog.InteractionSystem.Runtime.Core
{
    /// <summary>
    /// The built-in interaction HUD. Manages prompt text, hold-progress fill, and crosshair.
    /// Implements <see cref="IPromptDisplay"/> so it can be assigned to PlayerInteractor's
    /// "Prompt Display" slot.
    ///
    /// To use a completely custom UI: create a MonoBehaviour that implements IPromptDisplay
    /// and assign it instead of this component.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    [AddComponentMenu("Interaction/Interaction Prompt UI")]
    public class InteractionPromptUI : MonoBehaviour, IPromptDisplay
    {
        [Header("Prompt")]
        [Tooltip("TMP text element that shows the action label, e.g. '[E]  Open'.")]
        [SerializeField] private TMP_Text promptText;

        [Tooltip("How fast the prompt fades in/out (alpha per second).")]
        [SerializeField] private float fadeSpeed = 12f;

        [Header("Hold Progress")]
        [Tooltip("Radial or linear Image (Fill type). Leave empty if not needed.")]
        [SerializeField] private Image holdFillImage;

        [Header("Crosshair")]
        [SerializeField] private Image crosshairImage;

        [Tooltip("Default crosshair sprite shown when nothing is focused. " +
                 "If null, the image is hidden when nothing is focused.")]
        [SerializeField] private Sprite defaultCrosshairSprite;

        [SerializeField] private Color defaultCrosshairColor  = new Color(1f, 1f, 1f, 0.75f);
        [SerializeField] private Color availableColor         = new Color(1f, 1f, 1f, 0.95f);
        [SerializeField] private Color unavailableColor       = new Color(1f, 0.35f, 0.25f, 0.95f);

        private CanvasGroup canvasGroup;
        private float       targetAlpha;

        private void Awake()
        {
            canvasGroup       = GetComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            targetAlpha       = 0f;

            if (crosshairImage != null && defaultCrosshairSprite == null)
                defaultCrosshairSprite = crosshairImage.sprite;

            SetHoldProgress(0f);
            ResetCrosshair();
        }

        private void Update()
        {
            canvasGroup.alpha = Mathf.MoveTowards(
                canvasGroup.alpha, targetAlpha, Time.deltaTime * fadeSpeed);
        }

        // IPromptDisplay

        /// <inheritdoc/>
        public void Show(string message)
        {
            if (promptText != null) promptText.text = message;
            targetAlpha = 1f;
        }

        /// <inheritdoc/>
        public void Hide()
        {
            targetAlpha = 0f;
            SetHoldProgress(0f);
            ResetCrosshair();
        }

        /// <inheritdoc/>
        public void SetImmediate(bool visible)
        {
            targetAlpha = canvasGroup.alpha = visible ? 1f : 0f;
        }

        /// <inheritdoc/>
        public void SetHoldProgress(float progress01)
        {
            if (holdFillImage != null)
                holdFillImage.fillAmount = Mathf.Clamp01(progress01);
        }

        /// <inheritdoc/>
        public void SetCrosshair(Sprite sprite, bool isAvailable)
        {
            if (crosshairImage == null) return;
            Sprite chosen          = sprite != null ? sprite : defaultCrosshairSprite;
            crosshairImage.sprite  = chosen;
            crosshairImage.color   = isAvailable ? availableColor : unavailableColor;
            crosshairImage.enabled = chosen != null;
        }

        /// <inheritdoc/>
        public void ResetCrosshair()
        {
            if (crosshairImage == null) return;
            crosshairImage.sprite  = defaultCrosshairSprite;
            crosshairImage.color   = defaultCrosshairColor;
            crosshairImage.enabled = defaultCrosshairSprite != null;
        }
    }
}
