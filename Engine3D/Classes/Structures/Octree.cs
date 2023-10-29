using MagicPhysX;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Engine3D
{

    public class Octree
    {
        private const int MaxTriangles = 10;
        private const int MaxDepth = 10;

        public AABB Bounds { get; private set; }
        public List<triangle> Triangles { get; private set; }
        public Octree[] Children { get; private set; }
        public Octree Parent { get; set; }
        public bool split = false;
        public int triangleCount = 0;
        public int depth = 0;

        public bool IsLeaf { get { return Children[0] == null; } }

        public Octree()
        {
            Triangles = new List<triangle>();
            Children = new Octree[8];
        }

        private Octree(AABB bounds)
        {
            Bounds = bounds;
            Triangles = new List<triangle>();
            Children = new Octree[8];
        }

        public void Build(List<triangle> tris, AABB worldBounds)
        {
            Bounds = worldBounds;
            depth = 0;
            triangleCount = tris.Count();
            foreach (var triangle in tris)
            {
                Insert(triangle);
            }
        }

        public void Insert(triangle tri)
        {
            if(IsLeaf/* || depth == MaxDepth*/)
            {
                Triangles.Add(tri);
                if(Triangles.Count > MaxTriangles/* && depth < MaxDepth*/)
                {
                    Split();
                    foreach(triangle subTri in Triangles)
                    {
                        for (int i = 0; i < 8; i++)
                        {
                            int inPoints = Children[i].Bounds.ContainPoints(subTri);
                            if (inPoints >= 1)
                            {
                                Children[i].Insert(subTri);
                                break;
                            }
                        }
                    }
                    Triangles.Clear();
                }
            }
            else
            {
                for (int i = 0; i < 8; i++)
                {
                    int inPoints = Children[i].Bounds.ContainPoints(tri);
                    if (inPoints >= 1)
                    {
                        Children[i].Insert(tri);
                        break;
                    }
                }
            }
        }

        private void Split()
        {
            Vector3 size = Bounds.Max - Bounds.Min;
            Vector3 half = size / 2.0f;
            Vector3 min = Bounds.Min;
            Vector3 max = Bounds.Max;

            // Bottom-left-back quadrant
            Children[0] = new Octree(new AABB(min, min + half));
            Children[0].Parent = this;
            Children[0].depth = depth + 1;

            // Bottom-right-back quadrant
            Children[1] = new Octree(new AABB(new Vector3(min.X + half.X, min.Y, min.Z),
                                                     new Vector3(max.X, min.Y + half.Y, min.Z + half.Z)));
            Children[1].Parent = this;
            Children[1].depth = depth + 1;

            // Bottom-left-front quadrant
            Children[2] = new Octree(new AABB(new Vector3(min.X, min.Y, min.Z + half.Z),
                                                     new Vector3(min.X + half.X, min.Y + half.Y, max.Z)));
            Children[2].Parent = this;
            Children[2].depth = depth + 1;

            // Bottom-right-front quadrant
            Children[3] = new Octree(new AABB(new Vector3(min.X + half.X, min.Y, min.Z + half.Z),
                                                     new Vector3(max.X, min.Y + half.Y, max.Z)));
            Children[3].Parent = this;
            Children[3].depth = depth + 1;

            // Top-left-back quadrant
            Children[4] = new Octree(new AABB(new Vector3(min.X, min.Y + half.Y, min.Z),
                                                     new Vector3(min.X + half.X, max.Y, min.Z + half.Z)));
            Children[4].Parent = this;
            Children[4].depth = depth + 1;

            // Top-right-back quadrant
            Children[5] = new Octree(new AABB(new Vector3(min.X + half.X, min.Y + half.Y, min.Z),
                                                     new Vector3(max.X, max.Y, min.Z + half.Z)));
            Children[5].Parent = this;
            Children[5].depth = depth + 1;

            // Top-left-front quadrant
            Children[6] = new Octree(new AABB(new Vector3(min.X, min.Y + half.Y, min.Z + half.Z),
                                                     new Vector3(min.X + half.X, max.Y, max.Z)));
            Children[6].Parent = this;
            Children[6].depth = depth + 1;

            // Top-right-front quadrant
            Children[7] = new Octree(new AABB(new Vector3(min.X + half.X, min.Y + half.Y, min.Z + half.Z),
                                                     new Vector3(max.X, max.Y, max.Z)));
            Children[7].Parent = this;
            Children[7].depth = depth + 1;
        }

        public override int GetHashCode()
        {
            unchecked // Overflow is fine; it's by design
            {
                int hashCode = 17; // Prime number

                // Combine the hash codes of significant properties
                hashCode = hashCode * 23 + Bounds.GetHashCode();
                hashCode = hashCode * 23 + Triangles.GetHashCode();

                if (Children != null)
                {
                    foreach (var child in Children)
                    {
                        if (child != null)
                        {
                            hashCode = hashCode * 23 + child.GetHashCode();
                        }
                    }
                }

                return hashCode;
            }
        }

        public List<triangle> GetTriangles(Vector3 point, Frustum frustum)
        {
            int index = 0;
            List<triangle> tris = CollectTrianglesFromNodeOutward(point, ref frustum, ref index);
            List<triangle> tris2 = GetAllTriangles();

            return tris;
        }

        public List<triangle> GetAllTriangles()
        {
            List<triangle> allTriangles = new List<triangle>();

            CollectTriangles(this, allTriangles);

            return allTriangles;
        }

        private void CollectTriangles(Octree node, List<triangle> collectedTriangles)
        {
            if (node.IsLeaf)
            {
                collectedTriangles.AddRange(node.Triangles);
            }
            else
            {
                for (int i = 0; i < 8; i++)
                {
                    CollectTriangles(node.Children[i], collectedTriangles);
                }
            }
        }


        private Octree GetNodeContainingPoint(Vector3 point)
        {
            if (!Bounds.IsPointInsideAABB(point))
            {
                point.Y = Bounds.Max.Y - 10;
                if(!Bounds.IsPointInsideAABB(point))
                    return null;
            }

            // If this is a leaf node or no children (yet), return this
            if (IsLeaf)
            {
                return this;
            }

            // Otherwise, search children
            for (int i = 0; i < 8; i++)
            {
                Octree node = Children[i].GetNodeContainingPoint(point);
                if (node != null)
                {
                    return node;
                }
            }

            return null; // Shouldn't really reach here
        }

        public List<triangle> CollectTrianglesFromNodeOutward(Vector3 point, ref Frustum frustum, ref int index)
        {
            List<triangle> result = new List<triangle>();
            Octree startNode = GetNodeContainingPoint(point);
            if (startNode == null)
            {
                return result;
            }

            LinkedList<Octree> queue = new LinkedList<Octree>();
            HashSet<Octree> visited = new HashSet<Octree>();

            queue.AddFirst(startNode);

            while (queue.Count > 0)
            {
                Octree currentNode = queue.First();
                queue.RemoveFirst();

                ;
                if (currentNode.IsLeaf && currentNode.Triangles.Count > 0)
                {
                    List<triangle> tris = new List<triangle>(currentNode.Triangles);
                    foreach (triangle tri in tris)
                    {
                        tri.SetColor(Helper.CalcualteColorBasedOnDistance(index, triangleCount));
                        index++;
                    }
                    result.AddRange(tris);
                }
                else if(!currentNode.IsLeaf)
                {
                    foreach (var child in currentNode.Children)
                    {
                        if (child != currentNode && !visited.Contains(child))
                        {
                            if(frustum.IsAABBInside(child.Bounds))
                                queue.AddFirst(child);
                            visited.Add(child);
                        }
                    }
                }

                // Add parent's siblings (adjacent nodes on the same level)
                Octree parent = currentNode.Parent;
                if (parent != null)
                {
                    foreach (var child in parent.Children)
                    {
                        if (child != currentNode && !visited.Contains(child))
                        {
                            if (frustum.IsAABBInside(child.Bounds))
                                queue.AddLast(child);
                            visited.Add(child);
                        }
                    }
                }

                if (queue.Count() == 0)
                {
                    Octree v = GetNotVisited(currentNode.Parent, visited);
                    if (v != null)
                    {
                        if (frustum.IsAABBInside(v.Bounds))
                            queue.AddLast(v);
                        visited.Add(v);
                    }
                }
            }

            return result;
        }

        private Octree GetNotVisited(Octree parent, HashSet<Octree> visited)
        {
            foreach(var child in parent.Children)
            {
                if (!visited.Contains(child))
                    return child;
            }

            if (parent.Parent == null)
                return null;
            return GetNotVisited(parent.Parent, visited);
        }
    }

}
