using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using FontStashSharp;
using OpenTK.Mathematics;

namespace Engine3D
{
    public enum GridDir
    {
        Up,
        Down,
        Left,
        Right
    }

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

        private void AddNodeToSublist(List<GridNode> nodes, GridNode node)
        {
            lock (nodes)
                nodes.Add(node);
        }

        public List<GridNode> GetNodesInFrontOfCamera(Vector3 cameraPos, Camera camera)
        {
            Vector3i centerIndex = GetIndex(cameraPos);  // Get the grid index for the given point
            List<GridNode> result = new List<GridNode>();

            int rowStart = 0, rowEnd = Grid.GetLength(0) - 1;
            int colStart = 0, colEnd = Grid.GetLength(2) - 1;

            while (rowStart <= rowEnd && colStart <= colEnd)
            {
                for (int i = colStart; i <= colEnd; i = i + 1)
                {
                    AABB extendedBounds = Grid[rowStart, centerIndex.Y, i].Bounds;
                    extendedBounds.Min.Y = Bounds.Min.Y;
                    extendedBounds.Max.Y = Bounds.Max.Y;
                    if (camera.frustum.IsAABBInside(extendedBounds))
                    {
                        for (int i_ = 0; i_ < Grid.GetLength(1); i_++)
                        {
                            result.Add(Grid[rowStart, i_, i]);
                        }
                    }
                }

                rowStart = rowStart + 1;

                for (int i = rowStart; i <= rowEnd; i = i + 1)
                {
                    AABB extendedBounds = Grid[i, centerIndex.Y, colEnd].Bounds;
                    extendedBounds.Min.Y = Bounds.Min.Y;
                    extendedBounds.Max.Y = Bounds.Max.Y;
                    if (camera.frustum.IsAABBInside(extendedBounds))
                    {
                        for (int i_ = 0; i_ < Grid.GetLength(1); i_++)
                        {
                            result.Add(Grid[i, i_, colEnd]);
                        }
                    }
                }

                colEnd = colEnd - 1;

                if (rowStart <= rowEnd)
                {
                    for (int i = colEnd; i >= colStart; i = i - 1)
                    {
                        AABB extendedBounds = Grid[rowEnd, centerIndex.Y, i].Bounds;
                        extendedBounds.Min.Y = Bounds.Min.Y;
                        extendedBounds.Max.Y = Bounds.Max.Y;
                        if (camera.frustum.IsAABBInside(extendedBounds))
                        {
                            for (int i_ = 0; i_ < Grid.GetLength(1); i_++)
                            {
                                result.Add(Grid[rowEnd, i_, i]);
                            }
                        }
                    }

                    rowEnd = rowEnd - 1;
                }

                if (colStart <= colEnd)
                {
                    for (int i = rowEnd; i >= rowStart; i = i - 1)
                    {
                        AABB extendedBounds = Grid[i, centerIndex.Y, colStart].Bounds;
                        extendedBounds.Min.Y = Bounds.Min.Y;
                        extendedBounds.Max.Y = Bounds.Max.Y;
                        if (camera.frustum.IsAABBInside(extendedBounds))
                        {
                            for (int i_ = 0; i_ < Grid.GetLength(1); i_++)
                            {
                                result.Add(Grid[i, i_, colStart]);
                            }
                        }
                    }

                    colStart = colStart + 1;
                }
            }



            result.Reverse();
            return result;
        }


        public WireframeMesh ExtractWireframeRange(VAO wireVao, VBO wireVbo, int shaderId, ref Camera camera)
        {
            if (Grid == null)
            {
                return null;
            }

            WireframeMesh currentMesh = new WireframeMesh(wireVao, wireVbo, shaderId, ref camera.frustum, ref camera, Color4.Red);

            Vector3i centerIndex = GetIndex(camera.GetPosition());  // Get the grid index for the given point

            //if ((x_ >= 0 && x_ < Grid.GetLength(0)) && (y_ >= 0 && y_ < Grid.GetLength(1)) && (z_ >= 0 && z_ < Grid.GetLength(2)) &&
            //                            camera.frustum.IsAABBInside(Grid[x_, y_, z_].Bounds))
            //{
            //    currentMesh.lines.AddRange(Grid[x_, y_, z_].Bounds.GetLines());
            //}

            bool[,] visited = new bool[Grid.GetLength(0), Grid.GetLength(2)];
            int flatNodeCount = Grid.GetLength(0) * Grid.GetLength(2);
            int currentNodeCount = 0;

            int x = centerIndex.X;
            int y = centerIndex.Y;
            int z = centerIndex.Z;

            if (camera.frustum.IsAABBInside(Grid[x, y, z].Bounds))
            {
                currentMesh.lines.AddRange(Grid[x, y, z].Bounds.GetLines());
            }
            visited[x, z] = true;

            GridDir currentDir = GridDir.Right;

            while(currentNodeCount != flatNodeCount)
            {
                switch(currentDir)
                {
                    case GridDir.Right:
                        if (!visited[x, z - 1])
                        {
                            currentDir = GridDir.Down;
                            z--;
                        }
                        else
                            x++;
                        break;
                    case GridDir.Left:
                        if (!visited[x, z + 1])
                        {
                            currentDir = GridDir.Up;
                            z++;
                        }
                        else
                            x--;
                        break;
                    case GridDir.Up:
                        if (!visited[x + 1, z])
                        {
                            currentDir = GridDir.Right;
                            x++;
                        }
                        else
                            z++;
                        break;
                    case GridDir.Down:
                        if (!visited[x - 1, z])
                        {
                            currentDir = GridDir.Left;
                            x--;
                        }
                        else
                            z--;
                        break;
                }

                if (z < 0 || x < 0)
                    break;

                if (!visited[x,z])
                {
                    if (camera.frustum.IsAABBInside(Grid[x, y, z].Bounds))
                    {
                        currentMesh.lines.AddRange(Grid[x, y, z].Bounds.GetLines());
                    }
                    currentNodeCount++;
                    visited[x, z] = true;
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
