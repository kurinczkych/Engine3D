using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mario64
{
    public class Vec3d
    {
        public Vec3d()
        {
            W = 1.0f;
            color = Color4.White;
        }

        public Vec3d(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
            W = 1.0f;
            color = Color4.White;
        }

        public float X;
        public float Y;
        public float Z;
        public float W;

        public Vec3d GetCopy()
        {
            Vec3d v = new Vec3d(X, Y, Z);
            v.W = W;
            v.color = color;
            return v;
        }

        public static float Dot(Vec3d v1, Vec3d v2)
        {
            return v1.X * v2.X + v1.Y * v2.Y + v1.Z * v2.Z;
        }

        public static Vec3d Cross(Vec3d v1, Vec3d v2)
        {
            Vec3d v = new Vec3d();
            v.X = v1.Y * v2.Z - v1.Z * v2.Y;
            v.Y = v1.Z * v2.X - v1.X * v2.Z;
            v.Z = v1.X * v2.Y - v1.Y * v2.X;
            v.W = v1.W;
            v.color = v1.color;
            return v;
        }

        public static Vec3d Normalize(Vec3d v)
        {
            float l = v.Length;
            Vec3d v2 = new Vec3d(v.X / l, v.Y / l, v.Z / l);
            v2.W = v.W;
            v2.color = v.color;
            return v;
        }

        public void Normalize()
        {
            float l = Length;
            X /= l;
            Y /= l;
            Z /= l;
        }

        #region Operator overloads
        public static Vec3d operator -(Vec3d v1, Vec3d v2)
        {
            Vec3d v3 = new Vec3d(v1.X - v2.X, v1.Y - v2.Y, v1.Z - v2.Z);
            v3.W = v1.W;
            v3.color = v1.color;
            return v3;
        }
        public static Vec3d operator +(Vec3d v1, Vec3d v2)
        {
            Vec3d v3 = new Vec3d(v1.X + v2.X, v1.Y + v2.Y, v1.Z + v2.Z);
            v3.W = v1.W;
            v3.color = v1.color;
            return v3;
        }
        public static Vec3d operator /(Vec3d v1, float d)
        {
            if (d == 0)
                return v1;
            Vec3d v3 = new Vec3d(v1.X / d, v1.Y / d, v1.Z / d);
            v3.W = v1.W;
            v3.color = v1.color;
            return v3;
        }
        public static Vec3d operator *(Vec3d v1, float d)
        {
            Vec3d v3 = new Vec3d(v1.X * d, v1.Y * d, v1.Z * d);
            v3.W = v1.W;
            v3.color = v1.color;
            return v3;
        }
        #endregion

        public Color4 color;
        public float Length
        {
            get
            {
                return (float)Math.Sqrt(Vec3d.Dot(this, this));
            }
        }
    }
    public class Vec2d
    {
        public Vec2d() { }

        public Vec2d(float u, float v)
        {
            this.u = u;
            this.v = v;
            this.w = 1.0f;
        }

        public float u = 0.0f;
        public float v = 0.0f;
        public float w;


        public Vec2d GetCopy()
        {
            Vec2d v2 = new Vec2d(u, v);
            v2.w = w;
            return v2;
        }
    }
}
