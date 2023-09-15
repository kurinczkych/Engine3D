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
        public triangle()
        {
            p = new Vector3[3];
            c = new Color4[3];
            t = new Vec2d[3] { new Vec2d(), new Vec2d(), new Vec2d() };
        }

        public triangle(Vector3[] p)
        {
            this.p = new Vector3[p.Length];
            for (int p_ = 0; p_ < p.Length; p_++)
            {
                this.p[p_] = p[p_];
            }
            c = new Color4[p.Length];
            for (int c_ = 0; c_ < p.Length; c_++)
            {
                c[c_] = Color4.White;
            }

            t = new Vec2d[3] { new Vec2d(), new Vec2d(), new Vec2d() };
        }

        public triangle(Vector3[] p, Vec2d[] t)
        {
            this.p = new Vector3[p.Length];
            c = new Color4[p.Length];
            for (int c_ = 0; c_ < p.Length; c_++)
            {
                c[c_] = Color4.White;
            }
            this.t = new Vec2d[t.Length];
            for (int p_ = 0; p_ < p.Length; p_++)
            {
                this.p[p_] = p[p_];
            }
            for (int t_ = 0; t_ < t.Length; t_++)
            {
                this.t[t_] = t[t_].GetCopy();
            }
        }

        public Vector3 GetMiddle()
        {
            return (p[0] + p[1] + p[2]) / 3;
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

        public Vector3 ComputeNormal()
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
            tri.t[0] = t[0].GetCopy();
            tri.t[1] = t[1].GetCopy();
            tri.t[2] = t[2].GetCopy();

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

        public Vector3[] p;
        public Color4[] c;
        public Vec2d[] t;
    }
}
