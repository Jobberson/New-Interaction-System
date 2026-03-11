using UnityEngine;

namespace Snog.InteractionSystem.Runtime.Data
{
    /// <summary>
    /// Rich prompt data returned by <see cref="Snog.InteractionSystem.Runtime.Interfaces.ICustomPrompt"/>.
    /// Use this when a simple string label isn't enough — e.g. locked doors, icon-based prompts,
    /// or prompts that stay visible when the object is unavailable.
    /// </summary>
    public struct InteractionPromptData
    {
        /// <summary>The action label shown when the object is available, e.g. "Open".</summary>
        public string label;

        /// <summary>If true, the entire label is used as the prompt string with no key prefix appended.</summary>
        public bool isFullSentence;

        /// <summary>Whether to show a prompt at all when the object cannot be interacted with.</summary>
        public bool showWhenUnavailable;

        /// <summary>The label shown when the object is unavailable, e.g. "Locked — Requires Red Keycard".</summary>
        public string unavailableLabel;

        /// <summary>Icon shown in the crosshair when interaction is available.</summary>
        public Sprite availableIcon;

        /// <summary>Icon shown in the crosshair when interaction is unavailable.</summary>
        public Sprite unavailableIcon;

        /// <summary>Fallback icon used for both states if the state-specific icon is null.</summary>
        public Sprite icon;
    }
}
