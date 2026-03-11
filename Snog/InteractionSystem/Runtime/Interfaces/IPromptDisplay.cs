using UnityEngine;

namespace Snog.InteractionSystem.Runtime.Interfaces
{
    /// <summary>
    /// Abstraction for the interaction HUD. PlayerInteractor talks to this interface,
    /// so you can swap in any UI implementation — world-space prompts, controller glyphs,
    /// custom HUD frameworks — without touching PlayerInteractor.
    ///
    /// <para>The built-in <see cref="Snog.InteractionSystem.Runtime.Core.InteractionPromptUI"/>
    /// implements this interface out of the box.</para>
    ///
    /// To create your own: add a MonoBehaviour that implements IPromptDisplay, drop it on any
    /// GameObject, then assign it to the "Prompt Display" slot on PlayerInteractor.
    /// </summary>
    public interface IPromptDisplay
    {
        /// <summary>Shows the prompt text and fades the panel in.</summary>
        void Show(string message);

        /// <summary>Hides the prompt text. The crosshair, if any, stays visible.</summary>
        void Hide();

        /// <summary>Sets visibility instantly with no fade transition.</summary>
        void SetImmediate(bool visible);

        /// <summary>
        /// Updates the hold-progress indicator (0 = empty, 1 = full).
        /// Call with 0 to reset.
        /// </summary>
        void SetHoldProgress(float progress01);

        /// <summary>
        /// Switches the crosshair sprite and tints it based on whether interaction is available.
        /// Pass null for sprite to keep the current default.
        /// </summary>
        void SetCrosshair(Sprite sprite, bool isAvailable);

        /// <summary>Resets the crosshair to its default sprite and colour.</summary>
        void ResetCrosshair();
    }
}
