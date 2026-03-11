namespace Snog.InteractionSystem.Runtime.Interfaces
{
    /// <summary>
    /// Abstraction for the object-detection strategy used by PlayerInteractor.
    /// The default strategy is a forward SphereCast (first-person / third-person shooter).
    ///
    /// <para>Implement this interface on a MonoBehaviour and assign it to the "Detector" slot
    /// on PlayerInteractor to use a different detection strategy — for example:</para>
    /// <list type="bullet">
    ///   <item>Nearest interactable within a radius (top-down RPG, proximity-triggered)</item>
    ///   <item>Screen-space click raycasting (point-and-click)</item>
    ///   <item>VR hand overlap detection</item>
    /// </list>
    ///
    /// Built-in implementations:
    /// <list type="bullet">
    ///   <item><see cref="Snog.InteractionSystem.Runtime.Detection.RaycastDetector"/> — forward SphereCast (default)</item>
    ///   <item><see cref="Snog.InteractionSystem.Runtime.Detection.ProximityDetector"/> — nearest in radius</item>
    /// </list>
    /// </summary>
    public interface IInteractionDetector
    {
        /// <summary>
        /// Returns the best IInteractable candidate this frame, or null if nothing is in range.
        /// Called every frame by PlayerInteractor.
        /// </summary>
        IInteractable FindInteractable();
    }
}
