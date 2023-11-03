using FontStashSharp;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Engine3D
{
    
    public class BVHNode
    {
        public BVHNode()
        {
            visibility = new List<bool>();
        }

        public AABB bounds;
        public BVHNode left;
        public BVHNode right;
        public List<triangle> triangles;
        public bool frustumVisibility = false;
        public int depth = 0;

        public List<bool> visibility;
        public int samplesPassedPrevFrame = 1;

        public int pendingQuery = -1;
        public int key;

        public const int VisCount = 5;
    }

    public class Bin
    {
        public int Count { get; set; } = 0;
        public AABB Bounds { get; set; } = new AABB(); // Assuming you have an Axis-Aligned Bounding Box (AABB) structure
    }

    public class BVH
    {
        public BVHNode Root;
        public List<BVHNode> leaves;

        public BVH(List<triangle> triangles, int shaderId)
        {
            int index = 0;
            int depth = 0;
            leaves = new List<BVHNode>();
            Root = BuildBVH(triangles, ref index, ref depth);
            uniformLocations = new Dictionary<string, int>();
            GetUniformLocations(shaderId);
        }

        public BVH(List<triangle> triangles)
        {
            int index = 0;
            int depth = 0;
            leaves = new List<BVHNode>();
            Root = BuildBVH(triangles, ref index, ref depth);
        }

        private const int NUM_BINS = 12;  // for instance, you can adjust this
        private const float TRAVERSAL_COST = 1.0f;  // cost of traversing a BVH node
        private const float TRIANGLE_COST = 1.0f;   // cost of intersecting a triangle

        public int number_of_leaves = 0;
        public int number_of_nodes = 0;

        private const int leafLimit = 30;

        public Dictionary<string, int> uniformLocations;

        private BVHNode BuildBVH(List<triangle> triangles, ref int index, ref int depth)
        {
            BVHNode node = new BVHNode();
            number_of_nodes++;
            node.bounds = ComputeBounds(triangles);
            node.triangles = new List<triangle>();
            node.key = index;
            node.depth = depth;
            index++;

            if (triangles.Count <= leafLimit)  // leaf node
            {
                node.triangles.AddRange(triangles);
                number_of_leaves++;
                leaves.Add(node);
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

            depth++;
            node.left = BuildBVH(leftTriangles, ref index, ref depth);
            node.right = BuildBVH(rightTriangles, ref index, ref depth);

            return node;
        }

        private void GetUniformLocations(int shaderProgramId)
        {
            uniformLocations.Add("modelMatrix", GL.GetUniformLocation(shaderProgramId, "modelMatrix"));
            uniformLocations.Add("viewMatrix", GL.GetUniformLocation(shaderProgramId, "viewMatrix"));
            uniformLocations.Add("projectionMatrix", GL.GetUniformLocation(shaderProgramId, "projectionMatrix"));
        }

        public void GetFrustumVisibleTriangles(ref Frustum frustum, ref Camera camera)
        {
            GetFrustumVisibleTrianglesRec(ref frustum, ref camera, Root);
        }

        private void GetFrustumVisibleTrianglesRec(ref Frustum frustum, ref Camera camera, BVHNode node)
        {
            if (node == null)
                return;

            if (node.left == null && node.right == null && node.triangles != null)
            {
                if (frustum.IsAABBInside(node.bounds))
                {
                    node.triangles.ForEach(x => x.visibile = true);
                    node.frustumVisibility = true;

                    return;
                }
                else
                {
                    node.triangles.ForEach(x => x.visibile = false);
                    node.frustumVisibility = false;
                }
            }

            if (!frustum.IsAABBInside(node.bounds))
            {
                return;
            }

            if(node.left != null)
                GetFrustumVisibleTrianglesRec(ref frustum, ref camera, node.left);

            if(node.right != null)
                GetFrustumVisibleTrianglesRec(ref frustum, ref camera, node.right);
        }

        public List<WireframeMesh> ExtractWireframes(BVHNode node, VAO wireVao, VBO wireVbo, int shaderId, ref Camera camera)
        {
            List<WireframeMesh> meshes = new List<WireframeMesh>();

            if (node == null)
            {
                return meshes;
            }

            if (node.left == null && node.right == null && node.triangles != null)
            {

                //WireframeMesh(wireVao, wireVbo, noTextureShaderProgram.id, ref frustum, ref camera, Color4.White
                WireframeMesh currentMesh = new WireframeMesh(wireVao, wireVbo, shaderId, ref camera, Color4.Red);
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
            }
            else
            {
                if(node.left != null)
                    meshes.AddRange(ExtractWireframes(node.left, wireVao, wireVbo, shaderId, ref camera));

                if(node.right != null)
                    meshes.AddRange(ExtractWireframes(node.right, wireVao, wireVbo, shaderId, ref camera));
            }

            

            // Recursively extract from children

            return meshes;
        }

        public List<WireframeMesh> ExtractWireframesWithPos(BVHNode node, VAO wireVao, VBO wireVbo, int shaderId, ref Camera camera, Vector3 pos)
        {
            List<WireframeMesh> meshes = new List<WireframeMesh>();

            if (node == null)
            {
                return meshes;
            }

            if (!node.bounds.IsPointInsideAABB(pos))
                return meshes;

            if (node.left == null && node.right == null && node.triangles != null)
            {

                //WireframeMesh(wireVao, wireVbo, noTextureShaderProgram.id, ref frustum, ref camera, Color4.White
                WireframeMesh currentMesh = new WireframeMesh(wireVao, wireVbo, shaderId, ref camera, Color4.Red);
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
            }
            else
            {
                if(node.left != null)
                    meshes.AddRange(ExtractWireframesWithPos(node.left, wireVao, wireVbo, shaderId, ref camera, pos));

                if(node.right != null)
                    meshes.AddRange(ExtractWireframesWithPos(node.right, wireVao, wireVbo, shaderId, ref camera, pos));
            }

            return meshes;
        }

        public void TransformBVH(ref Matrix4 modelMatrix)
        {
            TransformBVHRecursive(Root, ref modelMatrix);
        }

        private void TransformBVHRecursive(BVHNode node, ref Matrix4 modelMatrix)
        {
            if(node == null) return;

            node.bounds = TransformAABB(node.bounds, ref modelMatrix);

            if(node.left != null)
                TransformBVHRecursive(node.left, ref modelMatrix);
            if(node.right != null)
                TransformBVHRecursive(node.right, ref modelMatrix);
        }

        private AABB TransformAABB(AABB aabb, ref Matrix4 modelMatrix)
        {
            AABB newAABB = new AABB();
            var points = aabb.GetCorners();
            for (int i = 0; i < points.Count(); i++)
            {
                points[i] = Vector3.TransformPosition(points[i], modelMatrix);
                newAABB.Enclose(points[i]);
            }
            return newAABB;
        }

        public void CalculateFrustumVisibility(ref Camera camera)
        {
            CalculateFrustumVisibilityRec(Root, ref camera);
        }

        private void CalculateFrustumVisibilityRec(BVHNode node, ref Camera camera)
        {
            if(node == null) return;

            if(camera.frustum.IsAABBInside(node.bounds)/* || camera.IsAABBClose(node.bounds)*/)
            {
                node.triangles.ForEach(x => x.visibile = true);

                if (node.left != null)
                    CalculateFrustumVisibilityRec(node.left, ref camera);
                if(node.right != null)
                    CalculateFrustumVisibilityRec(node.right, ref camera);
            }
            else
            {
                node.triangles.ForEach(x => x.visibile = false);
                return;
            }
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
            writer.WriteLine($"{indent}  IsVisible: {node.visibility.Any(x => x == true)}");  // Assuming your triangle has a simple representation
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
