using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MegaTanks.Editor
{
    public static class ArmorSetup
    {
        public const string BasicPath="Assets/MegaTanks/Data/Armor/BasicArmor.asset";
        public const string LightPath="Assets/MegaTanks/Data/Armor/LightArmor.asset";
        static Sprite Import(string name)
        {
            string path="Assets/MegaTanks/Art/Armor/"+name+".png";
            AssetDatabase.ImportAsset(path);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
            importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.maxTextureSize=512;
            importer.textureCompression=TextureImporterCompression.Uncompressed;importer.filterMode=FilterMode.Bilinear;importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        static ArmorDefinition Create(string path,string id,string title,Sprite icon,float hp,float armor,float speed,float evasion,float perLevel)
        {
            var definition=AssetDatabase.LoadAssetAtPath<ArmorDefinition>(path);
            if(definition==null)
            {
                definition=ScriptableObject.CreateInstance<ArmorDefinition>();definition.id=id;definition.displayName=title;
                definition.hp=hp;definition.armor=armor;definition.speed=speed;definition.evasion=evasion;definition.hpPerLevel=perLevel;
                definition.icon=icon;AssetDatabase.CreateAsset(definition,path);
            }
            if(definition.icon==null)definition.icon=icon;
            EditorUtility.SetDirty(definition);return definition;
        }
        [MenuItem("MegaTanks/Configure Sample Armor")]
        public static void Install()
        {
            if(!AssetDatabase.IsValidFolder("Assets/MegaTanks/Data/Armor"))AssetDatabase.CreateFolder("Assets/MegaTanks/Data","Armor");
            var basic=Create(BasicPath,"armor.basic","Basic Armor",Import("BasicArmor"),100,10,0,0,10);
            var light=Create(LightPath,"armor.light","Light Armor",Import("LightArmor"),50,5,.5f,5,5);
            var catalog=AssetDatabase.LoadAssetAtPath<FrontendCatalog>("Assets/MegaTanks/Data/UI/Frontend.asset");
            catalog.inventory=(catalog.inventory??Array.Empty<EquipmentDefinition>()).Concat(new EquipmentDefinition[]{basic,light}).Distinct().ToArray();
            EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssets();
            Debug.Log("ARMOR_SETUP_PASS");
        }
        public static void InstallAndGrant()
        {
            Install();
            var catalog=AssetDatabase.LoadAssetAtPath<FrontendCatalog>("Assets/MegaTanks/Data/UI/Frontend.asset");
            var wallet=new PlayerWallet();var progression=new PlayerProgression(catalog,wallet);
            PlaytestResources.GrantArmorOnce(catalog,progression);
            foreach(var armor in catalog.inventory.OfType<ArmorDefinition>())
                if(progression.Get(armor).Fragments<1000)throw new Exception("Armor play-test blueprints must be granted.");
            Debug.Log("ARMOR_BLUEPRINTS_GRANTED");
        }
    }
}
