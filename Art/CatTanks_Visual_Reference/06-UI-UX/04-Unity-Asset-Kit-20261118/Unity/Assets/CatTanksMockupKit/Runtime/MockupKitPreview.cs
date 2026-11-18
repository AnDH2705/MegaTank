using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace CatTanks.MockupKit
{
    [Serializable] public sealed class KitLayouts { public KitScreen[] screens; }
    [Serializable] public sealed class KitScreen
    {
        public string key, title, parentKey;
        public bool popup;
        public KitNode[] nodes;
    }
    [Serializable] public sealed class KitNode
    {
        public string id, parent, kind, sprite, text, action, color;
        public float x, y, w, h, value;
        public int fontSize = 20;
        public bool disabled, preserve, flipX, bold = true;
    }

    // A visual assembly preview. It deliberately does not access or mutate a game profile.
    public sealed class MockupKitPreview : MonoBehaviour
    {
        public MockupKitLibrary library;
        public TextAsset layoutData;
        public Font font;
        public string initialScreen = "march";
        KitLayouts layouts;
        string currentScreen, returnScreen = "march";
        RectTransform stage;
        public Action<string> onGameAction;

        void Start()
        {
            if (!font) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            // Editor-generated prefabs already have their visual children.
            stage = transform.Find("ArtKitStage") as RectTransform;
            if (!stage) Build(initialScreen);
            currentScreen = initialScreen;
            var spec = Find(initialScreen);
            if (spec != null && spec.popup) returnScreen = spec.parentKey;
            if (!FindObjectOfType<EventSystem>())
                new GameObject("MockupKit EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                var spec = Find(currentScreen);
                if (spec != null && spec.popup) Build(returnScreen);
                else if (currentScreen == "battle") Build("pause");
            }
        }
        KitScreen Find(string key)
        {
            if (layouts == null && layoutData) layouts = JsonUtility.FromJson<KitLayouts>(layoutData.text);
            if (layouts == null || layouts.screens == null) return null;
            foreach (var screen in layouts.screens) if (screen.key == key) return screen;
            return null;
        }
        public void Navigate(string action)
        {
            if (string.IsNullOrEmpty(action)) return;
            if (action == "close") { Build(returnScreen); return; }
            if (action.StartsWith("screen:")) { Build(action.Substring(7)); return; }
            // Expose a clear binding boundary for the real game controller.
            if (onGameAction != null) onGameAction(action);
            else Debug.Log("MockupKit preview action (no gameplay applied): " + action);
        }
        public void Build(string key)
        {
            var spec = Find(key);
            if (spec == null) throw new InvalidOperationException("Unknown MockupKit screen: " + key);
            if (!library) throw new InvalidOperationException("Import the MockupKit sprite library first.");
            if (!font) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var canvas = GetComponent<Canvas>();
            if (!canvas) canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = GetComponent<CanvasScaler>();
            if (!scaler) scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600, 900);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            scaler.referencePixelsPerUnit = 100;
            if (!GetComponent<GraphicRaycaster>()) gameObject.AddComponent<GraphicRaycaster>();
            var old = transform.Find("ArtKitStage");
            if (old)
            {
                old.gameObject.SetActive(false);
                if (Application.isPlaying) Destroy(old.gameObject); else DestroyImmediate(old.gameObject);
            }
            stage = Rect("ArtKitStage", transform, 0, 0, 1600, 900);
            stage.anchorMin = stage.anchorMax = stage.pivot = new Vector2(.5f, .5f);
            stage.anchoredPosition = Vector2.zero;
            if (spec.popup)
            {
                if (string.IsNullOrEmpty(currentScreen) || (Find(currentScreen) != null && Find(currentScreen).popup))
                    returnScreen = spec.parentKey;
                else returnScreen = currentScreen;
                var behind = Find(returnScreen);
                if (behind != null)
                {
                    var behindRoot = Rect("BackgroundScreen", stage, 0, 0, 1600, 900);
                    var group = behindRoot.gameObject.AddComponent<CanvasGroup>();
                    group.interactable = false; group.blocksRaycasts = false;
                    Draw(behind, behindRoot);
                }
                var scrim = Rect("ModalScrim", stage, 0, 0, 1600, 900).gameObject.AddComponent<Image>();
                scrim.color = new Color(0, .02f, .08f, .72f);
                scrim.raycastTarget = true;
            }
            Draw(spec, stage);
            currentScreen = initialScreen = key;
        }
        void Draw(KitScreen screen, RectTransform root)
        {
            var parents = new Dictionary<string, RectTransform>();
            foreach (var node in screen.nodes)
            {
                var parent = root;
                if (!string.IsNullOrEmpty(node.parent) && !parents.TryGetValue(node.parent, out parent))
                    throw new InvalidOperationException("Missing layout parent: " + node.parent);
                var rt = Rect(node.id, parent, node.x, node.y, node.w, node.h);
                parents[node.id] = rt;
                if (node.kind == "group") continue;
                if (node.kind == "label") { Label(rt, node.text, node.fontSize, node.bold, node.color, TextAnchor.MiddleLeft); continue; }
                if (node.kind == "input")
                {
                    Image(rt, node.sprite, false, node.color, true);
                    var label = Label(Rect("Value", rt, 12, 0, node.w - 24, node.h), node.text, node.fontSize, false, "#102340", TextAnchor.MiddleLeft);
                    var input = rt.gameObject.AddComponent<InputField>();
                    input.textComponent = label; input.targetGraphic = rt.GetComponent<Image>(); input.text = node.text;
                    input.lineType = InputField.LineType.SingleLine; input.characterLimit = 40;
                    continue;
                }
                var image = Image(rt, node.sprite, node.preserve, node.color, node.kind == "button" || node.kind == "viewport");
                if (node.flipX)
                {
                    rt.pivot = new Vector2(.5f, .5f);
                    rt.anchoredPosition += new Vector2(node.w * .5f, -node.h * .5f);
                    rt.localScale = new Vector3(-1, 1, 1);
                }
                if (node.kind == "button")
                {
                    var button = rt.gameObject.AddComponent<Button>(); button.targetGraphic = image;
                    button.interactable = !node.disabled;
                    var colors = button.colors; colors.normalColor = Color.white;
                    colors.highlightedColor = new Color(1, 1, 1, .95f);
                    colors.pressedColor = new Color(.72f, .80f, .90f, 1);
                    colors.disabledColor = new Color(.48f, .53f, .62f, .85f); button.colors = colors;
                    var labelRect = Rect("Label", rt, 8, 0, node.w - 16, node.h);
                    Label(labelRect, node.text, node.fontSize, true, node.sprite == "button_gold" ? "#102340" : "#FFFFFF", TextAnchor.MiddleCenter);
                    if (!string.IsNullOrEmpty(node.action)) rt.gameObject.AddComponent<MockupKitAction>().action = node.action;
                }
                if (node.kind == "bar")
                {
                    var fill = Image(Rect("Fill", rt, 5, 5, node.w - 10, node.h - 10), node.text, false, null, false);
                    fill.type = UnityEngine.UI.Image.Type.Filled;
                    fill.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
                    fill.fillOrigin = 0; fill.fillAmount = Mathf.Clamp01(node.value);
                }
                if (node.kind == "viewport")
                {
                    rt.gameObject.AddComponent<RectMask2D>();
                    var content = Rect("Content", rt, 8, 8, node.w - 16, Mathf.Max(node.h, node.value));
                    parents[node.id] = content;
                    var scroll = rt.gameObject.AddComponent<ScrollRect>();
                    scroll.viewport = rt; scroll.content = content; scroll.horizontal = false;
                    scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 30;
                }
            }
        }
        Image Image(RectTransform rt, string key, bool preserve, string color, bool raycast)
        {
            var image = rt.gameObject.AddComponent<Image>(); image.sprite = library.Get(key);
            if (!image.sprite && !string.IsNullOrEmpty(key)) Debug.LogWarning("Missing kit sprite: " + key);
            image.color = Tint(color); image.raycastTarget = raycast;
            image.preserveAspect = preserve; image.useSpriteMesh = false;
            bool slice = image.sprite && image.sprite.border.sqrMagnitude > 0 && !preserve;
            image.type = slice ? UnityEngine.UI.Image.Type.Sliced : UnityEngine.UI.Image.Type.Simple;
            if (slice)
            {
                var b = image.sprite.border;
                float targetEdge = key.StartsWith("bar_") || key.StartsWith("scroll_") ? 6f : 18f;
                image.pixelsPerUnitMultiplier = Mathf.Max(1, Mathf.Max(b.x, b.y, b.z, b.w) / targetEdge);
            }
            return image;
        }
        Text Label(RectTransform rt, string value, int size, bool bold, string color, TextAnchor alignment)
        {
            var label = rt.gameObject.AddComponent<Text>(); label.font = font;
            label.text = value ?? ""; label.fontSize = size > 0 ? size : 20;
            label.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal; label.color = Tint(color);
            label.alignment = alignment; label.raycastTarget = false;
            label.horizontalOverflow = HorizontalWrapMode.Wrap; label.verticalOverflow = VerticalWrapMode.Truncate;
            label.supportRichText = false;
            if (bold)
            {
                var outline = rt.gameObject.AddComponent<Outline>(); outline.effectColor = new Color(.01f, .03f, .10f, .95f);
                outline.effectDistance = new Vector2(1.5f, -1.5f); outline.useGraphicAlpha = true;
            }
            return label;
        }
        static Color Tint(string hex)
        {
            Color color;
            return !string.IsNullOrEmpty(hex) && ColorUtility.TryParseHtmlString(hex, out color) ? color : Color.white;
        }
        static RectTransform Rect(string name, Transform parent, float x, float y, float w, float h)
        {
            var rt = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rt.SetParent(parent, false); rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(x, -y); rt.sizeDelta = new Vector2(w, h);
            return rt;
        }
    }
}
