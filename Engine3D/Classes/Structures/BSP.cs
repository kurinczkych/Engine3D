using MagicPhysX;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Engine3D
{
    public class BSP
    {
        public BSPNode? Root;
        public AABB Bounds;
        public int triangleCount = 0;

        public BSP(List<triangle> triangles)
        {
            Bounds = new AABB();
            triangleCount = triangles.Count;
            Root = BuildNode(triangles);
        }

        private BSPNode? BuildNode(List<triangle> triangles)
        {
            if (triangles.Count == 0)
                return null;

            // Choose median polygon as splitter
            triangle splitter = triangles[triangles.Count / 2];
            Plane splitterPlane = new Plane(splitter);

            List<triangle> frontList = new List<triangle>();
            List<triangle> backList = new List<triangle>();

            BSPNode node = new BSPNode();
            node.SplitterPlane = splitterPlane;
            node.Bounds = new AABB();
            node.Triangles.Add(splitter);

            foreach (var tri in triangles)
            {
                Bounds.Enclose(tri);

                if (tri == splitter)
                    continue;

                Vector3 center = tri.GetCenter();
                switch (splitterPlane.ClassifyPoint(center))
                {
                    case TrianglePosition.InFront:
                        frontList.Add(tri);
                        break;
                    case TrianglePosition.Behind:
                        backList.Add(tri);
                        break;
                    case TrianglePosition.OnPlane:
                        node.Triangles.Add(tri);
                        node.Bounds.Enclose(tri);
                        break;
                }
            }

            node.Front = BuildNode(frontList);
            node.Back = BuildNode(backList);

            return node;
        }

        //private BSPNode BuildNode(List<triangle> triangles)
        //{
        //    if (triangles.Count == 0)
        //        return null;

        //    BSPNode node = new BSPNode();
        //    node.Bounds = new AABB();
        //    triangle splitter = triangles[0];
        //    node.Triangles.Add(splitter);
        //    node.Bounds.Enclose(splitter);
        //    Plane splitterPlane = new Plane(splitter);
        //    node.SplitterPlane = splitterPlane;

        //    List<triangle> frontList = new List<triangle>();
        //    List<triangle> backList = new List<triangle>();

        //    for (int i = 1; i < triangles.Count; i++)
        //    {
        //        triangle tri = triangles[i];
        //        Vector3 center = tri.GetCenter();

        //        switch (splitterPlane.ClassifyPoint(center))
        //        {
        //            case TrianglePosition.InFront:
        //                frontList.Add(tri);
        //                break;
        //            case TrianglePosition.Behind:
        //                backList.Add(tri);
        //                break;
        //            case TrianglePosition.OnPlane:
        //                node.Triangles.Add(tri);
        //                node.Bounds.Enclose(tri);
        //                break;
        //        }
        //    }

        //    node.Front = BuildNode(frontList);
        //    node.Back = BuildNode(backList);

        //    return node;
        //}

        public List<triangle> GetTrianglesFrontToBack(Camera camera)
        {
            List<triangle> orderedTriangles = new List<triangle>();
            Vector3 front = camera.front.Normalized(); // Ensure it's a unit vector.
            Vector3 pointOfInterest = camera.GetPosition() + front; // Move a unit distance along view direction.

            int index = 0;

            if(Root != null)
                TraverseFrontToBack(Root, pointOfInterest, orderedTriangles, ref index);

            return orderedTriangles;
        }

        public List<BSPNode> GetNodesFrontToBack(Camera camera)
        {
            List<BSPNode> orderedNodes = new List<BSPNode>();
            Vector3 front = camera.front.Normalized(); // Ensure it's a unit vector.
            Vector3 pointOfInterest = camera.GetPosition() + front; // Move a unit distance along view direction.
            if (Root != null)
                TraverseNodesFrontToBack(Root, pointOfInterest, orderedNodes);

            return orderedNodes;
        }

        private void TraverseFrontToBack(BSPNode node, Vector3 pointOfInterest, List<triangle> result, ref int index)
        {
            if (node == null) return;

            TrianglePosition pointPosRelativeToPlane = node.SplitterPlane.ClassifyPoint(pointOfInterest);

            if (pointPosRelativeToPlane == TrianglePosition.InFront)
            {
                // If camera is in front of the plane, process the front side first
                if (node.Front != null)
                    TraverseFrontToBack(node.Front, pointOfInterest, result, ref index);

                List<triangle> tris = new List<triangle>(node.Triangles);
                foreach (triangle tri in tris)
                {
                    tri.SetColor(Helper.CalcualteColorBasedOnDistance(index, triangleCount));
                    index++;
                }
                result.AddRange(tris);  // Add triangles on the plane

                if (node.Back != null)
                    TraverseFrontToBack(node.Back, pointOfInterest, result, ref index);
            }
            else // includes TrianglePosition.Behind and TrianglePosition.OnPlane cases
            {
                // If camera is behind or on the plane, process the back side first
                if (node.Back != null)
                    TraverseFrontToBack(node.Back, pointOfInterest, result, ref index);

                List<triangle> tris = new List<triangle>(node.Triangles);
                foreach (triangle tri in tris)
                {
                    tri.SetColor(Helper.CalcualteColorBasedOnDistance(index, triangleCount));
                    index++;
                }
                result.AddRange(tris);  // Add triangles on the plane

                if (node.Front != null)
                    TraverseFrontToBack(node.Front, pointOfInterest, result, ref index);
            }
        }

        private void TraverseNodesFrontToBack(BSPNode node, Vector3 cameraPosition, List<BSPNode> result)
        {
            if (node == null) return;

            TrianglePosition cameraPosRelativeToPlane = node.SplitterPlane.ClassifyPoint(cameraPosition);

            if (cameraPosRelativeToPlane == TrianglePosition.InFront)
            {
                // If camera is in front of the plane, process the front side first
                if(node.Front != null)
                    TraverseNodesFrontToBack(node.Front, cameraPosition, result);
                result.Add(node);  // Add the current node after processing front and before processing back
                if (node.Back != null)
                    TraverseNodesFrontToBack(node.Back, cameraPosition, result);
            }
            else // includes TrianglePosition.Behind and TrianglePosition.OnPlane cases
            {
                // If camera is behind or on the plane, process the back side first
                if (node.Back != null)
                    TraverseNodesFrontToBack(node.Back, cameraPosition, result);
                result.Add(node);  // Add the current node after processing back and before processing front
                if (node.Front != null)
                    TraverseNodesFrontToBack(node.Front, cameraPosition, result);
            }
        }
    }

    public class BSPNode
    {
        public List<triangle> Triangles { get; } = new List<triangle>();
        public BSPNode? Front { get; set; }
        public BSPNode? Back { get; set; }
        public Plane SplitterPlane { get; set; }
        public AABB Bounds { get; set; }
    }
}
