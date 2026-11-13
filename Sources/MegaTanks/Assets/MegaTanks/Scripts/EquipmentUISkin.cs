using UnityEngine;
namespace MegaTanks
{
    [CreateAssetMenu(menuName="MegaTanks/UI/Equipment Skin")]
    public sealed class EquipmentUISkin : ScriptableObject
    {
        public Sprite background,button,back,title,panel,plus,equippedBadge,equippedText;
        public Sprite itemCard,slot,slotSelected,dropdownArrow,dropdownField;
    }
}
