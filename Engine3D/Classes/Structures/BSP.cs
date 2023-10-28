using MagicPhysX;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine3D
{
    public class BSP
    {
        public BSPNode Root;

        public BSP(List<triangle> triangles)
        {
            Root = BuildBSP(triangles);
        }

        private BSPNode BuildBSP(List<triangle> triangles)
        {
            if (triangles.Count == 0)
            {
                return null;
            }

            if (triangles.Count <= 30)
            {
                BSPNode leafNode = new BSPNode();
                leafNode.Triangles = triangles;  // Assign the triangles to this leaf node
                leafNode.isLeaf = true;
                return leafNode;                  // Return the leaf node
            }

            // Take a triangle from the list to be the partitioning plane.
            triangle partitioningTriangle = triangles[0];

            BSPNode node = new BSPNode();
            node.PartitioningPlane = partitioningTriangle;

            List<triangle> frontList = new List<triangle>();
            List<triangle> backList = new List<triangle>();

            foreach (triangle triangle in triangles)
            {
                if (triangle != partitioningTriangle)
                {
                    ClassifyTriangle(triangle, partitioningTriangle, frontList, backList);
                }
            }

            node.Front = BuildBSP(frontList);
            node.Back = BuildBSP(backList);

            // For the purpose of this example, we are making all nodes (including non-leaves) have triangles.
            // You can modify this based on your exact requirements.
            //node.Triangles = triangles;

            return node;
        }

        private void ClassifyTriangle(triangle triangle, triangle partitioningPlane, List<triangle> frontList, List<triangle> backList)
        {
            int inFront = 0;
            int behind = 0;

            foreach (var vertex in triangle.p)
            {
                float distance = PointToPlaneDistance(vertex, partitioningPlane);

                if (distance > 0) inFront++;
                else if (distance < 0) behind++;
            }

            // If all vertices are in front
            if (inFront == 3)
            {
                frontList.Add(triangle);
            }
            // If all vertices are behind
            else if (behind == 3)
            {
                backList.Add(triangle);
            }
            // If the triangle straddles the plane
            else
            {
                // For simplicity, let's assign the triangle based on its centroid
                Vector3 centroid = (triangle.p[0] + triangle.p[1] + triangle.p[2]) / 3;
                float distanceCentroid = PointToPlaneDistance(centroid, partitioningPlane);
                if (distanceCentroid > 0)
                    frontList.Add(triangle);
                else
                    backList.Add(triangle);
            }
        }

        public List<triangle> GetTrianglesFrontToBack(Camera camera)
        {
            List<triangle> orderedTriangles = new List<triangle>();
            TraverseBSP(Root, camera.GetPosition(), orderedTriangles, camera);
            return orderedTriangles;
        }

        private void TraverseBSP(BSPNode node, Vector3 cameraPosition, List<triangle> orderedTriangles, Camera camera)
        {
            if (node == null)
            {
                return;
            }

            if (node.Triangles != null) // This is a leaf node
            {
                orderedTriangles.AddRange(node.Triangles);
                return;
            }

            // Determine which side of the partitioning plane the camera is on.
            float distance = PointToPlaneDistance(cameraPosition, node.PartitioningPlane);

            if (distance > 0) // camera is in front
            {
                TraverseBSP(node.Front, cameraPosition, orderedTriangles, camera);
                TraverseBSP(node.Back, cameraPosition, orderedTriangles, camera);
            }
            else // camera is behind or on the partitioning plane
            {
                TraverseBSP(node.Back, cameraPosition, orderedTriangles, camera);
                TraverseBSP(node.Front, cameraPosition, orderedTriangles, camera);
            }
        }

        private float PointToPlaneDistance(Vector3 point, triangle planeTriangle)
        {
            Vector3 normal = Vector3.Cross(planeTriangle.p[1] - planeTriangle.p[0], planeTriangle.p[2] - planeTriangle.p[0]);
            normal.Normalize();
            float d = -Vector3.Dot(normal, planeTriangle.p[0]);
            return Vector3.Dot(normal, point) + d;
        }
    }

    public class BSPNode
    {

        public triangle PartitioningPlane;
        public BSPNode Front;
        public BSPNode Back;
        public List<triangle> Triangles;
        public bool isLeaf;
    }
}
