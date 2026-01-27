using UnityEngine;

namespace Snog.InteractionSystem.Runtime.Data
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