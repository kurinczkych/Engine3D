using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mario64
{

    public abstract class BaseMesh
    {
        public int vbo;
        public int vaoId;
        public int vboId;
        protected int shaderProgramId;

        public List<triangle> tris;
        public bool hasIndices = false;
        public Object parentObject;

        public BaseMesh(int vaoId, int vboId, int shaderProgramId)
        {
            tris = new List<triangle>();

            this.vaoId = vaoId;
            this.vboId = vboId;
            this.shaderProgramId = shaderProgramId;

        }
        public void AddTriangle(triangle tri)
        {
            tris.Add(tri);
        }
        protected abstract void SendUniforms();

        protected static Vector3 ComputeFaceNormal(triangle triangle)
        {
            var edge1 = triangle.p[1] - triangle.p[0];
            var edge2 = triangle.p[2] - triangle.p[0];
            return Vector3.Cross(edge1, edge2).Normalized();
        }

        protected static Vector3 Average(List<Vector3> vectors)
        {
            Vector3 sum = Vector3.Zero;
            foreach (var vec in vectors)
            {
                sum += vec;
            }
            return sum / vectors.Count;
        }

        // Since Vector3 doesn't have a default equality comparer for dictionaries, we define one:
        protected class Vector3Comparer : IEqualityComparer<Vector3>
        {
            public bool Equals(Vector3 x, Vector3 y)
            {
                return x == y; // Use OpenTK's built-in equality check for Vector3
            }

            public int GetHashCode(Vector3 obj)
            {
                return obj.GetHashCode();
            }
        }

        public static void ComputeVertexNormals(ref List<triangle> triangles)
        {
            Dictionary<Vector3, List<Vector3>> vertexToNormals = new Dictionary<Vector3, List<Vector3>>(new Vector3Comparer());

            // Initialize mapping
            foreach (var triangle in triangles)
            {
                for (int i = 0; i < 3; i++)
                {
                    if (!vertexToNormals.ContainsKey(triangle.p[i]))
                        vertexToNormals[triangle.p[i]] = new List<Vector3>();
                }
            }

            // Accumulate face normals to the vertices
            foreach (var triangle in triangles)
            {
                var faceNormal = ComputeFaceNormal(triangle);
                for (int i = 0; i < 3; i++)
                {
                    vertexToNormals[triangle.p[i]].Add(faceNormal);
                }
            }

            // Compute the average normal for each vertex
            foreach (var triangle in triangles)
            {
                for (int i = 0; i < 3; i++)
                {
                    triangle.n[i] = Average(vertexToNormals[triangle.p[i]]).Normalized();
                }
            }
        }

    }
}
