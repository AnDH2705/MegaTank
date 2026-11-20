using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CatTanks
{
    public sealed partial class BattleView
    {
        const float SceneryScale = 1600f / 1920f;
        static readonly string[] LayerNames = {"Sky", "Distant Landscape", "Midground", "Road"};
        static readonly float[] LayerMotion = {.008f, .025f, .05f, 0f};
        static SceneryCatalog sceneryCatalog;

        [Serializable]
        sealed class SceneryCatalog { public SceneryEntry[] stages; }

        [Serializable]
        sealed class SceneryEntry
        {
            public int index;
            public SceneryLayer[] layers;
        }

        [Serializable]
        sealed class SceneryLayer { public int sceneTop, atlasY, height; }

        Texture2D sceneryAtlas;
        readonly Transform[] sceneryPlanes = new Transform[4];
        readonly Sprite[] scenerySprites = new Sprite[4];

        bool LoadScenery(int stageIndex)
        {
            if (stageIndex <= 0) return false;
            if (sceneryCatalog == null)
            {
                var data = Resources.Load<TextAsset>("Scenery/Layouts");
                if (!data) return false;
                sceneryCatalog = JsonUtility.FromJson<SceneryCatalog>(data.text);
            }

            if (sceneryCatalog?.stages == null) return false;
            var entry = Array.Find(sceneryCatalog.stages, stage => stage.index == stageIndex);
            if (entry?.layers == null || entry.layers.Length != LayerNames.Length) return false;
            sceneryAtlas = Resources.Load<Texture2D>($"Scenery/stage-{stageIndex:00}");
            if (!sceneryAtlas) return false;

            var backdrop = new GameObject("Mockup Kit Battle Background");
            backdrop.transform.SetParent(root.transform, false);
            for (int i = 0; i < entry.layers.Length; i++)
            {
                var layer = entry.layers[i];
                var bounds = new Rect(0, layer.atlasY, sceneryAtlas.width, layer.height);
                var art = Sprite.Create(sceneryAtlas, bounds, Vector2.one * .5f, 1, 0, SpriteMeshType.FullRect);
                var plane = new GameObject(LayerNames[i], typeof(SpriteRenderer));
                plane.transform.SetParent(backdrop.transform, false);
                plane.transform.localPosition = new Vector3(800, 900 - (layer.sceneTop + layer.height * .5f) * SceneryScale);
                plane.transform.localScale = new Vector3(SceneryScale, SceneryScale, 1);
                var renderer = plane.GetComponent<SpriteRenderer>();
                renderer.sprite = art;
                renderer.sortingOrder = -110 + i;
                sceneryPlanes[i] = plane.transform;
                scenerySprites[i] = art;
            }
            return true;
        }

        void ScrollScenery(float playerX)
        {
            if (!sceneryAtlas) return;
            float travel = playerX - 290f;
            for (int i = 0; i < sceneryPlanes.Length; i++)
            {
                var position = sceneryPlanes[i].localPosition;
                position.x = 800 - travel * LayerMotion[i];
                sceneryPlanes[i].localPosition = position;
            }
        }

        void DisposeScenery()
        {
            foreach (var sprite in scenerySprites) Object.Destroy(sprite);
            if (sceneryAtlas) Resources.UnloadAsset(sceneryAtlas);
        }
    }
}
