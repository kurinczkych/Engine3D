using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
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

        public override int GetHashCode()
        {
            unchecked // Overflow is fine; it's by design
            {
                int hashCode = 17; // Prime number

                // Combine the hash codes of significant properties
                hashCode = hashCode * 23 + Bounds.GetHashCode();

                return hashCode;
            }
        }
    }

    public class GridStructure
    {
        public AABB Bounds = new AABB();
        public int GridSize;
        public GridNode[,,] Grid;
        public int nodeCount;
        public Vector3i stepSize;
        public int biggestStepSize;

        public GridStructure(List<triangle> tris, AABB bounds, int gridSize) 
        { 
            Bounds = bounds;
            GridSize = gridSize;

            stepSize = new Vector3i((int)Bounds.Width / GridSize + 1, (int)Bounds.Height / GridSize + 1, (int)Bounds.Depth / GridSize + 1);
            biggestStepSize = stepSize.X;
            biggestStepSize = biggestStepSize < stepSize.Y ? stepSize.Y : (biggestStepSize < stepSize.Z ? stepSize.Z : biggestStepSize);

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
            Vector3i centerIndex = GetIndex(cameraPos);  // Get the grid index for the given point
            List<GridNode> result = new List<GridNode>();

            int maxRadius = biggestStepSize * GridSize;

            HashSet<GridNode> visited = new HashSet<GridNode>();

            for (int radius = 0; radius <= maxRadius; radius += GridSize)
            {
                List<GridNode> currentRadiusNodes = new List<GridNode>();

                // Define bounds for the current "radius"
                int startX = Math.Max(centerIndex.X - radius, 0);
                int endX = Math.Min(centerIndex.X + radius, stepSize.X - 1);
                int startY = Math.Max(centerIndex.Y - radius, 0);
                int endY = Math.Min(centerIndex.Y + radius, stepSize.Y - 1);
                int startZ = Math.Max(centerIndex.Z - radius, 0);
                int endZ = Math.Min(centerIndex.Z + radius, stepSize.Z - 1);

                // Iterate over the nodes within the current "radius"
                for (int x = startX; x <= endX; x++)
                {
                    for (int y = startY; y <= endY; y++)
                    {
                        for (int z = startZ; z <= endZ; z++)
                        {
                            // Only add nodes that are on the "surface" of the current radius
                            if (x == startX || x == endX || y == startY || y == endY || z == startZ || z == endZ)
                            {
                                if (camera.frustum.IsAABBInside(Grid[x, y, z].Bounds) && !visited.Contains(Grid[x, y, z]))
                                {
                                    currentRadiusNodes.Add(Grid[x, y, z]);
                                    visited.Add(Grid[x, y, z]);
                                }
                            }
                        }
                    }
                }

                result.AddRange(currentRadiusNodes);
            }

            return result;
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
