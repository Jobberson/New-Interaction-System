namespace Snog.InteractionSystem.Runtime.Data
{
    /// <summary>
    /// Defines how an interaction is triggered on a per-object basis.
    /// </summary>
    public enum InteractionMode
    {
        /// <summary>Use the PlayerInteractor's global setting (holdToInteract in InteractionSettings).</summary>
        InheritFromInteractor = 0,

        /// <summary>Interaction triggers on a single button press, regardless of the global setting.</summary>
        Press = 1,

        /// <summary>Interaction triggers after holding for the configured duration, regardless of the global setting.</summary>
        Hold = 2,
    }
}
