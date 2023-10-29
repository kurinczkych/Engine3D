using MagicPhysX;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine3D
{
    public enum TrianglePosition
    {
        InFront,
        Behind,
        OnPlane
    }

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

        public Plane(triangle tri)
        {
            normal = Vector3.Cross(tri.p[1] - tri.p[0], tri.p[2] - tri.p[0]).Normalized();
            distance = -Vector3.Dot(normal, tri.p[0]);
        }

        public float Distance(Vector3 point)
        {
            return Vector3.Dot(normal, point) + distance;
        }

        public TrianglePosition ClassifyPoint(Vector3 point)
        {
            float result = Vector3.Dot(normal, point) + distance;
            if (result > 0.00001f)
                return TrianglePosition.InFront;
            if (result < -0.00001f)
                return TrianglePosition.Behind;
            return TrianglePosition.OnPlane;
        }
    };
}
