using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FontStashSharp;
using OpenTK.Mathematics;

namespace Engine3D
{
    public class GridNode
    {
        public AABB Bounds;
        public List<triangle> triangles;

        public GridNode(AABB bounds)
        {
            Bounds = bounds;
            triangles = new List<triangle>();
        }

        public void Add(triangle triangle)
        {
            triangles.Add(triangle);
        }
    }

    public class GridStructure
    {
        public AABB Bounds = new AABB();
        public int GridSize;
        public GridNode[,,] Grid;
        public int nodeCount;
        public Vector3i stepSize;

        public GridStructure(List<triangle> tris, AABB bounds, int gridSize) 
        { 
            Bounds = bounds;
            GridSize = gridSize;

            stepSize = new Vector3i((int)Bounds.Width / GridSize + 1, (int)Bounds.Height / GridSize + 1, (int)Bounds.Depth / GridSize + 1);

            Grid = new GridNode[stepSize.X, stepSize.Y, stepSize.Z];
            nodeCount = stepSize.X * stepSize.Y * stepSize.Z;

            Vector3i currentIndex = new Vector3i();
            for (currentIndex.X = 0; currentIndex.X < stepSize.X; currentIndex.X++)
            {
                for (currentIndex.Y = 0; currentIndex.Y < stepSize.Y; currentIndex.Y++)
                {
                    for (currentIndex.Z = 0; currentIndex.Z < stepSize.Z; currentIndex.Z++)
                    {
                        AABB b = AABB.GetBoundsFromStep(Bounds, currentIndex, gridSize);
                        Grid[currentIndex.X, currentIndex.Y, currentIndex.Z] = new GridNode(b);
                    }
                }
            }

            foreach (triangle triangle in tris)
            {
                Vector3 center = triangle.GetMiddle();
                Vector3i index = GetIndex(center);
                Grid[index.X, index.Y, index.Z].Add(triangle);
            }
        }

        public List<triangle> GetTriangles(Camera camera)
        {
            List<triangle> triangles = new List<triangle>();

            Vector3 cameraPos = new Vector3(camera.GetPosition());
            if(cameraPos.X > Bounds.Max.X) cameraPos.X = Bounds.Max.X;
            if(cameraPos.X < Bounds.Min.X) cameraPos.X = Bounds.Min.X;
            if(cameraPos.Y > Bounds.Max.Y) cameraPos.Y = Bounds.Max.Y;
            if(cameraPos.Y < Bounds.Min.Y) cameraPos.Y = Bounds.Min.Y;
            if(cameraPos.Z > Bounds.Max.Z) cameraPos.Z = Bounds.Max.Z;
            if(cameraPos.Z < Bounds.Min.Z) cameraPos.Z = Bounds.Min.Z;

            List<GridNode> nodes = GetNodesInFrontOfCamera(cameraPos, camera);
            int index = 0;

            foreach (GridNode node in nodes)
            {
                List<triangle> trisSorted = new List<triangle>(node.triangles);

                for (int i = trisSorted.Count-1; i >= 0; i--)
                {
                    trisSorted[i].SetColor(Helper.CalcualteColorBasedOnDistance(index, nodes.Count()));
                }

                index++;
                triangles.AddRange(trisSorted);
            }

            return triangles;
        }

        public List<GridNode> GetNodesInFrontOfCamera(Vector3 cameraPos, Camera camera)
        {
            ConcurrentBag<(GridNode node, float distance)> nodesWithDistance = new ConcurrentBag<(GridNode, float)>();

            // Divide the grid into chunks based on the number of available CPU cores
            int numberOfChunks = BaseMesh.threadSize;

            Vector3i chunkSize = new Vector3i(stepSize.X / numberOfChunks, stepSize.Y, stepSize.Z);

            ParallelOptions parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = BaseMesh.threadSize }; // Adjust as needed
            Parallel.ForEach(Enumerable.Range(0, numberOfChunks), parallelOptions, chunkIndex =>
            {
                Vector3i start = new Vector3i(chunkIndex * chunkSize.X, 0, 0);
                Vector3i end = start + chunkSize;

                for (int x = start.X; x < end.X; x++)
                {
                    for (int y = 0; y < stepSize.Y; y++)
                    {
                        for (int z = 0; z < stepSize.Z; z++)
                        {
                            GridNode node = Grid[x, y, z];

                            if (node != null)
                            {
                                Vector3 centerOfNode = node.Bounds.Center;

                                float distance = (centerOfNode - cameraPos).Length;
                                if (camera.frustum.IsAABBInside(node.Bounds))
                                {
                                    nodesWithDistance.Add((node, distance));
                                    node.triangles.ForEach(x => x.visibile = true);
                                }
                            }
                        }
                    }
                }
            });

            // Sort nodes based on distance
            return nodesWithDistance.OrderBy(nd => nd.distance).Select(nd => nd.node).ToList();
        }


        public WireframeMesh ExtractWireframeRange(VAO wireVao, VBO wireVbo, int shaderId, ref Camera camera)
        {
            if (Grid == null)
            {
                return null;
            }

            WireframeMesh currentMesh = new WireframeMesh(wireVao, wireVbo, shaderId, ref camera.frustum, ref camera, Color4.Red);

            int range = 1;
            Vector3i index = GetIndex(camera.GetPosition());
            for (int x = -range; x <= range; x++)
            {
                for (int y = -range; y <= range; y++)
                {
                    for (int z = -range; z <= range; z++)
                    {
                        int x_ = Math.Max(0, Math.Min(Grid.GetLength(0)-1, index.X + x));
                        int y_ = Math.Max(0, Math.Min(Grid.GetLength(1)-1, index.Y + y));
                        int z_ = Math.Max(0, Math.Min(Grid.GetLength(2)-1, index.Z + z));
                        currentMesh.lines.AddRange(Grid[x_,y_,z_].Bounds.GetLines());
                    }
                }
            }

            return currentMesh;
        }

        public WireframeMesh ExtractWireframe(VAO wireVao, VBO wireVbo, int shaderId, ref Camera camera)
        {
            if (Grid == null)
            {
                return null;
            }

            WireframeMesh currentMesh = new WireframeMesh(wireVao, wireVbo, shaderId, ref camera.frustum, ref camera, Color4.Red);

            Vector3i currentIndex = new Vector3i();
            for (currentIndex.X = 0; currentIndex.X < stepSize.X; currentIndex.X++)
            {
                for (currentIndex.Y = 0; currentIndex.Y < stepSize.Y; currentIndex.Y++)
                {
                    for (currentIndex.Z = 0; currentIndex.Z < stepSize.Z; currentIndex.Z++)
                    {
                        currentMesh.lines.AddRange(Grid[currentIndex.X, currentIndex.Y, currentIndex.Z].Bounds.GetLines());
                    }
                }
            }

            return currentMesh;
        }

        public Vector3i GetIndex(Vector3 point)
        {
            Vector3 p = new Vector3(point);
            p += new Vector3(Math.Abs(Bounds.Min.X), Math.Abs(Bounds.Min.Y), Math.Abs(Bounds.Min.Z));
            Vector3i index = new Vector3i((int)p.X / GridSize, (int)p.Y / GridSize, (int)p.Z / GridSize);
            return index;
        }
    }
}
