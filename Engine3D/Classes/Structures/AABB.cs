using FontStashSharp;
using MagicPhysX;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine3D
{
    public class AABB
    {
        public Vector3 Min;
        public Vector3 Max;

        public float Width { get { return Max.X - Min.X; } }
        public float Height { get { return Max.Y - Min.Y; } }
        public float Depth { get { return Max.Z - Min.Z; } }

        public Vector3 Center
        {
            get
            {
                return new Vector3(
                (Min.X + Max.X) * 0.5f,
                (Min.Y + Max.Y) * 0.5f,
                (Min.Z + Max.Z) * 0.5f);
            }
        }

        public AABB()
        {
            Min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            Max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
        }

        public AABB(Vector3 min, Vector3 max)
        {
            Min = min;
            Max = max;
        }

        public int ContainPoints(triangle triangle)
        {
            int count = 0;
            if (IsPointInsideAABB(triangle.v[0].p)) count++;
            if (IsPointInsideAABB(triangle.v[1].p)) count++;
            if (IsPointInsideAABB(triangle.v[2].p)) count++;
            return count;
        }

        public bool Contains(triangle triangle)
        {
            return IsPointInsideAABB(triangle.v[0].p) && IsPointInsideAABB(triangle.v[1].p) && IsPointInsideAABB(triangle.v[2].p);
        }

        public void Enclose(triangle tri)
        {
            for (int i = 0; i < 3; i++)
            {
                Min = Helper.Vector3Min(Min, tri.v[i].p);
                Max = Helper.Vector3Max(Max, tri.v[i].p);
            }
        }

        public void Enclose(Vector3 point)
        {
            Min = Vector3.ComponentMin(Min, point);
            Max = Vector3.ComponentMax(Max, point);
        }
        public float SurfaceArea()
        {
            float dx = Max.X - Min.X;
            float dy = Max.Y - Min.Y;
            float dz = Max.Z - Min.Z;

            return 2.0f * (dx * dy + dy * dz + dx * dz);
        }

        public float LongestDistance(Vector3 position)
        {
            // Find all corners of the AABB
            Vector3[] corners = new Vector3[8];
            corners[0] = Min;
            corners[1] = new Vector3(Max.X, Min.Y, Min.Z);
            corners[2] = new Vector3(Min.X, Max.Y, Min.Z);
            corners[3] = new Vector3(Max.X, Max.Y, Min.Z);
            corners[4] = new Vector3(Min.X, Min.Y, Max.Z);
            corners[5] = new Vector3(Max.X, Min.Y, Max.Z);
            corners[6] = new Vector3(Min.X, Max.Y, Max.Z);
            corners[7] = Max;

            // Compute distance to each corner and keep track of the largest distance
            float longestDistanceSquared = 0.0f;
            foreach (var corner in corners)
            {
                float distanceSquared = (position - corner).LengthSquared;
                if (distanceSquared > longestDistanceSquared)
                {
                    longestDistanceSquared = distanceSquared;
                }
            }

            // Return the square root of the longest distance squared to get the actual distance
            return (float)Math.Sqrt(longestDistanceSquared);
        }

        public bool RayIntersects(Vector3 rayOrigin, Vector3 rayDirection, out float distance)
        {
            distance = 0.0f;
            float tmin = float.NegativeInfinity;
            float tmax = float.PositiveInfinity;

            // Check for intersection with all six bounds
            for (int i = 0; i < 3; ++i)
            {
                if (Math.Abs(rayDirection[i]) < float.Epsilon)
                {
                    // Ray is parallel to slab. No hit if origin not within slab
                    if (rayOrigin[i] < Min[i] || rayOrigin[i] > Max[i]) return false;
                }
                else
                {
                    // Compute intersection t value of ray with near and far plane of slab
                    float ood = 1.0f / rayDirection[i];
                    float t1 = (Min[i] - rayOrigin[i]) * ood;
                    float t2 = (Max[i] - rayOrigin[i]) * ood;
                    // Make t1 be intersection with near plane, t2 with far plane
                    if (t1 > t2) (t1, t2) = (t2, t1);
                    // Compute the intersection of slab intersection intervals
                    tmin = Math.Max(tmin, t1);
                    tmax = Math.Min(tmax, t2);
                    // Exit with no collision as soon as slab intersection becomes empty
                    if (tmin > tmax) return false;
                }
            }

            // Ray intersects all 3 slabs. Return point (q) and intersection t value (tmin)
            distance = tmin;
            return true;
        }

        public Vector3 FurthestCorner(Vector3 position)
        {
            // Find all corners of the AABB
            Vector3[] corners = new Vector3[8];
            corners[0] = Min;
            corners[1] = new Vector3(Max.X, Min.Y, Min.Z);
            corners[2] = new Vector3(Min.X, Max.Y, Min.Z);
            corners[3] = new Vector3(Max.X, Max.Y, Min.Z);
            corners[4] = new Vector3(Min.X, Min.Y, Max.Z);
            corners[5] = new Vector3(Max.X, Min.Y, Max.Z);
            corners[6] = new Vector3(Min.X, Max.Y, Max.Z);
            corners[7] = Max;

            // Initialize variables to keep track of the furthest corner
            float longestDistanceSquared = 0.0f;
            Vector3 furthest = new Vector3();

            foreach (var corner in corners)
            {
                float distanceSquared = (position - corner).LengthSquared;
                if (distanceSquared > longestDistanceSquared)
                {
                    longestDistanceSquared = distanceSquared;
                    furthest = corner; // update the furthest corner
                }
            }

            return furthest;
        }

        public List<float> GetTriangleVertices()
        {
            // Define the 8 vertices of the AABB
            Vector3 v0 = new Vector3(Min.X, Min.Y, Min.Z);
            Vector3 v1 = new Vector3(Max.X, Min.Y, Min.Z);
            Vector3 v2 = new Vector3(Max.X, Min.Y, Max.Z);
            Vector3 v3 = new Vector3(Min.X, Min.Y, Max.Z);
            Vector3 v4 = new Vector3(Min.X, Max.Y, Min.Z);
            Vector3 v5 = new Vector3(Max.X, Max.Y, Min.Z);
            Vector3 v6 = new Vector3(Max.X, Max.Y, Max.Z);
            Vector3 v7 = new Vector3(Min.X, Max.Y, Max.Z);

            // Define the 12 triangles using the vertices in counterclockwise order
            List<Vector3> triangleVertices = new List<Vector3>
            {
                // Bottom
                v0, v1, v2,
                v0, v2, v3,

                // Top
                v4, v7, v6,
                v4, v6, v5,

                // Front
                v0, v4, v5,
                v0, v5, v1,

                // Back
                v2, v6, v7,
                v2, v7, v3,

                // Left
                v0, v3, v7,
                v0, v7, v4,

                // Right
                v1, v5, v6,
                v1, v6, v2
            };

            // Convert to List<float> for use in OpenGL or other rendering systems
            List<float> vertices = new List<float>();
            foreach (Vector3 vertex in triangleVertices)
            {
                vertices.Add(vertex.X);
                vertices.Add(vertex.Y);
                vertices.Add(vertex.Z);
            }

            return vertices;
        }

        public List<Line> GetLines()
        {
            List<Line> lines = new List<Line>();

            Vector3[] corners = 
            {
                new Vector3(Min.X, Min.Y, Min.Z),
                new Vector3(Max.X, Min.Y, Min.Z),
                new Vector3(Max.X, Max.Y, Min.Z),
                new Vector3(Min.X, Max.Y, Min.Z),
                new Vector3(Min.X, Min.Y, Max.Z),
                new Vector3(Max.X, Min.Y, Max.Z),
                new Vector3(Max.X, Max.Y, Max.Z),
                new Vector3(Min.X, Max.Y, Max.Z)
             };

            int[,] edgePairs = 
            {
                {0, 1}, {1, 2}, {2, 3}, {3, 0},
                {4, 5}, {5, 6}, {6, 7}, {7, 4},
                {0, 4}, {1, 5}, {2, 6}, {3, 7}
            };

            for (int i = 0; i < 12; i++)
            {
                lines.Add(new Line
                (
                    corners[edgePairs[i, 0]],
                    corners[edgePairs[i, 1]]
                ));
            }

            return lines;
        }

        public bool IsPointInsideAABB(Vector3 point)
        {
            return point.X >= Min.X && point.X <= Max.X &&
                   point.Y >= Min.Y && point.Y <= Max.Y &&
                   point.Z >= Min.Z && point.Z <= Max.Z;
        }

        public Vector3[] GetCorners()
        {
            Vector3[] corners =
            {
                new Vector3(Min.X, Min.Y, Min.Z),
                new Vector3(Max.X, Min.Y, Min.Z),
                new Vector3(Max.X, Max.Y, Min.Z),
                new Vector3(Min.X, Max.Y, Min.Z),
                new Vector3(Min.X, Min.Y, Max.Z),
                new Vector3(Max.X, Min.Y, Max.Z),
                new Vector3(Max.X, Max.Y, Max.Z),
                new Vector3(Min.X, Max.Y, Max.Z)
             };

            return corners;
        }


        public static AABB Union(AABB a, AABB b)
        {
            Vector3 newMin = new Vector3(
                Math.Min(a.Min.X, b.Min.X),
                Math.Min(a.Min.Y, b.Min.Y),
                Math.Min(a.Min.Z, b.Min.Z)
            );

            Vector3 newMax = new Vector3(
                Math.Max(a.Max.X, b.Max.X),
                Math.Max(a.Max.Y, b.Max.Y),
                Math.Max(a.Max.Z, b.Max.Z)
            );

            return new AABB { Min = newMin, Max = newMax };
        }

        public static AABB GetBoundsFromStep(AABB bounds, Vector3i index, int gridSize)
        {
            Vector3 min = new Vector3(bounds.Min.X + (index.X * gridSize), bounds.Min.Y + (index.Y * gridSize), bounds.Min.Z + (index.Z * gridSize));
            Vector3 max = new Vector3(bounds.Min.X + ((index.X + 1) * gridSize), bounds.Min.Y + ((index.Y + 1) * gridSize), bounds.Min.Z + ((index.Z + 1) * gridSize));
            AABB b = new AABB(min, max);
            return b;
        }
    }
}
