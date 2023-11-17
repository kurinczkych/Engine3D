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
    public class LocalVertexCollections
    {
        public List<float> LocalVertices1 { get; set; } = new List<float>();
        public List<float> LocalVertices2 { get; set; } = new List<float>();
    }

    public abstract class BaseMesh
    {
        public int vbo;
        public int vaoId;
        public int vboId;
        public int shaderProgramId;

        public string modelName;
        public string modelPath;

        private bool _recalculate = false;
        public bool recalculate
        {
            get { return _recalculate; }
            set { _recalculate = value; recalculateOnlyPos = true; recalculateOnlyPosAndNormal = true; }
        }
        protected bool recalculateOnlyPos;
        protected bool recalculateOnlyPosAndNormal;

        public int useBillboarding = 0;

        protected List<Vertex> uniqueVertices = new List<Vertex>();
        protected List<uint> indices = new List<uint>();

        protected List<Vertex> visibleVertices = new List<Vertex>();
        protected List<float> visibleVerticesData = new List<float>();
        protected List<uint> visibleIndices = new List<uint>();
        protected uint maxVisibleIndex = 0;

        public List<triangle> tris = new List<triangle>();
        public List<Vector3> allVerts;
        public bool hasIndices = false;
        public Object parentObject;
        public AABB Bounds = new AABB();

        public bool useShading = true;

        protected Camera camera;

        public Dictionary<string, int> uniformLocations;

        public BVH? BVHStruct;

        protected Matrix4 scaleMatrix = Matrix4.Identity;
        protected Matrix4 rotationMatrix = Matrix4.Identity;
        protected Matrix4 translationMatrix = Matrix4.Identity;
        public Matrix4 modelMatrix = Matrix4.Identity;

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

        public void AddUniformLocation(string name)
        {
            if(!uniformLocations.ContainsKey(name) )
                uniformLocations.Add(name, GL.GetUniformLocation(shaderProgramId, name));
        }

        public void RemoveTexture(string name1, string name2)
        {
            GL.Uniform1(uniformLocations[name1], 0);
            GL.Uniform1(uniformLocations[name2], 0);
        }

        public void RecalculateModelMatrix(bool[] which, bool onlyModelMatrix=false)
        {
            if (!onlyModelMatrix)
            {

                if (which.Length != 3)
                    throw new Exception("Which matrix bool[] must be a length of 3");

                if (which[2])
                    scaleMatrix = Matrix4.CreateScale(parentObject.Scale);

                if (which[1])
                    rotationMatrix = Matrix4.CreateFromQuaternion(parentObject.Rotation);

                if (which[0])
                    translationMatrix = Matrix4.CreateTranslation(parentObject.Position);

                if (which[0] || which[1] || which[2])
                {
                    modelMatrix = scaleMatrix * rotationMatrix * translationMatrix;

                    if (BVHStruct != null &&
                       (GetType() == typeof(Mesh) ||
                        GetType() == typeof(InstancedMesh)))
                    {
                        BVHStruct.TransformBVH(ref modelMatrix);
                    }

                    CalculateFrustumVisibility();
                }
            }
            else
            {
                modelMatrix = scaleMatrix * rotationMatrix * translationMatrix;

                if (BVHStruct != null &&
                   (GetType() == typeof(Mesh) ||
                    GetType() == typeof(InstancedMesh)))
                {
                    BVHStruct.TransformBVH(ref modelMatrix);
                }

                CalculateFrustumVisibility();
            }
        }

        public void Delete()
        {
            tris.Clear();
            if (allVerts != null)
                allVerts.Clear();
            if (uniformLocations != null)
                uniformLocations.Clear();
        }

        protected abstract void SendUniforms();

        public void CalculateFrustumVisibility()
        {
            if (GetType() == typeof(Mesh) ||
                GetType() == typeof(InstancedMesh))
            {
                if (BVHStruct != null)
                {
                    BVHStruct.CalculateFrustumVisibility(ref camera);
                }
                else
                {
                    visibleIndices.Clear();

                    //foreach (triangle tri in tris)
                    //{
                    //    //visibleIndices.Add(tri.vi[0]);
                    //    //visibleIndices.Add(tri.vi[1]);
                    //    //visibleIndices.Add(tri.vi[2]);
                    //    if (modelMatrix != Matrix4.Identity)
                    //    {
                    //        bool visible = false;
                    //        for (int i = 0; i < 3; i++)
                    //        {
                    //            Vector3 p = Vector3.TransformPosition(tri.v[i].p, modelMatrix);
                    //            if (camera.frustum.IsInside(p) || camera.IsPointClose(p))
                    //            {
                    //                visible = true;
                    //                break;
                    //            }
                    //        }
                    //        tri.visibile = visible;

                    //        if (tri.visibile)
                    //        {
                    //            visibleIndices.Add(tri.vi[0]);
                    //            visibleIndices.Add(tri.vi[1]);
                    //            visibleIndices.Add(tri.vi[2]);
                    //        }
                    //    }
                    //    else
                    //    {
                    //        tri.visibile = camera.frustum.IsTriangleInside(tri) || camera.IsTriangleClose(tri);

                    //        if (tri.visibile)
                    //        {
                    //            visibleIndices.Add(tri.vi[0]);
                    //            visibleIndices.Add(tri.vi[1]);
                    //            visibleIndices.Add(tri.vi[2]);
                    //        }
                    //    }
                    //}

                    ParallelOptions parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = threadSize };
                    Parallel.ForEach(tris, parallelOptions,
                    () => new List<uint>(),
                    (tri, loopState, localIndices) =>
                    {
                        if (modelMatrix != Matrix4.Identity)
                        {
                            bool visible = false;
                            for (int i = 0; i < 3; i++)
                            {
                                Vector3 p = Vector3.TransformPosition(tri.v[i].p, modelMatrix);
                                if (camera.frustum.IsInside(p) || camera.IsPointClose(p))
                                {
                                    visible = true;
                                    break;
                                }
                            }
                            tri.visibile = visible;

                            if (tri.visibile)
                            {
                                localIndices.Add(tri.vi[0]);
                                localIndices.Add(tri.vi[1]);
                                localIndices.Add(tri.vi[2]);
                                if (tri.vi[0] > maxVisibleIndex)
                                    maxVisibleIndex = tri.vi[0];
                                if (tri.vi[1] > maxVisibleIndex)
                                    maxVisibleIndex = tri.vi[1];
                                if (tri.vi[2] > maxVisibleIndex)
                                    maxVisibleIndex = tri.vi[2];
                            }
                        }
                        else
                        {
                            tri.visibile = camera.frustum.IsTriangleInside(tri) || camera.IsTriangleClose(tri);

                            if (tri.visibile)
                            {
                                localIndices.Add(tri.vi[0]);
                                localIndices.Add(tri.vi[1]);
                                localIndices.Add(tri.vi[2]);
                                if (tri.vi[0] > maxVisibleIndex)
                                    maxVisibleIndex = tri.vi[0];
                                if (tri.vi[1] > maxVisibleIndex)
                                    maxVisibleIndex = tri.vi[1];
                                if (tri.vi[2] > maxVisibleIndex)
                                    maxVisibleIndex = tri.vi[2];
                            }
                        }
                        return localIndices;
                    },
                    localIndices =>
                    {
                        lock (visibleIndices)
                        {
                            visibleIndices.AddRange(localIndices);
                        }
                    });
                }
            }
        }

        protected static Vector3 ComputeFaceNormal(triangle tri)
        {
            Vector3 edge1 = tri.v[1].p - tri.v[0].p;
            Vector3 edge2 = tri.v[2].p - tri.v[0].p;
            Vector3 normal = Vector3.Cross(edge1, edge2);
            return normal;
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

        public void ComputeVertexNormalsSpherical()
        {
            visibleVerticesData.Clear();

            for (int i = 0; i < tris.Count(); i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    tris[i].v[j].n = tris[i].v[j].p.Normalized();
                    var v = uniqueVertices[(int)tris[i].vi[j]];
                    v.n = tris[i].v[j].n;
                    uniqueVertices[(int)tris[i].vi[j]] = v;
                }
            }

            foreach (Vertex v in uniqueVertices)
            {
                visibleVerticesData.AddRange(v.GetData());
            }
        }

        public void ComputeVertexNormals()
        {
            visibleVerticesData.Clear();

            Dictionary<Vector3, Vector3> vertexNormals = new Dictionary<Vector3, Vector3>();
            Dictionary<Vector3, int> vertexNormalsCounts = new Dictionary<Vector3, int>();


            foreach (var tri in tris)
            {
                Vector3 faceNormal = ComputeFaceNormal(tri);
                for (int i = 0; i < 3; i++)
                {
                    if (!vertexNormals.ContainsKey(tri.v[i].p))
                    {
                        vertexNormals[tri.v[i].p] = faceNormal;
                        vertexNormalsCounts[tri.v[i].p] = 1;
                    }
                    else
                    {
                        vertexNormals[tri.v[i].p] += faceNormal;
                        vertexNormalsCounts[tri.v[i].p]++;
                    }
                }
            }

            //{(0.57735026, 0.57735026, -0.57735026)}
            //{(0.40824828, 0.40824828, -0.81649655)}
            foreach (var tri in tris)
            {
                for (int i = 0; i < 3; i++)
                {
                    tri.v[i].n = (vertexNormals[tri.v[i].p] / vertexNormalsCounts[tri.v[i].p]).Normalized();
                    var v = uniqueVertices[(int)tri.vi[i]];
                    v.n = tri.v[i].n;
                    uniqueVertices[(int)tri.vi[i]] = v;
                }
            }

            foreach (Vertex v in uniqueVertices)
            {
                visibleVerticesData.AddRange(v.GetData());
            }
        }

        public void ComputeTangents()
        {
            visibleVerticesData.Clear();

            // Initialize tangent and bitangent lists with zeros
            Dictionary<Vector3, List<Vector3>> tangentSums = new Dictionary<Vector3, List<Vector3>>();
            Dictionary<Vector3, List<Vector3>> bitangentSums = new Dictionary<Vector3, List<Vector3>>();

            foreach (var tri in tris)
            {
                // Get the vertices of the triangle
                Vector3 p0 = tri.v[0].p;
                Vector3 p1 = tri.v[1].p;
                Vector3 p2 = tri.v[2].p;

                // Get UVs of the triangle
                Vector2 uv0 = new Vector2(tri.v[0].t.u, tri.v[0].t.v);
                Vector2 uv1 = new Vector2(tri.v[1].t.u, tri.v[1].t.v);
                Vector2 uv2 = new Vector2(tri.v[2].t.u, tri.v[2].t.v);

                // Compute the edges of the triangle in both object space and texture space
                Vector3 edge1 = p1 - p0;
                Vector3 edge2 = p2 - p0;

                Vector2 deltaUV1 = uv1 - uv0;
                Vector2 deltaUV2 = uv2 - uv0;

                float d = deltaUV1.X * deltaUV2.Y - deltaUV2.X * deltaUV1.Y;
                float f = 1.0f;
                if (d != 0)
                    f /= d;
                else
                    f = 0;

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
                foreach (var vertex in tri.v)
                {
                    if (!tangentSums.ContainsKey(vertex.p))
                    {
                        tangentSums[vertex.p] = new List<Vector3>();
                        bitangentSums[vertex.p] = new List<Vector3>();
                    }

                    tangentSums[vertex.p].Add(tangent);
                    bitangentSums[vertex.p].Add(bitangent);
                }
            }

            // Average and normalize tangents and bitangents
            foreach (var tri in tris)
            {
                for (int i = 0; i < 3; i++)
                {
                    Vector3 vertex = tri.v[i].p;

                    Vector3 avgTangent = Average(tangentSums[vertex]).Normalized();
                    if (float.IsNaN(avgTangent.X))
                        avgTangent = Vector3.Zero;
                    Vector3 avgBitangent = Average(bitangentSums[vertex]).Normalized();
                    if (float.IsNaN(avgBitangent.X))
                        avgBitangent = Vector3.Zero;

                    tri.v[i].tan = avgTangent;
                    tri.v[i].bitan = avgBitangent;

                    var v = uniqueVertices[(int)tri.vi[i]];
                    v.tan = avgTangent;
                    v.bitan = avgBitangent;
                    uniqueVertices[(int)tri.vi[i]] = v;
                }
            }

            foreach(Vertex v in uniqueVertices)
            {
                visibleVerticesData.AddRange(v.GetData());
            }
        }

        public void ProcessObj(string relativeModelPath, float cr=1, float cg=1, float cb=1, float ca=1)
        {
            tris = new List<triangle>();
            Color4 color = new Color4(cr, cg, cb, ca);

            string result;
            int fPerCount = -1;
            List<Vector3> verts = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<Vec2d> uvs = new List<Vec2d>();

            Dictionary<int, uint> vertexHash = new Dictionary<int, uint>();

            string filePath = Environment.CurrentDirectory + "\\Assets\\" + FileType.Models.ToString() + "\\" + relativeModelPath;
            if(!File.Exists(filePath))
            {
                Engine.consoleManager.AddLog("File '" + filePath + "' not found!", LogType.Warning);
                return;
            }

            using (Stream stream = FileManager.GetFileStream(filePath))
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

                                    tris.Last().v[0].pi = v[0] - 1;
                                    tris.Last().v[1].pi = v[1] - 1;
                                    tris.Last().v[2].pi = v[2] - 1;

                                    tris.Last().v[0].c = color;
                                    tris.Last().v[1].c = color;
                                    tris.Last().v[2].c = color;

                                    tris.Last().visibile = true;

                                    Vertex v1 = new Vertex(verts[v[0] - 1], normals[n[0] - 1], uvs[uv[0] - 1]) { pi = v[0] - 1, c = color };
                                    Vertex v2 = new Vertex(verts[v[1] - 1], normals[n[1] - 1], uvs[uv[1] - 1]) { pi = v[1] - 1, c = color };
                                    Vertex v3 = new Vertex(verts[v[2] - 1], normals[n[2] - 1], uvs[uv[2] - 1]) { pi = v[2] - 1, c = color };
                                    int v1h = v1.GetHashCode();
                                    if (!vertexHash.ContainsKey(v1h))
                                    {
                                        uniqueVertices.Add(v1);
                                        visibleVerticesData.AddRange(v1.GetData());
                                        indices.Add((uint)uniqueVertices.Count - 1);
                                        vertexHash.Add(v1h, (uint)uniqueVertices.Count - 1);
                                        tris.Last().vi[0] = (uint)uniqueVertices.Count - 1;
                                    }
                                    else
                                    {
                                        indices.Add(vertexHash[v1h]);
                                        tris.Last().vi[0] = vertexHash[v1h];
                                    }
                                    int v2h = v2.GetHashCode();
                                    if (!vertexHash.ContainsKey(v2h))
                                    {
                                        uniqueVertices.Add(v2);
                                        visibleVerticesData.AddRange(v2.GetData());
                                        indices.Add((uint)uniqueVertices.Count - 1);
                                        vertexHash.Add(v2h, (uint)uniqueVertices.Count - 1);
                                        tris.Last().vi[1] = (uint)uniqueVertices.Count - 1;
                                    }
                                    else
                                    {
                                        indices.Add(vertexHash[v2h]);
                                        tris.Last().vi[1] = vertexHash[v2h];
                                    }
                                    int v3h = v3.GetHashCode();
                                    if (!vertexHash.ContainsKey(v3h))
                                    {
                                        uniqueVertices.Add(v3);
                                        visibleVerticesData.AddRange(v3.GetData());
                                        indices.Add((uint)uniqueVertices.Count - 1);
                                        vertexHash.Add(v3h, (uint)uniqueVertices.Count - 1);
                                        tris.Last().vi[2] = (uint)uniqueVertices.Count - 1;
                                    }
                                    else
                                    {
                                        indices.Add(vertexHash[v3h]);
                                        tris.Last().vi[2] = vertexHash[v3h];
                                    }

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

                                    tris.Last().v[0].pi = v[0] - 1;
                                    tris.Last().v[1].pi = v[1] - 1;
                                    tris.Last().v[2].pi = v[2] - 1;

                                    tris.Last().v[0].c = color;
                                    tris.Last().v[1].c = color;
                                    tris.Last().v[2].c = color;

                                    tris.Last().visibile = true;

                                    Vertex v1 = new Vertex(verts[v[0] - 1], uvs[uv[0] - 1]) { pi = v[0] - 1, c = color };
                                    Vertex v2 = new Vertex(verts[v[1] - 1], uvs[uv[1] - 1]) { pi = v[1] - 1, c = color };
                                    Vertex v3 = new Vertex(verts[v[2] - 1], uvs[uv[2] - 1]) { pi = v[2] - 1, c = color };
                                    int v1h = v1.GetHashCode();
                                    if (!vertexHash.ContainsKey(v1h))
                                    {
                                        uniqueVertices.Add(v1);
                                        visibleVerticesData.AddRange(v1.GetData());
                                        indices.Add((uint)uniqueVertices.Count - 1);
                                        vertexHash.Add(v1h, (uint)uniqueVertices.Count - 1);
                                        tris.Last().vi[0] = (uint)uniqueVertices.Count - 1;
                                    }
                                    else
                                    {
                                        indices.Add(vertexHash[v1h]);
                                        tris.Last().vi[0] = vertexHash[v1h];
                                    }
                                    int v2h = v2.GetHashCode();
                                    if (!vertexHash.ContainsKey(v2h))
                                    {
                                        uniqueVertices.Add(v2);
                                        visibleVerticesData.AddRange(v2.GetData());
                                        indices.Add((uint)uniqueVertices.Count - 1);
                                        vertexHash.Add(v2h, (uint)uniqueVertices.Count - 1);
                                        tris.Last().vi[1] = (uint)uniqueVertices.Count - 1;
                                    }
                                    else
                                    {
                                        indices.Add(vertexHash[v2h]);
                                        tris.Last().vi[1] = vertexHash[v2h];
                                    }
                                    int v3h = v3.GetHashCode();
                                    if (!vertexHash.ContainsKey(v3h))
                                    {
                                        uniqueVertices.Add(v3);
                                        visibleVerticesData.AddRange(v3.GetData());
                                        indices.Add((uint)uniqueVertices.Count - 1);
                                        vertexHash.Add(v3h, (uint)uniqueVertices.Count - 1);
                                        tris.Last().vi[2] = (uint)uniqueVertices.Count - 1;
                                    }
                                    else
                                    {
                                        indices.Add(vertexHash[v3h]);
                                        tris.Last().vi[2] = vertexHash[v3h];
                                    }

                                    Bounds.Enclose(tris.Last());

                                    hasIndices = true;
                                }

                            }
                            else
                            {
                                string[] vStr = result.Substring(2).Split(" ");
                                int[] v = { int.Parse(vStr[0]), int.Parse(vStr[1]), int.Parse(vStr[2]) };

                                tris.Add(new triangle(new Vector3[] { verts[v[0] - 1], verts[v[1] - 1], verts[v[2] - 1] }));

                                tris.Last().v[0].pi = v[0] - 1;
                                tris.Last().v[1].pi = v[1] - 1;
                                tris.Last().v[2].pi = v[2] - 1;

                                tris.Last().v[0].c = color;
                                tris.Last().v[1].c = color;
                                tris.Last().v[2].c = color;

                                tris.Last().visibile = true;

                                Vertex v1 = new Vertex(verts[v[0] - 1]) { pi = v[0] - 1, c = color };
                                Vertex v2 = new Vertex(verts[v[1] - 1]) { pi = v[1] - 1, c = color };
                                Vertex v3 = new Vertex(verts[v[2] - 1]) { pi = v[2] - 1, c = color };
                                int v1h = v1.GetHashCode();
                                if (!vertexHash.ContainsKey(v1h))
                                {
                                    uniqueVertices.Add(v1);
                                    visibleVerticesData.AddRange(v1.GetData());
                                    indices.Add((uint)uniqueVertices.Count - 1);
                                    vertexHash.Add(v1h, (uint)uniqueVertices.Count - 1);
                                    tris.Last().vi[0] = (uint)uniqueVertices.Count - 1;
                                }
                                else
                                {
                                    indices.Add(vertexHash[v1h]);
                                    tris.Last().vi[0] = vertexHash[v1h];
                                }
                                int v2h = v2.GetHashCode();
                                if (!vertexHash.ContainsKey(v2h))
                                {
                                    uniqueVertices.Add(v2);
                                    visibleVerticesData.AddRange(v2.GetData());
                                    indices.Add((uint)uniqueVertices.Count - 1);
                                    vertexHash.Add(v2h, (uint)uniqueVertices.Count - 1);
                                    tris.Last().vi[1] = (uint)uniqueVertices.Count - 1;
                                }
                                else
                                {
                                    indices.Add(vertexHash[v2h]);
                                    tris.Last().vi[1] = vertexHash[v2h];
                                }
                                int v3h = v3.GetHashCode();
                                if (!vertexHash.ContainsKey(v3h))
                                {
                                    uniqueVertices.Add(v3);
                                    visibleVerticesData.AddRange(v3.GetData());
                                    indices.Add((uint)uniqueVertices.Count - 1);
                                    vertexHash.Add(v3h, (uint)uniqueVertices.Count - 1);
                                    tris.Last().vi[2] = (uint)uniqueVertices.Count - 1;
                                }
                                else
                                {
                                    indices.Add(vertexHash[v3h]);
                                    tris.Last().vi[2] = vertexHash[v3h];
                                }

                                Bounds.Enclose(tris.Last());

                                hasIndices = true;
                            }
                        }
                    }

                    if (result == null)
                        break;
                }
            }

            visibleIndices = new List<uint>(indices);
        }

    }
}
