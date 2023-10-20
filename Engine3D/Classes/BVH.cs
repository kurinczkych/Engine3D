using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Engine3D
{
    public class AABB
    {
        public Vector3 Min;
        public Vector3 Max;

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

        public void Enclose(triangle tri)
        {
            for (int i = 0; i < 3; i++)
            {
                Min = Helper.Vector3Min(Min, tri.p[i]);
                Max = Helper.Vector3Max(Max, tri.p[i]);
            }
        }
        public float SurfaceArea()
        {
            float dx = Max.X - Min.X;
            float dy = Max.Y - Min.Y;
            float dz = Max.Z - Min.Z;

            return 2.0f * (dx * dy + dy * dz + dx * dz);
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

    }
    public class BVHNode
    {
        public AABB bounds;
        public BVHNode left;
        public BVHNode right;
        public List<triangle> triangles;

        public bool isVisible = false;
    }

    public class Bin
    {
        public int Count { get; set; } = 0;
        public AABB Bounds { get; set; } = new AABB(); // Assuming you have an Axis-Aligned Bounding Box (AABB) structure
    }

    public class BVH
    {
        public BVHNode Root;

        public BVH(List<triangle> triangles)
        {
            Root = BuildBVH(triangles);
        }

        private const int NUM_BINS = 12;  // for instance, you can adjust this
        private const float TRAVERSAL_COST = 1.0f;  // cost of traversing a BVH node
        private const float TRIANGLE_COST = 1.0f;   // cost of intersecting a triangle

        private BVHNode BuildBVH(List<triangle> triangles)
        {
            BVHNode node = new BVHNode();
            node.bounds = ComputeBounds(triangles);
            node.triangles = new List<triangle>();

            if (triangles.Count <= 75)  // leaf node
            {
                node.triangles.AddRange(triangles);
                return node;
            }

            Bin[] bins = new Bin[NUM_BINS];
            for (int i = 0; i < NUM_BINS; i++)
            {
                bins[i] = new Bin();
            }

            int splitAxis = -1;
            int splitBin = -1;
            float splitCost = float.MaxValue;

            float bestMinCentroid = 0;
            float bestBinSize = 0;

            for (int axis = 0; axis < 3; axis++)
            {
                float minCentroid = float.MaxValue;
                float maxCentroid = float.MinValue;

                // 1. Compute bin boundaries
                foreach (var tri in triangles)
                {
                    float centroidPos = (tri.p[0][axis] + tri.p[1][axis] + tri.p[2][axis]) / 3.0f;
                    minCentroid = Math.Min(minCentroid, centroidPos);
                    maxCentroid = Math.Max(maxCentroid, centroidPos);
                }

                float binSize = (maxCentroid - minCentroid) / NUM_BINS;

                // Reset bins for the current axis
                foreach (var bin in bins)
                {
                    bin.Count = 0;
                    bin.Bounds = new AABB();
                }

                // 2. Map triangle centroids to bins
                foreach (var tri in triangles)
                {
                    float centroidPos = (tri.p[0][axis] + tri.p[1][axis] + tri.p[2][axis]) / 3.0f;
                    int binIndex = (int)((centroidPos - minCentroid) / binSize);
                    binIndex = Math.Max(binIndex, 0);  // Ensure it's not negative
                    binIndex = Math.Min(binIndex, NUM_BINS - 1);  // Clamp to the max index

                    bins[binIndex].Count++;
                    bins[binIndex].Bounds = AABB.Union(bins[binIndex].Bounds, ComputeBounds(new List<triangle> { tri }));
                }

                // Evaluate SAH cost for each bin boundary
                for (int i = 0; i < NUM_BINS - 1; i++)
                {
                    AABB leftBounds = new AABB();
                    AABB rightBounds = new AABB();
                    int leftCount = 0;
                    int rightCount = 0;

                    for (int j = 0; j <= i; j++)
                    {
                        leftCount += bins[j].Count;
                        leftBounds = AABB.Union(leftBounds, bins[j].Bounds);
                    }

                    for (int j = i + 1; j < NUM_BINS; j++)
                    {
                        rightCount += bins[j].Count;
                        rightBounds = AABB.Union(rightBounds, bins[j].Bounds);
                    }

                    float leftArea = leftBounds.SurfaceArea();
                    float rightArea = rightBounds.SurfaceArea();

                    float cost = TRAVERSAL_COST + TRIANGLE_COST *
                                (leftCount * leftArea + rightCount * rightArea);

                    if (cost < splitCost)
                    {
                        splitAxis = axis;
                        splitBin = i;
                        splitCost = cost;
                        bestMinCentroid = minCentroid;
                        bestBinSize = binSize;
                    }
                }
            }

            // Split triangles based on the best SAH split
            List<triangle> leftTriangles = new List<triangle>();
            List<triangle> rightTriangles = new List<triangle>();
            float splitPosition = bestMinCentroid + splitBin * bestBinSize;

            foreach (var tri in triangles)
            {
                float centroidPos = (tri.p[0][splitAxis] + tri.p[1][splitAxis] + tri.p[2][splitAxis]) / 3.0f;
                if (centroidPos <= splitPosition)
                    leftTriangles.Add(tri);
                else
                    rightTriangles.Add(tri);
            }

            node.left = BuildBVH(leftTriangles);
            node.right = BuildBVH(rightTriangles);

            return node;
        }

        public List<WireframeMesh> ExtractWireframes(BVHNode node, VAO wireVao, VBO wireVbo, int shaderId, ref Frustum frustum, ref Camera camera)
        {
            List<WireframeMesh> meshes = new List<WireframeMesh>();

            if (node == null)
            {
                return meshes;
            }

            //WireframeMesh(wireVao, wireVbo, noTextureShaderProgram.id, ref frustum, ref camera, Color4.White
            WireframeMesh currentMesh = new WireframeMesh(wireVao, wireVbo, shaderId, ref frustum, ref camera, Color4.Red);
            AABB box = node.bounds;

            // Create lines for each edge of the bounding box
            Vector3[] corners = {
                new Vector3(box.Min.X, box.Min.Y, box.Min.Z),
                new Vector3(box.Max.X, box.Min.Y, box.Min.Z),
                new Vector3(box.Max.X, box.Max.Y, box.Min.Z),
                new Vector3(box.Min.X, box.Max.Y, box.Min.Z),
                new Vector3(box.Min.X, box.Min.Y, box.Max.Z),
                new Vector3(box.Max.X, box.Min.Y, box.Max.Z),
                new Vector3(box.Max.X, box.Max.Y, box.Max.Z),
                new Vector3(box.Min.X, box.Max.Y, box.Max.Z)
            };

            int[,] edgePairs = {
                {0, 1}, {1, 2}, {2, 3}, {3, 0},
                {4, 5}, {5, 6}, {6, 7}, {7, 4},
                {0, 4}, {1, 5}, {2, 6}, {3, 7}
            };

            for (int i = 0; i < 12; i++)
            {
                currentMesh.lines.Add(new Line
                (
                    corners[edgePairs[i, 0]],
                    corners[edgePairs[i, 1]]
                ));
            }

            meshes.Add(currentMesh);

            // Recursively extract from children
            meshes.AddRange(ExtractWireframes(node.left, wireVao, wireVbo, shaderId, ref frustum, ref camera));
            meshes.AddRange(ExtractWireframes(node.right, wireVao, wireVbo, shaderId, ref frustum, ref camera));

            return meshes;
        }

        private AABB ComputeBounds(List<triangle> triangles)
        {
            AABB box = new AABB();

            foreach (var tri in triangles)
            {
                box.Enclose(tri);
            }

            return box;
        }

        public void WriteBVHToFile(string file)
        {
            using (StreamWriter writer = new StreamWriter(file))
            {
                WriteNode(writer, Root, 0);  // Start at depth 0
            }
        }


        private void WriteNode(StreamWriter writer, BVHNode node, int depth)
        {
            if (node == null) return;

            string indent = new string(' ', depth * 2);  // Indentation for visualization
            writer.WriteLine($"{indent}Node Bounds: {node.bounds.Min} to {node.bounds.Max}");

            writer.WriteLine($"{indent}  Triangle: {node.triangles.Count.ToString()}");  // Assuming your triangle has a simple representation
            writer.WriteLine($"{indent}  IsVisible: {node.isVisible}");  // Assuming your triangle has a simple representation
            if (node.triangles != null && node.triangles.Count > 0)  // It's a leaf node
            {
                writer.WriteLine($"{indent}  LEAFLEAFLEAFLEAFLEAFLEAF");  // Assuming your triangle has a simple representation
            }
            else
            {
                writer.WriteLine($"{indent}Left Child:");
                WriteNode(writer, node.left, depth + 1);

                writer.WriteLine($"{indent}Right Child:");
                WriteNode(writer, node.right, depth + 1);
            }
        }
    }
}
