
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class PlayerInteractor : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private InteractionPromptUI promptUI;
    [SerializeField] private InteractionSettings settings;

#if ENABLE_INPUT_SYSTEM
    [Header("Input (New Input System)")]
    [Tooltip("If assigned, this action will be used for interaction. If left null, the component falls back to Legacy Input (KeyCode).")]
    [SerializeField] private InputActionReference interactAction;
#endif

    private IInteractable current;
    private float holdTimer = 0f;

    private void Reset()
    {
        playerCamera = Camera.main;
    }

    private void OnEnable()
    {
#if ENABLE_INPUT_SYSTEM
        if (interactAction != null && interactAction.action != null)
        {
            interactAction.action.Enable();
        }
#endif
    }

    private void OnDisable()
    {
        if (current != null)
        {
            current.OnDefocus(gameObject);
            current = null;
        }

        promptUI?.Hide();
        holdTimer = 0f;

#if ENABLE_INPUT_SYSTEM
        if (interactAction != null && interactAction.action != null)
        {
            interactAction.action.Disable();
        }
#endif
    }

    private void Update()
    {
        if (playerCamera == null || settings == null)
            return;

        UpdateFocus();
        UpdateInteraction();
    }

    private void UpdateFocus()
    {
        IInteractable found = FindInteractableInView();

        if (!ReferenceEquals(found, current))
        {
            if (current != null)
                current.OnDefocus(gameObject);

            current = found;

            if (current != null)
                current.OnFocus(gameObject);
        }

        if (current != null && current.CanInteract(gameObject))
        {
            string action = current.GetInteractionPrompt();

            InteractionMode mode = InteractionMode.InheritFromInteractor;
            if (current is BaseInteractable baseInt)
                mode = baseInt.GetInteractionMode();

            bool useHold = settings != null && settings.holdToInteract;
            if (mode == InteractionMode.Press) useHold = false;
            else if (mode == InteractionMode.Hold) useHold = true;

            string key = GetPromptKeyGlyph();
            string fmt = useHold ? settings.holdPromptFormat : settings.pressPromptFormat;
            string message = string.Format(fmt, key, action);

            promptUI?.Show(message);
        }
        else
        {
            promptUI?.Hide();
            holdTimer = 0f;
            promptUI?.SetHoldProgress(0f);
        }
    }

    private void UpdateInteraction()
    {
        if (current == null || !current.CanInteract(gameObject))
            return;

        InteractionMode mode = InteractionMode.InheritFromInteractor;
        float effectiveHold = settings != null ? settings.holdDuration : 0.5f;

        if (current is BaseInteractable baseInt)
        {
            mode = baseInt.GetInteractionMode();
            if (baseInt.TryGetHoldDuration(out float customHold))
            {
                effectiveHold = customHold;
            }
        }

        bool useHold = settings != null && settings.holdToInteract;
        if (mode == InteractionMode.Press) useHold = false;
        else if (mode == InteractionMode.Hold) useHold = true;

        if (useHold)
        {
            if (IsInteractHeld())
            {
                holdTimer += Time.deltaTime;

                float progress = Mathf.Clamp01(holdTimer / Mathf.Max(0.0001f, effectiveHold));
                promptUI?.SetHoldProgress(progress);

                if (holdTimer >= effectiveHold)
                {
                    current.Interact(gameObject);
                    holdTimer = 0f;
                    promptUI?.SetHoldProgress(0f);
                }
            }
            else
            {
                holdTimer = 0f;
                promptUI?.SetHoldProgress(0f);
            }
        }
        else
        {
            if (WasInteractPressedThisFrame())
            {
                current.Interact(gameObject);
            }

            promptUI?.SetHoldProgress(0f);
        }
    }

    private IInteractable FindInteractableInView()
    {
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit hit;

        bool didHit = Physics.SphereCast(
            ray,
            settings.sphereRadius,
            out hit,
            settings.interactDistance,
            settings.interactableMask,
            QueryTriggerInteraction.Ignore
        );

        if (!didHit)
            return null;

        return hit.collider.GetComponentInParent<IInteractable>();
    }

    // -------- Input Abstraction --------

    private bool WasInteractPressedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        if (HasValidNewInputAction())
        {
            // For button-type actions, .triggered is true on the frame performed
            return interactAction.action.triggered;
        }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER || !ENABLE_INPUT_SYSTEM
        // Fallback to legacy KeyCode
        return Input.GetKeyDown(settings.interactKey);
#else
        return false;
#endif
    }

    private bool IsInteractHeld()
    {
#if ENABLE_INPUT_SYSTEM
        if (HasValidNewInputAction())
        {
            return interactAction.action.IsPressed();
        }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER || !ENABLE_INPUT_SYSTEM
        return Input.GetKey(settings.interactKey);
#else
        return false;
#endif
    }

    private string GetPromptKeyGlyph()
    {
#if ENABLE_INPUT_SYSTEM
        if (HasValidNewInputAction())
        {
            return interactAction.action.GetBindingDisplayString();
        }
#endif
        return settings.interactKey.ToString();
    }

#if ENABLE_INPUT_SYSTEM
    private bool HasValidNewInputAction()
    {
        return interactAction != null && interactAction.action != null;
    }
#endif
}