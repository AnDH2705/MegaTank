using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace CatTanks.Editor
{
    [InitializeOnLoad]
    public static class MonsterAnimationImporter
    {
        public const string Destination="Assets/CatTanks/Art/MonsterAnimations";
        public const string LibraryPath="Assets/CatTanks/Resources/MonsterAnimations.asset";
        public static string Source=>Path.GetFullPath(Path.Combine(Application.dataPath,"../../../Art/CatTanks_Visual_Reference/01-Quai-va-animation"));
        sealed class Plan{public string species,atlas,transition;public int version;}
        static FileSystemWatcher watcher;
        static int pending;static long changedAt;
        static bool importing;
        static MonsterAnimationImporter()
        {
            if(Application.isBatchMode)return;
            EditorApplication.delayCall+=StartWatching;
            EditorApplication.update+=Update;
            AssemblyReloadEvents.beforeAssemblyReload+=StopWatching;
            EditorApplication.quitting+=StopWatching;
        }
        static void StartWatching()
        {
            if(!Directory.Exists(Source))return;
            watcher=new FileSystemWatcher(Source,"*"){IncludeSubdirectories=true,NotifyFilter=NotifyFilters.LastWrite|NotifyFilters.FileName|NotifyFilters.DirectoryName};
            watcher.Changed+=(s,e)=>Queue();watcher.Created+=(s,e)=>Queue();watcher.Deleted+=(s,e)=>Queue();watcher.Renamed+=(s,e)=>Queue();
            watcher.EnableRaisingEvents=true;Queue();
        }
        static void StopWatching(){if(watcher!=null){watcher.Dispose();watcher=null;}}
        static void Queue(){Interlocked.Exchange(ref changedAt,DateTime.UtcNow.Ticks);Interlocked.Exchange(ref pending,1);}
        static void Update()
        {
            if(pending==0||importing||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;
            if(DateTime.UtcNow.Ticks-Interlocked.Read(ref changedAt)<TimeSpan.FromSeconds(2).Ticks)return;
            Interlocked.Exchange(ref pending,0);
            try{Sync(false);}catch(Exception e){Debug.LogWarning("MONSTER_ANIMATION_SYNC_PENDING: "+e.Message);Queue();}
        }
        [MenuItem("CatTanks/Combat/Import Latest Monster Animations")]
        public static void ImportLatest(){Sync(true);}
        static int Version(string path)
        {
            return Regex.Matches(path,@"(?:^|[_/\\-])v(\d+)(?=[_/\\.\-]|$)",RegexOptions.IgnoreCase).Cast<Match>().Select(m=>int.Parse(m.Groups[1].Value)).DefaultIfEmpty(0).Max();
        }
        static List<Plan> Plans()
        {
            if(!Directory.Exists(Source))throw new DirectoryNotFoundException(Source);
            var result=new List<Plan>();
            foreach(var folder in Directory.GetDirectories(Source).OrderBy(p=>p,StringComparer.Ordinal))
            {
                var match=Regex.Match(Path.GetFileName(folder),@"_(bug-\d+|beast-\d+)$");if(!match.Success)continue;
                string atlas=Directory.GetFiles(folder,"*.png",SearchOption.AllDirectories)
                    .Where(p=>Path.GetFileName(p).Contains("sheet-idle-run-attack-hit"))
                    .OrderByDescending(Version).ThenByDescending(File.GetLastWriteTimeUtc).ThenBy(p=>p,StringComparer.Ordinal).FirstOrDefault();
                if(atlas==null)throw new InvalidDataException("Missing animation atlas: "+folder);
                string transition=Directory.GetFiles(Path.GetDirectoryName(atlas),"*.png")
                    .Where(p=>Path.GetFileName(p).IndexOf("4x1",StringComparison.OrdinalIgnoreCase)>=0&&Regex.IsMatch(Path.GetFileName(p),"vao|idle-sang-run",RegexOptions.IgnoreCase)).OrderBy(p=>p,StringComparer.Ordinal).FirstOrDefault();
                if(transition==null)throw new InvalidDataException("Missing idle-to-run transition: "+atlas);
                result.Add(new Plan{species=match.Groups[1].Value,atlas=atlas,transition=transition,version=Version(atlas)});
            }
            var required=Catalog.Load().monsters.Select(m=>m.asset).OrderBy(p=>p).ToArray();
            if(!required.SequenceEqual(result.Select(p=>p.species).OrderBy(p=>p)))throw new InvalidDataException("Animation source does not cover the game monster catalog exactly.");
            return result;
        }
        static string Signature(List<Plan> plans)
        {
            using(var sha=SHA256.Create())
            {
                var data=new StringBuilder();
                foreach(var p in plans)foreach(var file in new[]{p.atlas,p.transition})data.Append(file).Append('|').Append(Convert.ToBase64String(sha.ComputeHash(File.ReadAllBytes(file)))).Append('\n');
                return Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(data.ToString())));
            }
        }
        static void Sync(bool force)
        {
            if(importing)return;
            var plans=Plans();string signature=Signature(plans);
            var library=AssetDatabase.LoadAssetAtPath<MonsterAnimationLibrary>(LibraryPath);
            if(!force&&library&&library.sourceSignature==signature)return;
            importing=true;
            try
            {
                var entries=new List<MonsterAnimationLibrary.Entry>();
                foreach(var plan in plans)
                {
                    string folder=Destination+"/"+plan.species;Directory.CreateDirectory(folder);
                    string atlas=folder+"/atlas.png",transition=folder+"/enter-run.png";
                    File.Copy(plan.atlas,atlas,true);File.Copy(plan.transition,transition,true);
                    AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                    int cell;float referenceWidth;Vector2 pivot;
                    using(var source=new Decoded(plan.atlas))
                    {
                        var texture=source.texture;cell=texture.width/6;
                        if(texture.width%6!=0||texture.height!=cell*4)throw new InvalidDataException("Expected 6x4 square-cell atlas: "+plan.atlas);
                        var pixels=texture.GetPixels32();int minX=cell,minY=cell,maxX=-1,maxY=-1;
                        for(int y=0;y<cell;y++)for(int x=0;x<cell;x++)if(pixels[(texture.height-cell+y)*texture.width+x].a>8)
                        {minX=Mathf.Min(minX,x);maxX=Mathf.Max(maxX,x);minY=Mathf.Min(minY,y);maxY=Mathf.Max(maxY,y);}
                        if(maxX<minX)throw new InvalidDataException("Empty idle reference: "+plan.atlas);
                        referenceWidth=maxX-minX+1;pivot=new Vector2((minX+maxX+1)*.5f/cell,(float)minY/cell);
                    }
                    var frames=Slice(atlas,plan.species,new[]{"idle","run","attack","hit"},6,pivot,cell);
                    var enter=Slice(transition,plan.species,new[]{"enter"},4,pivot,cell);
                    Sprite[] Row(string pose)=>Enumerable.Range(0,6).Select(i=>frames[plan.species+"_"+pose+"_"+i]).ToArray();
                    entries.Add(new MonsterAnimationLibrary.Entry{species=plan.species,source=plan.atlas,version=plan.version,idle=Row("idle"),run=Row("run"),attack=Row("attack"),hit=Row("hit"),enterRun=new[]{enter[plan.species+"_enter_1"],enter[plan.species+"_enter_2"]},referenceWidth=referenceWidth,cellWidth=cell});
                }
                if(!library){library=ScriptableObject.CreateInstance<MonsterAnimationLibrary>();AssetDatabase.CreateAsset(library,LibraryPath);}
                library.entries=entries;library.sourceSignature=signature;library.revision++;EditorUtility.SetDirty(library);AssetDatabase.SaveAssets();
                Directory.CreateDirectory("Logs");File.WriteAllText("Logs/monster-animation-import.json",JsonConvert.SerializeObject(new{verdict="PASS",species=entries.Count,frames=entries.Sum(e=>e.idle.Length+e.run.Length+e.attack.Length+e.hit.Length+e.enterRun.Length),entries=entries.Select(e=>new{e.species,e.version,e.source,e.referenceWidth})},Formatting.Indented));
                Debug.Log("MONSTER_ANIMATION_IMPORT_OK: "+entries.Count+" species, latest version per species, stable foot pivots.");
            }
            finally{importing=false;}
        }
        sealed class Decoded:IDisposable
        {
            public Texture2D texture;
            public Decoded(string path){texture=new Texture2D(2,2,TextureFormat.RGBA32,false);if(!texture.LoadImage(File.ReadAllBytes(path)))throw new InvalidDataException(path);}
            public void Dispose(){UnityEngine.Object.DestroyImmediate(texture);}
        }
        static Dictionary<string,Sprite> Slice(string path,string species,string[] poses,int columns,Vector2 pivot,int cell)
        {
            using(var decoded=new Decoded(path))if(decoded.texture.width!=columns*cell||decoded.texture.height!=poses.Length*cell)throw new InvalidDataException("Cell dimensions differ: "+path);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Multiple;importer.spritePixelsPerUnit=cell;
            importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.npotScale=TextureImporterNPOTScale.None;importer.wrapMode=TextureWrapMode.Clamp;importer.filterMode=FilterMode.Bilinear;
            importer.maxTextureSize=2048;importer.textureCompression=TextureImporterCompression.CompressedHQ;
            var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);settings.spriteMeshType=SpriteMeshType.FullRect;importer.SetTextureSettings(settings);
            var factory=new SpriteDataProviderFactories();factory.Init();var provider=factory.GetSpriteEditorDataProviderFromObject(importer);provider.InitSpriteEditorDataProvider();
            var old=provider.GetSpriteRects().ToDictionary(r=>r.name,r=>r.spriteID);var rects=new List<SpriteRect>();
            for(int row=0;row<poses.Length;row++)for(int col=0;col<columns;col++)
            {
                string name=species+"_"+poses[row]+"_"+col;
                rects.Add(new SpriteRect{name=name,spriteID=old.TryGetValue(name,out var id)?id:UnityEditor.GUID.Generate(),rect=new Rect(col*cell,(poses.Length-1-row)*cell,cell,cell),alignment=SpriteAlignment.Custom,pivot=pivot});
            }
            provider.SetSpriteRects(rects.ToArray());var names=provider.GetDataProvider<ISpriteNameFileIdDataProvider>();if(names!=null)names.SetNameFileIdPairs(rects.Select(r=>new SpriteNameFileIdPair(r.name,r.spriteID)));
            provider.Apply();importer.SaveAndReimport();return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToDictionary(s=>s.name);
        }
    }
}
