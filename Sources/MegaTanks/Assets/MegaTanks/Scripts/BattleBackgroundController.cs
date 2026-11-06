using System;
using System.Collections.Generic;
using UnityEngine;

namespace MegaTanks
{
    [DefaultExecutionOrder(200)]
    public sealed class BattleBackgroundController : MonoBehaviour
    {
        public const string TankLayerName = "Tank Backdrop";
        public const string EnemyLayerName = "Enemy Backdrop";
        sealed class LayerView
        {
            public BattleBackgroundLayer data;
            public Transform root;
            public int order;
            public readonly List<SpriteRenderer> tiles = new List<SpriteRenderer>();
        }
        sealed class CameraView
        {
            public Camera camera;
            public int layer, originalMask;
            public GameObject root;
            public readonly List<LayerView> layers = new List<LayerView>();
        }
        BattleBackgroundCatalog catalog;
        readonly List<CameraView> views = new List<CameraView>();
        public int CurrentIndex { get; private set; } = -1;
        public int SelectionCount { get; private set; }
        public BattleBackgroundPreset CurrentPreset => catalog.presets[CurrentIndex];

        public void Configure(BattleBackgroundCatalog data, Camera tank)
        {
            catalog = data;
            if (catalog == null || catalog.presets == null || catalog.presets.Length == 0)
                throw new InvalidOperationException("Battle backgrounds are missing. Run MegaTanks > Import Battle Backgrounds.");
            int tankLayer = LayerMask.NameToLayer(TankLayerName), enemyLayer = LayerMask.NameToLayer(EnemyLayerName);
            if (tankLayer < 0 || enemyLayer < 0) throw new InvalidOperationException("Backdrop camera layers are missing. Import battle backgrounds.");
            int mask = (1 << tankLayer) | (1 << enemyLayer);
            views.Add(new CameraView { camera=tank, layer=tankLayer, originalMask=tank.cullingMask });
            tank.cullingMask=(tank.cullingMask&~mask)|(1<<tankLayer);
            ApplyPreset(0);
        }
        public void SelectRandom()
        {
            SelectionCount++;
            ApplyPreset(UnityEngine.Random.Range(0, catalog.presets.Length));
        }
        public void ApplyPreset(int index)
        {
            if (index < 0 || index >= catalog.presets.Length) throw new ArgumentOutOfRangeException(nameof(index));
            CurrentIndex = index;
            foreach (var view in views)
            {
                if (view.root != null) { view.root.SetActive(false); Destroy(view.root); }
                view.root = new GameObject(view.camera.name + " background");
                view.root.transform.SetParent(transform, false);
                view.layers.Clear();
                var preset = catalog.presets[index];
                for (int i = 0; i < preset.layers.Length; i++)
                {
                    var data = preset.layers[i];
                    if (data.sprite == null) throw new InvalidOperationException("Missing layer sprite in " + preset.id);
                    var root = new GameObject(data.sprite.name).transform; root.SetParent(view.root.transform, false);
                    view.layers.Add(new LayerView { data = data, root = root, order = -200 + i });
                }
            }
            RefreshViews();
        }
        void LateUpdate() { RefreshViews(); }
        public void RefreshViews()
        {
            foreach (var view in views)
            {
                float height = view.camera.orthographicSize * 2;
                float viewWidth = height * view.camera.aspect;
                foreach (var layer in view.layers)
                {
                    float scale = height / layer.data.sprite.bounds.size.y;
                    if (!layer.data.repeatHorizontally) scale = Mathf.Max(scale, viewWidth / layer.data.sprite.bounds.size.x);
                    float width = layer.data.sprite.bounds.size.x * scale;
                    int radius = layer.data.repeatHorizontally ? Mathf.Max(1, Mathf.CeilToInt(viewWidth / width)) : 0;
                    int count = radius * 2 + 1;
                    while (layer.tiles.Count < count)
                    {
                        var obj = new GameObject("Background tile"); obj.transform.SetParent(layer.root, false); obj.layer = view.layer;
                        var renderer = obj.AddComponent<SpriteRenderer>(); renderer.sprite = layer.data.sprite; renderer.sortingOrder = layer.order;
                        layer.tiles.Add(renderer);
                    }
                    while (layer.tiles.Count > count)
                    {
                        int last = layer.tiles.Count - 1; layer.tiles[last].gameObject.SetActive(false);
                        Destroy(layer.tiles[last].gameObject); layer.tiles.RemoveAt(last);
                    }
                    // Camera travel is converted to a screen-space offset independently for each depth layer.
                    double travel = (double)view.camera.transform.position.x * layer.data.parallaxFactor;
                    double centreTile = Math.Floor(travel / width);
                    for (int i = 0; i < count; i++)
                    {
                        double tile = centreTile + i - radius;
                        var renderer = layer.tiles[i];
                        float x = layer.data.repeatHorizontally
                            ? view.camera.transform.position.x + (float)(tile * width - travel) : view.camera.transform.position.x;
                        renderer.transform.position = new Vector3(x, view.camera.transform.position.y, 5);
                        renderer.transform.localScale = Vector3.one * scale;
                        // Alternating mirror tiles join identical edges; these source images are not seamless repeats.
                        renderer.flipX = layer.data.repeatHorizontally && Math.Abs(tile % 2) > .5;
                    }
                }
            }
        }
        void OnDestroy()
        {
            foreach (var view in views) if (view.camera != null) view.camera.cullingMask = view.originalMask;
        }
    }
}
