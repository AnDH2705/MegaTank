using UnityEngine;

namespace MegaTanks
{
    // Shared sprites avoid allocating textures or materials per entity/projectile.
    public static class ShapeFactory
    {
        static Sprite square;
        static Sprite circle;
        static Texture2D circleTexture;

        public static SpriteRenderer Create(string name, Transform parent, Vector2 position,
            Vector2 size, Color color, bool round = false, int order = 0)
        {
            if (square == null)
                square = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, Texture2D.whiteTexture.width, Texture2D.whiteTexture.height), Vector2.one * .5f, Texture2D.whiteTexture.width);
            if (round && circle == null)
            {
                const int resolution = 64;
                circleTexture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
                circleTexture.name = "Shared circle texture";
                var pixels = new Color[resolution * resolution];
                for (int y = 0; y < resolution; y++)
                    for (int x = 0; x < resolution; x++)
                    {
                        float distance = Vector2.Distance(new Vector2(x + .5f, y + .5f), Vector2.one * resolution * .5f);
                        pixels[y * resolution + x] = new Color(1, 1, 1, Mathf.Clamp01(resolution * .5f - distance));
                    }
                circleTexture.SetPixels(pixels);
                circleTexture.Apply();
                circle = Sprite.Create(circleTexture, new Rect(0, 0, resolution, resolution), Vector2.one * .5f, resolution);
            }
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = position;
            obj.transform.localScale = new Vector3(size.x, size.y, 1);
            var renderer = obj.AddComponent<SpriteRenderer>();
            renderer.sprite = round ? circle : square;
            renderer.color = color;
            renderer.sortingOrder = order;
            return renderer;
        }

        public static void Release()
        {
            if (square != null) Object.Destroy(square);
            if (circle != null) Object.Destroy(circle);
            if (circleTexture != null) Object.Destroy(circleTexture);
            square = circle = null;
            circleTexture = null;
        }
    }
}
