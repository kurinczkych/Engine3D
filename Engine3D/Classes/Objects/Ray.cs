using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine3D
{
    public class Ray
    {
        public Vector3 Origin = Vector3.Zero;
        public Vector3 Dir = Vector3.Zero;

        public Ray(Vector3 origin, Vector3 dir)
        {
            Origin = origin;
            Dir = dir;
        }

        public Vector3? RayIntersectionSamePlane(Ray ray2)
        {
            Vector3 s = ray2.Origin - Origin;
            Vector3 d1 = Dir;
            Vector3 d2 = ray2.Dir;

            float determinant = d1.X * d2.Y - d1.Y * d2.X;

            // Check for parallel rays
            if (Math.Abs(determinant) < 1e-6)
            {
                return null; // Rays are parallel or nearly parallel
            }

            float t1 = (s.X * d2.Y - s.Y * d2.X) / determinant;
            float t2 = (s.Y * d1.X - s.X * d1.Y) / determinant;

            // Check if t1 and t2 are in the valid range for a ray (t >= 0)
            if (t1 < 0 || t2 < 0)
            {
                return null; // Intersection occurs behind the ray origins
            }

            // Calculate the intersection point using t1 (or t2, both should give the same result)
            Vector3 intersectionPoint = Origin + t1 * Dir;
            return intersectionPoint;
        }

    }
}
