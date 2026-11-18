using System;
using System.Collections.Generic;
using System.IO;

namespace MegaTanks.Editor
{
    // Editor-only decoder for converting the supplied 3.8 track export to Spine 4.2 JSON.
    // Binary layout reference: github.com/EsotericSoftware/spine-runtimes/tree/3.8/spine-csharp/src
    public static class TrackSpineReader
    {
        public sealed class Bone { public string name; public int parent; public float rotation, x, y, scaleX, scaleY, shearX, shearY, length; }
        public sealed class Slot { public string name, attachment; public int bone; }
        public sealed class Constraint { public string name; public int[] bones; public int target, positionMode, spacingMode, rotateMode; public float rotation, position, spacing, rotateMix, translateMix; }
        public sealed class Weight { public int bone; public float x, y, weight; }
        public sealed class Vertex { public Weight[] weights; public float x, y; }
        public sealed class Attachment
        {
            public string name, path; public int slot, kind; public float rotation, x, y, scaleX, scaleY, width, height;
            public bool closed, constantSpeed; public Vertex[] vertices; public float[] lengths;
        }
        public sealed class Frame { public float time; public float[] values, curve; }
        public sealed class Timeline { public string kind; public int index, type; public Frame[] frames; }
        public sealed class Animation { public string name; public Timeline[] timelines; }
        public sealed class Data { public string version; public Bone[] bones; public Slot[] slots; public Constraint[] constraints; public Attachment[] attachments; public Animation[] animations; }
        sealed class Input : IDisposable
        {
            readonly BinaryReader reader;
            public string[] strings;
            public Input(string path) { reader = new BinaryReader(File.OpenRead(path)); }
            public byte Byte() { return reader.ReadByte(); }
            public bool Bool() { return Byte() != 0; }
            public int Int()
            {
                byte[] data = reader.ReadBytes(4); if (data.Length != 4) throw new EndOfStreamException();
                if (BitConverter.IsLittleEndian) Array.Reverse(data); return BitConverter.ToInt32(data, 0);
            }
            public float Float() { return BitConverter.ToSingle(BitConverter.GetBytes(Int()), 0); }
            public int Var()
            {
                int value = 0, shift = 0;
                while (shift < 35) { byte next = Byte(); value |= (next & 127) << shift; if ((next & 128) == 0) return value; shift += 7; }
                throw new InvalidDataException("Invalid variable integer.");
            }
            public string Text()
            {
                int length = Var(); if (length == 0) return null; if (length == 1) return "";
                return System.Text.Encoding.UTF8.GetString(reader.ReadBytes(length - 1));
            }
            public string Ref() { int index = Var(); return index == 0 ? null : strings[index - 1]; }
            public float[] Floats(int n) { var values = new float[n]; for (int i = 0; i < n; i++) values[i] = Float(); return values; }
            public float[] Curve()
            {
                int type = Byte(); if (type == 0) return new float[0]; if (type == 1) return new[] { -1f };
                if (type == 2) return Floats(4); throw new InvalidDataException("Unknown curve.");
            }
            public void Zero(string group) { int n = Var(); if (n != 0) throw new InvalidDataException("Unsupported track export group: " + group + " (" + n + ")"); }
            public bool AtEnd { get { return reader.BaseStream.Position == reader.BaseStream.Length; } }
            public void Dispose() { reader.Dispose(); }
        }
        public static Data Read(string path)
        {
            using (var input = new Input(path))
            {
                input.Text(); var data = new Data { version = input.Text() };
                if (data.version != "3.8.99") throw new InvalidDataException("Expected supplied Spine 3.8.99 track export.");
                input.Floats(4); bool extra = input.Bool();
                if (extra) { input.Float(); input.Text(); input.Text(); }
                input.strings = new string[input.Var()]; for (int i = 0; i < input.strings.Length; i++) input.strings[i] = input.Text();
                data.bones = new Bone[input.Var()];
                for (int i = 0; i < data.bones.Length; i++)
                {
                    var bone = new Bone { name = input.Text(), parent = i == 0 ? -1 : input.Var(), rotation = input.Float(), x = input.Float(), y = input.Float(),
                        scaleX = input.Float(), scaleY = input.Float(), shearX = input.Float(), shearY = input.Float(), length = input.Float() };
                    if (input.Var() != 0) throw new InvalidDataException("Track bones must use normal transforms.");
                    input.Bool(); if (extra) input.Int(); data.bones[i] = bone;
                }
                data.slots = new Slot[input.Var()];
                for (int i = 0; i < data.slots.Length; i++)
                {
                    var slot = new Slot { name = input.Text(), bone = input.Var() }; input.Int(); input.Int(); slot.attachment = input.Ref(); input.Var(); data.slots[i] = slot;
                }
                input.Zero("IK constraints"); input.Zero("transform constraints");
                data.constraints = new Constraint[input.Var()];
                for (int i = 0; i < data.constraints.Length; i++)
                {
                    var constraint = new Constraint { name = input.Text() }; input.Var(); input.Bool();
                    constraint.bones = new int[input.Var()]; for (int j = 0; j < constraint.bones.Length; j++) constraint.bones[j] = input.Var();
                    constraint.target = input.Var(); constraint.positionMode = input.Var(); constraint.spacingMode = input.Var(); constraint.rotateMode = input.Var();
                    constraint.rotation = input.Float(); constraint.position = input.Float(); constraint.spacing = input.Float(); constraint.rotateMix = input.Float(); constraint.translateMix = input.Float();
                    data.constraints[i] = constraint;
                }
                var attachments = new List<Attachment>();
                int slots = input.Var();
                for (int i = 0; i < slots; i++)
                {
                    int slot = input.Var(), count = input.Var();
                    for (int j = 0; j < count; j++)
                    {
                        string key = input.Ref(); var attachment = new Attachment { slot = slot, name = input.Ref() ?? key, kind = input.Byte() };
                        if (attachment.kind == 0)
                        {
                            attachment.path = input.Ref() ?? attachment.name;
                            attachment.rotation = input.Float(); attachment.x = input.Float(); attachment.y = input.Float();
                            attachment.scaleX = input.Float(); attachment.scaleY = input.Float(); attachment.width = input.Float(); attachment.height = input.Float(); input.Int();
                        }
                        else if (attachment.kind == 4)
                        {
                            attachment.closed = input.Bool(); attachment.constantSpeed = input.Bool();
                            attachment.vertices = new Vertex[input.Var()]; bool weighted = input.Bool();
                            for (int k = 0; k < attachment.vertices.Length; k++)
                            {
                                var vertex = new Vertex();
                                if (weighted)
                                {
                                    vertex.weights = new Weight[input.Var()];
                                    for (int w = 0; w < vertex.weights.Length; w++) vertex.weights[w] = new Weight { bone = input.Var(), x = input.Float(), y = input.Float(), weight = input.Float() };
                                }
                                else { vertex.x = input.Float(); vertex.y = input.Float(); }
                                attachment.vertices[k] = vertex;
                            }
                            attachment.lengths = input.Floats(attachment.vertices.Length / 3); if (extra) input.Int();
                        }
                        else throw new InvalidDataException("Unsupported attachment in track file: " + attachment.kind);
                        attachments.Add(attachment);
                    }
                }
                data.attachments = attachments.ToArray(); input.Zero("additional skins"); input.Zero("events");
                data.animations = new Animation[input.Var()];
                for (int i = 0; i < data.animations.Length; i++)
                {
                    var animation = new Animation { name = input.Text() }; var timelines = new List<Timeline>();
                    input.Zero("slot timelines"); input.Zero("bone timelines"); input.Zero("IK timelines"); input.Zero("transform timelines");
                    int constraints = input.Var();
                    for (int j = 0; j < constraints; j++)
                    {
                        int index = input.Var(), count = input.Var();
                        for (int k = 0; k < count; k++)
                        {
                            int type = input.Byte(); var timeline = new Timeline { kind = "path", index = index, type = type, frames = new Frame[input.Var()] };
                            for (int f = 0; f < timeline.frames.Length; f++)
                                timeline.frames[f] = new Frame { time = input.Float(), values = input.Floats(type == 2 ? 2 : 1), curve = f == timeline.frames.Length - 1 ? new float[0] : input.Curve() };
                            timelines.Add(timeline);
                        }
                    }
                    input.Zero("deformation timelines"); input.Zero("draw-order timelines"); input.Zero("event timelines"); animation.timelines = timelines.ToArray(); data.animations[i] = animation;
                }
                if (!input.AtEnd) throw new InvalidDataException("Unexpected trailing skeleton data.");
                return data;
            }
        }
    }
}
