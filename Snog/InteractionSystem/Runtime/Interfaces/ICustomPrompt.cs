using Snog.InteractionSystem.Runtime.Data;

namespace Snog.InteractionSystem.Runtime.Interfaces
{
    /// <summary>
    /// Optional interface for interactables that need rich prompt data beyond a simple string label.
    /// Implement this alongside IInteractable to supply icons, unavailable states, and full-sentence prompts.
    /// </summary>
    public interface ICustomPrompt
    {
        InteractionPromptData GetPromptData();
    }
}
