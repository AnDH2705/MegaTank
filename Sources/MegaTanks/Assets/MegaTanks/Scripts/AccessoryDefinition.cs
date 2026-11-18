using UnityEngine;
using UnityEngine.Serialization;
namespace MegaTanks
{
    public enum AccessoryBonusType { ReloadSpeed, Accuracy, Evasion }
    [CreateAssetMenu(menuName = "MegaTanks/Equipment/Accessory")]
    public sealed class AccessoryDefinition : EquipmentDefinition
    {
        [Tooltip("Permanent save identity. Preserve across renames.")] public string id;
        public AccessoryBonusType bonusType;
        [FormerlySerializedAs("reloadSpeedBonus"),Min(0),Tooltip("Percentage bonus at level 1.")] public float baseBonus=10;
        [FormerlySerializedAs("reloadSpeedPerLevel"),Min(0),Tooltip("Percentage points added per level after level 1.")] public float bonusPerLevel=.5f;
        public override EquipmentType Type => EquipmentType.Accessory;
        public string StatLabel=>bonusType==AccessoryBonusType.ReloadSpeed?"Reload Speed":bonusType.ToString();
        public float Bonus(int level)=>Mathf.Max(0,baseBonus)+Mathf.Max(0,bonusPerLevel)*Mathf.Max(0,level-1);
        public float ReloadSpeed(int level)=>bonusType==AccessoryBonusType.ReloadSpeed?Bonus(level):0;
        public static float ReloadTime(float baseSeconds,float reloadSpeed)=>WeaponCombatStats.CalculateReloadTime(baseSeconds,reloadSpeed);
        void OnValidate(){if(string.IsNullOrWhiteSpace(id))id=System.Guid.NewGuid().ToString("N");}
    }
}
