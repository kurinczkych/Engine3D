using OpenTK.Mathematics;
using OpenTK.Platform.Windows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mario64
{
    public class triangle
    {
        public Vector3[] p;
        public Vector3[] n;
        public Color4[] c;
        public Vec2d[] t;
        public bool gotPointNormals;

        public triangle()
        {
            gotPointNormals = false;
            p = new Vector3[3];
            n = new Vector3[3];
            c = new Color4[3];
            t = new Vec2d[3] { new Vec2d(), new Vec2d(), new Vec2d() };
        }

        public triangle(Vector3[] p)
        {
            gotPointNormals = false;
            this.p = new Vector3[p.Length];
            for (int p_ = 0; p_ < p.Length; p_++)
            {
                this.p[p_] = p[p_];
            }
            n = new Vector3[] { new Vector3(), new Vector3(), new Vector3() };
            c = new Color4[p.Length];
            for (int c_ = 0; c_ < p.Length; c_++)
            {
                c[c_] = Color4.White;
            }

            t = new Vec2d[3] { new Vec2d(), new Vec2d(), new Vec2d() };
        }

        public triangle(Vector3[] p, Vec2d[] t)
        {
            gotPointNormals = false;
            this.p = new Vector3[p.Length];
            for (int p_ = 0; p_ < p.Length; p_++)
            {
                this.p[p_] = p[p_];
            }
            n = new Vector3[] { new Vector3(), new Vector3(), new Vector3() };
            c = new Color4[p.Length];
            for (int c_ = 0; c_ < p.Length; c_++)
            {
                c[c_] = Color4.White;
            }
            this.t = new Vec2d[t.Length];
            for (int t_ = 0; t_ < t.Length; t_++)
            {
                this.t[t_] = t[t_].GetCopy();
            }
        }

        public triangle(Vector3[] p, Vector3[] n, Vec2d[] t)
        {
            gotPointNormals = true;
            this.p = new Vector3[p.Length];
            for (int p_ = 0; p_ < p.Length; p_++)
            {
                this.p[p_] = p[p_];
            }
            this.n = new Vector3[] { new Vector3(), new Vector3(), new Vector3() };
            for (int p_ = 0; p_ < p.Length; p_++)
            {
                this.n[p_] = n[p_];
            }
            c = new Color4[p.Length];
            for (int c_ = 0; c_ < p.Length; c_++)
            {
                c[c_] = Color4.White;
            }
            this.t = new Vec2d[t.Length];
            for (int t_ = 0; t_ < t.Length; t_++)
            {
                this.t[t_] = t[t_].GetCopy();
            }
        }

        public Vector3 GetMiddle()
        {
            return (p[0] + p[1] + p[2]) / 3;
        }

        public void TransformPosition(Vector3 transform)
        {
            p[0] += transform;
            p[1] += transform;
            p[2] += transform;
        }

        public void Color(Color4 c)
        {
            for (int i = 0; i < this.c.Length; i++)
            {
                this.c[i] = c;
            }
        }

        public void Color(triangle t)
        {
            for (int i = 0; i < this.c.Length; i++)
            {
                this.c[i] = t.c[i];
            }
        }

        public string GetPointsStr()
        {
            return p[0].ToString() + p[1].ToString() + p[2].ToString();
        }

        public Vector3 ComputeTriangleNormal()
        {
            Vector3 normal, line1, line2;
            line1 = p[1] - p[0];
            line2 = p[2] - p[0];

            normal = Vector3.Cross(line1, line2);
            normal.Normalize();
            return normal;
        }

        public triangle GetCopy()
        {
            triangle tri = new triangle();
            tri.p[0] = p[0];
            tri.p[1] = p[1];
            tri.p[2] = p[2];
            tri.n[0] = n[0];
            tri.n[1] = n[1];
            tri.n[2] = n[2];
            tri.t[0] = t[0].GetCopy();
            tri.t[1] = t[1].GetCopy();
            tri.t[2] = t[2].GetCopy();
            tri.c[0] = c[0];
            tri.c[1] = c[1];
            tri.c[2] = c[2];

            return tri;
        }

        public int CompareTo(triangle tri)
        {
            double z1 = (p[0].Z + p[1].Z + p[2].Z) / 3.0f;
            double z2 = (tri.p[0].Z + tri.p[1].Z + tri.p[2].Z) / 3.0f;

            if (z1 < z2)
                return 1;
            else if (z1 > z2)
                return -1;
            else
                return 0;
        }

        public bool IsPointInTriangle(Vector3 p, out float distance)
        {
            Vector3 a = this.p[0];
            Vector3 b = this.p[1];
            Vector3 c = this.p[2];

            Vector3 normal = Vector3.Cross(b - a, c - a).Normalized();
            distance = Vector3.Dot(normal, p - a);

            Vector3 u = b - a;
            Vector3 v = c - a;
            Vector3 w = p - a;

            float uu = Vector3.Dot(u, u);
            float uv = Vector3.Dot(u, v);
            float vv = Vector3.Dot(v, v);
            float wu = Vector3.Dot(w, u);
            float wv = Vector3.Dot(w, v);
            float denom = 1.0f / (uv * uv - uu * vv);

            float s = (uv * wv - vv * wu) * denom;
            float t = (uv * wu - uu * wv) * denom;

            return s >= 0 && t >= 0 && (s + t) <= 1;
        }
    }
}
