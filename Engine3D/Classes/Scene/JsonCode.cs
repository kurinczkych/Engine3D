using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Mathematics;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json.Linq;

#pragma warning disable CS8765
#pragma warning disable CS8625

namespace Engine3D
{
    public class AllPropertiesContractResolver : DefaultContractResolver
    {
        protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
        {
            var props = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                            .Select(p => base.CreateProperty(p, memberSerialization))
                            .ToList();

            // Serialize fields too, if desired
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                             .Where(f => !f.Name.Contains("k__BackingField"))
                             .Select(f => base.CreateProperty(f, memberSerialization))
                             .ToList();

            props.AddRange(fields);

            // Ensure that all properties are writable, even if they are non-public
            foreach (var prop in props)
            {
                prop.Writable = true;
                prop.Readable = true;
            }

            return props;
        }
    }

    public class QuaternionConverter : JsonConverter<Quaternion>
    {
        public override void WriteJson(JsonWriter writer, Quaternion value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("X");
            writer.WriteValue(value.X);
            writer.WritePropertyName("Y");
            writer.WriteValue(value.Y);
            writer.WritePropertyName("Z");
            writer.WriteValue(value.Z);
            writer.WritePropertyName("W");
            writer.WriteValue(value.W);
            writer.WriteEndObject();
        }

        public override Quaternion ReadJson(JsonReader reader, Type objectType, Quaternion existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            float x = 0, y = 0, z = 0, w = 0;

            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.PropertyName)
                {
                    string? propName = (string?)reader.Value;
                    if (propName == null)
                        continue;

                    reader.Read();

                    switch (propName)
                    {
                        case "X":
                            x = (float)Convert.ToDouble(reader.Value);
                            break;
                        case "Y":
                            y = (float)Convert.ToDouble(reader.Value);
                            break;
                        case "Z":
                            z = (float)Convert.ToDouble(reader.Value);
                            break;
                        case "W":
                            w = (float)Convert.ToDouble(reader.Value);
                            break;
                    }
                }

                if (reader.TokenType == JsonToken.EndObject)
                    break;
            }

            return new Quaternion(x, y, z, w);
        }
    }

    public class Vector2Converter : JsonConverter<Vector2>
    {
        public override void WriteJson(JsonWriter writer, Vector2 value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("X");
            writer.WriteValue(value.X);
            writer.WritePropertyName("Y");
            writer.WriteValue(value.Y);
            writer.WriteEndObject();
        }

        public override Vector2 ReadJson(JsonReader reader, Type objectType, Vector2 existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            float x = 0, y = 0;

            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.PropertyName)
                {
                    string? propName = (string?)reader.Value;
                    if (propName == null)
                        continue;

                    reader.Read();

                    switch (propName)
                    {
                        case "X":
                            x = (float)Convert.ToDouble(reader.Value);
                            break;
                        case "Y":
                            y = (float)Convert.ToDouble(reader.Value);
                            break;
                    }
                }

                if (reader.TokenType == JsonToken.EndObject)
                    break;
            }

            return new Vector2(x, y);
        }
    }

    public class Color4Converter : JsonConverter<Color4>
    {
        public override void WriteJson(JsonWriter writer, Color4 value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("R");
            writer.WriteValue(value.R);
            writer.WritePropertyName("G");
            writer.WriteValue(value.G);
            writer.WritePropertyName("B");
            writer.WriteValue(value.B);
            writer.WritePropertyName("A");
            writer.WriteValue(value.A);
            writer.WriteEndObject();
        }

        public override Color4 ReadJson(JsonReader reader, Type objectType, Color4 existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            float r = 0, g = 0, b = 0, a = 0;

            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.PropertyName)
                {
                    string? propName = (string?)reader.Value;
                    if (propName == null)
                        continue;

                    reader.Read();

                    switch (propName)
                    {
                        case "R": r = (float)Convert.ToDouble(reader.Value); break;
                        case "G": g = (float)Convert.ToDouble(reader.Value); break;
                        case "B": b = (float)Convert.ToDouble(reader.Value); break;
                        case "A": a = (float)Convert.ToDouble(reader.Value); break;
                    }
                }

                if (reader.TokenType == JsonToken.EndObject)
                    break;
            }

            return new Color4(r, g, b, a);
        }
    }

    public class Vector4Converter : JsonConverter<OpenTK.Mathematics.Vector4>
    {
        public override void WriteJson(JsonWriter writer, OpenTK.Mathematics.Vector4 value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("X");
            writer.WriteValue(value.X);
            writer.WritePropertyName("Y");
            writer.WriteValue(value.Y);
            writer.WritePropertyName("Z");
            writer.WriteValue(value.Z);
            writer.WritePropertyName("W");
            writer.WriteValue(value.W);
            writer.WriteEndObject();
        }

        public override OpenTK.Mathematics.Vector4 ReadJson(JsonReader reader, Type objectType, OpenTK.Mathematics.Vector4 existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            float x = 0, y = 0, z = 0, w = 0;

            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.PropertyName)
                {
                    string? propName = (string?)reader.Value;
                    if (propName == null)
                        continue;

                    reader.Read();

                    switch (propName)
                    {
                        case "X": x = (float)Convert.ToDouble(reader.Value); break;
                        case "Y": y = (float)Convert.ToDouble(reader.Value); break;
                        case "Z": z = (float)Convert.ToDouble(reader.Value); break;
                        case "W": w = (float)Convert.ToDouble(reader.Value); break;
                    }
                }

                if (reader.TokenType == JsonToken.EndObject)
                    break;
            }

            return new OpenTK.Mathematics.Vector4(x, y, z, w);
        }
    }

    public class Vector3Converter : JsonConverter<OpenTK.Mathematics.Vector3>
    {
        public override void WriteJson(JsonWriter writer, OpenTK.Mathematics.Vector3 value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("X");
            writer.WriteValue(value.X);
            writer.WritePropertyName("Y");
            writer.WriteValue(value.Y);
            writer.WritePropertyName("Z");
            writer.WriteValue(value.Z);
            writer.WriteEndObject();
        }

        public override OpenTK.Mathematics.Vector3 ReadJson(JsonReader reader, Type objectType, OpenTK.Mathematics.Vector3 existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            float x = 0, y = 0, z = 0;

            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.PropertyName)
                {
                    string? propName = (string?)reader.Value;
                    if (propName == null)
                        continue;

                    reader.Read();

                    switch (propName)
                    {
                        case "X": x = (float)Convert.ToDouble(reader.Value); break;
                        case "Y": y = (float)Convert.ToDouble(reader.Value); break;
                        case "Z": z = (float)Convert.ToDouble(reader.Value); break;
                    }
                }

                if (reader.TokenType == JsonToken.EndObject)
                    break;
            }

            return new OpenTK.Mathematics.Vector3(x, y, z);
        }
    }

    public class Matrix4Converter : JsonConverter<Matrix4>
    {
        public override void WriteJson(JsonWriter writer, Matrix4 value, JsonSerializer serializer)
        {

            if (value.GetType() != typeof(Matrix4))
            {
                throw new JsonSerializationException($"Expected Matrix4 but got {value.GetType().Name}");
            }

            //if (value == null)
            //{
            //    writer.WriteNull();
            //    return;
            //}

            writer.WriteStartObject();

            // Serialize each row of the Matrix4 as individual properties
            writer.WritePropertyName("M11");
            writer.WriteValue(value.M11);
            writer.WritePropertyName("M12");
            writer.WriteValue(value.M12);
            writer.WritePropertyName("M13");
            writer.WriteValue(value.M13);
            writer.WritePropertyName("M14");
            writer.WriteValue(value.M14);

            writer.WritePropertyName("M21");
            writer.WriteValue(value.M21);
            writer.WritePropertyName("M22");
            writer.WriteValue(value.M22);
            writer.WritePropertyName("M23");
            writer.WriteValue(value.M23);
            writer.WritePropertyName("M24");
            writer.WriteValue(value.M24);

            writer.WritePropertyName("M31");
            writer.WriteValue(value.M31);
            writer.WritePropertyName("M32");
            writer.WriteValue(value.M32);
            writer.WritePropertyName("M33");
            writer.WriteValue(value.M33);
            writer.WritePropertyName("M34");
            writer.WriteValue(value.M34);

            writer.WritePropertyName("M41");
            writer.WriteValue(value.M41);
            writer.WritePropertyName("M42");
            writer.WriteValue(value.M42);
            writer.WritePropertyName("M43");
            writer.WriteValue(value.M43);
            writer.WritePropertyName("M44");
            writer.WriteValue(value.M44);

            writer.WriteEndObject();
        }

        public override Matrix4 ReadJson(JsonReader reader, Type objectType, Matrix4 existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            float m11 = 0, m12 = 0, m13 = 0, m14 = 0;
            float m21 = 0, m22 = 0, m23 = 0, m24 = 0;
            float m31 = 0, m32 = 0, m33 = 0, m34 = 0;
            float m41 = 0, m42 = 0, m43 = 0, m44 = 0;

            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.PropertyName)
                {
                    string? propName = (string?)reader.Value;
                    if (propName == null)
                        continue;

                    reader.Read();

                    switch (propName)
                    {
                        case "M11": m11 = (float)Convert.ToDouble(reader.Value); break;
                        case "M12": m12 = (float)Convert.ToDouble(reader.Value); break;
                        case "M13": m13 = (float)Convert.ToDouble(reader.Value); break;
                        case "M14": m14 = (float)Convert.ToDouble(reader.Value); break;

                        case "M21": m21 = (float)Convert.ToDouble(reader.Value); break;
                        case "M22": m22 = (float)Convert.ToDouble(reader.Value); break;
                        case "M23": m23 = (float)Convert.ToDouble(reader.Value); break;
                        case "M24": m24 = (float)Convert.ToDouble(reader.Value); break;

                        case "M31": m31 = (float)Convert.ToDouble(reader.Value); break;
                        case "M32": m32 = (float)Convert.ToDouble(reader.Value); break;
                        case "M33": m33 = (float)Convert.ToDouble(reader.Value); break;
                        case "M34": m34 = (float)Convert.ToDouble(reader.Value); break;

                        case "M41": m41 = (float)Convert.ToDouble(reader.Value); break;
                        case "M42": m42 = (float)Convert.ToDouble(reader.Value); break;
                        case "M43": m43 = (float)Convert.ToDouble(reader.Value); break;
                        case "M44": m44 = (float)Convert.ToDouble(reader.Value); break;
                    }
                }

                if (reader.TokenType == JsonToken.EndObject)
                    break;
            }

            return new Matrix4(m11, m12, m13, m14, m21, m22, m23, m24, m31, m32, m33, m34, m41, m42, m43, m44);
        }
    }

    public class Vector3DConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Assimp.Vector3D);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var vector = (Assimp.Vector3D)value;
            writer.WriteStartObject();
            writer.WritePropertyName("X");
            writer.WriteValue(vector.X);
            writer.WritePropertyName("Y");
            writer.WriteValue(vector.Y);
            writer.WritePropertyName("Z");
            writer.WriteValue(vector.Z);
            writer.WriteEndObject();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JObject obj = JObject.Load(reader);
            float x = (float)obj["X"];
            float y = (float)obj["Y"];
            float z = (float)obj["Z"];
            return new Assimp.Vector3D(x, y, z);
        }
    }

    public class Color4DConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Assimp.Color4D);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var color = (Assimp.Color4D)value;
            writer.WriteStartObject();
            writer.WritePropertyName("R");
            writer.WriteValue(color.R);
            writer.WritePropertyName("G");
            writer.WriteValue(color.G);
            writer.WritePropertyName("B");
            writer.WriteValue(color.B);
            writer.WritePropertyName("A");
            writer.WriteValue(color.A);
            writer.WriteEndObject();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JObject obj = JObject.Load(reader);
            float r = (float)obj["R"];
            float g = (float)obj["G"];
            float b = (float)obj["B"];
            float a = (float)obj["A"];
            return new Assimp.Color4D(r, g, b, a);
        }
    }

    public class MeshConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Assimp.Mesh);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var mesh = (Assimp.Mesh)value;

            writer.WriteStartObject();

            writer.WritePropertyName("Vertices");
            serializer.Serialize(writer, mesh.Vertices);

            writer.WritePropertyName("Normals");
            serializer.Serialize(writer, mesh.Normals);

            writer.WritePropertyName("Faces");
            serializer.Serialize(writer, mesh.Faces);

            writer.WritePropertyName("MaterialIndex");
            writer.WriteValue(mesh.MaterialIndex);

            // Add more fields if needed
            writer.WriteEndObject();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var mesh = new Assimp.Mesh();

            JObject obj = JObject.Load(reader);

            // Deserialize vertices
            var vertices = obj["Vertices"].ToObject<List<Assimp.Vector3D>>();
            mesh.Vertices.AddRange(vertices);

            // Deserialize normals
            if (obj["Normals"] != null)
            {
                var normals = obj["Normals"].ToObject<List<Assimp.Vector3D>>();
                mesh.Normals.AddRange(normals);
            }

            // Deserialize faces
            if (obj["Faces"] != null)
            {
                var faces = obj["Faces"].ToObject<List<Assimp.Face>>();
                mesh.Faces.AddRange(faces);
            }

            // Set other properties
            mesh.MaterialIndex = obj["MaterialIndex"].ToObject<int>();

            // More fields can be handled here

            return mesh;
        }
    }

    public class IComponentListConverter : JsonConverter<List<IComponent>>
    {
        public override void WriteJson(JsonWriter writer, List<IComponent> value, JsonSerializer serializer)
        {
            writer.WriteStartArray();
            foreach (var comp in value)
            {
                if (comp is BaseMesh bm)
                {
                    if(bm.modelPath != null && bm.modelPath != "")
                    {
                        var originalModelData = bm.model;
                        bm.model = null;

                        serializer.Serialize(writer, bm, typeof(BaseMesh));
                        bm.model = originalModelData;
                    }
                    else
                        serializer.Serialize(writer, bm, typeof(BaseMesh));
                }
                else if (comp is Camera cam)
                {
                    serializer.Serialize(writer, comp, typeof(IComponent));
                }
                else if(comp is Light light)
                {
                    serializer.Serialize(writer, comp, typeof(IComponent));
                }
            }
            writer.WriteEndArray();
        }

        public override List<IComponent> ReadJson(JsonReader reader, Type objectType, List<IComponent> existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var components = new List<IComponent>();

            if (reader.TokenType == JsonToken.StartArray)
            {
                while (reader.Read() && reader.TokenType != JsonToken.EndArray)
                {
                    IComponent? comp = serializer.Deserialize<IComponent>(reader);
                    if (comp != null)
                    {
                        components.Add(comp);
                    }
                }
            }

            return components;
        }
    }
}
