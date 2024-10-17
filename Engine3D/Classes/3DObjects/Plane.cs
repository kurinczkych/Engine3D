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

    public enum Axis
    {
        X,
        Y,
        Z
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
            normal = Vector3.Cross(tri.v[1].p - tri.v[0].p, tri.v[2].p - tri.v[0].p).Normalized();
            distance = -Vector3.Dot(normal, tri.v[0].p);
        }

        public Plane(Vector3 normal, Vector3 position)
        {
            this.normal = normal.Normalized();
            distance = Vector3.Dot(this.normal, position);
        }

        public Plane(Vector3 normal, float distance)
        {
            this.normal = normal.Normalized();
            this.distance = distance;
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

        public Vector3? RayPlaneIntersection(Vector3 origin, Vector3 dir)
        {
            float denominator = Vector3.Dot(normal, dir);
            if (Math.Abs(denominator) < 0.0001f) // Adjust this threshold as needed
            {
                return null;
            }

            float t = (distance - Vector3.Dot(normal, origin)) / denominator;
            if (t < 0)
            {
                return null;
            }

            Vector3 intersectionPoint = origin + t * dir;
            return intersectionPoint;
        }

        public bool IsPointOnPlane(Vector3 point)
        {
            // Normalize the normal vector to ensure the equation works correctly
            Vector3 normalizedNormal = Vector3.Normalize(normal);

            // Calculate the dot product of the point and the plane's normal vector
            float dot = Vector3.Dot(normalizedNormal, point);

            // Check if the point satisfies the plane equation
            return Math.Abs(dot + distance) < 1e-6; // Allowing for a small margin of error
        }

        public bool IsDirectionOnPlane(Vector3 dir)
        {
            // Calculate the dot product of the ray's direction and the plane's normal
            float dotProduct = Vector3.Dot(dir, normal);

            // Check if the dot product is close to zero
            return Math.Abs(dotProduct) < 1e-6; // Allowing for a small margin of error
        }
    };
}
