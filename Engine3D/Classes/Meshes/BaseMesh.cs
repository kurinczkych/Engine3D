using FontStashSharp;
using MagicPhysX;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Formats.Asn1.AsnWriter;

namespace Engine3D
{

    public abstract class BaseMesh
    {
        public int vbo;
        public int vaoId;
        public int vboId;
        protected int shaderProgramId;

        public bool recalculate = false;

        public int useBillboarding = 0;

        public List<triangle> tris;
        public List<triangle> visibleTris;
        public List<Vector3> allVerts;
        public bool hasIndices = false;
        public Object parentObject;
        public AABB Bounds = new AABB();

        protected Dictionary<string, int> uniformLocations;

        public BVH BVHStruct { get; private set; }

        protected Matrix4 scaleMatrix = Matrix4.Identity;
        protected Matrix4 rotationMatrix = Matrix4.Identity;
        protected Matrix4 translationMatrix = Matrix4.Identity;
        protected Matrix4 modelMatrix = Matrix4.Identity;

        //protected int threadSize;
        //private int desiredPercentage = 80;
        public static int threadSize = 32;

        public BaseMesh(int vaoId, int vboId, int shaderProgramId)
        {
            tris = new List<triangle>();
            allVerts = new List<Vector3>();

            this.vaoId = vaoId;
            this.vboId = vboId;
            this.shaderProgramId = shaderProgramId;

            uniformLocations = new Dictionary<string, int>();

            //threadSize = (int)(Environment.ProcessorCount * (80 / 100.0));
            //threadSize = 16;
        }

        public void RecalculateModelMatrix(bool[] which)
        {
            if (which.Length != 3)
                throw new Exception("Which matrix bool[] must be a length of 3");

            if (which[2])
                scaleMatrix = Matrix4.CreateScale(parentObject.Scale);

            if (which[1])
                rotationMatrix = Matrix4.CreateFromQuaternion(parentObject.Rotation);

            if (which[0])
                translationMatrix = Matrix4.CreateTranslation(parentObject.Position);

            modelMatrix = scaleMatrix * rotationMatrix * translationMatrix;

            TransformBVH();
        }

        private void TransformBVH()
        {

        }

        public void AddTriangle(triangle tri)
        {
            tris.Add(tri);
        }
        protected abstract void SendUniforms();

        public void CalculateFrustumVisibility(Camera camera, BVH? bvh)
        {
            if (bvh != null)
            {
                ParallelOptions parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = threadSize }; // Adjust as needed
                Parallel.ForEach(bvh.leaves, parallelOptions, node =>
                {
                    bool v = camera.frustum.IsAABBInside(node.bounds) || node.bounds.IsPointInsideAABB(camera.GetPosition());
                    node.triangles.ForEach(x => x.visibile = v);
                });
            }
            else
            {
                ParallelOptions parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = threadSize }; // Adjust as needed
                Parallel.ForEach(tris, parallelOptions, tri =>
                {
                    tri.visibile = camera.frustum.IsTriangleInside(tri) || camera.IsTriangleClose(tri);
                });
            }
        }

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
                triangle.gotPointNormals = true;
                for (int i = 0; i < 3; i++)
                {
                    triangle.n[i] = Average(vertexToNormals[triangle.p[i]]).Normalized();
                }
            }
        }

        protected void ComputeTangents(ref List<triangle> tris)
        {
            // Initialize tangent and bitangent lists with zeros
            Dictionary<Vector3, List<Vector3>> tangentSums = new Dictionary<Vector3, List<Vector3>>();
            Dictionary<Vector3, List<Vector3>> bitangentSums = new Dictionary<Vector3, List<Vector3>>();

            foreach (var tri in tris)
            {
                // Get the vertices of the triangle
                Vector3 p0 = tri.p[0];
                Vector3 p1 = tri.p[1];
                Vector3 p2 = tri.p[2];

                // Get UVs of the triangle
                Vector2 uv0 = new Vector2(tri.t[0].u, tri.t[0].v);
                Vector2 uv1 = new Vector2(tri.t[1].u, tri.t[1].v);
                Vector2 uv2 = new Vector2(tri.t[2].u, tri.t[2].v);

                // Compute the edges of the triangle in both object space and texture space
                Vector3 edge1 = p1 - p0;
                Vector3 edge2 = p2 - p0;

                Vector2 deltaUV1 = uv1 - uv0;
                Vector2 deltaUV2 = uv2 - uv0;

                float f = 1.0f / (deltaUV1.X * deltaUV2.Y - deltaUV2.X * deltaUV1.Y);

                // Calculate tangent and bitangent
                Vector3 tangent = new Vector3(
                    f * (deltaUV2.Y * edge1.X - deltaUV1.Y * edge2.X),
                    f * (deltaUV2.Y * edge1.Y - deltaUV1.Y * edge2.Y),
                    f * (deltaUV2.Y * edge1.Z - deltaUV1.Y * edge2.Z)
                );

                Vector3 bitangent = new Vector3(
                    f * (-deltaUV2.X * edge1.X + deltaUV1.X * edge2.X),
                    f * (-deltaUV2.X * edge1.Y + deltaUV1.X * edge2.Y),
                    f * (-deltaUV2.X * edge1.Z + deltaUV1.X * edge2.Z)
                );

                // Accumulate the tangents and bitangents
                foreach (var vertex in tri.p)
                {
                    if (!tangentSums.ContainsKey(vertex))
                    {
                        tangentSums[vertex] = new List<Vector3>();
                        bitangentSums[vertex] = new List<Vector3>();
                    }

                    tangentSums[vertex].Add(tangent);
                    bitangentSums[vertex].Add(bitangent);
                }
            }

            // Average and normalize tangents and bitangents
            foreach (var tri in tris)
            {
                for (int i = 0; i < 3; i++)
                {
                    Vector3 vertex = tri.p[i];

                    Vector3 avgTangent = Average(tangentSums[vertex]).Normalized();
                    Vector3 avgBitangent = Average(bitangentSums[vertex]).Normalized();

                    tri.tan[i] = avgTangent;
                    tri.bitan[i] = avgBitangent;
                }
            }
        }

        public void ProcessObj(string filename)
        {
            tris = new List<triangle>();

            string result;
            int fPerCount = -1;
            List<Vector3> verts = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<Vec2d> uvs = new List<Vec2d>();

            using (Stream stream = FileManager.GetFileStream(filename, FileType.Models))
            using (StreamReader reader = new StreamReader(stream))
            {
                while (true)
                {
                    result = reader.ReadLine();
                    if (result != null && result.Length > 0)
                    {
                        if (result[0] == 'v')
                        {
                            if (result[1] == 't')
                            {
                                string[] vStr = result.Substring(3).Split(" ");
                                var a = float.Parse(vStr[0]);
                                var b = float.Parse(vStr[1]);
                                Vec2d v = new Vec2d(a, b);
                                uvs.Add(v);
                            }
                            else if (result[1] == 'n')
                            {
                                string[] vStr = result.Substring(3).Split(" ");
                                var a = float.Parse(vStr[0]);
                                var b = float.Parse(vStr[1]);
                                var c = float.Parse(vStr[2]);
                                Vector3 v = new Vector3(a, b, c);
                                normals.Add(v);
                            }
                            else
                            {
                                string[] vStr = result.Substring(2).Split(" ");
                                var a = float.Parse(vStr[0]);
                                var b = float.Parse(vStr[1]);
                                var c = float.Parse(vStr[2]);
                                Vector3 v = new Vector3(a, b, c);
                                verts.Add(v);
                                allVerts.Add(v);
                            }
                        }
                        else if (result[0] == 'f')
                        {
                            if (result.Contains("//"))
                            {

                            }
                            else if (result.Contains("/"))
                            {
                                string[] vStr = result.Substring(2).Split(" ");
                                if (vStr.Length > 3)
                                    throw new Exception();

                                if (fPerCount == -1)
                                    fPerCount = vStr[0].Count(x => x == '/');

                                if (fPerCount == 2)
                                {
                                    // 1/1/1, 2/2/2, 3/3/3
                                    int[] v = new int[3];
                                    int[] n = new int[3];
                                    int[] uv = new int[3];
                                    for (int i = 0; i < 3; i++)
                                    {
                                        string[] fStr = vStr[i].Split("/");
                                        v[i] = int.Parse(fStr[0]);
                                        uv[i] = int.Parse(fStr[1]);
                                        n[i] = int.Parse(fStr[2]);
                                    }

                                    tris.Add(new triangle(new Vector3[] { verts[v[0] - 1], verts[v[1] - 1], verts[v[2] - 1] },
                                                          new Vector3[] { normals[n[0] - 1], normals[n[1] - 1], normals[n[2] - 1] },
                                                          new Vec2d[] { uvs[uv[0] - 1], uvs[uv[1] - 1], uvs[uv[2] - 1] }));

                                    tris.Last().pi[0] = v[0] - 1;
                                    tris.Last().pi[1] = v[1] - 1;
                                    tris.Last().pi[2] = v[2] - 1;

                                    tris.Last().visibile = true;

                                    Bounds.Enclose(tris.Last());

                                    hasIndices = true;
                                }
                                else if (fPerCount == 1)
                                {
                                    // 1/1, 2/2, 3/3
                                    int[] v = new int[3];
                                    int[] uv = new int[3];
                                    for (int i = 0; i < 3; i++)
                                    {
                                        string[] fStr = vStr[i].Split("/");
                                        v[i] = int.Parse(fStr[0]);
                                        uv[i] = int.Parse(fStr[1]);
                                    }

                                    tris.Add(new triangle(new Vector3[] { verts[v[0] - 1], verts[v[1] - 1], verts[v[2] - 1] },
                                                          new Vec2d[] { uvs[uv[0] - 1], uvs[uv[1] - 1], uvs[uv[2] - 1] }));

                                    tris.Last().pi[0] = v[0] - 1;
                                    tris.Last().pi[1] = v[1] - 1;
                                    tris.Last().pi[2] = v[2] - 1;

                                    tris.Last().visibile = true;

                                    Bounds.Enclose(tris.Last());

                                    hasIndices = true;
                                }

                            }
                            else
                            {
                                string[] vStr = result.Substring(2).Split(" ");
                                int[] f = { int.Parse(vStr[0]), int.Parse(vStr[1]), int.Parse(vStr[2]) };

                                tris.Add(new triangle(new Vector3[] { verts[f[0] - 1], verts[f[1] - 1], verts[f[2] - 1] }));

                                tris.Last().pi[0] = f[0] - 1;
                                tris.Last().pi[1] = f[1] - 1;
                                tris.Last().pi[2] = f[2] - 1;

                                tris.Last().visibile = true;

                                Bounds.Enclose(tris.Last());

                                hasIndices = true;
                            }
                        }
                    }

                    if (result == null)
                        break;
                }
            }
        }

    }
}
