
# Snog's Interaction System

A drop-in, designer-friendly interaction toolkit for Unity.
Focus objects, show context prompts, support **Press**/**Hold** interactions, and drive an on-screen **crosshair** that swaps to **per-interaction icons** (with optional "unavailable" state icons). Includes a **Setup Wizard**, **Interaction Creator (Scaffolder)**, **QuickStart Prefab**, **demo scene**, and editor **quick-action** buttons.

> Works with Legacy Input (KeyCode) and the **New Input System**.
> Designed for quick onboarding **and** scalable production.

---

## Features

- **Plug-and-play interactor** using a forward SphereCast from the player camera.
- **Press / Hold** interaction modes, with per-object **hold-time overrides**.
- **Prompt UI** with:
  - Dynamic prompt text (e.g., `"Press E to Open"` / `"Hold E to Save"`),
  - **Hold progress ring**,
  - Center **crosshair** that swaps to **available / unavailable icons** per interactable.
- **Custom prompts** via `ICustomPrompt` + `InteractionPromptData`.
- **Editor tools**
  - **Setup Wizard**: creates settings, a QuickStart prefab, optional demo scene.
  - **Interaction Creator (Scaffolder)**: generates interactable scripts & prefabs from presets (Door, Pickup, etc.).
  - **Quick Actions** on interactables: one-click **Play Audio**, **Animator Trigger**, **Set Active**, etc.
- **Extensible API**: `IInteractable`, `BaseInteractable`, and UnityEvents for no-code hooks.
- **Demo** scene to test immediately.

---

## Requirements

- **Unity**: 2021.3 LTS or newer (recommend LTS).
  - **Tested on Unity 6 6000.0.62f1** 
---

## Quick Start (60 seconds)

1. **Open the Setup Wizard**  
   `Tools -> Interaction -> Setup Wizard`

2. Click **Create / Update Assets**  
   This creates:
   - `InteractionSettings.asset` (in `Resources/`),
   - `Interaction_Starter.prefab` (camera + UI + interactor),
   - Optional `Interaction_Demo.unity`.

3. **Play the demo**  
   - Click **Open Demo Scene (if created)**, or
   - Drag **Interaction_Starter.prefab** into any scene and press **Play**.

You should see a **crosshair** in the center. Look at the example object to see a **prompt** (and hold ring if configured). Interact with **E** (or your New Input System binding).

---

## Core Concepts

### PlayerInteractor

The brain of the system:

- Performs a SphereCast from the **player camera** to find an `IInteractable`.
- Tracks focus changes (`OnFocus` / `OnDefocus`).
- Decides whether to **Press** or **Hold** (global setting vs per-object override).
- Sends prompt text, **hold progress**, and **icon selection** to the UI.

**Key fields (serialized):**

- `playerCamera` - Camera used for ray/SphereCast.
- `promptUI` - Reference to `InteractionPromptUI`.
- `settings` - `InteractionSettings` ScriptableObject.

### InteractionSettings (ScriptableObject)

Global configuration:

- **Detection**: `interactDistance`, `interactableMask`, `sphereRadius`.
- **Input**: `interactKey` (Legacy), `holdToInteract`, `holdDuration`.
- **Prompt**: `pressPromptFormat`, `holdPromptFormat`  
  (e.g., `"Press {0} to {1}"` => `{0}` = input glyph; `{1}` = action label).

> The Setup Wizard creates and assigns this automatically.

### InteractionPromptUI

Handles on-screen feedback:

- `Show(string message)` / `Hide()` - prompt text + fade.
- `SetHoldProgress(float)` - updates the **radial hold ring**.
- `SetCrosshair(Sprite, bool isAvailable)` / `ResetCrosshair()` - center icon:
  - Always visible **by default** (use a default crosshair sprite).
  - Swaps to the interactable's **available** or **unavailable** icon when focused.

**Assign in prefab/inspector:**

- `promptText` (UGUI Text or TMP variant)
- `holdFillImage` (radial Image)
- `crosshairImage` (center Image)
- `defaultCrosshairSprite` (a small dot/plus sprite)

### IInteractable & BaseInteractable

- `IInteractable` is the minimal contract: prompt text, availability, focus hooks, `Interact`.  
- `BaseInteractable` implements most of this and adds:
  - Per-object **Interaction Mode**: `Press`, `Hold`, or `InheritFromInteractor`.
  - Per-object **hold duration override**.
  - `OnInteractEvent` (UnityEvent) so designers can hook actions without code.

Derive your own interactables by inheriting `BaseInteractable` and implementing:

```csharp
protected override void PerformInteraction(GameObject interactor)
{
    // Your logic here
}
```

### ICustomPrompt & InteractionPromptData

For richer prompts & icons, implement `ICustomPrompt`:

```csharp
public InteractionPromptData GetPromptData()
{
    return new InteractionPromptData
    {
        label = "Open",
        isFullSentence = false,
        showWhenUnavailable = isLocked,
        unavailableLabel = "Locked — Requires Red Keycard",
        availableIcon = handIcon,
        unavailableIcon = lockIcon,
        icon = handIcon // optional single-icon fallback
    };
}
```

- `availableIcon`/`unavailableIcon` power the **center crosshair** when focused.
- `showWhenUnavailable` + `unavailableLabel` let the UI show a **“blocked”** message.
- If only one icon is assigned, the system can **fallback** to use it for both states.

---

## Editor Tools

### Setup Wizard

- Creates **Settings**, **QuickStart Prefab**, and an optional **Demo Scene**.
- Optionally generates a basic **InputActionAsset** and assigns an **InputActionReference** for the New Input System.

### Scaffolder

`Tools -> Interaction -> Interactable Scaffolder (Extended)`

- Choose a preset (Door, Pickup, Lever, Readable, Button, SceneLoader, SavePoint, Custom).
- Generates:
  - A script inheriting `BaseInteractable` with sensible defaults.
  - An optional prefab (with collider/material).
- Great for standardizing interactables across a team.

### BaseInteractable Editor (Quick Actions)

In the inspector for any `BaseInteractable`, add common actions with one click:

- **Play Audio** - adds/assigns `AudioSource` & binds to `OnInteractEvent`.
- **Animator Trigger** - adds/assigns `Animator` & triggers a named parameter.
- **Set Active** - toggles or sets active state on target GameObjects.

Built with **Undo**, **Prefab override** support, and **duplicate guard** for listeners.

---

## Input Support

- **Legacy Input**: uses `KeyCode` (e.g., `E`).
- **New Input System**:
  - Assign an `InputActionReference` to the interactor (Wizard can generate one).
  - `GetBindingDisplayString()` is used for the `{0}` in your prompt formats to display a platform-appropriate glyph when possible.

---

## Demo Scene

Open **Interaction_Demo.unity** to see:

- A basic environment
- The **Interaction_Starter** prefab in action
- An example interactable with prompt and hold behavior

---

## Layers

The Wizard will ensure an **"Interactable"** layer exists and include it in `InteractionSettings.interactableMask`.  
Add your interactable objects to this layer (or include your object's layer in the mask).

---

## Best Practices

- **Default crosshair**: assign a small sprite (e.g., a dot) to `defaultCrosshairSprite`.
- **Icon sizing**: keep crosshair small when idle (8-12 px); your per-interaction icons can be larger (24-32 px) if desired.
- **Performance**: SphereCast once per frame from the camera; keep interaction distance modest (2-4 m) and mask tight.
- **Designer workflow**: prefer `BaseInteractable` + **Quick Actions** for no-code behavior. Use `ICustomPrompt` for bespoke states/messages/icons.

---

## Extending

- **Custom UI**: replicate `InteractionPromptUI` in TMP or your own style; wire fields (text, ring, crosshair).
- **New interactables**: inherit `BaseInteractable` and implement `PerformInteraction`.
- **Conditional availability**: override `CanInteract()` and/or implement `ICustomPrompt` to surface "blocked" reasons & icons.
- **Scaffolder presets**: add your own presets for your project's common patterns.

---

## Troubleshooting

- **No crosshair shown**  
  Ensure the crosshair `Image` has a sprite or `defaultCrosshairSprite` is assigned. The UI disables the image if there's no sprite.

- **Prompt not showing**  
  Check:
  - `InteractionSettings.interactableMask` includes your object's layer,
  - The object has a collider,
  - `CanInteract()` returns `true` (or `showWhenUnavailable = true` if blocked).

- **Hold interaction never completes**  
  Verify the effective hold time:
  - Global `holdDuration` in `InteractionSettings`,
  - Per-object override in `BaseInteractable` if enabled.

- **New Input System not responding**  
  Confirm:
  - `ENABLE_INPUT_SYSTEM` is defined (Project Settings -> Player -> Scripting Define Symbols),
  - The `InputActionReference` is assigned and the action is **enabled**,
  - The Interact binding is present (Keyboard/Gamepad).
