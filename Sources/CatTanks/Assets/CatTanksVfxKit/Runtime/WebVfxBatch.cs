using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace CatTanks.VfxKit
{
    sealed class WebVfxBatch : IDisposable
    {
        internal enum Shape { Solid, Disk, Glow, Smoke, Ring, Particle, Saw }

        readonly List<Vector3> vertices;
        readonly List<Color32> colors;
        readonly List<Vector2> uvs, shapes;
        readonly List<int> indices;
        readonly Mesh mesh;
        readonly GameObject root;
        readonly Material material;
        readonly int capacity;

        public int Count => vertices.Count / 4;

        public WebVfxBatch(Transform parent, Shader shader, string label, int order, int capacity, bool additive)
        {
            this.capacity = capacity;
            vertices = new List<Vector3>(capacity * 4);
            colors = new List<Color32>(capacity * 4);
            uvs = new List<Vector2>(capacity * 4);
            shapes = new List<Vector2>(capacity * 4);
            indices = new List<int>(capacity * 6);
            root = new GameObject(label, typeof(MeshFilter), typeof(MeshRenderer));
            root.transform.SetParent(parent, false);
            mesh = new Mesh { name = label };
            mesh.MarkDynamic();
            root.GetComponent<MeshFilter>().sharedMesh = mesh;
            material = new Material(shader);
            material.SetInt("_DstBlend", (int)(additive ? BlendMode.One : BlendMode.OneMinusSrcAlpha));
            var renderer = root.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.sortingOrder = order;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        public void Clear()
        {
            vertices.Clear();
            colors.Clear();
            uvs.Clear();
            shapes.Clear();
            indices.Clear();
        }

        public void Add(Shape shape, Vector2 center, Vector2 size, Color32 color, float angle = 0, float inner = .88f)
        {
            if (color.a == 0 || Count == capacity) return;
            var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            var right = direction * (size.x * .5f);
            var up = new Vector2(-direction.y, direction.x) * (size.y * .5f);
            int v = vertices.Count;
            Vertex(center - right - up, new Vector2(-1, -1));
            Vertex(center - right + up, new Vector2(-1, 1));
            Vertex(center + right + up, new Vector2(1, 1));
            Vertex(center + right - up, new Vector2(1, -1));
            indices.Add(v); indices.Add(v + 1); indices.Add(v + 2);
            indices.Add(v); indices.Add(v + 2); indices.Add(v + 3);

            void Vertex(Vector2 position, Vector2 uv)
            {
                vertices.Add(position);
                colors.Add(color);
                uvs.Add(uv);
                shapes.Add(new Vector2((int)shape, inner));
            }
        }

        public void Upload()
        {
            mesh.Clear(true);
            if (vertices.Count == 0) return;
            mesh.SetVertices(vertices);
            mesh.SetColors(colors);
            mesh.SetUVs(0, uvs);
            mesh.SetUVs(1, shapes);
            mesh.SetTriangles(indices, 0);
        }

        public void Dispose()
        {
            Object.Destroy(root);
            Object.Destroy(mesh);
            Object.Destroy(material);
        }
    }
}
