using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine3D
{
    public class Plane
    {
        // unit vector
        public Vector3 normal = new Vector3(0.0f, 0.0f, 0.0f);

        // distance from origin to the nearest point in the plane
        public float distance = 0.0f;

        public Plane() { }

        public Plane(Vector3 p1, Vector3 p2, Vector3 p3)
        {
            normal = Vector3.Cross(p2 - p1, p3 - p1).Normalized();
            distance = -Vector3.Dot(normal, p1);
        }

        public float Distance(Vector3 point)
        {
            return Vector3.Dot(normal, point) + distance;
        }
    };
}
