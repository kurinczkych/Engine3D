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
        public BSPNode Root { get; private set; }

        public BSP(List<triangle> triangles)
        {
            Root = new BSPNode(triangles);
        }

        public List<triangle> GetTrianglesFrontToBack(Vector3 cameraPosition)
        {
            return GetTrianglesFrontToBackRecursive(Root, cameraPosition);
        }

        private List<triangle> GetTrianglesFrontToBackRecursive(BSPNode node, Vector3 cameraPosition)
        {
            List<triangle> result = new List<triangle>();

            if (node == null) return result;

            var distance = node.Splitter.GetPlane().Distance(cameraPosition);

            // Camera is in front of the splitter triangle
            if (distance > 0)
            {
                result.AddRange(GetTrianglesFrontToBackRecursive(node.Front, cameraPosition));
                result.AddRange(node.CoplanarTriangles);
                result.AddRange(GetTrianglesFrontToBackRecursive(node.Back, cameraPosition));
            }
            // Camera is behind the splitter triangle
            else
            {
                result.AddRange(GetTrianglesFrontToBackRecursive(node.Back, cameraPosition));
                result.AddRange(node.CoplanarTriangles);
                result.AddRange(GetTrianglesFrontToBackRecursive(node.Front, cameraPosition));
            }

            return result;
        }
    }

    public class BSPNode
    { 

        public triangle Splitter { get; private set; }
        public List<triangle> CoplanarTriangles { get; private set; } = new List<triangle>();
        public BSPNode Front { get; private set; }
        public BSPNode Back { get; private set; }

        public BSPNode(List<triangle> triangles)
        {
            if (triangles == null || triangles.Count == 0) return;

            // Here we just take the first triangle as the splitter for simplicity
            Splitter = triangles[0];
            Plane splitterPlane = Splitter.GetPlane();

            var frontList = new List<triangle>();
            var backList = new List<triangle>();

            for (int i = 1; i < triangles.Count; i++)  // Start from 1 because 0 is the splitter
            {
                var triangle = triangles[i];
                bool isFront = false, isBack = false;

                foreach (var point in triangle.p)
                {
                    var distance = splitterPlane.Distance(point);
                    if (distance > 0.0001f) isFront = true;   // A tiny epsilon to avoid precision issues
                    if (distance < -0.0001f) isBack = true;
                }

                if (isFront && isBack)
                {
                    // Handle splitting the triangle if needed
                    // Here we just add to both lists for simplicity, but in a full implementation you'd split the triangle
                    frontList.Add(triangle);
                    backList.Add(triangle);
                }
                else if (isFront)
                {
                    frontList.Add(triangle);
                }
                else if (isBack)
                {
                    backList.Add(triangle);
                }
                else
                {
                    CoplanarTriangles.Add(triangle);
                }
            }

            if (frontList.Any()) Front = new BSPNode(frontList);
            if (backList.Any()) Back = new BSPNode(backList);
        }
    }
}
