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
    public class MeshData
    {
        public List<Vertex> vertices = new List<Vertex>();
        public List<uint> indices = new List<uint>();
        public List<float> visibleVerticesData = new List<float>();
        public AABB bounds = new AABB();

        public MeshData() { }

        public MeshData(List<Vertex> vertices, List<uint> indices, List<float> visibleVerticesData, AABB bounds)
        {
            this.vertices = vertices ?? new List<Vertex>();
            this.indices = indices ?? new List<uint>();
            this.visibleVerticesData = visibleVerticesData ?? new List<float>();
            this.bounds = bounds ?? new AABB();
        }
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

        public List<Vertex> uniqueVertices = new List<Vertex>();
        public List<uint> indices = new List<uint>();
        protected List<List<uint>> groupedIndices = new List<List<uint>>();

        protected List<Vertex> visibleVertices = new List<Vertex>();
        protected List<float> visibleVerticesData = new List<float>();
        protected List<uint> visibleIndices = new List<uint>();
        protected uint maxVisibleIndex = 0;

        protected List<float> visibleVerticesDataOnlyPos = new List<float>();
        protected List<float> visibleVerticesDataOnlyPosAndNormal = new List<float>();

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

                    #region not used foreach frustum calc
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
                    #endregion

                    ParallelOptions parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = threadSize };
                    Parallel.ForEach(groupedIndices, parallelOptions,
                    () => new List<uint>(),
                    (indices, loopState, localIndices) =>
                    {
                        if (modelMatrix != Matrix4.Identity)
                        {
                            bool visible = false;
                            for (int i = 0; i < 3; i++)
                            {
                                Vector3 p = Vector3.TransformPosition(uniqueVertices[(int)indices[i]].p, modelMatrix);
                                if (camera.frustum.IsInside(p) || camera.IsPointClose(p))
                                {
                                    visible = true;
                                    break;
                                }
                            }

                            if (visible)
                            {
                                localIndices.Add(indices[0]);
                                localIndices.Add(indices[1]);
                                localIndices.Add(indices[2]);
                                if (indices[0] > maxVisibleIndex)
                                    maxVisibleIndex = indices[0];
                                if (indices[1] > maxVisibleIndex)
                                    maxVisibleIndex = indices[1];
                                if (indices[2] > maxVisibleIndex)
                                    maxVisibleIndex = indices[2];
                            }
                        }
                        else
                        {
                            bool visible = false;
                            for (int i = 0; i < 3; i++)
                            {
                                if (camera.frustum.IsInside(uniqueVertices[(int)indices[i]].p) || camera.IsPointClose(uniqueVertices[(int)indices[i]].p))
                                {
                                    visible = true;
                                    break;
                                }
                            }

                            if (visible)
                            {
                                localIndices.Add(indices[0]);
                                localIndices.Add(indices[1]);
                                localIndices.Add(indices[2]);
                                if (indices[0] > maxVisibleIndex)
                                    maxVisibleIndex = indices[0];
                                if (indices[1] > maxVisibleIndex)
                                    maxVisibleIndex = indices[1];
                                if (indices[2] > maxVisibleIndex)
                                    maxVisibleIndex = indices[2];
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

        protected static Vector3 ComputeFaceNormal(Vector3 p1, Vector3 p2, Vector3 p3)
        {
            Vector3 edge1 = p2 - p1;
            Vector3 edge2 = p3 - p1;
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
            visibleVerticesDataOnlyPos.Clear();
            visibleVerticesDataOnlyPosAndNormal.Clear();

            for (int i = 0; i < indices.Count; i++)
            {
                var v = uniqueVertices[(int)indices[i]];
                v.n = uniqueVertices[(int)indices[i]].p.Normalized();
                uniqueVertices[(int)indices[i]] = v;

                visibleVerticesData.AddRange(v.GetData());
                visibleVerticesDataOnlyPos.AddRange(v.GetDataOnlyPos());
                visibleVerticesDataOnlyPosAndNormal.AddRange(v.GetDataOnlyPosAndNormal());
            }
        }

        public void ComputeVertexNormals()
        {
            visibleVerticesData.Clear();
            visibleVerticesDataOnlyPos.Clear();
            visibleVerticesDataOnlyPosAndNormal.Clear();

            Dictionary<Vector3, Vector3> vertexNormals = new Dictionary<Vector3, Vector3>();
            Dictionary<Vector3, int> vertexNormalsCounts = new Dictionary<Vector3, int>();

            for (int i = 0; i < indices.Count; i+=3)
            {
                var v = uniqueVertices[(int)indices[i]];
                Vector3 faceNormal = ComputeFaceNormal(uniqueVertices[(int)indices[i]].p, uniqueVertices[(int)indices[i+1]].p, uniqueVertices[(int)indices[i+2]].p);
                for (int j = 0; j < 3; j++)
                {
                    if (!vertexNormals.ContainsKey(uniqueVertices[(int)indices[i + j]].p))
                    {
                        vertexNormals[uniqueVertices[(int)indices[i + j]].p] = faceNormal;
                        vertexNormalsCounts[uniqueVertices[(int)indices[i + j]].p] = 1;
                    }
                    else
                    {
                        vertexNormals[uniqueVertices[(int)indices[i + j]].p] += faceNormal;
                        vertexNormalsCounts[uniqueVertices[(int)indices[i + j]].p]++;
                    }
                }
            }

            for (int i = 0; i < indices.Count; i++)
            {
                var v = uniqueVertices[(int)indices[i]];
                v.n = (vertexNormals[uniqueVertices[(int)indices[i]].p] / vertexNormalsCounts[uniqueVertices[(int)indices[i]].p]).Normalized();
                uniqueVertices[(int)indices[i]] = v;

                visibleVerticesData.AddRange(v.GetData());
                visibleVerticesDataOnlyPos.AddRange(v.GetDataOnlyPos());
                visibleVerticesDataOnlyPosAndNormal.AddRange(v.GetDataOnlyPosAndNormal());
            }
        }

        public void ComputeTangents()
        {
            visibleVerticesData.Clear();

            // Initialize tangent and bitangent lists with zeros
            Dictionary<Vector3, List<Vector3>> tangentSums = new Dictionary<Vector3, List<Vector3>>();
            Dictionary<Vector3, List<Vector3>> bitangentSums = new Dictionary<Vector3, List<Vector3>>();

            for (int i = 0; i < indices.Count; i += 3)
            {
                // Get the vertices of the triangle
                Vector3 p0 = uniqueVertices[(int)indices[i]].p;
                Vector3 p1 = uniqueVertices[(int)indices[i+1]].p;
                Vector3 p2 = uniqueVertices[(int)indices[i+2]].p;

                // Get UVs of the triangle
                Vector2 uv0 = new Vector2(uniqueVertices[(int)indices[i]].t.u, uniqueVertices[(int)indices[i]].t.v);
                Vector2 uv1 = new Vector2(uniqueVertices[(int)indices[i+1]].t.u, uniqueVertices[(int)indices[i+1]].t.v);
                Vector2 uv2 = new Vector2(uniqueVertices[(int)indices[i+2]].t.u, uniqueVertices[(int)indices[i+2]].t.v);

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

                for (int j = 0; j < 3; j++)
                {
                    if (!tangentSums.ContainsKey(uniqueVertices[(int)indices[i + j]].p))
                    {
                        tangentSums[uniqueVertices[(int)indices[i + j]].p] = new List<Vector3>();
                        bitangentSums[uniqueVertices[(int)indices[i + j]].p] = new List<Vector3>();
                    }

                    tangentSums[uniqueVertices[(int)indices[i + j]].p].Add(tangent);
                    bitangentSums[uniqueVertices[(int)indices[i + j]].p].Add(bitangent);
                }
            }

            // Average and normalize tangents and bitangents
            for (int i = 0; i < indices.Count; i++)
            {
                Vector3 vertex = uniqueVertices[(int)indices[i]].p;

                Vector3 avgTangent = Average(tangentSums[vertex]).Normalized();
                if (float.IsNaN(avgTangent.X))
                    avgTangent = Vector3.Zero;
                Vector3 avgBitangent = Average(bitangentSums[vertex]).Normalized();
                if (float.IsNaN(avgBitangent.X))
                    avgBitangent = Vector3.Zero;

                var v = uniqueVertices[(int)indices[i]];
                v.tan = avgTangent;
                v.bitan = avgBitangent;
                uniqueVertices[(int)indices[i]] = v;

                visibleVerticesData.AddRange(v.GetData());
            }
        }

        public void ProcessObj(string relativeModelPath, float cr=1, float cg=1, float cb=1, float ca=1)
        {
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

                                    Vertex v1 = new Vertex(verts[v[0] - 1], normals[n[0] - 1], uvs[uv[0] - 1]) { pi = v[0] - 1, c = color };
                                    Vertex v2 = new Vertex(verts[v[1] - 1], normals[n[1] - 1], uvs[uv[1] - 1]) { pi = v[1] - 1, c = color };
                                    Vertex v3 = new Vertex(verts[v[2] - 1], normals[n[2] - 1], uvs[uv[2] - 1]) { pi = v[2] - 1, c = color };
                                    int v1h = v1.GetHashCode();
                                    if (!vertexHash.ContainsKey(v1h))
                                    {
                                        uniqueVertices.Add(v1);
                                        visibleVerticesData.AddRange(v1.GetData());
                                        visibleVerticesDataOnlyPos.AddRange(v1.GetDataOnlyPos());
                                        visibleVerticesDataOnlyPosAndNormal.AddRange(v1.GetDataOnlyPosAndNormal());
                                        indices.Add((uint)uniqueVertices.Count - 1);
                                        vertexHash.Add(v1h, (uint)uniqueVertices.Count - 1);
                                        Bounds.Enclose(v1);
                                    }
                                    else
                                    {
                                        indices.Add(vertexHash[v1h]);
                                    }
                                    int v2h = v2.GetHashCode();
                                    if (!vertexHash.ContainsKey(v2h))
                                    {
                                        uniqueVertices.Add(v2);
                                        visibleVerticesData.AddRange(v2.GetData());
                                        visibleVerticesDataOnlyPos.AddRange(v2.GetDataOnlyPos());
                                        visibleVerticesDataOnlyPosAndNormal.AddRange(v2.GetDataOnlyPosAndNormal());
                                        indices.Add((uint)uniqueVertices.Count - 1);
                                        vertexHash.Add(v2h, (uint)uniqueVertices.Count - 1);
                                        Bounds.Enclose(v2);
                                    }
                                    else
                                    {
                                        indices.Add(vertexHash[v2h]);
                                    }
                                    int v3h = v3.GetHashCode();
                                    if (!vertexHash.ContainsKey(v3h))
                                    {
                                        uniqueVertices.Add(v3);
                                        visibleVerticesData.AddRange(v3.GetData());
                                        visibleVerticesDataOnlyPos.AddRange(v3.GetDataOnlyPos());
                                        visibleVerticesDataOnlyPosAndNormal.AddRange(v3.GetDataOnlyPosAndNormal());
                                        indices.Add((uint)uniqueVertices.Count - 1);
                                        vertexHash.Add(v3h, (uint)uniqueVertices.Count - 1);
                                        Bounds.Enclose(v3);
                                    }
                                    else
                                    {
                                        indices.Add(vertexHash[v3h]);
                                    }

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

                                    Vertex v1 = new Vertex(verts[v[0] - 1], uvs[uv[0] - 1]) { pi = v[0] - 1, c = color };
                                    Vertex v2 = new Vertex(verts[v[1] - 1], uvs[uv[1] - 1]) { pi = v[1] - 1, c = color };
                                    Vertex v3 = new Vertex(verts[v[2] - 1], uvs[uv[2] - 1]) { pi = v[2] - 1, c = color };
                                    int v1h = v1.GetHashCode();
                                    if (!vertexHash.ContainsKey(v1h))
                                    {
                                        uniqueVertices.Add(v1);
                                        visibleVerticesData.AddRange(v1.GetData());
                                        visibleVerticesDataOnlyPos.AddRange(v1.GetDataOnlyPos());
                                        visibleVerticesDataOnlyPosAndNormal.AddRange(v1.GetDataOnlyPosAndNormal());
                                        indices.Add((uint)uniqueVertices.Count - 1);
                                        vertexHash.Add(v1h, (uint)uniqueVertices.Count - 1);
                                        Bounds.Enclose(v1);
                                    }
                                    else
                                    {
                                        indices.Add(vertexHash[v1h]);
                                    }
                                    int v2h = v2.GetHashCode();
                                    if (!vertexHash.ContainsKey(v2h))
                                    {
                                        uniqueVertices.Add(v2);
                                        visibleVerticesData.AddRange(v2.GetData());
                                        visibleVerticesDataOnlyPos.AddRange(v2.GetDataOnlyPos());
                                        visibleVerticesDataOnlyPosAndNormal.AddRange(v2.GetDataOnlyPosAndNormal());
                                        indices.Add((uint)uniqueVertices.Count - 1);
                                        vertexHash.Add(v2h, (uint)uniqueVertices.Count - 1);
                                        Bounds.Enclose(v2);
                                    }
                                    else
                                    {
                                        indices.Add(vertexHash[v2h]);
                                    }
                                    int v3h = v3.GetHashCode();
                                    if (!vertexHash.ContainsKey(v3h))
                                    {
                                        uniqueVertices.Add(v3);
                                        visibleVerticesData.AddRange(v3.GetData());
                                        visibleVerticesDataOnlyPos.AddRange(v3.GetDataOnlyPos());
                                        visibleVerticesDataOnlyPosAndNormal.AddRange(v3.GetDataOnlyPosAndNormal());
                                        indices.Add((uint)uniqueVertices.Count - 1);
                                        vertexHash.Add(v3h, (uint)uniqueVertices.Count - 1);
                                        Bounds.Enclose(v3);
                                    }
                                    else
                                    {
                                        indices.Add(vertexHash[v3h]);
                                    }

                                    hasIndices = true;
                                }

                            }
                            else
                            {
                                string[] vStr = result.Substring(2).Split(" ");
                                int[] v = { int.Parse(vStr[0]), int.Parse(vStr[1]), int.Parse(vStr[2]) };

                                Vertex v1 = new Vertex(verts[v[0] - 1]) { pi = v[0] - 1, c = color };
                                Vertex v2 = new Vertex(verts[v[1] - 1]) { pi = v[1] - 1, c = color };
                                Vertex v3 = new Vertex(verts[v[2] - 1]) { pi = v[2] - 1, c = color };
                                int v1h = v1.GetHashCode();
                                if (!vertexHash.ContainsKey(v1h))
                                {
                                    uniqueVertices.Add(v1);
                                    visibleVerticesData.AddRange(v1.GetData());
                                    visibleVerticesDataOnlyPos.AddRange(v1.GetDataOnlyPos());
                                    visibleVerticesDataOnlyPosAndNormal.AddRange(v1.GetDataOnlyPosAndNormal());
                                    indices.Add((uint)uniqueVertices.Count - 1);
                                    vertexHash.Add(v1h, (uint)uniqueVertices.Count - 1);
                                    Bounds.Enclose(v1);
                                }
                                else
                                {
                                    indices.Add(vertexHash[v1h]);
                                }
                                int v2h = v2.GetHashCode();
                                if (!vertexHash.ContainsKey(v2h))
                                {
                                    uniqueVertices.Add(v2);
                                    visibleVerticesData.AddRange(v2.GetData());
                                    visibleVerticesDataOnlyPos.AddRange(v2.GetDataOnlyPos());
                                    visibleVerticesDataOnlyPosAndNormal.AddRange(v2.GetDataOnlyPosAndNormal());
                                    indices.Add((uint)uniqueVertices.Count - 1);
                                    vertexHash.Add(v2h, (uint)uniqueVertices.Count - 1);
                                    Bounds.Enclose(v2);
                                }
                                else
                                {
                                    indices.Add(vertexHash[v2h]);
                                }
                                int v3h = v3.GetHashCode();
                                if (!vertexHash.ContainsKey(v3h))
                                {
                                    uniqueVertices.Add(v3);
                                    visibleVerticesData.AddRange(v3.GetData());
                                    visibleVerticesDataOnlyPos.AddRange(v3.GetDataOnlyPos());
                                    visibleVerticesDataOnlyPosAndNormal.AddRange(v3.GetDataOnlyPosAndNormal());
                                    indices.Add((uint)uniqueVertices.Count - 1);
                                    vertexHash.Add(v3h, (uint)uniqueVertices.Count - 1);
                                    Bounds.Enclose(v3);
                                }
                                else
                                {
                                    indices.Add(vertexHash[v3h]);
                                }

                                hasIndices = true;
                            }
                        }
                    }

                    if (result == null)
                        break;
                }
            }

            visibleIndices = new List<uint>(indices);
            groupedIndices = indices
               .Select((x, i) => new { Index = i, Value = x })
               .GroupBy(x => x.Index / 3)
               .Select(x => x.Select(v => v.Value).ToList())
               .ToList();
        }

    }
}
