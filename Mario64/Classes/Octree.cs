using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mario64
{
    public class OctreeNode
    {
        public BoundingBox Bounds { get; set; }
        public List<triangle> Triangles { get; set; } = new List<triangle>();
        public OctreeNode[] Children { get; set; }

        public void Subdivide(int depth)
        {
            if (depth <= 0 || Triangles.Count <= 1) return; // Termination criteria

            Children = new OctreeNode[8];

            // Compute half dimensions and midpoint for the current bounding box
            var half = (Bounds.Max - Bounds.Min) / 2f;
            var mid = Bounds.Min + half;

            // Define the bounds for the eight children
            BoundingBox[] childBounds = {
                new BoundingBox(Bounds.Min, mid), // 0: min to midpoint
                new BoundingBox(new Vector3(mid.X, Bounds.Min.Y, Bounds.Min.Z), new Vector3(Bounds.Max.X, mid.Y, mid.Z)), // 1: front-bottom-right quadrant
                new BoundingBox(new Vector3(Bounds.Min.X, mid.Y, Bounds.Min.Z), new Vector3(mid.X, Bounds.Max.Y, mid.Z)), // 2: front-top-left quadrant
                new BoundingBox(new Vector3(Bounds.Min.X, Bounds.Min.Y, mid.Z), new Vector3(mid.X, mid.Y, Bounds.Max.Z)), // 3: back-bottom-left quadrant
                new BoundingBox(new Vector3(mid.X, mid.Y, Bounds.Min.Z), Bounds.Max), // 4: front-top-right quadrant
                new BoundingBox(new Vector3(mid.X, Bounds.Min.Y, mid.Z), new Vector3(Bounds.Max.X, mid.Y, Bounds.Max.Z)), // 5: back-bottom-right quadrant
                new BoundingBox(new Vector3(Bounds.Min.X, mid.Y, mid.Z), new Vector3(mid.X, Bounds.Max.Y, Bounds.Max.Z)), // 6: back-top-left quadrant
                new BoundingBox(mid, Bounds.Max)}; // 7: back-top-right quadrant (midpoint to max)

            for (int i = 0; i < 8; i++)
            {
                Children[i] = new OctreeNode { Bounds = childBounds[i] };

                foreach (var triangle in Triangles)
                {
                    if (Intersects(Children[i].Bounds, triangle))
                    {
                        Children[i].Triangles.Add(triangle);
                    }
                }

                Children[i].Subdivide(depth - 1);
            }

            // Optionally clear triangles in the current node to save memory
            Triangles.Clear();
        }

        private bool Intersects(BoundingBox box, triangle tri)
        {
            Vector3[] boxVertices = {
                                        box.Min,
                                        new Vector3(box.Max.X, box.Min.Y, box.Min.Z),
                                        new Vector3(box.Min.X, box.Max.Y, box.Min.Z),
                                        new Vector3(box.Min.X, box.Min.Y, box.Max.Z),
                                        new Vector3(box.Max.X, box.Max.Y, box.Min.Z),
                                        new Vector3(box.Min.X, box.Max.Y, box.Max.Z),
                                        new Vector3(box.Max.X, box.Min.Y, box.Max.Z),
                                        box.Max
                                    };

            Vector3 E1 = tri.p[1] - tri.p[0];
            Vector3 E2 = tri.p[2] - tri.p[0];
            Vector3 E3 = tri.p[2] - tri.p[1];
            Vector3[] axes = {
                                new Vector3(1, 0, 0),
                                new Vector3(0, 1, 0),
                                new Vector3(0, 0, 1),
                                Vector3.Cross(E1, new Vector3(1, 0, 0)),
                                Vector3.Cross(E1, new Vector3(0, 1, 0)),
                                Vector3.Cross(E1, new Vector3(0, 0, 1)),
                                Vector3.Cross(E2, new Vector3(1, 0, 0)),
                                Vector3.Cross(E2, new Vector3(0, 1, 0)),
                                Vector3.Cross(E2, new Vector3(0, 0, 1)),
                                Vector3.Cross(E3, new Vector3(1, 0, 0)),
                                Vector3.Cross(E3, new Vector3(0, 1, 0)),
                                Vector3.Cross(E3, new Vector3(0, 0, 1)),
                                Vector3.Cross(E1, E2).Normalized()
                            };

            foreach (Vector3 axis in axes)
            {
                float minBox = float.MaxValue;
                float maxBox = float.MinValue;

                foreach (Vector3 vertex in boxVertices)
                {
                    float projection = Vector3.Dot(vertex, axis);
                    minBox = Math.Min(minBox, projection);
                    maxBox = Math.Max(maxBox, projection);
                }

                float minTri = Math.Min(Vector3.Dot(tri.p[0], axis), Math.Min(Vector3.Dot(tri.p[1], axis), Vector3.Dot(tri.p[2], axis)));
                float maxTri = Math.Max(Vector3.Dot(tri.p[0], axis), Math.Max(Vector3.Dot(tri.p[1], axis), Vector3.Dot(tri.p[2], axis)));

                if (maxBox < minTri || maxTri < minBox)
                    return false;
            }

            return true;
        }
    }

    public class Octree
    {
        public OctreeNode Root { get; private set; }

        private int maxDepth;

        public Octree(List<triangle> triangles, BoundingBox bounds, int maxDepth)
        {
            Root = new OctreeNode { Triangles = triangles, Bounds = bounds };
            this.maxDepth = maxDepth;
            Root.Subdivide(maxDepth);
        }

        public List<triangle> GetNearTriangles(Vector3 v)
        {
            List<triangle> result = new List<triangle>();
            CollectNearTriangles(Root, v, result);
            return result;
        }

        private void CollectNearTriangles(OctreeNode node, Vector3 v, List<triangle> result)
        {
            if (node == null || !node.Bounds.Contains(v)) return;

            // If it's a leaf node or has no children, collect its triangles
            if (node.Children == null || node.Children.Length == 0)
            {
                result.AddRange(node.Triangles);
                return;
            }

            // Otherwise, recursively check children
            foreach (var child in node.Children)
            {
                CollectNearTriangles(child, v, result);
            }
        }
    }
}
