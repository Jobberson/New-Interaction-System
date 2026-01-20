using UnityEngine;

namespace Snog.InteractionSystem.Scripts.Data
{
    public struct InteractionPromptData
    {
        public string label;
        public bool isFullSentence;

        public bool showWhenUnavailable;
        public string unavailableLabel;

        public Sprite availableIcon;
        public Sprite unavailableIcon;

        public Sprite icon;
    }
}