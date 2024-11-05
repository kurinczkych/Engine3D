using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine3D
{
    public class Projection
    {
        public float left;
        public float right;
        public float top;
        public float bottom;
        public float near;
        public float far;

        public Projection(float left, float right, float top, float bottom, float near, float far)
        {
            this.left = left;
            this.right = right;
            this.top = top;
            this.bottom = bottom;
            this.near = near;
            this.far = far;
        }

        public static Projection ShadowSmall
        {
            get
            {
                return new Projection(-10, 10, 10, -10, 0.1f, 100);
            }
        }

        public static Projection ShadowMedium
        {
            get
            {
                return new Projection(-30, 30, 30, -30, 0.1f, 100);
            }
        }

        public static Projection ShadowLarge
        {
            get
            {
                return new Projection(-70, 70, 70, -70, 0.1f, 100);
            }
        }
    }
}
