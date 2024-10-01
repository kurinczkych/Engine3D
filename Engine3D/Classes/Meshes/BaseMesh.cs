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
using static OpenTK.Graphics.OpenGL.GL;
using static System.Formats.Asn1.AsnWriter;

namespace Engine3D
{

    public unsafe abstract class BaseMesh : IComponent
    {
        public int vbo;
        public int vaoId;
        public int vboId;
        public int shaderProgramId;

        protected string modelName_;
        public string modelName
        {
            get { return modelName_; }
            set
            {
                string relativePath = AssetManager.GetRelativeModelsFolder(value);
                modelPath = relativePath;
                modelName_ = Path.GetFileName(modelPath);
                ProcessObj(modelPath);

                if (model.meshes.Count > 0 && model.meshes[0].uniqueVertices.Count > 0 && !model.meshes[0].uniqueVertices[0].gotNormal)
                {
                    ComputeVertexNormalsSpherical();
                }
                else if (model.meshes.Count > 0 && model.meshes[0].uniqueVertices.Count > 0 && model.meshes[0].uniqueVertices[0].gotNormal)
                    ComputeVertexNormals();

                ComputeTangents();
                recalculate = true;
            }
        }
        public string modelPath;

        private bool _recalculate = false;
        public bool recalculate
        {
            get { return _recalculate; }
            set { _recalculate = value; recalculateOnlyPos = true; recalculateOnlyPosAndNormal = true; }
        }
        protected bool recalculateOnlyPos;
        protected bool recalculateOnlyPosAndNormal;

        protected int useBillboarding_ = 0;
        public int useBillboarding
        {
            get
            {
                return useBillboarding_;
            }
            set
            {
                Type meshType = GetType();
                if (meshType != typeof(Mesh) && meshType != typeof(InstancedMesh))
                    throw new Exception("Billboarding can only be used on type 'Mesh' and 'InstancedMesh'!");

                useBillboarding = value;
            }
        }

        public ModelData model = new ModelData();

        public Object parentObject;

        #region Texture
        public Texture? texture;
        public Texture? textureNormal;
        public Texture? textureHeight;
        public Texture? textureAO;
        public Texture? textureRough;
        public Texture? textureMetal;

        private string _textureName;
        public string textureName
        {
            get
            {
                if (texture != null)
                    return Path.GetFileName(texture.TexturePath);

                return "";
            }
            set
            {
                string texturePath = value;
                if (texturePath == "" && texture != null)
                {
                    Engine.textureManager.DeleteTexture(texture, this, "");
                    texture = null;
                }
                else if (texture != null)
                {
                    Engine.textureManager.DeleteTexture(texture, this, "");
                    texture = Engine.textureManager.AddTexture(texturePath, out bool success);
                    if (!success || texture == null)
                    {
                        Engine.consoleManager.AddLog("Texture: " + texturePath + "was not found!", LogType.Warning);
                    }
                }
                else
                {
                    texture = Engine.textureManager.AddTexture(texturePath, out bool success);
                    if (!success || texture == null)
                    {
                        Engine.consoleManager.AddLog("Texture: " + texturePath + "was not found!", LogType.Warning);
                    }
                    else
                        AddUniformLocation("textureSampler");
                }
            }
        }
        private string _textureNormalName;
        public string textureNormalName
        {
            get
            {
                if (textureNormal != null)
                    return Path.GetFileName(textureNormal.TexturePath);

                return "";
            }
            set
            {
                string texturePath = value;
                if (texturePath == "" && textureNormal != null)
                {
                    Engine.textureManager.DeleteTexture(textureNormal, this, "Normal");
                    textureNormal = null;
                }
                else if (textureNormal != null)
                {
                    Engine.textureManager.DeleteTexture(textureNormal, this, "Normal");
                    textureNormal = Engine.textureManager.AddTexture(texturePath, out bool success);
                    if (!success || textureNormal == null)
                    {
                        Engine.consoleManager.AddLog("Texture: " + texturePath + "was not found!", LogType.Warning);
                    }
                }
                else
                {
                    textureNormal = Engine.textureManager.AddTexture(texturePath, out bool success);
                    if (!success || textureNormal == null)
                    {
                        Engine.consoleManager.AddLog("Texture: " + texturePath + "was not found!", LogType.Warning);
                    }
                    else
                        AddUniformLocation("textureSamplerNormal");
                }
            }
        }
        private string _textureHeightName;
        public string textureHeightName
        {
            get
            {
                if (textureHeight != null)
                    return Path.GetFileName(textureHeight.TexturePath);

                return "";
            }
            set
            {
                string texturePath = value;
                if (texturePath == "" && textureHeight != null)
                {
                    Engine.textureManager.DeleteTexture(textureHeight, this, "Height");
                    textureHeight = null;
                }
                else if (textureHeight != null)
                {
                    Engine.textureManager.DeleteTexture(textureHeight, this, "Height");
                    textureHeight = Engine.textureManager.AddTexture(texturePath, out bool success);
                    if (!success || textureHeight == null)
                    {
                        Engine.consoleManager.AddLog("Texture: " + texturePath + "was not found!", LogType.Warning);
                    }
                }
                else
                {
                    textureHeight = Engine.textureManager.AddTexture(texturePath, out bool success);
                    if (!success || textureHeight == null)
                    {
                        Engine.consoleManager.AddLog("Texture: " + texturePath + "was not found!", LogType.Warning);
                    }
                    else
                        AddUniformLocation("textureSamplerHeight");
                }
            }
        }
        private string _textureAOName;
        public string textureAOName
        {
            get
            {
                if (textureAO != null)
                    return Path.GetFileName(textureAO.TexturePath);

                return "";
            }
            set
            {
                string texturePath = value;
                if (texturePath == "" && textureAO != null)
                {
                    Engine.textureManager.DeleteTexture(textureAO, this, "AO");
                    textureAO = null;
                }
                else if (textureAO != null)
                {
                    Engine.textureManager.DeleteTexture(textureAO, this, "AO");
                    textureAO = Engine.textureManager.AddTexture(texturePath, out bool success);
                    if (!success || textureAO == null)
                    {
                        Engine.consoleManager.AddLog("Texture: " + texturePath + "was not found!", LogType.Warning);
                    }
                }
                else
                {
                    textureAO = Engine.textureManager.AddTexture(texturePath, out bool success);
                    if (!success || textureAO == null)
                    {
                        Engine.consoleManager.AddLog("Texture: " + texturePath + "was not found!", LogType.Warning);
                    }
                    else
                        AddUniformLocation("textureSamplerAO");
                }
            }
        }
        private string _textureRoughName;
        public string textureRoughName
        {
            get
            {
                if (textureRough != null)
                    return Path.GetFileName(textureRough.TexturePath);

                return "";
            }
            set
            {
                string texturePath = value;
                if (texturePath == "" && textureRough != null)
                {
                    Engine.textureManager.DeleteTexture(textureRough, this, "Rough");
                    textureRough = null;
                }
                else if (textureRough != null)
                {
                    Engine.textureManager.DeleteTexture(textureRough, this, "Rough");
                    textureRough = Engine.textureManager.AddTexture(texturePath, out bool success);
                    if (!success || textureRough == null)
                    {
                        Engine.consoleManager.AddLog("Texture: " + texturePath + "was not found!", LogType.Warning);
                    }
                }
                else
                {
                    textureRough = Engine.textureManager.AddTexture(texturePath, out bool success);
                    if (!success || textureRough == null)
                    {
                        Engine.consoleManager.AddLog("Texture: " + texturePath + "was not found!", LogType.Warning);
                    }
                    else
                        AddUniformLocation("textureSamplerRough");
                }
            }
        }
        private string _textureMetalName;
        public string textureMetalName
        {
            get
            {
                if (textureMetal != null)
                    return Path.GetFileName(textureMetal.TexturePath);

                return "";
            }
            set
            {
                string texturePath = value;
                if (texturePath == "" && textureMetal != null)
                {
                    Engine.textureManager.DeleteTexture(textureMetal, this, "Metal");
                    textureMetal = null;
                }
                else if (textureMetal != null)
                {
                    Engine.textureManager.DeleteTexture(textureMetal, this, "Metal");
                    textureMetal = Engine.textureManager.AddTexture(texturePath, out bool success);
                    if (!success || textureMetal == null)
                    {
                        Engine.consoleManager.AddLog("Texture: " + texturePath + "was not found!", LogType.Warning);
                    }
                }
                else
                {
                    textureMetal = Engine.textureManager.AddTexture(texturePath, out bool success);
                    if (!success || textureMetal == null)
                    {
                        Engine.consoleManager.AddLog("Texture: " + texturePath + "was not found!", LogType.Warning);
                    }
                    else
                        AddUniformLocation("textureSamplerMetal");
                }
            }
        }
        #endregion

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

        public bool isValidMesh(PxVec3* vertices, int numVertices, int* indices, int numIndices)
        {
            if (numVertices == 0 || numIndices == 0 || numIndices % 3 != 0)
                return false;

            for (int i = 0; i < numIndices; ++i)
            {
                if (indices[i] >= numVertices)
                    return false;
            }

            // Additional checks (e.g., degenerate triangles, manifoldness, etc.) would go here.

            return true;
        }

        public void SetInstancedData(List<InstancedMeshData> data)
        {
            if (GetType() != typeof(InstancedMesh))
                throw new Exception("Cannot set instanced mesh data for '" + GetType().ToString() + "'!");

            ((InstancedMesh)this).instancedData = data;
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
                    scaleMatrix = Matrix4.CreateScale(parentObject.transformation.Scale);

                if (which[1])
                    rotationMatrix = Matrix4.CreateFromQuaternion(parentObject.transformation.Rotation);

                if (which[0])
                    translationMatrix = Matrix4.CreateTranslation(parentObject.transformation.Position);

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

        public void Delete(ref TextureManager textureManager)
        {
            foreach (MeshData mesh in model.meshes)
            {
                if (mesh.allVerts != null)
                    mesh.allVerts.Clear();
            }
            if (uniformLocations != null)
                uniformLocations.Clear();

            if (texture != null)
            {
                textureManager.DeleteTexture(textureName);
                texture = null;
            }
            if (textureNormal != null)
            {
                textureManager.DeleteTexture(textureNormalName);
                textureNormal = null;
            }
            if (textureHeight != null)
            {
                textureManager.DeleteTexture(textureHeightName);
                textureHeight = null;
            }
            if (textureAO != null)
            {
                textureManager.DeleteTexture(textureAOName);
                textureAO = null;
            }
            if (textureRough != null)
            {
                textureManager.DeleteTexture(textureRoughName);
                textureRough = null;
            }
            if (textureMetal != null)
            {
                textureManager.DeleteTexture(textureMetalName);
                textureMetal = null;
            }
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

        #region SimpleMeshes
        private static void AddToVertexList(ref List<Vertex> list, Vector3[] v, Vector3[] n, Vec2d[] t, Color4 c)
        {
            list.Add(new Vertex(v[0], n[0], t[0]) { c = c });
            list.Add(new Vertex(v[1], n[1], t[1]) { c = c });
            list.Add(new Vertex(v[2], n[2], t[2]) { c = c });
        }
        private static void AddToVertexList(ref List<Vertex> list, Vector3[] v, Vec2d[] t, Color4 c)
        {
            list.Add(new Vertex(v[0], t[0]) { c = c });
            list.Add(new Vertex(v[1], t[1]) { c = c });
            list.Add(new Vertex(v[2], t[2]) { c = c });
        }
        private static void AddToVertexList(ref List<Vertex> list, Vector3[] v, Color4 c)
        {
            list.Add(new Vertex(v[0]) { c = c });
            list.Add(new Vertex(v[1]) { c = c });
            list.Add(new Vertex(v[2]) { c = c });
        }

        public static ModelData GetUnitCube(float r = 1.0f, float g = 1.0f, float b = 1.0f, float a = 1.0f)
        {
            Color4 c = new Color4(r, g, b, a);

            MeshData meshData = new MeshData();

            float halfSize = 0.5f;

            // Define cube vertices
            Vector3 p1 = new Vector3(-halfSize, -halfSize, -halfSize);
            Vector3 p2 = new Vector3(halfSize, -halfSize, -halfSize);
            Vector3 p3 = new Vector3(halfSize, halfSize, -halfSize);
            Vector3 p4 = new Vector3(-halfSize, halfSize, -halfSize);
            Vector3 p5 = new Vector3(-halfSize, -halfSize, halfSize);
            Vector3 p6 = new Vector3(halfSize, -halfSize, halfSize);
            Vector3 p7 = new Vector3(halfSize, halfSize, halfSize);
            Vector3 p8 = new Vector3(-halfSize, halfSize, halfSize);

            Vec2d t1 = new Vec2d(0, 0);
            Vec2d t2 = new Vec2d(1, 0);
            Vec2d t3 = new Vec2d(1, 1);
            Vec2d t4 = new Vec2d(0, 1);

            Vector3 normalBack = new Vector3(0, 0, -1);  // Back face (-Z)
            Vector3 normalFront = new Vector3(0, 0, 1);  // Front face (+Z)
            Vector3 normalLeft = new Vector3(-1, 0, 0);  // Left face (-X)
            Vector3 normalRight = new Vector3(1, 0, 0);  // Right face (+X)
            Vector3 normalTop = new Vector3(0, 1, 0);    // Top face (+Y)
            Vector3 normalBottom = new Vector3(0, -1, 0); // Bottom face (-Y)

            List<Vertex> list = new List<Vertex>();

            AddToVertexList(ref list, new Vector3[] { p1, p3, p2 }, new Vector3[] { normalBack, normalBack, normalBack }, new Vec2d[] { t1, t3, t2 }, c);
            AddToVertexList(ref list, new Vector3[] { p3, p1, p4 }, new Vector3[] { normalBack, normalBack, normalBack }, new Vec2d[] { t3, t1, t4 }, c);

            // Front face (+Z)
            AddToVertexList(ref list, new Vector3[] { p5, p6, p7 }, new Vector3[] { normalFront, normalFront, normalFront }, new Vec2d[] { t1, t2, t3 }, c);
            AddToVertexList(ref list, new Vector3[] { p7, p8, p5 }, new Vector3[] { normalFront, normalFront, normalFront }, new Vec2d[] { t3, t4, t1 }, c);

            // Left face (-X)
            AddToVertexList(ref list, new Vector3[] { p1, p8, p4 }, new Vector3[] { normalLeft, normalLeft, normalLeft }, new Vec2d[] { t1, t3, t2 }, c);
            AddToVertexList(ref list, new Vector3[] { p8, p1, p5 }, new Vector3[] { normalLeft, normalLeft, normalLeft }, new Vec2d[] { t3, t1, t4 }, c);

            // Right face (+X)
            AddToVertexList(ref list, new Vector3[] { p2, p3, p7 }, new Vector3[] { normalRight, normalRight, normalRight }, new Vec2d[] { t1, t2, t3 }, c);
            AddToVertexList(ref list, new Vector3[] { p7, p6, p2 }, new Vector3[] { normalRight, normalRight, normalRight }, new Vec2d[] { t3, t4, t1 }, c);

            // Top face (+Y)
            AddToVertexList(ref list, new Vector3[] { p4, p7, p3 }, new Vector3[] { normalTop, normalTop, normalTop }, new Vec2d[] { t1, t3, t2 }, c);
            AddToVertexList(ref list, new Vector3[] { p7, p4, p8 }, new Vector3[] { normalTop, normalTop, normalTop }, new Vec2d[] { t3, t1, t4 }, c);

            // Bottom face (-Y)
            AddToVertexList(ref list, new Vector3[] { p1, p2, p6 }, new Vector3[] { normalBottom, normalBottom, normalBottom }, new Vec2d[] { t1, t2, t3 }, c);
            AddToVertexList(ref list, new Vector3[] { p6, p5, p1 }, new Vector3[] { normalBottom, normalBottom, normalBottom }, new Vec2d[] { t3, t4, t1 }, c);

            //AddToVertexList(ref list, new Vector3[] { p1, p3, p2 }, new Vec2d[] { t1, t3, t2 }, c);
            //AddToVertexList(ref list, new Vector3[] { p3, p1, p4 }, new Vec2d[] { t3, t1, t4 }, c);
            //AddToVertexList(ref list, new Vector3[] { p5, p6, p7 }, new Vec2d[] { t1, t2, t3 }, c);
            //AddToVertexList(ref list, new Vector3[] { p7, p8, p5 }, new Vec2d[] { t3, t4, t1 }, c);
            //AddToVertexList(ref list, new Vector3[] { p1, p8, p4 }, new Vec2d[] { t1, t3, t2 }, c);
            //AddToVertexList(ref list, new Vector3[] { p8, p1, p5 }, new Vec2d[] { t3, t1, t4 }, c);
            //AddToVertexList(ref list, new Vector3[] { p2, p3, p7 }, new Vec2d[] { t1, t2, t3 }, c);
            //AddToVertexList(ref list, new Vector3[] { p7, p6, p2 }, new Vec2d[] { t3, t4, t1 }, c);
            //AddToVertexList(ref list, new Vector3[] { p4, p7, p3 }, new Vec2d[] { t1, t3, t2 }, c);
            //AddToVertexList(ref list, new Vector3[] { p7, p4, p8 }, new Vec2d[] { t3, t1, t4 }, c);
            //AddToVertexList(ref list, new Vector3[] { p1, p2, p6 }, new Vec2d[] { t1, t2, t3 }, c);
            //AddToVertexList(ref list, new Vector3[] { p6, p5, p1 }, new Vec2d[] { t3, t4, t1 }, c);

            Dictionary<int, uint> hash = new Dictionary<int, uint>();
            for (int i = 0; i < list.Count; i++)
            {
                int vh = list[i].GetHashCode();
                if (!hash.ContainsKey(vh))
                {
                    meshData.uniqueVertices.Add(list[i]);
                    meshData.visibleVerticesData.AddRange(list[i].GetData());
                    meshData.visibleVerticesDataOnlyPos.AddRange(list[i].GetDataOnlyPos());
                    meshData.visibleVerticesDataOnlyPosAndNormal.AddRange(list[i].GetDataOnlyPosAndNormal());
                    meshData.indices.Add((uint)meshData.uniqueVertices.Count - 1);
                    hash.Add(vh, (uint)meshData.uniqueVertices.Count - 1);
                    meshData.Bounds.Enclose(list[i]);
                }
                else
                {
                    meshData.indices.Add(hash[vh]);
                }
            }

            meshData.CalculateGroupedIndices();

            ModelData model = new ModelData();
            model.meshes.Add(meshData);
            return model;
        }

        public static ModelData GetUnitFace(float r = 1.0f, float g = 1.0f, float b = 1.0f, float a = 1.0f)
        {
            Color4 c = new Color4(r, g, b, a);

            MeshData meshData = new MeshData();

            float halfSize = 0.5f;

            // Define cube vertices (only what's needed for the front face)
            Vector3 p5 = new Vector3(-halfSize, -halfSize, halfSize);
            Vector3 p6 = new Vector3(halfSize, -halfSize, halfSize);
            Vector3 p7 = new Vector3(halfSize, halfSize, halfSize);
            Vector3 p8 = new Vector3(-halfSize, halfSize, halfSize);

            Vec2d t1 = new Vec2d(0, 0);
            Vec2d t2 = new Vec2d(1, 0);
            Vec2d t3 = new Vec2d(1, 1);
            Vec2d t4 = new Vec2d(0, 1);

            List<Vertex> list = new List<Vertex>();

            // Front face
            AddToVertexList(ref list, new Vector3[] { p5, p6, p7 }, new Vec2d[] { t1, t2, t3 }, c);
            AddToVertexList(ref list, new Vector3[] { p7, p8, p5 }, new Vec2d[] { t3, t4, t1 }, c);

            Dictionary<int, uint> hash = new Dictionary<int, uint>();
            for (int i = 0; i < list.Count; i++)
            {
                int vh = list[i].GetHashCode();
                if (!hash.ContainsKey(vh))
                {
                    meshData.uniqueVertices.Add(list[i]);
                    meshData.visibleVerticesData.AddRange(list[i].GetData());
                    meshData.visibleVerticesDataOnlyPos.AddRange(list[i].GetDataOnlyPos());
                    meshData.visibleVerticesDataOnlyPosAndNormal.AddRange(list[i].GetDataOnlyPosAndNormal());
                    meshData.indices.Add((uint)meshData.uniqueVertices.Count - 1);
                    hash.Add(vh, (uint)meshData.uniqueVertices.Count - 1);
                    meshData.Bounds.Enclose(list[i]);
                }
                else
                {
                    meshData.indices.Add(hash[vh]);
                }
            }

            meshData.CalculateGroupedIndices();

            ModelData model = new ModelData();
            model.meshes.Add(meshData);
            return model;
        }

        public static ModelData GetUnitSphere(float radius = 1, int resolution = 10,
                                                                                  float r = 1.0f, float g = 1.0f, float b = 1.0f, float a = 1.0f)
        {
            Color4 c = new Color4(r, g, b, a);

            MeshData meshData = new MeshData();

            // Validate inputs
            if (radius <= 0 || resolution < 3)
                throw new ArgumentException("Invalid radius or resolution.");

            List<Vertex> list = new List<Vertex>();
            for (int i = 0; i < resolution; i++)
            {
                for (int j = 0; j < resolution; j++)
                {
                    float u1 = i / (float)resolution * MathF.PI * 2;
                    float u2 = (i + 1) / (float)resolution * MathF.PI * 2;
                    float v1 = j / (float)resolution * MathF.PI;
                    float v2 = (j + 1) / (float)resolution * MathF.PI;

                    Vector3 p1 = new Vector3(
                        radius * MathF.Sin(v1) * MathF.Cos(u1),
                        radius * MathF.Cos(v1),
                        radius * MathF.Sin(v1) * MathF.Sin(u1));

                    Vector3 p2 = new Vector3(
                        radius * MathF.Sin(v1) * MathF.Cos(u2),
                        radius * MathF.Cos(v1),
                        radius * MathF.Sin(v1) * MathF.Sin(u2));

                    Vector3 p3 = new Vector3(
                        radius * MathF.Sin(v2) * MathF.Cos(u1),
                        radius * MathF.Cos(v2),
                        radius * MathF.Sin(v2) * MathF.Sin(u1));

                    Vector3 p4 = new Vector3(
                        radius * MathF.Sin(v2) * MathF.Cos(u2),
                        radius * MathF.Cos(v2),
                        radius * MathF.Sin(v2) * MathF.Sin(u2));

                    AddToVertexList(ref list, new Vector3[] { p1, p2, p3 }, c);
                    AddToVertexList(ref list, new Vector3[] { p2, p4, p3 }, c);
                }
            }

            Dictionary<int, uint> hash = new Dictionary<int, uint>();
            for (int i = 0; i < list.Count; i++)
            {
                int vh = list[i].GetHashCode();
                if (!hash.ContainsKey(vh))
                {
                    meshData.uniqueVertices.Add(list[i]);
                    meshData.visibleVerticesData.AddRange(list[i].GetData());
                    meshData.visibleVerticesDataOnlyPos.AddRange(list[i].GetDataOnlyPos());
                    meshData.visibleVerticesDataOnlyPosAndNormal.AddRange(list[i].GetDataOnlyPosAndNormal());
                    meshData.indices.Add((uint)meshData.uniqueVertices.Count - 1);
                    hash.Add(vh, (uint)meshData.uniqueVertices.Count - 1);
                    meshData.Bounds.Enclose(list[i]);
                }
                else
                {
                    meshData.indices.Add(hash[vh]);
                }
            }

            meshData.CalculateGroupedIndices();

            ModelData model = new ModelData();
            model.meshes.Add(meshData);
            return model;
        }

        public static ModelData GetUnitCapsule(float radius = 0.5f, float halfHeight = 0.5f, int resolution = 10,
                                                                                   float r = 1.0f, float g = 1.0f, float b = 1.0f, float a = 1.0f)
        {
            Color4 c = new Color4(r, g, b, a);

            MeshData meshData = new MeshData();

            // Validate inputs
            if (radius <= 0 || resolution < 3)
                throw new ArgumentException("Invalid radius or resolution.");

            List<Vertex> list = new List<Vertex>();
            // Generate the top and bottom hemispheres
            for (int i = 0; i < resolution; i++)
            {
                for (int j = 0; j < resolution / 2; j++) // Only half the resolution for hemispheres
                {
                    float u1 = i / (float)resolution * MathF.PI * 2;
                    float u2 = (i + 1) / (float)resolution * MathF.PI * 2;
                    float v1 = j / (float)resolution * MathF.PI;
                    float v2 = (j + 1) / (float)resolution * MathF.PI;

                    Vector3 p1 = new Vector3(
                        halfHeight + radius * MathF.Cos(v1),
                        radius * MathF.Sin(v1) * MathF.Cos(u1),
                        radius * MathF.Sin(v1) * MathF.Sin(u1));

                    Vector3 p2 = new Vector3(
                        halfHeight + radius * MathF.Cos(v1),
                        radius * MathF.Sin(v1) * MathF.Cos(u2),
                        radius * MathF.Sin(v1) * MathF.Sin(u2));

                    Vector3 p3 = new Vector3(
                        halfHeight + radius * MathF.Cos(v2),
                        radius * MathF.Sin(v2) * MathF.Cos(u1),
                        radius * MathF.Sin(v2) * MathF.Sin(u1));

                    Vector3 p4 = new Vector3(
                        halfHeight + radius * MathF.Cos(v2),
                        radius * MathF.Sin(v2) * MathF.Cos(u2),
                        radius * MathF.Sin(v2) * MathF.Sin(u2));

                    AddToVertexList(ref list, new Vector3[] { p1, p3, p2 }, c);
                    AddToVertexList(ref list, new Vector3[] { p2, p3, p4 }, c);

                    // Back hemisphere (invert the x-coordinates)
                    Vector3 p1b = new Vector3(-p1.X, p1.Y, p1.Z);
                    Vector3 p2b = new Vector3(-p2.X, p2.Y, p2.Z);
                    Vector3 p3b = new Vector3(-p3.X, p3.Y, p3.Z);
                    Vector3 p4b = new Vector3(-p4.X, p4.Y, p4.Z);

                    AddToVertexList(ref list, new Vector3[] { p1b, p2b, p3b }, c);
                    AddToVertexList(ref list, new Vector3[] { p2b, p4b, p3b }, c);
                }
            }

            // Generate the cylindrical segment
            for (int i = 0; i < resolution; i++)
            {
                float u1 = i / (float)resolution * MathF.PI * 2;
                float u2 = (i + 1) / (float)resolution * MathF.PI * 2;

                // Creating vertices for the cylinder
                Vector3 p1 = new Vector3(halfHeight, radius * MathF.Cos(u1), radius * MathF.Sin(u1));
                Vector3 p2 = new Vector3(halfHeight, radius * MathF.Cos(u2), radius * MathF.Sin(u2));
                Vector3 p3 = new Vector3(-halfHeight, p1.Y, p1.Z);
                Vector3 p4 = new Vector3(-halfHeight, p2.Y, p2.Z);

                AddToVertexList(ref list, new Vector3[] { p1, p3, p2 }, c);
                AddToVertexList(ref list, new Vector3[] { p2, p3, p4 }, c);
            }

            Dictionary<int, uint> hash = new Dictionary<int, uint>();
            for (int i = 0; i < list.Count; i++)
            {
                int vh = list[i].GetHashCode();
                if (!hash.ContainsKey(vh))
                {
                    meshData.uniqueVertices.Add(list[i]);
                    meshData.visibleVerticesData.AddRange(list[i].GetData());
                    meshData.visibleVerticesDataOnlyPos.AddRange(list[i].GetDataOnlyPos());
                    meshData.visibleVerticesDataOnlyPosAndNormal.AddRange(list[i].GetDataOnlyPosAndNormal());
                    meshData.indices.Add((uint)meshData.uniqueVertices.Count - 1);
                    hash.Add(vh, (uint)meshData.uniqueVertices.Count - 1);
                    meshData.Bounds.Enclose(list[i]);
                }
                else
                {
                    meshData.indices.Add(hash[vh]);
                }
            }

            meshData.CalculateGroupedIndices();

            ModelData model = new ModelData();
            model.meshes.Add(meshData);
            return model;
        }
        #endregion

    }
}
