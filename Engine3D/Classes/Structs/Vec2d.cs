using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine3D
{
    public struct Vec2d
    {
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

        public static Vec2d Zero
        {
            get { return new Vec2d(0.0f, 0.0f); }
        }
    }
}
