using System;
using System.IO;
using UnityEditor;
using UnityEngine;

// Copy the Crew folder under Assets. This only affects textures beside its manifest.
public sealed class CrewUISpriteImporter : AssetPostprocessor
{
    [Serializable] private sealed class Entry
    {
        public string file;
        public int[] border_lbrt;
    }
    [Serializable] private sealed class Manifest
    {
        public Entry[] nine_slice;
    }

    private void OnPreprocessTexture()
    {
        string normalized = assetPath.Replace('\\', '/');
        bool sliced = normalized.Contains("/Crew/slices/");
        bool decoration = normalized.Contains("/Crew/decor/");
        bool illustration = normalized.Contains("/Crew/illustrations/") || normalized.Contains("/Crew/layers/") || normalized.Contains("/Crew/text/") || normalized.Contains("/Crew/font/");
        if ((!sliced && !decoration && !illustration) || !normalized.EndsWith(".png", StringComparison.OrdinalIgnoreCase)) return;
        string crewDirectory = Path.GetDirectoryName(Path.GetDirectoryName(assetPath));
        string manifestPath = Path.Combine(crewDirectory, "ui-sprites.json");
        if (!File.Exists(manifestPath)) return;

        var importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 400;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.isReadable = false;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.filterMode = FilterMode.Bilinear;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        importer.SetTextureSettings(settings);
        importer.spriteBorder = Vector4.zero;
        if (!sliced) return;

        Manifest manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText(manifestPath));
        string localFile = "slices/" + Path.GetFileName(assetPath);
        foreach (Entry entry in manifest.nine_slice)
        {
            if (entry.file != localFile || entry.border_lbrt == null || entry.border_lbrt.Length != 4) continue;
            int[] b = entry.border_lbrt;
            importer.spriteBorder = new Vector4(b[0], b[1], b[2], b[3]);
            break;
        }
    }
}
