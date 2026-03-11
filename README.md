<div align="center">

# ⚡ Interaction System
### A modular, extensible interaction framework for Unity

[![Unity](https://img.shields.io/badge/Unity-2021.3%2B-black?logo=unity&logoColor=white)](https://unity.com)
[![License](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![TextMeshPro](https://img.shields.io/badge/Requires-TextMeshPro-orange)](https://docs.unity3d.com/Packages/com.unity.textmeshpro@latest)
[![Input System](https://img.shields.io/badge/Input-Legacy%20%2B%20New%20Input%20System-green)](https://docs.unity3d.com/Packages/com.unity.inputsystem@latest)

**Beginners get a working interaction system in minutes. Advanced users get a set of clean interfaces to replace any part of it.**

[Quick Start](#-quick-start) · [Architecture](#-architecture) · [Detection](#-detection-strategies) · [Conditions](#-interaction-conditions) · [Custom UI](#-custom-prompt-ui) · [Events](#-events) · [API Reference](#-api-reference)

</div>

---

## ✨ Features

| | |
|---|---|
| 🎯 **SphereCast + Proximity detection** | Forward SphereCast for FPS; nearest-in-radius for top-down/mobile |
| 🖥️ **Pluggable UI** | Implement `IPromptDisplay` to swap the HUD with zero code changes |
| 🔌 **Pluggable detection** | Implement `IInteractionDetector` for VR, click-to-interact, or any custom strategy |
| 📢 **C# events** | `OnFocusChanged` and `OnInteracted` for zero-coupling external systems |
| ⏱️ **Cooldown & charge system** | Per-object cooldowns and max-use limits, built into the base class |
| 📋 **ScriptableObject conditions** | Gate any interaction behind designer-friendly condition assets — no code required |
| 🔀 **Per-object press/hold override** | Individual objects can override the global hold/press mode |
| 🛠️ **Interactable Scaffolder** | Editor window that generates a full script + prefab from a preset in one click |
| 🔍 **Live debug overlay** | Runtime HUD shows focus, distance, `CanInteract`, and hold progress |
| ⌨️ **Dual input support** | Legacy `KeyCode` and Unity's New Input System, auto-detected via compile guards |
| 📦 **Assembly definitions** | Three `.asmdef` files for clean compile isolation |

---

## 🚀 Quick Start

### 1 — Player setup

Add `PlayerInteractor` to your player GameObject.

```
Player
 └─ PlayerInteractor ← the driver
 └─ InteractionDebugOverlay ← optional, toggle with F9
```

In the Inspector, assign:
- **Player Camera** — your main camera
- **Prompt Display** — an `InteractionPromptUI` component on your HUD canvas
- **Settings** — an `InteractionSettings` ScriptableObject (create via `Assets › Create › Snog › InteractionSystem › Settings`)

> **Tip:** Click **"Add Debug Overlay"** in the `PlayerInteractor` inspector to add the runtime HUD in one click.

---

### 2 — Create an interactable

**Option A — No code (Inspector only)**

1. Add `BaseInteractable` (or any subclass) to a GameObject.
2. Add a collider and set its layer to `Interactable`.
3. Wire up the `onInteract` UnityEvent using the **Add Quick Actions** buttons in the Inspector.

**Option B — Scaffolder (one click)**

Open `Tools › Interaction › Interactable Scaffolder`, pick a preset (Door, Pickup, Lever…), and click **Create**. A script and prefab are generated automatically.

**Option C — Write a subclass**

```csharp
using UnityEngine;
using Snog.InteractionSystem.Runtime.Helpers;

public class ChestInteractable : BaseInteractable
{
    [SerializeField] private GameObject lidPivot;
    private bool isOpen;

    protected override void Awake()
    {
        base.Awake(); // always call — initialises cooldown & use-count state
    }

    protected override void PerformInteraction(GameObject interactor)
    {
        isOpen = !isOpen;
        lidPivot.transform.localRotation = Quaternion.Euler(isOpen ? -90f : 0f, 0f, 0f);
    }

    public override string GetInteractionPrompt() => isOpen ? "Close" : "Open";
}
```

> `onInteract` fires automatically after `PerformInteraction` returns — don't call it yourself.

---

### 3 — Layer setup

Create an **Interactable** layer in your project, assign interactable objects to it, then select it in `InteractionSettings › Interactable Mask`.

> The settings inspector warns you if the mask is set to **Everything** (which causes the player's own collider to interfere with detection) or **Nothing**.

---

## 🏗️ Architecture

```
Runtime/
 ├─ Interfaces/
 │ ├─ IInteractable ← core contract (5 methods)
 │ ├─ IInteractableMode ← optional: per-object press/hold override
 │ ├─ ICustomPrompt ← optional: rich prompt data (icons, unavailable label)
 │ ├─ IInteractionDetector ← optional: swap the detection strategy
 │ └─ IPromptDisplay ← optional: swap the HUD
 ├─ Core/
 │ ├─ PlayerInteractor ← the driver; talks only to interfaces
 │ └─ InteractionPromptUI ← built-in HUD (implements IPromptDisplay)
 ├─ Helpers/
 │ ├─ BaseInteractable ← convenience base class (implements IInteractable + IInteractableMode)
 │ └─ QuickActions/ ← 10 ready-made MonoBehaviours for common onInteract responses
 ├─ Detection/
 │ ├─ RaycastDetector ← forward SphereCast (default, FPS)
 │ └─ ProximityDetector ← nearest in radius (top-down, mobile)
 ├─ Conditions/
 │ ├─ InteractionCondition ← abstract ScriptableObject base
 │ ├─ RequiresConditions ← MonoBehaviour that gates CanInteract()
 │ ├─ GameObjectActiveCondition
 │ └─ InvertCondition
 ├─ Data/
 │ ├─ InteractionSettings ← ScriptableObject: distance, mask, input, prompt format
 │ ├─ InteractionMode ← enum: InheritFromInteractor / Press / Hold
 │ └─ InteractionPromptData ← struct: label, icons, unavailable state
 └─ Debug/
     └─ InteractionDebugOverlay ← runtime HUD, safe in builds
```

The entire system is driven by **interfaces**. `PlayerInteractor` never holds a concrete reference to a detector, a prompt UI, or a `BaseInteractable` — only to `IInteractable`, `IInteractionDetector`, and `IPromptDisplay`. You can replace any layer without modifying the core.

---

## 🎛️ InteractionSettings

Create via `Assets › Create › Snog › InteractionSystem › Settings`.

| Field | Default | Description |
|---|---|---|
| `interactDistance` | `3.0` | Max range of the SphereCast |
| `interactableMask` | `Default` | Layers to detect — **don't use Everything** |
| `sphereRadius` | `0.05` | Cast radius — increase for easier aiming |
| `triggerInteraction` | `Ignore` | Whether trigger colliders are detected |
| `interactKey` | `E` | Legacy input fallback key |
| `holdToInteract` | `false` | Global press/hold mode |
| `holdDuration` | `0.5` | Seconds to hold (overridable per object) |
| `pressPromptFormat` | `[{0}] {1}` | `{0}` = key glyph, `{1}` = action label |
| `holdPromptFormat` | `Hold [{0}] to {1}` | Same tokens |

---

## 📡 Detection Strategies

### Built-in: RaycastDetector *(default)*
Forward SphereCast from the player camera. Best for FPS and third-person shooters.

```
Player
 └─ PlayerInteractor (Detector slot: RaycastDetector)
 └─ RaycastDetector
```

### Built-in: ProximityDetector
Selects the nearest `IInteractable` within a configurable radius using `OverlapSphereNonAlloc`. Best for top-down RPGs, isometric games, and mobile games with a single interact button.

```
Player
 └─ PlayerInteractor (Detector slot: ProximityDetector)
 └─ ProximityDetector (Radius: 2.5)
```

Both can be added via the **Detection** panel in the `PlayerInteractor` inspector — no drag-and-drop needed.

### Custom detector

```csharp
using UnityEngine;
using Snog.InteractionSystem.Runtime.Interfaces;

// Click-to-interact example
public class MouseClickDetector : MonoBehaviour, IInteractionDetector
{
    [SerializeField] private Camera cam;
    [SerializeField] private LayerMask mask;

    public IInteractable FindInteractable()
    {
        if (!Input.GetMouseButtonDown(0)) return null;
        var ray = cam.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit, 100f, mask)) return null;
        return hit.collider.GetComponentInParent<IInteractable>();
    }
}
```

Attach it to any GameObject, then drag it into the **Detector** slot on `PlayerInteractor`.

---

## 🎨 Custom Prompt UI

Implement `IPromptDisplay` on any `MonoBehaviour` and assign it to **Prompt Display** on `PlayerInteractor`.

```csharp
using UnityEngine;
using Snog.InteractionSystem.Runtime.Interfaces;

// World-space floating label example
public class WorldSpacePromptUI : MonoBehaviour, IPromptDisplay
{
    [SerializeField] private TextMeshPro label;
    [SerializeField] private Transform targetObject;

    public void Show(string message)
    {
        label.text = message;
        label.gameObject.SetActive(true);
        transform.position = targetObject.position + Vector3.up * 1.5f;
    }

    public void Hide() => label.gameObject.SetActive(false);
    public void SetImmediate(bool visible) => label.gameObject.SetActive(visible);
    public void SetHoldProgress(float progress01) { /* optional */ }
    public void SetCrosshair(Sprite s, bool avail) { /* optional */ }
    public void ResetCrosshair() { /* optional */ }
}
```

---

## 📋 Interaction Conditions

Gate any interactable behind designer-configured `ScriptableObject` conditions — **no code required**.

### Setup

1. Create a condition asset via `Assets › Create › Snog › InteractionSystem › Conditions › ...`
2. Add a `RequiresConditions` component to your interactable GameObject.
3. Drag the condition asset into the **Conditions** list.
4. Choose **AND** (all must pass) or **OR** (any one passing is enough).

### Built-in conditions

| Condition | Description |
|---|---|
| `GameObjectActiveCondition` | Passes when a target GameObject is active (or inactive) |
| `InvertCondition` | Flips any other condition — passes when it fails, fails when it passes |

### Custom condition

```csharp
using UnityEngine;
using Snog.InteractionSystem.Runtime.Conditions;

[CreateAssetMenu(menuName = "Interaction/Conditions/Has Item")]
public class HasItemCondition : InteractionCondition
{
    [SerializeField] private string requiredItemId;

    public override bool Evaluate(GameObject interactor)
    {
        // plug into your inventory system
        return Inventory.Instance.Has(requiredItemId);
    }

    public override string GetFailureReason() =>
        $"Requires {requiredItemId}";
}
```

The `RequiresConditions` inspector shows a **live pass/fail status** for each condition during Play Mode.

---

## 📢 Events

Subscribe to `PlayerInteractor`'s C# events for zero-coupling integration with external systems.

```csharp
void Start()
{
    var interactor = GetComponent<PlayerInteractor>();

    // Fires whenever the focused object changes (either value may be null)
    interactor.OnFocusChanged += (prev, next) =>
    {
        if (next != null)
            tutorialManager.OnPlayerLookedAt(next);
    };

    // Fires after every completed interaction
    interactor.OnInteracted += interactable =>
    {
        questManager.NotifyInteraction(interactable);
        achievementTracker.Track(interactable);
        audioManager.PlayInteractSfx();
    };
}
```

No `UnityEvent` wiring in the Inspector — subscribe in code and stay decoupled.

---

## ⏱️ Cooldown & Charges

Configure per-object on any `BaseInteractable` subclass — no code needed.

| Setting | Description |
|---|---|
| **Cooldown (s)** | Seconds before the object can be used again. `0` = no cooldown. |
| **Max Uses** | How many times it can be triggered total. `-1` = unlimited. |

The inspector shows a plain-English summary: *"Up to 3 use(s) with 2.0s cooldown"*. During Play Mode, a live panel shows cooldown remaining and uses left.

```csharp
// Runtime API
interactable.CooldownRemaining // float — seconds left on cooldown
interactable.UsesRemaining // int — uses left (-1 if unlimited)
interactable.ResetCooldown(); // clears the cooldown immediately
interactable.ResetUses(); // restores uses to max
```

---

## 🧩 Quick Actions

Ten ready-made components you can add via the **Add Quick Actions** buttons in the `BaseInteractable` inspector. Each is auto-wired to `onInteract`.

| Component | Does |
|---|---|
| `OnInteract_PlayAudio` | Plays an `AudioSource` |
| `OnInteract_AnimatorTrigger` | Sets an `Animator` trigger |
| `OnInteract_SetActive` | Sets GameObjects active/inactive |
| `OnInteract_ToggleActive` | Toggles GameObject active state |
| `OnInteract_EnableComponent` | Enables `Behaviour` components |
| `OnInteract_DisableComponent` | Disables `Behaviour` components |
| `OnInteract_InvokeEvent` | Fires a standalone `UnityEvent` |
| `OnInteract_ReloadScene` | Reloads the active scene |
| `OnInteract_QuitApplication` | Quits the application |
| `OnInteract_DebugLog` | Prints a message to the Console |

---

## 🔌 Implementing IInteractable Directly

Skip `BaseInteractable` entirely for full control:

```csharp
using UnityEngine;
using Snog.InteractionSystem.Runtime.Data;
using Snog.InteractionSystem.Runtime.Interfaces;

public class TripwireTrap : MonoBehaviour, IInteractable, IInteractableMode, ICustomPrompt
{
    private bool armed = true;

    // IInteractable
    public string GetInteractionPrompt() => armed ? "Disarm" : "(disarmed)";
    public bool CanInteract(GameObject g) => armed;
    public void OnFocus(GameObject g) { /* show glow */ }
    public void OnDefocus(GameObject g) { /* hide glow */ }
    public void Interact(GameObject g) { armed = false; }

    // IInteractableMode — force hold-to-disarm regardless of global setting
    public InteractionMode GetInteractionMode() => InteractionMode.Hold;
    public bool TryGetHoldDuration(out float s) { s = 2f; return true; }

    // ICustomPrompt — red lock icon when unavailable
    public InteractionPromptData GetPromptData() => new InteractionPromptData
    {
        label = "Disarm",
        showWhenUnavailable = true,
        unavailableLabel = "(already disarmed)",
        availableIcon = disarmIcon,
    };
    [SerializeField] private Sprite disarmIcon;
}
```

---

## 📖 API Reference

### `PlayerInteractor`

| Member | Description |
|---|---|
| `IInteractable CurrentInteractable` | The object currently in focus, or `null` |
| `float HoldTimer` | Seconds the button has been held this cycle |
| `InteractionSettings Settings` | The active settings asset |
| `Camera PlayerCamera` | The camera used for built-in detection |
| `event OnFocusChanged(prev, next)` | Fires when focused object changes |
| `event OnInteracted(interactable)` | Fires after each completed interaction |

### `BaseInteractable`

| Member | Description |
|---|---|
| `void SetEnabled(bool)` | Enable/disable at runtime |
| `float CooldownRemaining` | Seconds left on cooldown (`0` if not cooling down) |
| `int UsesRemaining` | Uses left (`-1` if unlimited) |
| `bool HasUnlimitedUses` | `true` when `maxUses == -1` |
| `void ResetCooldown()` | Clears the cooldown immediately |
| `void ResetUses()` | Restores uses to the configured max |
| `UnityEvent OnInteractEvent` | The `onInteract` event (for editor tooling) |
| `protected abstract void PerformInteraction(GameObject)` | Your interaction logic goes here |

### `RequiresConditions`

| Member | Description |
|---|---|
| `bool Evaluate(GameObject)` | Evaluates all conditions using AND/OR logic |
| `string GetFirstFailureReason(GameObject)` | Returns the first failing condition's reason string |
| `InteractionCondition[] Conditions` | Read-only access to the condition list |
| `bool RequireAll` | `true` = AND, `false` = OR |

---

## 📁 Project Structure

```
InteractionSystem/
 ├─ Runtime/
 │ ├─ Snog.InteractionSystem.Runtime.asmdef
 │ ├─ Core/ PlayerInteractor, InteractionPromptUI
 │ ├─ Interfaces/ IInteractable, IInteractableMode, ICustomPrompt,
 │ │ IInteractionDetector, IPromptDisplay
 │ ├─ Helpers/ BaseInteractable, QuickActions/
 │ ├─ Detection/ RaycastDetector, ProximityDetector
 │ ├─ Conditions/ InteractionCondition, RequiresConditions,
 │ │ GameObjectActiveCondition, InvertCondition
 │ ├─ Data/ InteractionSettings, InteractionMode, InteractionPromptData
 │ └─ Debug/ InteractionDebugOverlay
 ├─ Editor/
 │ ├─ Snog.InteractionSystem.Editor.asmdef
 │ ├─ Inspectors/ Custom inspectors for all runtime types
 │ └─ Windows/ InteractableScaffolderWindow
 └─ Samples/
     ├─ Snog.InteractionSystem.Samples.asmdef
     └─ Examples/ KeycardDoorInteractable (full ICustomPrompt demo)
```

---

## 🔧 Requirements

- **Unity** 2021.3 LTS or newer
- **TextMeshPro** (included in Unity — install via Package Manager if missing)
- **New Input System** *(optional)* — legacy `KeyCode` works out of the box

