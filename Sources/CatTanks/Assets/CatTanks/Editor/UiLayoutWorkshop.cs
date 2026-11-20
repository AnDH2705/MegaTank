using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace CatTanks.Editor
{
    public sealed class UiLayoutWorkshop : EditorWindow
    {
        const int Width = 1600;
        const int Height = 900;
        const int MinimumSize = 8;
        const int Schema = 1;

        [Serializable]
        sealed class Draft
        {
            public int version = Schema;
            public string project = "CatTanks";
            public string coordinates = "parent-top-left";
            public int width = Width;
            public int height = Height;
            public List<Edit> edits = new List<Edit>();
        }

        [Serializable]
        sealed class Edit
        {
            public string view, widgetId, kind, label, prefab, parentWidgetId, sprite, note;
            public string replacementSprite, replacementId;
            public Geometry before, after;
        }

        [Serializable]
        struct Geometry
        {
            public float x, y, width, height;
            public Rect Rect => new Rect(x, y, width, height);
            public Geometry(Rect rect)
            {
                x = rect.x;
                y = rect.y;
                width = rect.width;
                height = rect.height;
            }
        }

        sealed class Widget
        {
            public string View;
            public StudioWidgetBinding Binding;
            public Rect Frame, Visible, Local;
            public string Label;
            public string Key => View + "/" + Binding.id;
        }

        struct RectState
        {
            readonly RectTransform rect;
            readonly Vector2 min, max, pivot, size;
            readonly Vector3 position, scale;

            public RectState(RectTransform rect)
            {
                this.rect = rect;
                min = rect.anchorMin;
                max = rect.anchorMax;
                pivot = rect.pivot;
                position = rect.anchoredPosition3D;
                size = rect.sizeDelta;
                scale = rect.localScale;
            }

            public void Restore()
            {
                rect.anchorMin = min;
                rect.anchorMax = max;
                rect.pivot = pivot;
                rect.sizeDelta = size;
                rect.anchoredPosition3D = position;
                rect.localScale = scale;
            }
        }

        readonly List<Widget> widgets = new List<Widget>();
        readonly Dictionary<string, Edit> edits = new Dictionary<string, Edit>();
        [SerializeField] List<Edit> savedEdits = new List<Edit>();
        readonly Vector3[] corners = new Vector3[4];
        RenderTexture preview;
        Vector2 listScroll;
        string view = "", selected = "", filter = "", message = "";
        bool dragging, resizing, refreshQueued;
        Vector2 dragOrigin;
        Rect dragRect;

        [MenuItem("CatTanks/UI/Layout Workshop")]
        static void Open()
        {
            var window = GetWindow<UiLayoutWorkshop>("CatTanks Layout");
            window.minSize = new Vector2(740, 460);
        }

        void OnEnable()
        {
            foreach (var edit in savedEdits)
                edits[edit.view + "/" + edit.widgetId] = edit;
            EditorApplication.playModeStateChanged += PlayModeChanged;
            if (EditorApplication.isPlaying) QueuePreview();
        }

        void OnDisable()
        {
            EditorApplication.playModeStateChanged -= PlayModeChanged;
            ClearPreview();
        }

        void PlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode) ClearPreview();
        }

        void ClearPreview()
        {
            EditorApplication.delayCall -= RefreshPreview;
            refreshQueued = false;
            dragging = false;
            widgets.Clear();
            if (preview) DestroyImmediate(preview);
        }

        void OnGUI()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                using (new EditorGUI.DisabledScope(!EditorApplication.isPlaying))
                    if (GUILayout.Button("Chụp UI đang mở", EditorStyles.toolbarButton)) QueuePreview();
                if (GUILayout.Button("Mở JSON", EditorStyles.toolbarButton)) OpenDraft();
                using (new EditorGUI.DisabledScope(edits.Count == 0))
                    if (GUILayout.Button("Xuất JSON", EditorStyles.toolbarButton)) SaveDraft();
                GUILayout.FlexibleSpace();
                GUILayout.Label($"{edits.Count} mục đã đánh dấu", EditorStyles.miniLabel);
            }
            if (!EditorApplication.isPlaying)
                EditorGUILayout.HelpBox("Mở Main.unity, nhấn Play, vào màn cần chỉnh rồi bấm Chụp UI đang mở.", MessageType.Info);
            if (!string.IsNullOrEmpty(message))
                EditorGUILayout.HelpBox(message, MessageType.Info);
            if (!preview || !EditorApplication.isPlaying) return;
            widgets.RemoveAll(w => !w.Binding);

            using (new EditorGUILayout.HorizontalScope())
            {
                var area = GUILayoutUtility.GetRect(400, 320, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                DrawPreview(area);
                using (new EditorGUILayout.VerticalScope(GUILayout.Width(280)))
                    DrawInspector();
            }
        }

        void DrawInspector()
        {
            string[] views = widgets.Select(w => w.View).Distinct().ToArray();
            if (views.Length == 0) return;
            int index = Mathf.Max(0, Array.IndexOf(views, view));
            view = views[EditorGUILayout.Popup("Màn / popup", index, views)];
            filter = EditorGUILayout.TextField("Tìm nút hoặc ID", filter);
            using (var scroll = new EditorGUILayout.ScrollViewScope(listScroll, GUILayout.Height(156)))
            {
                listScroll = scroll.scrollPosition;
                foreach (var widget in widgets.Where(w => w.View == view &&
                    (w.Label.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    w.Binding.id.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)))
                    if (GUILayout.Toggle(selected == widget.Key, widget.Label, EditorStyles.miniButton))
                        selected = widget.Key;
            }

            var selectedWidget = Selection();
            if (selectedWidget == null) return;
            EditorGUILayout.LabelField("ID", selectedWidget.Binding.id);
            var rect = Current(selectedWidget);
            EditorGUI.BeginChangeCheck();
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label("X", GUILayout.Width(18));
                rect.x = EditorGUILayout.FloatField(rect.x);
                GUILayout.Label("Y", GUILayout.Width(18));
                rect.y = EditorGUILayout.FloatField(rect.y);
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label("Rộng", GUILayout.Width(42));
                rect.width = EditorGUILayout.FloatField(rect.width);
                GUILayout.Label("Cao", GUILayout.Width(34));
                rect.height = EditorGUILayout.FloatField(rect.height);
            }
            if (EditorGUI.EndChangeCheck()) ChangeRect(selectedWidget, rect);
            string note = edits.TryGetValue(selectedWidget.Key, out var edit) ? edit.note : "";
            if (Artwork(selectedWidget))
            {
                var previous = Replacement(edit);
                var next = (Sprite)EditorGUILayout.ObjectField("Sprite thay", previous, typeof(Sprite), false);
                if (next != previous)
                {
                    var item = EditFor(selectedWidget);
                    item.replacementSprite = next ? AssetDatabase.GetAssetPath(next) : "";
                    item.replacementId = next ? GlobalObjectId.GetGlobalObjectIdSlow(next).ToString() : "";
                    QueuePreview();
                }
            }
            EditorGUILayout.LabelField("Ghi chú hình ảnh hoặc nội dung");
            string updated = EditorGUILayout.TextArea(note ?? "", GUILayout.Height(58));
            if (updated != (note ?? "")) EditFor(selectedWidget).note = updated;
            using (new EditorGUI.DisabledScope(!edits.ContainsKey(selectedWidget.Key)))
            {
                if (GUILayout.Button("Khôi phục mục này"))
                {
                    savedEdits.Remove(edits[selectedWidget.Key]);
                    edits.Remove(selectedWidget.Key);
                    QueuePreview();
                }
            }
        }

        void DrawPreview(Rect area)
        {
            float scale = Mathf.Min(area.width / Width, area.height / Height);
            if (scale <= 0) return;
            var board = new Rect(area.center.x - Width * scale / 2, area.center.y - Height * scale / 2, Width * scale, Height * scale);
            GUI.DrawTexture(board, preview, ScaleMode.StretchToFill, false);
            var selectedWidget = Selection();
            Rect handle = default;
            if (selectedWidget != null)
            {
                var frame = DisplayRect(selectedWidget.Frame, board, scale);
                var visible = Intersect(frame, board);
                if (visible.width > 0)
                {
                    EditorGUI.DrawRect(visible, new Color(0, .8f, 1, .10f));
                    EditorGUI.DrawRect(new Rect(visible.x, visible.y, visible.width, 1), Color.cyan);
                    EditorGUI.DrawRect(new Rect(visible.x, visible.yMax - 1, visible.width, 1), Color.cyan);
                    EditorGUI.DrawRect(new Rect(visible.x, visible.y, 1, visible.height), Color.cyan);
                    EditorGUI.DrawRect(new Rect(visible.xMax - 1, visible.y, 1, visible.height), Color.cyan);
                    handle = Intersect(new Rect(frame.xMax - 7, frame.yMax - 7, 14, 14), board);
                    EditorGUI.DrawRect(handle, Color.cyan);
                    EditorGUIUtility.AddCursorRect(handle, MouseCursor.ResizeUpLeft);
                }
            }

            var input = Event.current;
            if (input.type == EventType.MouseDown && input.button == 0 && board.Contains(input.mousePosition))
            {
                resizing = selectedWidget != null && handle.Contains(input.mousePosition);
                var hit = resizing ? selectedWidget : widgets.LastOrDefault(w => w.View == view &&
                    DisplayRect(w.Visible, board, scale).Contains(input.mousePosition));
                if (hit != null)
                {
                    selected = hit.Key;
                    dragging = true;
                    dragOrigin = input.mousePosition;
                    dragRect = Current(hit);
                    input.Use();
                }
            }
            if (dragging && selectedWidget != null && input.type == EventType.MouseDrag)
            {
                var delta = (input.mousePosition - dragOrigin) / scale;
                var rect = dragRect;
                if (resizing)
                {
                    rect.size += delta;
                    if (input.shift)
                    {
                        float scaleX = rect.width / dragRect.width;
                        float scaleY = rect.height / dragRect.height;
                        float ratio = Mathf.Abs(scaleX - 1) >= Mathf.Abs(scaleY - 1) ? scaleX : scaleY;
                        rect.size = dragRect.size * Mathf.Max(ratio, MinimumSize / Mathf.Min(dragRect.width, dragRect.height));
                    }
                }
                else rect.position += delta;
                ChangeRect(selectedWidget, rect);
                input.Use();
            }
            if (dragging && (input.rawType == EventType.MouseUp || input.type == EventType.Ignore))
                dragging = false;
        }

        Widget Selection() => widgets.Find(w => w.View == view && w.Key == selected);
        Rect Current(Widget widget) => edits.TryGetValue(widget.Key, out var edit) ? edit.after.Rect : widget.Local;

        static Rect DisplayRect(Rect frame, Rect board, float scale)
        {
            return new Rect(board.x + frame.x * scale, board.y + frame.y * scale, frame.width * scale, frame.height * scale);
        }

        Edit EditFor(Widget widget)
        {
            if (edits.TryGetValue(widget.Key, out var edit)) return edit;
            var graphic = Artwork(widget);
            var parent = widget.Binding.transform.parent.GetComponentInParent<StudioWidgetBinding>();
            edit = new Edit
            {
                view = widget.View, widgetId = widget.Binding.id, kind = widget.Binding.kind, label = widget.Label,
                prefab = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(widget.Binding.gameObject),
                parentWidgetId = parent ? parent.id : "",
                sprite = graphic && graphic.sprite ? AssetDatabase.GetAssetPath(graphic.sprite) : "",
                before = new Geometry(widget.Local), after = new Geometry(widget.Local), note = ""
            };
            edits.Add(widget.Key, edit);
            savedEdits.Add(edit);
            return edit;
        }

        static Image Artwork(Widget widget)
        {
            var layout = widget.Binding.GetComponent<StudioButtonLayout>();
            return layout && layout.icon && layout.icon.sprite ? layout.icon : widget.Binding.GetComponent<Image>();
        }

        static Sprite Replacement(Edit edit)
        {
            if (edit == null || string.IsNullOrEmpty(edit.replacementId) ||
                !GlobalObjectId.TryParse(edit.replacementId, out var id)) return null;
            return GlobalObjectId.GlobalObjectIdentifierToObjectSlow(id) as Sprite;
        }

        void ChangeRect(Widget widget, Rect rect)
        {
            rect.x = Mathf.Round(rect.x);
            rect.y = Mathf.Round(rect.y);
            rect.width = Mathf.Max(MinimumSize, Mathf.Round(rect.width));
            rect.height = Mathf.Max(MinimumSize, Mathf.Round(rect.height));
            var geometry = new Geometry(rect);
            if (!Valid(geometry)) return;
            EditFor(widget).after = geometry;
            QueuePreview();
        }

        static bool Valid(Geometry rect)
        {
            return Finite(rect.x) && Finite(rect.y) && Finite(rect.width) && Finite(rect.height) && rect.width > 0 && rect.height > 0;
        }

        static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

        void QueuePreview()
        {
            if (refreshQueued) return;
            refreshQueued = true;
            EditorApplication.delayCall += RefreshPreview;
        }

        void RefreshPreview()
        {
            refreshQueued = false;
            if (!this || !EditorApplication.isPlaying) return;
            var game = FindObjectOfType<CatTanksGame>();
            if (!game || !game.CanvasRoot || !Camera.main)
            {
                message = "Đợi game mở xong rồi chụp lại.";
                Repaint();
                return;
            }
            try
            {
                game.RefreshCanvas();
                Capture(game.CanvasRoot, Camera.main);
                message = "";
            }
            catch (Exception error)
            {
                message = error.Message;
            }
            Repaint();
        }

        Rect InParent(RectTransform rect, RectTransform parent)
        {
            rect.GetWorldCorners(corners);
            var topLeft = parent.InverseTransformPoint(corners[1]);
            var bottomRight = parent.InverseTransformPoint(corners[3]);
            return Rect.MinMaxRect(topLeft.x - parent.rect.xMin, parent.rect.yMax - topLeft.y,
                bottomRight.x - parent.rect.xMin, parent.rect.yMax - bottomRight.y);
        }

        void Capture(StudioUIRoot ui, Camera main)
        {
            if (!preview)
                preview = new RenderTexture(960, 540, 24) { hideFlags = HideFlags.HideAndDontSave };
            var cameraObject = new GameObject("CatTanks Layout Camera", typeof(Camera)) { hideFlags = HideFlags.HideAndDontSave };
            var camera = cameraObject.GetComponent<Camera>();
            var canvas = ui.GetComponent<Canvas>();
            var previousMode = canvas.renderMode;
            var previousCamera = canvas.worldCamera;
            var root = (RectTransform)canvas.transform;
            var rootState = new RectState(root);
            var viewportState = new RectState(ui.viewport);
            var modified = new List<RectState>();
            var layouts = new List<StudioButtonLayout>();
            var sprites = new List<(Image image, Sprite original)>();
            var activeTexture = RenderTexture.active;
            try
            {
                camera.CopyFrom(main);
                camera.enabled = false;
                camera.cullingMask |= 1 << ui.gameObject.layer;
                camera.targetTexture = preview;
                camera.rect = new Rect(0, 0, 1, 1);
                camera.aspect = (float)Width / Height;
                camera.orthographic = true;
                camera.orthographicSize = Height / 2f;
                camera.transform.SetPositionAndRotation(new Vector3(800, 450, -1000), Quaternion.identity);
                canvas.renderMode = RenderMode.WorldSpace;
                canvas.worldCamera = camera;
                root.pivot = Vector2.one / 2;
                root.sizeDelta = new Vector2(Width, Height);
                root.position = new Vector3(800, 450, -999);
                root.localScale = Vector3.one;
                ui.viewport.localScale = Vector3.one;
                ui.viewport.anchoredPosition = Vector2.zero;
                Canvas.ForceUpdateCanvases();

                widgets.Clear();
                foreach (var screen in ui.Views.Where(v => v.isActiveAndEnabled))
                {
                    foreach (var binding in screen.GetComponentsInChildren<StudioWidgetBinding>())
                    {
                        var rect = (RectTransform)binding.transform;
                        if (binding.layoutOwner || !(rect.parent is RectTransform parent) || string.IsNullOrEmpty(binding.id)) continue;
                        var local = InParent(rect, parent);
                        if (local.width <= 0 || local.height <= 0) continue;
                        var text = binding.GetComponentInChildren<Text>();
                        string label = text && !string.IsNullOrWhiteSpace(text.text) ? text.text.Replace('\n', ' ') : binding.id;
                        widgets.Add(new Widget { View = screen.key, Binding = binding, Label = label, Local = local });
                    }
                }
                if (!widgets.Any(w => w.View == view))
                    view = widgets.LastOrDefault()?.View ?? "";

                foreach (var widget in widgets)
                {
                    if (!edits.TryGetValue(widget.Key, out var edit)) continue;
                    if (Replacement(edit) is Sprite replacement && Artwork(widget) is Image image)
                    {
                        sprites.Add((image, image.sprite));
                        image.sprite = replacement;
                    }
                    if (edit.before.Rect == edit.after.Rect) continue;
                    var rect = (RectTransform)widget.Binding.transform;
                    modified.Add(new RectState(rect));
                    rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
                    rect.anchoredPosition = new Vector2(edit.after.x, -edit.after.y);
                    rect.sizeDelta = edit.after.Rect.size;
                    rect.localScale = Vector3.one;
                    var layout = widget.Binding.GetComponent<StudioButtonLayout>();
                    if (layout)
                    {
                        layouts.Add(layout);
                        layout.Apply();
                    }
                }
                Canvas.ForceUpdateCanvases();
                foreach (var widget in widgets)
                {
                    var rect = (RectTransform)widget.Binding.transform;
                    widget.Frame = InParent(rect, ui.viewport);
                    widget.Visible = widget.Frame;
                    for (var ancestor = rect.parent; ancestor && ancestor != ui.viewport; ancestor = ancestor.parent)
                    {
                        if (!(ancestor is RectTransform clip) || !clip.GetComponent<RectMask2D>()) continue;
                        widget.Visible = Intersect(widget.Visible, InParent(clip, ui.viewport));
                    }
                }
                camera.Render();
            }
            finally
            {
                foreach (var (image, original) in sprites) image.sprite = original;
                for (int i = modified.Count - 1; i >= 0; i--) modified[i].Restore();
                canvas.renderMode = previousMode;
                canvas.worldCamera = previousCamera;
                rootState.Restore();
                viewportState.Restore();
                foreach (var layout in layouts) layout.Apply();
                Canvas.ForceUpdateCanvases();
                RenderTexture.active = activeTexture;
                DestroyImmediate(cameraObject);
            }
        }

        static Rect Intersect(Rect a, Rect b)
        {
            float left = Mathf.Max(a.xMin, b.xMin), top = Mathf.Max(a.yMin, b.yMin);
            float right = Mathf.Min(a.xMax, b.xMax), bottom = Mathf.Min(a.yMax, b.yMax);
            return right > left && bottom > top ? Rect.MinMaxRect(left, top, right, bottom) : default;
        }

        void OpenDraft()
        {
            string path = EditorUtility.OpenFilePanel("Mở bố cục CatTanks", "", "json");
            if (string.IsNullOrEmpty(path)) return;
            try
            {
                var draft = JsonUtility.FromJson<Draft>(File.ReadAllText(path));
                if (draft == null || draft.version != Schema || draft.project != "CatTanks" || draft.coordinates != "parent-top-left" ||
                    draft.width != Width || draft.height != Height || draft.edits == null)
                    throw new InvalidDataException("File bố cục CatTanks không tương thích.");
                var imported = new Dictionary<string, Edit>();
                foreach (var edit in draft.edits)
                {
                    if (edit == null || string.IsNullOrWhiteSpace(edit.view) || string.IsNullOrWhiteSpace(edit.widgetId)
                        || !Valid(edit.before) || !Valid(edit.after))
                        throw new InvalidDataException("File có một mục thiếu ID hoặc tọa độ không hợp lệ.");
                    imported.Add(edit.view + "/" + edit.widgetId, edit);
                }
                if (edits.Count > 0 && !EditorUtility.DisplayDialog("Mở bố cục", "Thay bản nháp hiện tại bằng file vừa chọn?", "Mở file", "Quay lại"))
                    return;
                edits.Clear();
                savedEdits.Clear();
                foreach (var item in imported) edits.Add(item.Key, item.Value);
                savedEdits.AddRange(imported.Values);
                message = "Đã mở " + Path.GetFileName(path);
                if (EditorApplication.isPlaying) QueuePreview();
            }
            catch (Exception error)
            {
                message = error.Message;
            }
        }

        void SaveDraft()
        {
            string path = EditorUtility.SaveFilePanel("Xuất bố cục CatTanks", "", "CatTanks-UI-Layout", "json");
            if (string.IsNullOrEmpty(path)) return;
            try
            {
                var draft = new Draft { edits = edits.Values.OrderBy(e => e.view).ThenBy(e => e.widgetId).ToList() };
                File.WriteAllText(path, JsonUtility.ToJson(draft, true));
                message = "Đã xuất " + path;
            }
            catch (Exception error)
            {
                message = error.Message;
            }
        }
    }
}
