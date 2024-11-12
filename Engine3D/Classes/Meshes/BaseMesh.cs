using Assimp;
using FontStashSharp;
using MagicPhysX;
using Newtonsoft.Json;
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
        public int vaoId;
        public int vboId;
        public int shaderProgramId;

        protected string modelName_;
        [JsonIgnore]
        public string modelName
        {
            get { return modelName_; }
            set
            {
                string relativePath = AssetManager.GetRelativeModelsFolder(value);
                modelPath = relativePath;
                modelName_ = Path.GetFileName(modelPath);
                ProcessObj(modelPath);


                ComputeNormalsIfNeeded();

                ComputeTangents();
                recalculate = true;
            }
        }
        public string modelPath;

        public bool alwaysVisible = false;

        [JsonIgnore]
        private bool _recalculate = false;
        [JsonIgnore]
        public bool recalculate
        {
            get { return _recalculate; }
            set { _recalculate = value; recalculateOnlyPos = true; recalculateOnlyPosAndNormal = true; }
        }
        [JsonIgnore]
        protected bool recalculateOnlyPos;
        [JsonIgnore]
        protected bool recalculateOnlyPosAndNormal;

        protected int useBillboarding_ = 0;
        [JsonIgnore]
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

        [JsonIgnore]
        public Object parentObject;

        #region Texture
        [JsonIgnore]
        public Texture? texture;
        [JsonIgnore]
        public Texture? textureNormal;
        [JsonIgnore]
        public Texture? textureHeight;
        [JsonIgnore]
        public Texture? textureAO;
        [JsonIgnore]
        public Texture? textureRough;
        [JsonIgnore]
        public Texture? textureMetal;

        public string _textureName;
        [JsonIgnore]
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
                _textureName = value;
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
        public string _textureNormalName;
        [JsonIgnore]
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
                _textureNormalName = value;
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
        public string _textureHeightName;
        [JsonIgnore]
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
                _textureHeightName = value;
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
        public string _textureAOName;
        [JsonIgnore]
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
                _textureAOName = value;
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
        public string _textureRoughName;
        [JsonIgnore]
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
                _textureRoughName = value;
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
        public string _textureMetalName;
        [JsonIgnore]
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
                _textureMetalName = value;
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

        [JsonIgnore]
        public Camera camera;

        [JsonIgnore]
        public Dictionary<string, int> uniformLocations = new Dictionary<string, int>();
        [JsonIgnore]
        public Dictionary<string, int> uniformAnimLocations = new Dictionary<string, int>();

        [JsonIgnore]
        public BVH? BVHStruct;

        [JsonIgnore]
        protected Matrix4 scaleMatrix = Matrix4.Identity;
        [JsonIgnore]
        protected Matrix4 rotationMatrix = Matrix4.Identity;
        [JsonIgnore]
        protected Matrix4 translationMatrix = Matrix4.Identity;
        [JsonIgnore]
        public Matrix4 modelMatrix = Matrix4.Identity;

        //protected int threadSize;
        //private int desiredPercentage = 80;
        public static int threadSize = 32;

        protected BaseMesh()
        {
            
        }

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
            if(uniformLocations.ContainsKey(name1))
                GL.Uniform1(uniformLocations[name1], 0);
            if(uniformLocations.ContainsKey(name2))
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
            model.meshes.Clear();
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

        public void CalculateFrustumVisibility(bool allVisible=false, bool globalPosition=false)
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
                    if (allVisible || alwaysVisible)
                    {
                        AllIndicesVisible();
                    }
                    else
                    {
                        foreach (MeshData mesh in model.meshes)
                        {
                            mesh.visibleIndices.Clear();

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
                                        Vector3 p = AHelp.AssimpToOpenTK(mesh.mesh.Vertices[(int)indices_[i]]);
                                        if (!globalPosition)
                                            p = Vector3.TransformPosition(AHelp.AssimpToOpenTK(mesh.mesh.Vertices[(int)indices_[i]]), modelMatrix);
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
                                        if (camera.frustum.IsInside(AHelp.AssimpToOpenTK(mesh.mesh.Vertices[(int)indices_[i]])) || camera.IsPointClose(AHelp.AssimpToOpenTK(mesh.mesh.Vertices[(int)indices_[i]])))
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
            else if(GetType() == typeof(Gizmo))
            {
                if (allVisible || alwaysVisible)
                {
                    AllIndicesVisible();
                }
                else
                {
                    foreach (MeshData mesh in model.meshes)
                    {
                        mesh.visibleIndices.Clear();

                        bool visible = false;
                        foreach (Vector3D v in mesh.mesh.Vertices)
                        {
                            if (camera.frustum.IsInside(AHelp.AssimpToOpenTK(v)) || camera.IsPointClose(AHelp.AssimpToOpenTK(v)))
                            {
                                visible = true;
                                break;
                            }
                        }
                        if (visible)
                            AllIndicesVisible();
                    }
                }
            }
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
            foreach (MeshData meshData in model.meshes)
            {
                Assimp.Mesh mesh = meshData.mesh;
                // Clear any existing normals if present
                if (mesh.HasNormals)
                {
                    mesh.Normals.Clear();
                }

                // Loop through each vertex in the mesh
                for (int i = 0; i < mesh.Vertices.Count; i++)
                {
                    // Get the vertex position
                    Vector3D position = mesh.Vertices[i];

                    // Compute the spherical normal by normalizing the position
                    Vector3D sphericalNormal = position;
                    sphericalNormal.Normalize();

                    // Store the computed normal in the mesh
                    mesh.Normals.Add(sphericalNormal);
                }
            }
        }

        public void ComputeNormalsIfNeeded()
        {
            foreach (MeshData meshData in model.meshes)
            {
                //if (meshData.mesh.HasNormals)
                //    continue;

                Assimp.Mesh mesh = meshData.mesh;

                meshData.visibleVerticesData.Clear();
                meshData.visibleVerticesDataOnlyPos.Clear();
                meshData.visibleVerticesDataOnlyPosAndNormal.Clear();
                bool anim = false;
                if (meshData.visibleVerticesDataWithAnim.Count > 0)
                {
                    meshData.visibleVerticesDataWithAnim.Clear();
                    anim = true;
                }

                Dictionary<Vector3, Vector3> vertexNormals = new Dictionary<Vector3, Vector3>();
                Dictionary<Vector3, int> vertexNormalsCounts = new Dictionary<Vector3, int>();

                int[] indices = mesh.GetIndices();
                for (int i = 0; i < indices.Length; i += 3)
                {
                    Vector3D faceNormal = ComputeFaceNormal(mesh.Vertices[indices[i]],
                                                           mesh.Vertices[indices[i + 1]],
                                                           mesh.Vertices[indices[i + 2]]);
                    for (int j = 0; j < 3; j++)
                    {
                        if (!vertexNormals.ContainsKey(AHelp.AssimpToOpenTK(mesh.Vertices[indices[i + j]])))
                        {
                            vertexNormals[AHelp.AssimpToOpenTK(mesh.Vertices[indices[i + j]])] = AHelp.AssimpToOpenTK(faceNormal);
                            vertexNormalsCounts[AHelp.AssimpToOpenTK(mesh.Vertices[indices[i + j]])] = 1;
                        }
                        else
                        {
                            vertexNormals[AHelp.AssimpToOpenTK(mesh.Vertices[indices[i + j]])] += AHelp.AssimpToOpenTK(faceNormal);
                            vertexNormalsCounts[AHelp.AssimpToOpenTK(mesh.Vertices[indices[i + j]])]++;
                        }
                    }
                }

                mesh.Normals.Clear();
                for (int i = 0; i < mesh.Vertices.Count; i++)
                {
                    Vector3 n = (vertexNormals[AHelp.AssimpToOpenTK(mesh.Vertices[i])] / vertexNormalsCounts[AHelp.AssimpToOpenTK(mesh.Vertices[i])]).Normalized();
                    mesh.Normals.Add(AHelp.OpenTKToAssimp(n));
                }

                meshData.visibleVerticesData.AddRange(BaseMesh.GetMeshData(mesh));
                if (anim)
                    meshData.visibleVerticesDataWithAnim.AddRange(BaseMesh.GetMeshDataWithAnim(mesh));
                meshData.visibleVerticesDataOnlyPos.AddRange(BaseMesh.GetMeshDataOnlyPos(mesh));
                meshData.visibleVerticesDataOnlyPosAndNormal.AddRange(BaseMesh.GetMeshDataOnlyPosAndNormal(mesh));
            }
        }

        protected Vector3D ComputeFaceNormal(Vector3D p1, Vector3D p2, Vector3D p3)
        {
            Vector3D edge1 = p2 - p1;
            Vector3D edge2 = p3 - p1;
            Vector3D normal = Vector3D.Cross(edge1, edge2);
            normal.Normalize();
            return normal;
        }

        public void ComputeNormalsIfNeededFlat()
        {
            foreach(MeshData mesh in model.meshes)
            {
                // Check if the mesh already has normals
                if (mesh.mesh.HasNormals)
                {
                    continue; // Skip this mesh if normals already exist
                }

                // If not, we need to compute the normals
                // Initialize an empty list of normals for the mesh
                mesh.mesh.Normals.Clear();

                // Initialize a dictionary to accumulate face normals for each vertex
                Dictionary<int, List<Vector3D>> vertexNormalSums = new Dictionary<int, List<Vector3D>>();

                // Iterate through each face (triangle) in the mesh
                for (int i = 0; i < mesh.mesh.Faces.Count; i++)
                {
                    var face = mesh.mesh.Faces[i];
                    if (face.Indices.Count != 3) continue; // Only process triangles

                    // Get the vertices of the triangle
                    Vector3D p0 = mesh.mesh.Vertices[face.Indices[0]];
                    Vector3D p1 = mesh.mesh.Vertices[face.Indices[1]];
                    Vector3D p2 = mesh.mesh.Vertices[face.Indices[2]];

                    // Compute the face normal (cross product of two edges of the triangle)
                    Vector3D edge1 = p1 - p0;
                    Vector3D edge2 = p2 - p0;
                    Vector3D faceNormal = Vector3D.Cross(edge1, edge2);
                    faceNormal.Normalize();

                    // Accumulate the face normal for each vertex of the triangle
                    for (int j = 0; j < 3; j++)
                    {
                        int vertexIndex = face.Indices[j];

                        if (!vertexNormalSums.ContainsKey(vertexIndex))
                        {
                            vertexNormalSums[vertexIndex] = new List<Vector3D>();
                        }

                        vertexNormalSums[vertexIndex].Add(faceNormal);
                    }
                }

                // Compute the average normal for each vertex and normalize it
                for (int i = 0; i < mesh.mesh.Vertices.Count; i++)
                {
                    if (vertexNormalSums.ContainsKey(i))
                    {
                        Vector3D averageNormal = Average(vertexNormalSums[i]);
                        averageNormal.Normalize();
                        mesh.mesh.Normals.Add(averageNormal); // Add the computed normal to the mesh
                    }
                    else
                    {
                        // If no normal has been computed for the vertex, add a default normal
                        mesh.mesh.Normals.Add(new Vector3D(0, 0, 1)); // Upward normal as a fallback
                    }
                }
            }
        }


        public void ComputeTangents()
        {
            foreach (MeshData mesh in model.meshes)
            {
                // Check if tangents are already available
                if (mesh.mesh.HasTangentBasis)
                {
                    continue; // Skip the calculation if tangents already exist
                }

                // Ensure there are texture coordinates, as tangents depend on them
                if (!mesh.mesh.HasTextureCoords(0))
                {
                    return;
                    //throw new InvalidOperationException("Mesh does not have texture coordinates. Tangents cannot be computed.");
                }

                // Initialize tangent and bitangent lists with zeros
                Dictionary<int, List<Vector3D>> tangentSums = new Dictionary<int, List<Vector3D>>();
                Dictionary<int, List<Vector3D>> bitangentSums = new Dictionary<int, List<Vector3D>>();

                // Iterate through each face (triangle) in the mesh
                for (int i = 0; i < mesh.mesh.Faces.Count; i++)
                {
                    var face = mesh.mesh.Faces[i];
                    if (face.Indices.Count != 3) continue; // Only process triangles

                    // Get the vertices and texture coordinates of the triangle
                    Vector3D p0 = mesh.mesh.Vertices[face.Indices[0]];
                    Vector3D p1 = mesh.mesh.Vertices[face.Indices[1]];
                    Vector3D p2 = mesh.mesh.Vertices[face.Indices[2]];

                    Vector3D uv0 = mesh.mesh.TextureCoordinateChannels[0][face.Indices[0]];
                    Vector3D uv1 = mesh.mesh.TextureCoordinateChannels[0][face.Indices[1]];
                    Vector3D uv2 = mesh.mesh.TextureCoordinateChannels[0][face.Indices[2]];

                    // Compute the edges of the triangle in both object space and texture space
                    Vector3D edge1 = p1 - p0;
                    Vector3D edge2 = p2 - p0;

                    Vector3D deltaUV1 = uv1 - uv0;
                    Vector3D deltaUV2 = uv2 - uv0;

                    float d = deltaUV1.X * deltaUV2.Y - deltaUV2.X * deltaUV1.Y;
                    float f = d != 0.0f ? 1.0f / d : 0.0f;

                    // Calculate tangent and bitangent
                    Vector3D tangent = new Vector3D(
                        f * (deltaUV2.Y * edge1.X - deltaUV1.Y * edge2.X),
                        f * (deltaUV2.Y * edge1.Y - deltaUV1.Y * edge2.Y),
                        f * (deltaUV2.Y * edge1.Z - deltaUV1.Y * edge2.Z)
                    );

                    Vector3D bitangent = new Vector3D(
                        f * (-deltaUV2.X * edge1.X + deltaUV1.X * edge2.X),
                        f * (-deltaUV2.X * edge1.Y + deltaUV1.X * edge2.Y),
                        f * (-deltaUV2.X * edge1.Z + deltaUV1.X * edge2.Z)
                    );

                    // Store the tangent and bitangent for each vertex
                    for (int j = 0; j < 3; j++)
                    {
                        int vertexIndex = face.Indices[j];

                        if (!tangentSums.ContainsKey(vertexIndex))
                        {
                            tangentSums[vertexIndex] = new List<Vector3D>();
                            bitangentSums[vertexIndex] = new List<Vector3D>();
                        }

                        tangentSums[vertexIndex].Add(tangent);
                        bitangentSums[vertexIndex].Add(bitangent);
                    }
                }

                // Average and normalize tangents and bitangents
                for (int i = 0; i < mesh.mesh.Vertices.Count; i++)
                {
                    Vector3D avgTangent = Average(tangentSums[i]);
                    avgTangent.Normalize();
                    Vector3D avgBitangent = Average(bitangentSums[i]);
                    avgBitangent.Normalize();

                    // Assign the computed tangents and bitangents to the mesh
                    mesh.mesh.Tangents.Add(avgTangent);
                    mesh.mesh.BiTangents.Add(avgBitangent);
                }
            }
        }

        // Helper function to calculate the average of a list of vectors
        private Vector3D Average(List<Vector3D> vectors)
        {
            Vector3D sum = new Vector3D(0, 0, 0);
            foreach (var vec in vectors)
            {
                sum += vec;
            }
            return sum / vectors.Count;
        }

        public void ProcessObj(string relativeModelPath, float cr=1, float cg=1, float cb=1, float ca=1)
        {
            ModelData? md = Engine.assimpManager.ProcessModel(relativeModelPath, cr, cg, cb, ca);
            if (md != null)
            {
                model = md;
            }
        }

        public void AllIndicesVisible()
        {
            foreach (MeshData mesh in model.meshes)
                mesh.visibleIndices = mesh.mesh.GetIndices().Select(x => (uint)x).ToList();
        }

        #region GetMeshData methods
        public static float[] GetMeshData(Assimp.Mesh mesh)
        {
            // List to store all vertex data
            List<float> vertexData = new List<float>();

            // Loop through each vertex in the mesh
            for (int i = 0; i < mesh.Vertices.Count; i++)
            {
                // Get position (p.X, p.Y, p.Z)
                Vector3D position = mesh.Vertices[i];
                vertexData.Add(position.X);
                vertexData.Add(position.Y);
                vertexData.Add(position.Z);

                // Get normal (n.X, n.Y, n.Z) if the mesh has normals
                if (mesh.HasNormals)
                {
                    Vector3D normal = mesh.Normals[i];
                    vertexData.Add(normal.X);
                    vertexData.Add(normal.Y);
                    vertexData.Add(normal.Z);
                }
                else
                {
                    // Default normal values if missing
                    vertexData.Add(0);
                    vertexData.Add(0);
                    vertexData.Add(0);
                }

                // Get texture coordinates (t.u, t.v) if available
                if (mesh.HasTextureCoords(0))
                {
                    Vector3D texCoord = mesh.TextureCoordinateChannels[0][i];
                    vertexData.Add(texCoord.X); // u
                    vertexData.Add(texCoord.Y); // v
                }
                else
                {
                    // Default texture coordinate values if missing
                    vertexData.Add(0);
                    vertexData.Add(0);
                }

                // Get vertex color (c.R, c.G, c.B, c.A) if available
                if (mesh.HasVertexColors(0))
                {
                    Color4D color = mesh.VertexColorChannels[0][i];
                    vertexData.Add(color.R);
                    vertexData.Add(color.G);
                    vertexData.Add(color.B);
                    vertexData.Add(color.A);
                }
                else
                {
                    // Default color values if missing
                    vertexData.Add(1); // R
                    vertexData.Add(1); // G
                    vertexData.Add(1); // B
                    vertexData.Add(1); // A
                }

                // Get tangent (tan.X, tan.Y, tan.Z) if available
                if (mesh.HasTangentBasis)
                {
                    Vector3D tangent = mesh.Tangents[i];
                    vertexData.Add(tangent.X);
                    vertexData.Add(tangent.Y);
                    vertexData.Add(tangent.Z);
                }
                else
                {
                    // Default tangent values if missing
                    vertexData.Add(0);
                    vertexData.Add(0);
                    vertexData.Add(0);
                }
            }

            // Return the list as a float array
            return vertexData.ToArray();
        }

        public static float[] GetMeshDataWithAnim(Assimp.Mesh mesh)
        {
            // List to store all vertex data
            List<float> vertexData = new List<float>();

            // Dictionary to hold bone IDs and weights for each vertex
            var vertexBoneIDs = new Dictionary<int, List<int>>();
            var vertexBoneWeights = new Dictionary<int, List<float>>();

            // Initialize the dictionaries
            for (int i = 0; i < mesh.Vertices.Count; i++)
            {
                vertexBoneIDs[i] = new List<int> { 0, 0, 0, 0 };    // Assumes a maximum of 4 bones per vertex
                vertexBoneWeights[i] = new List<float> { 0.0f, 0.0f, 0.0f, 0.0f };
            }

            // Populate bone IDs and weights from mesh bones
            if (mesh.HasBones)
            {
                for (int boneIndex = 0; boneIndex < mesh.Bones.Count; boneIndex++)
                {
                    var bone = mesh.Bones[boneIndex];
                    foreach (var weight in bone.VertexWeights)
                    {
                        int vertexId = weight.VertexID;
                        float boneWeight = weight.Weight;

                        // Assign bone ID and weight to the first available slot (max 4 bones per vertex)
                        for (int i = 0; i < 4; i++)
                        {
                            if (vertexBoneWeights[vertexId][i] == 0.0f)
                            {
                                vertexBoneIDs[vertexId][i] = boneIndex; // Store bone index
                                vertexBoneWeights[vertexId][i] = boneWeight; // Store bone weight
                                break;
                            }
                        }
                    }
                }
            }

            // Loop through each vertex in the mesh
            for (int i = 0; i < mesh.Vertices.Count; i++)
            {
                // Get position (p.X, p.Y, p.Z)
                Vector3D position = mesh.Vertices[i];
                vertexData.Add(position.X);
                vertexData.Add(position.Y);
                vertexData.Add(position.Z);

                // Get normal (n.X, n.Y, n.Z) if the mesh has normals
                if (mesh.HasNormals)
                {
                    Vector3D normal = mesh.Normals[i];
                    vertexData.Add(normal.X);
                    vertexData.Add(normal.Y);
                    vertexData.Add(normal.Z);
                }
                else
                {
                    // Default normal values if missing
                    vertexData.Add(0);
                    vertexData.Add(0);
                    vertexData.Add(0);
                }

                // Get texture coordinates (t.u, t.v) if available
                if (mesh.HasTextureCoords(0))
                {
                    Vector3D texCoord = mesh.TextureCoordinateChannels[0][i];
                    vertexData.Add(texCoord.X); // u
                    vertexData.Add(texCoord.Y); // v
                }
                else
                {
                    // Default texture coordinate values if missing
                    vertexData.Add(0);
                    vertexData.Add(0);
                }

                // Get vertex color (c.R, c.G, c.B, c.A) if available
                if (mesh.HasVertexColors(0))
                {
                    Color4D color = mesh.VertexColorChannels[0][i];
                    vertexData.Add(color.R);
                    vertexData.Add(color.G);
                    vertexData.Add(color.B);
                    vertexData.Add(color.A);
                }
                else
                {
                    // Default color values if missing
                    vertexData.Add(1); // R
                    vertexData.Add(1); // G
                    vertexData.Add(1); // B
                    vertexData.Add(1); // A
                }

                // Get tangent (tan.X, tan.Y, tan.Z) if available
                if (mesh.HasTangentBasis)
                {
                    Vector3D tangent = mesh.Tangents[i];
                    vertexData.Add(tangent.X);
                    vertexData.Add(tangent.Y);
                    vertexData.Add(tangent.Z);
                }
                else
                {
                    // Default tangent values if missing
                    vertexData.Add(0);
                    vertexData.Add(0);
                    vertexData.Add(0);
                }

                // Add bone IDs (max 4)
                vertexData.AddRange(vertexBoneIDs[i].Take(4).Select(x => (float)x));

                // Add bone weights (max 4)
                vertexData.AddRange(vertexBoneWeights[i].Take(4));

                // Add bone count (the number of non-zero bone weights)
                int boneCount = vertexBoneWeights[i].Count(weight => weight > 0);
                vertexData.Add(boneCount);
            }

            // Return the list as a float array
            return vertexData.ToArray();
        }

        public static float[] GetMeshDataOnlyPos(Assimp.Mesh mesh)
        {
            // List to store all position data
            List<float> positionData = new List<float>();

            // Loop through each vertex in the mesh
            for (int i = 0; i < mesh.Vertices.Count; i++)
            {
                // Get position (p.X, p.Y, p.Z)
                Vector3D position = mesh.Vertices[i];
                positionData.Add(position.X);
                positionData.Add(position.Y);
                positionData.Add(position.Z);
            }

            // Return the list as a float array
            return positionData.ToArray();
        }

        public static float[] GetMeshDataOnlyPosAndNormal(Assimp.Mesh mesh)
        {
            // List to store all position and normal data
            List<float> data = new List<float>();

            // Loop through each vertex in the mesh
            for (int i = 0; i < mesh.Vertices.Count; i++)
            {
                // Get position (p.X, p.Y, p.Z)
                Vector3D position = mesh.Vertices[i];
                data.Add(position.X);
                data.Add(position.Y);
                data.Add(position.Z);

                // Get normal (n.X, n.Y, n.Z) if the mesh has normals
                if (mesh.HasNormals)
                {
                    Vector3D normal = mesh.Normals[i];
                    data.Add(normal.X);
                    data.Add(normal.Y);
                    data.Add(normal.Z);
                }
                else
                {
                    // If no normals exist, add default values (0, 0, 0)
                    data.Add(0);
                    data.Add(0);
                    data.Add(0);
                }
            }

            // Return the list as a float array
            return data.ToArray();
        }

        public static float[] GetMeshDataOnlyPosAndColor(Assimp.Mesh mesh)
        {
            // List to store all position and normal data
            List<float> data = new List<float>();

            // Loop through each vertex in the mesh
            for (int i = 0; i < mesh.Vertices.Count; i++)
            {
                // Get position (p.X, p.Y, p.Z)
                Vector3D position = mesh.Vertices[i];
                data.Add(position.X);
                data.Add(position.Y);
                data.Add(position.Z);

                // Get normal (n.X, n.Y, n.Z) if the mesh has normals
                if (mesh.HasVertexColors(0))
                {
                    Color4D color = mesh.VertexColorChannels[0][i];
                    data.Add(color.R);
                    data.Add(color.G);
                    data.Add(color.B);
                    data.Add(color.A);
                }
                else
                {
                    data.Add(1.0f);
                    data.Add(1.0f);
                    data.Add(1.0f);
                    data.Add(1.0f);
                }
            }

            // Return the list as a float array
            return data.ToArray();
        }
        #endregion

        #region SimpleMeshes
        public static ModelData GetUnitCube(float r = 1.0f, float g = 1.0f, float b = 1.0f, float a = 1.0f)
        {
            Color4D c = new Color4D(r, g, b, a);

            float halfSize = 0.5f;

            // Define cube vertices
            Vector3D p1 = new Vector3D(-halfSize, -halfSize, -halfSize);
            Vector3D p2 = new Vector3D(halfSize, -halfSize, -halfSize);
            Vector3D p3 = new Vector3D(halfSize, halfSize, -halfSize);
            Vector3D p4 = new Vector3D(-halfSize, halfSize, -halfSize);
            Vector3D p5 = new Vector3D(-halfSize, -halfSize, halfSize);
            Vector3D p6 = new Vector3D(halfSize, -halfSize, halfSize);
            Vector3D p7 = new Vector3D(halfSize, halfSize, halfSize);
            Vector3D p8 = new Vector3D(-halfSize, halfSize, halfSize);

            Vector3D t1 = new Vector3D(0, 0, 0); // Assimp uses 3D vectors for texcoords as well
            Vector3D t2 = new Vector3D(1, 0, 0);
            Vector3D t3 = new Vector3D(1, 1, 0);
            Vector3D t4 = new Vector3D(0, 1, 0);

            Vector3D normalBack = new Vector3D(0, 0, -1);  // Back face (-Z)
            Vector3D normalFront = new Vector3D(0, 0, 1);  // Front face (+Z)
            Vector3D normalLeft = new Vector3D(-1, 0, 0);  // Left face (-X)
            Vector3D normalRight = new Vector3D(1, 0, 0);  // Right face (+X)
            Vector3D normalTop = new Vector3D(0, 1, 0);    // Top face (+Y)
            Vector3D normalBottom = new Vector3D(0, -1, 0); // Bottom face (-Y)

            Assimp.Mesh assimpMesh = new Assimp.Mesh(Assimp.PrimitiveType.Triangle);

            List<Vector3D> vertices = new List<Vector3D>();
            List<Vector3D> normals = new List<Vector3D>();
            List<Vector3D> texCoords = new List<Vector3D>();
            List<Color4D> colors = new List<Color4D>();
            List<int> indices = new List<int>();

            void AddFace(Vector3D[] faceVertices, Vector3D[] faceNormals, Vector3D[] faceTexCoords, Color4D[] faceColors)
            {
                for (int i = 0; i < 3; i++)
                {
                    vertices.Add(faceVertices[i]);
                    normals.Add(faceNormals[i]);
                    texCoords.Add(faceTexCoords[i]);
                    colors.Add(faceColors[i]);
                    indices.Add(vertices.Count - 1); // Add index
                }
            }

            AddFace(new[] { p1, p3, p2 }, new[] { normalBack, normalBack, normalBack }, new[] { t1, t3, t2 }, new[] { c, c, c, c });
            AddFace(new[] { p3, p1, p4 }, new[] { normalBack, normalBack, normalBack }, new[] { t3, t1, t4 }, new[] { c, c, c, c });

            // Front face (+Z)
            AddFace(new[] { p5, p6, p7 }, new[] { normalFront, normalFront, normalFront }, new[] { t1, t2, t3 }, new[] { c, c, c, c });
            AddFace(new[] { p7, p8, p5 }, new[] { normalFront, normalFront, normalFront }, new[] { t3, t4, t1 }, new[] { c, c, c, c });

            // Left face (-X)
            AddFace(new[] { p1, p8, p4 }, new[] { normalLeft, normalLeft, normalLeft }, new[] { t1, t3, t2 }, new[] { c, c, c, c });
            AddFace(new[] { p8, p1, p5 }, new[] { normalLeft, normalLeft, normalLeft }, new[] { t3, t1, t4 }, new[] { c, c, c, c });

            // Right face (+X)
            AddFace(new[] { p2, p3, p7 }, new[] { normalRight, normalRight, normalRight }, new[] { t1, t2, t3 }, new[] { c, c, c, c });
            AddFace(new[] { p7, p6, p2 }, new[] { normalRight, normalRight, normalRight }, new[] { t3, t4, t1 }, new[] { c, c, c, c });

            // Top face (+Y)
            AddFace(new[] { p4, p7, p3 }, new[] { normalTop, normalTop, normalTop }, new[] { t1, t3, t2 }, new[] { c, c, c, c });
            AddFace(new[] { p7, p4, p8 }, new[] { normalTop, normalTop, normalTop }, new[] { t3, t1, t4 }, new[] { c, c, c, c });

            // Bottom face (-Y)
            AddFace(new[] { p1, p2, p6 }, new[] { normalBottom, normalBottom, normalBottom }, new[] { t1, t2, t3 }, new[] { c, c, c, c });
            AddFace(new[] { p6, p5, p1 }, new[] { normalBottom, normalBottom, normalBottom }, new[] { t3, t4, t1 }, new[] { c, c, c, c });

            assimpMesh.Vertices.AddRange(vertices);
            assimpMesh.Normals.AddRange(normals);
            assimpMesh.TextureCoordinateChannels[0].AddRange(texCoords);
            assimpMesh.VertexColorChannels[0].AddRange(colors);

            for (int i = 0; i < indices.Count; i += 3)
            {
                Face face = new Face();
                face.Indices.Add(indices[i]);
                face.Indices.Add(indices[i + 1]);
                face.Indices.Add(indices[i + 2]);
                assimpMesh.Faces.Add(face); // Add each face directly to the mesh's Faces collection
            }

            ModelData modelData = new ModelData();
            modelData.meshes.Add(new MeshData(assimpMesh));
            modelData.meshes[0].CalculateGroupedIndices();

            return modelData;
        }

        public static ModelData GetUnitFace(float r = 1.0f, float g = 1.0f, float b = 1.0f, float a = 1.0f)
        {
            Color4D c = new Color4D(r, g, b, a);

            float halfSize = 0.5f;

            // Define vertices for the front face
            Vector3D p5 = new Vector3D(-halfSize, -halfSize, halfSize);
            Vector3D p6 = new Vector3D(halfSize, -halfSize, halfSize);
            Vector3D p7 = new Vector3D(halfSize, halfSize, halfSize);
            Vector3D p8 = new Vector3D(-halfSize, halfSize, halfSize);

            // Texture coordinates
            Vector3D t1 = new Vector3D(0, 0, 0);
            Vector3D t2 = new Vector3D(1, 0, 0);
            Vector3D t3 = new Vector3D(1, 1, 0);
            Vector3D t4 = new Vector3D(0, 1, 0);

            // Create Assimp Mesh for a single face
            Assimp.Mesh assimpMesh = new Assimp.Mesh(Assimp.PrimitiveType.Triangle);

            List<Vector3D> vertices = new List<Vector3D>();
            List<Vector3D> texCoords = new List<Vector3D>();
            List<Color4D> colors = new List<Color4D>();
            List<int> indices = new List<int>();

            // Helper method to add vertices, texcoords, and indices to the mesh
            void AddFace(Vector3D[] faceVertices, Vector3D[] faceTexCoords, Color4D[] faceColors)
            {
                for (int i = 0; i < 3; i++)
                {
                    vertices.Add(faceVertices[i]);
                    texCoords.Add(faceTexCoords[i]);
                    colors.Add(faceColors[i]);
                    indices.Add(vertices.Count - 1); // Add index
                }
            }

            // Add front face triangles
            AddFace(new[] { p5, p6, p7 }, new[] { t1, t2, t3 }, new[] { c, c, c, c }); // First triangle
            AddFace(new[] { p7, p8, p5 }, new[] { t3, t4, t1 }, new[] { c, c, c, c }); // Second triangle

            // Populate Assimp mesh with vertices and texture coordinates
            assimpMesh.Vertices.AddRange(vertices);
            assimpMesh.TextureCoordinateChannels[0].AddRange(texCoords);

            // Now add the faces directly to the mesh
            for (int i = 0; i < indices.Count; i += 3)
            {
                Face face = new Face();
                face.Indices.Add(indices[i]);
                face.Indices.Add(indices[i + 1]);
                face.Indices.Add(indices[i + 2]);
                assimpMesh.Faces.Add(face); // Add each face directly to the mesh's Faces collection
            }

            ModelData modelData = new ModelData();
            modelData.meshes.Add(new MeshData(assimpMesh));
            modelData.meshes[0].CalculateGroupedIndices();

            return modelData;
        }

        public static ModelData GetUnitSphere(float radius = 1, int resolution = 10,
                                      float r = 1.0f, float g = 1.0f, float b = 1.0f, float a = 1.0f)
        {
            Color4D c = new Color4D(r, g, b, a);

            // Validate inputs
            if (radius <= 0 || resolution < 3)
                throw new ArgumentException("Invalid radius or resolution.");

            // Create Assimp Mesh for the sphere
            Assimp.Mesh assimpMesh = new Assimp.Mesh(Assimp.PrimitiveType.Triangle);

            List<Vector3D> vertices = new List<Vector3D>();
            List<Vector3D> normals = new List<Vector3D>();
            List<int> indices = new List<int>();
            List<Color4D> vertexColors = new List<Color4D>();
            List<Vector3D> texCoords = new List<Vector3D>();

            // Helper method to add vertices, normals, colors, texCoords, and indices to the mesh
            void AddTriangle(Vector3D p1, Vector3D p2, Vector3D p3, Vector3D uv1, Vector3D uv2, Vector3D uv3)
            {
                // Add vertices
                vertices.Add(p1);
                vertices.Add(p2);
                vertices.Add(p3);

                // Compute normals
                Vector3D normal1 = p1;
                Vector3D normal2 = p2;
                Vector3D normal3 = p3;
                normal1.Normalize();
                normal2.Normalize();
                normal3.Normalize();

                normals.Add(normal1);
                normals.Add(normal2);
                normals.Add(normal3);

                // Add vertex colors
                vertexColors.Add(c); // Color for p1
                vertexColors.Add(c); // Color for p2
                vertexColors.Add(c); // Color for p3

                // Add texture coordinates (TexUVs)
                texCoords.Add(uv1);
                texCoords.Add(uv2);
                texCoords.Add(uv3);

                // Add indices for the new triangle
                indices.Add(vertices.Count - 3);
                indices.Add(vertices.Count - 2);
                indices.Add(vertices.Count - 1);
            }

            // Generate vertices, normals, colors, texCoords, and triangles for the sphere
            for (int i = 0; i < resolution; i++)
            {
                for (int j = 0; j < resolution; j++)
                {
                    float u1 = i / (float)resolution * MathF.PI * 2;
                    float u2 = (i + 1) / (float)resolution * MathF.PI * 2;
                    float v1 = j / (float)resolution * MathF.PI;
                    float v2 = (j + 1) / (float)resolution * MathF.PI;

                    // Vertex positions
                    Vector3D p1 = new Vector3D(
                        radius * MathF.Sin(v1) * MathF.Cos(u1),
                        radius * MathF.Cos(v1),
                        radius * MathF.Sin(v1) * MathF.Sin(u1));

                    Vector3D p2 = new Vector3D(
                        radius * MathF.Sin(v1) * MathF.Cos(u2),
                        radius * MathF.Cos(v1),
                        radius * MathF.Sin(v1) * MathF.Sin(u2));

                    Vector3D p3 = new Vector3D(
                        radius * MathF.Sin(v2) * MathF.Cos(u1),
                        radius * MathF.Cos(v2),
                        radius * MathF.Sin(v2) * MathF.Sin(u1));

                    Vector3D p4 = new Vector3D(
                        radius * MathF.Sin(v2) * MathF.Cos(u2),
                        radius * MathF.Cos(v2),
                        radius * MathF.Sin(v2) * MathF.Sin(u2));

                    // Texture coordinates (u, v)
                    Vector3D uv1 = new Vector3D(i / (float)resolution, j / (float)resolution, 0);
                    Vector3D uv2 = new Vector3D((i + 1) / (float)resolution, j / (float)resolution, 0);
                    Vector3D uv3 = new Vector3D(i / (float)resolution, (j + 1) / (float)resolution, 0);
                    Vector3D uv4 = new Vector3D((i + 1) / (float)resolution, (j + 1) / (float)resolution, 0);

                    // Add two triangles for each quad
                    AddTriangle(p1, p2, p3, uv1, uv2, uv3); // First triangle
                    AddTriangle(p2, p4, p3, uv2, uv4, uv3); // Second triangle
                }
            }

            // Populate Assimp mesh with vertices, normals, colors, and texCoords
            assimpMesh.Vertices.AddRange(vertices);
            assimpMesh.Normals.AddRange(normals);
            assimpMesh.VertexColorChannels[0].AddRange(vertexColors);
            assimpMesh.TextureCoordinateChannels[0].AddRange(texCoords);

            // Add faces (triangles)
            for (int i = 0; i < indices.Count; i += 3)
            {
                Face face = new Face();
                face.Indices.Add(indices[i]);
                face.Indices.Add(indices[i + 1]);
                face.Indices.Add(indices[i + 2]);
                assimpMesh.Faces.Add(face);
            }

            // Create a new ModelData object and add the mesh to it
            ModelData modelData = new ModelData();
            modelData.meshes.Add(new MeshData(assimpMesh));
            modelData.meshes[0].CalculateGroupedIndices();

            return modelData;
        }

        public static ModelData GetUnitCapsule(float radius = 0.5f, float halfHeight = 0.5f, int resolution = 10,
                                       float r = 1.0f, float g = 1.0f, float b = 1.0f, float a = 1.0f)
        {
            Color4D color = new Color4D(r, g, b, a);

            // Validate inputs
            if (radius <= 0 || resolution < 3)
                throw new ArgumentException("Invalid radius or resolution.");

            // Create Assimp Mesh for the capsule
            Assimp.Mesh assimpMesh = new Assimp.Mesh(Assimp.PrimitiveType.Triangle);

            List<Vector3D> vertices = new List<Vector3D>();
            List<Vector3D> normals = new List<Vector3D>();
            List<int> indices = new List<int>();
            List<Color4D> vertexColors = new List<Color4D>();
            List<Vector3D> texCoords = new List<Vector3D>();

            // Helper method to add vertices, normals, colors, texCoords, and indices to the mesh
            void AddTriangle(Vector3D p1, Vector3D p2, Vector3D p3, Vector3D uv1, Vector3D uv2, Vector3D uv3)
            {
                vertices.Add(p1);
                vertices.Add(p2);
                vertices.Add(p3);

                Vector3D normal1 = p1;
                Vector3D normal2 = p2;
                Vector3D normal3 = p3;

                normal1.Normalize();
                normal2.Normalize();
                normal3.Normalize();

                normals.Add(normal1);
                normals.Add(normal2);
                normals.Add(normal3);

                vertexColors.Add(color); // Add vertex color for p1
                vertexColors.Add(color); // Add vertex color for p2
                vertexColors.Add(color); // Add vertex color for p3

                texCoords.Add(uv1); // Add texture coordinates for p1
                texCoords.Add(uv2); // Add texture coordinates for p2
                texCoords.Add(uv3); // Add texture coordinates for p3

                indices.Add(vertices.Count - 3); // Add indices for the new triangle
                indices.Add(vertices.Count - 2);
                indices.Add(vertices.Count - 1);
            }

            // Generate the top and bottom hemispheres
            for (int i = 0; i < resolution; i++)
            {
                for (int j = 0; j < resolution / 2; j++) // Only half the resolution for hemispheres
                {
                    float u1 = i / (float)resolution * MathF.PI * 2;
                    float u2 = (i + 1) / (float)resolution * MathF.PI * 2;
                    float v1 = j / (float)resolution * MathF.PI;
                    float v2 = (j + 1) / (float)resolution * MathF.PI;

                    // Top hemisphere
                    Vector3D p1 = new Vector3D(
                        halfHeight + radius * MathF.Cos(v1),
                        radius * MathF.Sin(v1) * MathF.Cos(u1),
                        radius * MathF.Sin(v1) * MathF.Sin(u1));

                    Vector3D p2 = new Vector3D(
                        halfHeight + radius * MathF.Cos(v1),
                        radius * MathF.Sin(v1) * MathF.Cos(u2),
                        radius * MathF.Sin(v1) * MathF.Sin(u2));

                    Vector3D p3 = new Vector3D(
                        halfHeight + radius * MathF.Cos(v2),
                        radius * MathF.Sin(v2) * MathF.Cos(u1),
                        radius * MathF.Sin(v2) * MathF.Sin(u1));

                    Vector3D p4 = new Vector3D(
                        halfHeight + radius * MathF.Cos(v2),
                        radius * MathF.Sin(v2) * MathF.Cos(u2),
                        radius * MathF.Sin(v2) * MathF.Sin(u2));

                    // Texture coordinates (u, v)
                    Vector3D uv1 = new Vector3D(i / (float)resolution, j / (float)resolution, 0);
                    Vector3D uv2 = new Vector3D((i + 1) / (float)resolution, j / (float)resolution, 0);
                    Vector3D uv3 = new Vector3D(i / (float)resolution, (j + 1) / (float)resolution, 0);
                    Vector3D uv4 = new Vector3D((i + 1) / (float)resolution, (j + 1) / (float)resolution, 0);

                    AddTriangle(p1, p3, p2, uv1, uv3, uv2); // First triangle
                    AddTriangle(p2, p3, p4, uv2, uv3, uv4); // Second triangle

                    // Bottom hemisphere (invert the X-coordinates)
                    Vector3D p1b = new Vector3D(-p1.X, p1.Y, p1.Z);
                    Vector3D p2b = new Vector3D(-p2.X, p2.Y, p2.Z);
                    Vector3D p3b = new Vector3D(-p3.X, p3.Y, p3.Z);
                    Vector3D p4b = new Vector3D(-p4.X, p4.Y, p4.Z);

                    AddTriangle(p1b, p2b, p3b, uv1, uv2, uv3); // First bottom triangle
                    AddTriangle(p2b, p4b, p3b, uv2, uv4, uv3); // Second bottom triangle
                }
            }

            // Generate the cylindrical segment
            for (int i = 0; i < resolution; i++)
            {
                float u1 = i / (float)resolution * MathF.PI * 2;
                float u2 = (i + 1) / (float)resolution * MathF.PI * 2;

                // Creating vertices for the cylinder
                Vector3D p1 = new Vector3D(halfHeight, radius * MathF.Cos(u1), radius * MathF.Sin(u1));
                Vector3D p2 = new Vector3D(halfHeight, radius * MathF.Cos(u2), radius * MathF.Sin(u2));
                Vector3D p3 = new Vector3D(-halfHeight, p1.Y, p1.Z);
                Vector3D p4 = new Vector3D(-halfHeight, p2.Y, p2.Z);

                // Texture coordinates for cylinder segment
                Vector3D uv1 = new Vector3D(i / (float)resolution, 0, 0);
                Vector3D uv2 = new Vector3D((i + 1) / (float)resolution, 0, 0);
                Vector3D uv3 = new Vector3D(i / (float)resolution, 1, 0);
                Vector3D uv4 = new Vector3D((i + 1) / (float)resolution, 1, 0);

                AddTriangle(p1, p3, p2, uv1, uv3, uv2); // First triangle for the cylinder
                AddTriangle(p2, p3, p4, uv2, uv3, uv4); // Second triangle for the cylinder
            }

            // Populate Assimp mesh with vertices, normals, colors, and texCoords
            assimpMesh.Vertices.AddRange(vertices);
            assimpMesh.Normals.AddRange(normals);
            assimpMesh.VertexColorChannels[0].AddRange(vertexColors);
            assimpMesh.TextureCoordinateChannels[0].AddRange(texCoords);

            // Add faces
            for (int i = 0; i < indices.Count; i += 3)
            {
                Face face = new Face();
                face.Indices.Add(indices[i]);
                face.Indices.Add(indices[i + 1]);
                face.Indices.Add(indices[i + 2]);
                assimpMesh.Faces.Add(face);
            }

            // Create a new ModelData object and add the Assimp mesh to it
            ModelData modelData = new ModelData();
            modelData.meshes.Add(new MeshData(assimpMesh));
            modelData.meshes[0].CalculateGroupedIndices();

            return modelData;
        }

        #endregion

    }
}
