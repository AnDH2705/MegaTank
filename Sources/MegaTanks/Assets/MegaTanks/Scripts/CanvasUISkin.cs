using UnityEngine;
namespace MegaTanks
{
    [CreateAssetMenu(menuName="MegaTanks/UI/Canvas Skin")]
    public sealed class CanvasUISkin : ScriptableObject
    {
        public Sprite buttonBlue,buttonDark,buttonDisabled,buttonMint,buttonRed,buttonSky,buttonYellow;
        public Sprite panel,panelHeader,roundFrame,itemFrame;
        public Sprite check,close,menu,pause,previous,next,music,sound,settings;
        public Sprite sliderBackground,sliderFill,sliderHandle,barBackground,barFill,switchBackground,switchFill,switchHandle;
    }
}
