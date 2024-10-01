using Newtonsoft.Json;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine3D
{
    public struct Vertex
    {
        [JsonConverter(typeof(Vector3Converter))]
        public Vector3 p;
        [JsonConverter(typeof(Vector3Converter))]
        public Vector3 n;
        [JsonConverter(typeof(Color4Converter))]
        public Color4 c;
        public Vec2d t;
        public int pi;
        [JsonConverter(typeof(Vector3Converter))]
        public Vector3 tan;
        [JsonConverter(typeof(Vector3Converter))]
        public Vector3 bitan;
        public List<int> boneIDs = new List<int>();
        public List<float> boneWeights = new List<float>();
        public int boneCount = 0;

        public bool gotNormal;

        public Vertex()
        {
            p = Vector3.Zero;
            n = Vector3.Zero;
            c = Color4.White;
            t = Vec2d.Zero;
            tan = Vector3.Zero;
            bitan = Vector3.Zero;
            pi = 0;
            gotNormal = false;
        }

        public Vertex(Vector3 pos)
        {
            p = pos;
            n = Vector3.Zero;
            c = Color4.White;
            t = Vec2d.Zero;
            tan = Vector3.Zero;
            bitan = Vector3.Zero;
            pi = 0;
            gotNormal = false;
        }

        public Vertex(Vector3 pos, Vec2d tex)
        {
            p = pos;
            n = Vector3.Zero;
            c = Color4.White;
            t = tex;
            tan = Vector3.Zero;
            bitan = Vector3.Zero;
            pi = 0;
            gotNormal = false;
        }

        public Vertex(Vector3 pos, Vector3 normal, Vec2d tex)
        {
            p = pos;
            n = normal;
            c = Color4.White;
            t = tex;
            tan = Vector3.Zero;
            bitan = Vector3.Zero;
            pi = 0;
            gotNormal = true;
        }

        public float[] GetData()
        {
            return new float[]
            {
                p.X, p.Y, p.Z,
                n.X, n.Y, n.Z,
                t.u, t.v,
                c.R, c.G, c.B, c.A,
                tan.X, tan.Y, tan.Z
            };
        }

        public float[] GetDataWithAnim()
        {
            return new float[]
            {
                p.X, p.Y, p.Z,
                n.X, n.Y, n.Z,
                t.u, t.v,
                c.R, c.G, c.B, c.A,
                tan.X, tan.Y, tan.Z,
                boneIDs[0], boneIDs[1], boneIDs[2], boneIDs[3],
                boneWeights[0], boneWeights[1], boneWeights[2], boneWeights[3],
                boneCount
            };
        }

        public float[] GetDataOnlyPos()
        {
            return new float[]
            {
                p.X, p.Y, p.Z
            };
        }

        public float[] GetDataOnlyPosAndNormal()
        {
            return new float[]
            {
                p.X, p.Y, p.Z,
                n.X, n.Y, n.Z
            };
        }

        public override int GetHashCode()
        {
            var hashCode = new HashCode();
            hashCode.Add(p);
            hashCode.Add(n);
            hashCode.Add(t);
            return hashCode.ToHashCode();
        }
    }
}
