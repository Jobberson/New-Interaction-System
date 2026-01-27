
using System;

namespace Snog.InteractionSystem.Scripts.Data
{
    /// <summary>
    /// Defines how an interaction should be triggered.
    /// </summary>
    public enum InteractionMode
    {
        /// <summary>
        /// Use the interactor's current setting (for example, global press/hold setting).
        /// </summary>
        InheritFromInteractor = 0,

        /// <summary>
        /// Interaction triggers on a single press.
        /// </summary>
        Press = 1,

        /// <summary>
        /// Interaction triggers after holding for the configured duration.
        /// </summary>
        Hold = 2
    }
}
