using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class InteractionPromptUI : MonoBehaviour
{
    [SerializeField] private Text promptText;
    [SerializeField] private float fadeSpeed = 12f;

    private CanvasGroup canvasGroup;
    private float targetAlpha = 0f;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
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
    }

    public void SetImmediate(bool visible)
    {
        targetAlpha = visible ? 1f : 0f;
        canvasGroup.alpha = targetAlpha;
    }
}
