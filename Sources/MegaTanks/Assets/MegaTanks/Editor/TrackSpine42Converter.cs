using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace MegaTanks.Editor
{
    // Converts only the supplied, validated 3.8 track rig. This is not a general Spine upgrader.
    public static class TrackSpine42Converter
    {
        static Dictionary<string, object> Obj(params object[] pairs)
        {
            var result = new Dictionary<string, object>();
            for (int i = 0; i < pairs.Length; i += 2) result.Add((string)pairs[i], pairs[i + 1]);
            return result;
        }
        public static string Convert(TrackSpineReader.Data source)
        {
            var bones = source.bones.Select(b =>
            {
                var value = Obj("name", b.name, "rotation", b.rotation, "x", b.x, "y", b.y,
                    "scaleX", b.scaleX, "scaleY", b.scaleY, "shearX", b.shearX, "shearY", b.shearY, "length", b.length);
                if (b.parent >= 0) value.Add("parent", source.bones[b.parent].name);
                return value;
            }).ToArray();
            var slots = source.slots.Select(s => Obj("name", s.name, "bone", source.bones[s.bone].name, "attachment", s.attachment)).ToArray();
            var paths = source.constraints.Select(c =>
            {
                if (c.positionMode != 1 || c.spacingMode != 0 || c.rotateMode != 0)
                    throw new InvalidOperationException("Unexpected source track constraint modes.");
                return Obj("name", c.name, "bones", c.bones.Select(i => source.bones[i].name).ToArray(),
                    "target", source.slots[c.target].name, "positionMode", "percent", "spacingMode", "length", "rotateMode", "tangent",
                    "rotation", c.rotation, "position", c.position, "spacing", c.spacing,
                    "mixRotate", c.rotateMix, "mixX", c.translateMix, "mixY", c.translateMix);
            }).ToArray();
            var attachments = new Dictionary<string, object>();
            foreach (var a in source.attachments)
            {
                string slot = source.slots[a.slot].name;
                if (!attachments.ContainsKey(slot)) attachments.Add(slot, new Dictionary<string, object>());
                Dictionary<string, object> entry;
                if (a.kind == 0)
                    entry = Obj("type", "region", "path", a.path, "x", a.x, "y", a.y, "rotation", a.rotation,
                        "scaleX", a.scaleX, "scaleY", a.scaleY, "width", a.width, "height", a.height);
                else if (a.kind == 4)
                {
                    var vertices = new List<float>();
                    foreach (var v in a.vertices)
                    {
                        if (v.weights == null) { vertices.Add(v.x); vertices.Add(v.y); }
                        else
                        {
                            vertices.Add(v.weights.Length);
                            foreach (var w in v.weights) vertices.AddRange(new[] { (float)w.bone, w.x, w.y, w.weight });
                        }
                    }
                    entry = Obj("type", "path", "closed", a.closed, "constantSpeed", a.constantSpeed,
                        "vertexCount", a.vertices.Length, "vertices", vertices, "lengths", a.lengths);
                }
                else throw new InvalidOperationException("Unsupported source attachment.");
                ((Dictionary<string, object>)attachments[slot]).Add(a.name, entry);
            }
            var animations = new Dictionary<string, object>();
            foreach (var animation in source.animations)
            {
                var animatedPaths = new Dictionary<string, object>();
                foreach (var timeline in animation.timelines)
                {
                    string constraint = source.constraints[timeline.index].name;
                    if (!animatedPaths.ContainsKey(constraint)) animatedPaths.Add(constraint, new Dictionary<string, object>());
                    if (timeline.type > 1) throw new InvalidOperationException("Unexpected source track mix timeline.");
                    var frames = timeline.frames.Select(f =>
                    {
                        var frame = Obj("time", f.time, "value", f.values[0]);
                        if (f.curve.Length == 1 && f.curve[0] == -1) frame.Add("curve", "stepped");
                        else if (f.curve.Length != 0) throw new InvalidOperationException("Unexpected nonlinear source track timeline.");
                        return frame;
                    }).ToArray();
                    ((Dictionary<string, object>)animatedPaths[constraint]).Add(timeline.type == 0 ? "position" : "spacing", frames);
                }
                animations.Add(animation.name, Obj("path", animatedPaths));
            }
            var root = Obj("skeleton", Obj("spine", "4.2.00", "hash", "MegaTanks-track-conversion-from-" + source.version),
                "bones", bones, "slots", slots, "path", paths,
                "skins", new[] { Obj("name", "default", "attachments", attachments) }, "animations", animations);
            var output = new StringBuilder(); Write(output, root); return output.ToString() + "\n";
        }
        static void Write(StringBuilder output, object value)
        {
            if (value == null) { output.Append("null"); return; }
            if (value is string text)
            {
                output.Append('"');
                foreach (char c in text)
                {
                    if (c == '"' || c == '\\') output.Append('\\').Append(c);
                    else if (c < 32) output.Append("\\u").Append(((int)c).ToString("x4"));
                    else output.Append(c);
                }
                output.Append('"'); return;
            }
            if (value is IDictionary dictionary)
            {
                output.Append('{'); bool first = true;
                foreach (DictionaryEntry pair in dictionary)
                {
                    if (!first) output.Append(','); first = false; Write(output, pair.Key); output.Append(':'); Write(output, pair.Value);
                }
                output.Append('}'); return;
            }
            if (value is IEnumerable sequence)
            {
                output.Append('['); bool first = true;
                foreach (var item in sequence) { if (!first) output.Append(','); first = false; Write(output, item); }
                output.Append(']'); return;
            }
            if (value is bool boolean) output.Append(boolean ? "true" : "false");
            else output.Append(System.Convert.ToString(value, CultureInfo.InvariantCulture));
        }
    }
}
