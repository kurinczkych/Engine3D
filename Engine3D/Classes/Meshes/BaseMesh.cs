using Assimp;
using Engine3D.Classes;
using FontStashSharp;
using MagicPhysX;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
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

        public ModelData model = new ModelData();

        public Object parentObject;

        public bool useShading = true;

        protected Camera camera;

        public Dictionary<string, int> uniformLocations = new Dictionary<string, int>();
        public Dictionary<string, int> uniformAnimLocations = new Dictionary<string, int>();

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
            foreach (MeshData mesh in model.meshes)
            {
                if (mesh.allVerts != null)
                    mesh.allVerts.Clear();
            }
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
                    foreach (MeshData mesh in model.meshes)
                    {
                        mesh.visibleIndices.Clear();

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

                        var a = this.GetType();

                        ParallelOptions parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = threadSize };
                        Parallel.ForEach(mesh.groupedIndices, parallelOptions,
                        () => new List<uint>(),
                        (indices_, loopState, localIndices) =>
                        {
                            if (modelMatrix != Matrix4.Identity)
                            {
                                bool visible = false;
                                for (int i = 0; i < 3; i++)
                                {
                                    Vector3 p = Vector3.TransformPosition(mesh.uniqueVertices[(int)indices_[i]].p, modelMatrix);
                                    if (camera.frustum.IsInside(p) || camera.IsPointClose(p))
                                    {
                                        visible = true;
                                        break;
                                    }
                                }

                                if (visible)
                                {
                                    localIndices.Add(indices_[0]);
                                    localIndices.Add(indices_[1]);
                                    localIndices.Add(indices_[2]);
                                    if (indices_[0] > mesh.maxVisibleIndex)
                                        mesh.maxVisibleIndex = indices_[0];
                                    if (indices_[1] > mesh.maxVisibleIndex)
                                        mesh.maxVisibleIndex = indices_[1];
                                    if (indices_[2] > mesh.maxVisibleIndex)
                                        mesh.maxVisibleIndex = indices_[2];
                                }
                            }
                            else
                            {
                                bool visible = false;
                                for (int i = 0; i < 3; i++)
                                {
                                    if (camera.frustum.IsInside(mesh.uniqueVertices[(int)indices_[i]].p) || camera.IsPointClose(mesh.uniqueVertices[(int)indices_[i]].p))
                                    {
                                        visible = true;
                                        break;
                                    }
                                }

                                if (visible)
                                {
                                    localIndices.Add(indices_[0]);
                                    localIndices.Add(indices_[1]);
                                    localIndices.Add(indices_[2]);
                                    if (indices_[0] > mesh.maxVisibleIndex)
                                        mesh.maxVisibleIndex = indices_[0];
                                    if (indices_[1] > mesh.maxVisibleIndex)
                                        mesh.maxVisibleIndex = indices_[1];
                                    if (indices_[2] > mesh.maxVisibleIndex)
                                        mesh.maxVisibleIndex = indices_[2];
                                }
                            }
                            return localIndices;
                        },
                        localIndices =>
                        {
                            lock (mesh.visibleIndices)
                            {
                                mesh.visibleIndices.AddRange(localIndices);
                            }
                        });
                    }
                }
            }
        }

        public void AllIndicesVisible()
        {
            foreach(MeshData mesh in model.meshes)
                mesh.visibleIndices = new List<uint>(mesh.indices);
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

        protected class Vector3Comparer : IEqualityComparer<Vector3>
        {
            public bool Equals(Vector3 x, Vector3 y)
            {
                return x == y;
            }

            public int GetHashCode(Vector3 obj)
            {
                return obj.GetHashCode();
            }
        }

        public void ComputeVertexNormalsSpherical()
        {
            foreach (MeshData mesh in model.meshes)
            {
                mesh.visibleVerticesData.Clear();
                mesh.visibleVerticesDataOnlyPos.Clear();
                mesh.visibleVerticesDataOnlyPosAndNormal.Clear();
                bool anim = false;
                if (mesh.visibleVerticesDataWithAnim.Count > 0)
                {
                    mesh.visibleVerticesDataWithAnim.Clear();
                    anim = true;
                }

                for (int i = 0; i < mesh.uniqueVertices.Count; i++)
                {
                    var v = mesh.uniqueVertices[i];
                    v.n = v.p.Normalized();
                    mesh.uniqueVertices[i] = v;

                    mesh.visibleVerticesData.AddRange(v.GetData());
                    if(anim)
                        mesh.visibleVerticesDataWithAnim.AddRange(v.GetDataWithAnim());
                    mesh.visibleVerticesDataOnlyPos.AddRange(v.GetDataOnlyPos());
                    mesh.visibleVerticesDataOnlyPosAndNormal.AddRange(v.GetDataOnlyPosAndNormal());
                }
            }
        }

        public void ComputeVertexNormals()
        {
            foreach (MeshData mesh in model.meshes)
            {
                mesh.visibleVerticesData.Clear();
                mesh.visibleVerticesDataOnlyPos.Clear();
                mesh.visibleVerticesDataOnlyPosAndNormal.Clear();
                bool anim = false;
                if (mesh.visibleVerticesDataWithAnim.Count > 0)
                {
                    mesh.visibleVerticesDataWithAnim.Clear();
                    anim = true;
                }

                Dictionary<Vector3, Vector3> vertexNormals = new Dictionary<Vector3, Vector3>();
                Dictionary<Vector3, int> vertexNormalsCounts = new Dictionary<Vector3, int>();

                for (int i = 0; i < mesh.indices.Count; i += 3)
                {
                    var v = mesh.uniqueVertices[(int)mesh.indices[i]];
                    Vector3 faceNormal = ComputeFaceNormal(mesh.uniqueVertices[(int)mesh.indices[i]].p, 
                                                           mesh.uniqueVertices[(int)mesh.indices[i + 1]].p,
                                                           mesh.uniqueVertices[(int)mesh.indices[i + 2]].p);
                    for (int j = 0; j < 3; j++)
                    {
                        if (!vertexNormals.ContainsKey(mesh.uniqueVertices[(int)mesh.indices[i + j]].p))
                        {
                            vertexNormals[mesh.uniqueVertices[(int)mesh.indices[i + j]].p] = faceNormal;
                            vertexNormalsCounts[mesh.uniqueVertices[(int)mesh.indices[i + j]].p] = 1;
                        }
                        else
                        {
                            vertexNormals[mesh.uniqueVertices[(int)mesh.indices[i + j]].p] += faceNormal;
                            vertexNormalsCounts[mesh.uniqueVertices[(int)mesh.indices[i + j]].p]++;
                        }
                    }
                }


                for (int i = 0; i < mesh.uniqueVertices.Count; i++)
                {
                    var v = mesh.uniqueVertices[i];
                    v.n = (vertexNormals[mesh.uniqueVertices[i].p] / vertexNormalsCounts[mesh.uniqueVertices[i].p]).Normalized();
                    mesh.uniqueVertices[i] = v;

                    mesh.visibleVerticesData.AddRange(v.GetData());
                    if(anim)
                        mesh.visibleVerticesDataWithAnim.AddRange(v.GetDataWithAnim());
                    mesh.visibleVerticesDataOnlyPos.AddRange(v.GetDataOnlyPos());
                    mesh.visibleVerticesDataOnlyPosAndNormal.AddRange(v.GetDataOnlyPosAndNormal());
                }
            }
        }

        public void ComputeTangents()
        {
            foreach (MeshData mesh in model.meshes)
            {
                mesh.visibleVerticesData.Clear();
                mesh.visibleVerticesDataOnlyPos.Clear();
                mesh.visibleVerticesDataOnlyPosAndNormal.Clear();
                bool anim = false;
                if (mesh.visibleVerticesDataWithAnim.Count > 0)
                {
                    mesh.visibleVerticesDataWithAnim.Clear();
                    anim = true;
                }

                // Initialize tangent and bitangent lists with zeros
                Dictionary<Vector3, List<Vector3>> tangentSums = new Dictionary<Vector3, List<Vector3>>();
                Dictionary<Vector3, List<Vector3>> bitangentSums = new Dictionary<Vector3, List<Vector3>>();

                for (int i = 0; i < mesh.indices.Count; i += 3)
                {
                    // Get the vertices of the triangle
                    Vector3 p0 = mesh.uniqueVertices[(int)mesh.indices[i]].p;
                    Vector3 p1 = mesh.uniqueVertices[(int)mesh.indices[i + 1]].p;
                    Vector3 p2 = mesh.uniqueVertices[(int)mesh.indices[i + 2]].p;

                    // Get UVs of the triangle
                    Vector2 uv0 = new Vector2(mesh.uniqueVertices[(int)mesh.indices[i]].t.u, mesh.uniqueVertices[(int)mesh.indices[i]].t.v);
                    Vector2 uv1 = new Vector2(mesh.uniqueVertices[(int)mesh.indices[i + 1]].t.u, mesh.uniqueVertices[(int)mesh.indices[i + 1]].t.v);
                    Vector2 uv2 = new Vector2(mesh.uniqueVertices[(int)mesh.indices[i + 2]].t.u, mesh.uniqueVertices[(int)mesh.indices[i + 2]].t.v);

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
                        if (!tangentSums.ContainsKey(mesh.uniqueVertices[(int)mesh.indices[i + j]].p))
                        {
                            tangentSums[mesh.uniqueVertices[(int)mesh.indices[i + j]].p] = new List<Vector3>();
                            bitangentSums[mesh.uniqueVertices[(int)mesh.indices[i + j]].p] = new List<Vector3>();
                        }

                        tangentSums[mesh.uniqueVertices[(int)mesh.indices[i + j]].p].Add(tangent);
                        bitangentSums[mesh.uniqueVertices[(int)mesh.indices[i + j]].p].Add(bitangent);
                    }
                }

                // Average and normalize tangents and bitangents
                for (int i = 0; i < mesh.uniqueVertices.Count; i++)
                {
                    Vector3 vertex = mesh.uniqueVertices[i].p;

                    Vector3 avgTangent = Average(tangentSums[vertex]).Normalized();
                    if (float.IsNaN(avgTangent.X))
                        avgTangent = Vector3.Zero;
                    Vector3 avgBitangent = Average(bitangentSums[vertex]).Normalized();
                    if (float.IsNaN(avgBitangent.X))
                        avgBitangent = Vector3.Zero;

                    var v = mesh.uniqueVertices[i];
                    v.tan = avgTangent;
                    v.bitan = avgBitangent;
                    mesh.uniqueVertices[i] = v;

                    mesh.visibleVerticesData.AddRange(v.GetData());
                    if(anim)
                        mesh.visibleVerticesDataWithAnim.AddRange(v.GetDataWithAnim());
                    mesh.visibleVerticesDataOnlyPos.AddRange(v.GetDataOnlyPos());
                    mesh.visibleVerticesDataOnlyPosAndNormal.AddRange(v.GetDataOnlyPosAndNormal());
                }
            }
        }

        public void ProcessObj(string relativeModelPath, float cr=1, float cg=1, float cb=1, float ca=1)
        {
            ModelData? md = Engine.assimpManager.ProcessModel(relativeModelPath, cr, cg, cb, ca);
            if (md != null)
            {
                model = md;
            }
        }

    }
}
