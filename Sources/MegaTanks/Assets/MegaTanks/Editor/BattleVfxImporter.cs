using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MegaTanks.Editor
{
    public static class BattleVfxImporter
    {
        public const string ArtRoot="Assets/MegaTanks/Art/VFX/";
        public const string CatalogPath="Assets/MegaTanks/Data/Resources/BattleVfx.asset";
        [Serializable] sealed class Frame {public int index;public string file;}
        [Serializable] sealed class Clip
        {
            public string name;public int frame_count;public float fps;public bool loop;
            public float[] pivot_normalized_bottom_left;public Frame[] frames;
        }
        [Serializable] sealed class Manifest {public Clip[] clips;}

        [MenuItem("MegaTanks/Import Tank VFX")]
        public static void Import()
        {
            AssetDatabase.Refresh();
            var manifest=JsonUtility.FromJson<Manifest>(File.ReadAllText(ArtRoot+"vfx_manifest.json"));
            Directory.CreateDirectory(Path.GetDirectoryName(CatalogPath));AssetDatabase.Refresh();
            var catalog=AssetDatabase.LoadAssetAtPath<BattleVfxCatalog>(CatalogPath);
            if(catalog==null){catalog=ScriptableObject.CreateInstance<BattleVfxCatalog>();AssetDatabase.CreateAsset(catalog,CatalogPath);}
            foreach(var clip in manifest.clips)
            {
                var frames=clip.frames.OrderBy(frame=>frame.index).Select(frame=>
                {
                    string path=ArtRoot+frame.file;
                    var importer=(TextureImporter)AssetImporter.GetAtPath(path);
                    if(importer==null)throw new InvalidOperationException("Missing VFX frame: "+path);
                    importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
                    importer.spritePixelsPerUnit=320;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;
                    importer.npotScale=TextureImporterNPOTScale.None;importer.maxTextureSize=512;
                    importer.textureCompression=TextureImporterCompression.Uncompressed;
                    importer.filterMode=FilterMode.Bilinear;importer.wrapMode=TextureWrapMode.Clamp;
                    var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);
                    settings.spriteAlignment=9;settings.spriteMeshType=SpriteMeshType.FullRect;
                    settings.spritePivot=new Vector2(clip.pivot_normalized_bottom_left[0],clip.pivot_normalized_bottom_left[1]);
                    importer.SetTextureSettings(settings);importer.SaveAndReimport();
                    return AssetDatabase.LoadAssetAtPath<Sprite>(path);
                }).ToArray();
                if(frames.Length!=clip.frame_count||frames.Any(frame=>frame==null))throw new InvalidOperationException("Incomplete VFX clip: "+clip.name);
                var data=new BattleVfxClip {frames=frames,framesPerSecond=clip.fps,loop=clip.loop};
                switch(clip.name)
                {
                    case "ground_fire":catalog.groundFire=data;break;
                    case "explosion":catalog.explosion=data;break;
                    case "mg_tracer":catalog.tracer=data;break;
                    case "muzzle_flash":catalog.muzzle=data;break;
                    case "shell_smoke_trail":catalog.smoke=data;break;
                    case "impact_sparks":catalog.impact=data;break;
                    default:throw new InvalidOperationException("Unknown VFX clip: "+clip.name);
                }
            }
            EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssets();
            Debug.Log("MEGATANKS_VFX_IMPORT_OK: 48 supplied frames, 6 clips, manifest pivots/fps/loops, alpha sprites and runtime catalog.");
        }
    }
}
