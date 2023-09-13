using OpenTK.Mathematics;
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
            p = new Vec3d[3];
            t = new Vec2d[3] { new Vec2d(), new Vec2d(), new Vec2d() };
        }

        public triangle(Vec3d[] p)
        {
            this.p = new Vec3d[p.Length];
            for (int p_ = 0; p_ < p.Length; p_++)
            {
                this.p[p_] = p[p_].GetCopy();
            }

            t = new Vec2d[3] { new Vec2d(), new Vec2d(), new Vec2d() };
        }

        public triangle(Vec3d[] p, Vec2d[] t)
        {
            this.p = new Vec3d[p.Length];
            this.t = new Vec2d[t.Length];
            for (int p_ = 0; p_ < p.Length; p_++)
            {
                this.p[p_] = p[p_].GetCopy();
            }
            for (int t_ = 0; t_ < t.Length; t_++)
            {
                this.t[t_] = t[t_].GetCopy();
            }
        }

        public Vec3d GetMiddle()
        {
            return (p[0] + p[1] + p[2]) / 3;
        }

        public void Color(Color4 c)
        {
            foreach (Vec3d p_ in p)
                p_.color = c;
        }

        public void Color(triangle t)
        {
            for (int p_ = 0; p_ < p.Length; p_++)
            {
                p[p_].color = t.p[p_].color;
            }
        }

        public string GetPointsStr()
        {
            return p[0].ToString() + p[1].ToString() + p[2].ToString();
        }

        public Vec3d ComputeNormal()
        {
            Vec3d normal, line1, line2;
            line1 = p[1] - p[0];
            line2 = p[2] - p[0];

            normal = Vec3d.Cross(line1, line2);
            normal.Normalize();
            return normal;
        }

        public triangle GetCopy()
        {
            triangle tri = new triangle();
            tri.p[0] = p[0].GetCopy();
            tri.p[1] = p[1].GetCopy();
            tri.p[2] = p[2].GetCopy();
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

        public Vec3d[] p;
        public Vec2d[] t;
    }
}
