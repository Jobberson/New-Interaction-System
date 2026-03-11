using UnityEngine;

namespace Snog.InteractionSystem.Runtime.Interfaces
{
    public interface IPromptRenderer
    {
        void Show(string message);
        void Hide();
        void SetHoldProgress(float progress01);
        void SetCrosshair(Sprite sprite, bool isAvailable);
        void ResetCrosshair();
        void SetImmediate(bool visible);
    }
}
