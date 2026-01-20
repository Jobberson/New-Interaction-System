
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class InteractionPromptUI : MonoBehaviour
{
    [Header("Prompt")]
    [SerializeField] private Text promptText;
    [SerializeField] private float fadeSpeed = 12f;

    [Header("Hold")]
    [SerializeField] private Image holdFillImage;

    [Header("Crosshair Icon")]
    [SerializeField] private Image crosshairImage;
    [SerializeField] private Sprite defaultCrosshairSprite;
    [SerializeField] private Color availableColor = new Color(1f, 1f, 1f, 0.75f);
    [SerializeField] private Color unavailableColor = new Color(1f, 0.35f, 0.25f, 0.85f);

    private CanvasGroup canvasGroup;
    private float targetAlpha = 0f;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;

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

    public void SetCrosshairIcon(Sprite icon, bool isAvailable)
    {
        if (crosshairImage == null)
        {
            return;
        }

        crosshairImage.sprite = icon != null ? icon : defaultCrosshairSprite;
        crosshairImage.color = isAvailable ? availableColor : unavailableColor;

        crosshairImage.enabled = crosshairImage.sprite != null;
    }

    public void ResetCrosshair()
    {
        SetCrosshairIcon(null, true);
    }
}
