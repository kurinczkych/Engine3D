using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine3D
{
    public class InstancedMeshData
    {
        public Vector3 Position = Vector3.Zero;
        public Quaternion Rotation = Quaternion.Identity;
        public Vector3 Scale = Vector3.One;
        public Color4 Color = Color4.White;

        public InstancedMeshData() { }

        public InstancedMeshData(Vector3 pos, Quaternion rot, Vector3 scale, Color4 color)
        {
            Position = pos;
            Rotation = rot;
            Scale = scale;
            Color = color;
        }
    }

    public class InstancedMesh : BaseMesh
    {
        public static int floatCount = 16;
        public static int instancedFloatCount = 15;

        private List<float> vertices = new List<float>();
        private List<float> instancedVertices = new List<float>();

        private Vector2 windowSize;

        Matrix4 viewMatrix, projectionMatrix;

        private InstancedVAO Vao;
        private VBO Vbo;

        public List<InstancedMeshData> instancedData = new List<InstancedMeshData>();

        public InstancedMesh(InstancedVAO vao, VBO vbo, int shaderProgramId, string modelName, string textureName, Vector2 windowSize, ref Camera camera, ref Object parentObject) : base(vao.id, vbo.id, shaderProgramId)
        {
            this.parentObject = parentObject;

            parentObject.texture = Engine.textureManager.AddTexture(textureName);

            Vao = vao;
            Vbo = vbo;

            this.windowSize = windowSize;
            this.camera = camera;

            this.modelName = modelName;
            ProcessObj(modelName);

            ComputeVertexNormals();
            ComputeTangents();

            GetUniformLocations();
            SendUniforms();
        }

        public InstancedMesh(InstancedVAO vao, VBO vbo, int shaderProgramId, string modelName, List<triangle> tris, string textureName, Vector2 windowSize, ref Camera camera, ref Object parentObject) : base(vao.id, vbo.id, shaderProgramId)
        {
            this.parentObject = parentObject;

            parentObject.texture = Engine.textureManager.AddTexture(textureName);

            Vao = vao;
            Vbo = vbo;

            this.windowSize = windowSize;
            this.camera = camera;

            this.modelName = modelName;
            this.tris = new List<triangle>(tris);

            ComputeVertexNormals();
            ComputeTangents();

            GetUniformLocations();
            SendUniforms();
        }

        public InstancedMesh(InstancedVAO vao, VBO vbo, int shaderProgramId, string modelName, List<triangle> tris, Vector2 windowSize, ref Camera camera, ref Object parentObject) : base(vao.id, vbo.id, shaderProgramId)
        {
            this.parentObject = parentObject;

            Vao = vao;
            Vbo = vbo;

            this.windowSize = windowSize;
            this.camera = camera;

            this.modelName = modelName;
            this.tris = new List<triangle>(tris);

            ComputeVertexNormals();
            ComputeTangents();

            GetUniformLocations();
            SendUniforms();
        }

        private void GetUniformLocations()
        {
            uniformLocations.Add("windowSize", GL.GetUniformLocation(shaderProgramId, "windowSize"));
            uniformLocations.Add("modelMatrix", GL.GetUniformLocation(shaderProgramId, "modelMatrix"));
            uniformLocations.Add("viewMatrix", GL.GetUniformLocation(shaderProgramId, "viewMatrix"));
            uniformLocations.Add("projectionMatrix", GL.GetUniformLocation(shaderProgramId, "projectionMatrix"));
            uniformLocations.Add("cameraPosition", GL.GetUniformLocation(shaderProgramId, "cameraPosition"));
            uniformLocations.Add("useBillboarding", GL.GetUniformLocation(shaderProgramId, "useBillboarding"));
            uniformLocations.Add("useTexture", GL.GetUniformLocation(shaderProgramId, "useTexture"));
            if (parentObject.texture != null)
            {
                uniformLocations.Add("textureSampler", GL.GetUniformLocation(shaderProgramId, "textureSampler"));
                if (parentObject.textureNormal != null)
                {
                    uniformLocations.Add("textureSamplerNormal", GL.GetUniformLocation(shaderProgramId, "textureSamplerNormal"));
                    uniformLocations.Add("useNormal", GL.GetUniformLocation(shaderProgramId, "useNormal"));
                }
                if (parentObject.textureHeight != null)
                {
                    uniformLocations.Add("textureSamplerHeight", GL.GetUniformLocation(shaderProgramId, "textureSamplerHeight"));
                    uniformLocations.Add("useHeight", GL.GetUniformLocation(shaderProgramId, "useHeight"));
                }
                if (parentObject.textureAO != null)
                {
                    uniformLocations.Add("textureSamplerAO", GL.GetUniformLocation(shaderProgramId, "textureSamplerAO"));
                    uniformLocations.Add("useAO", GL.GetUniformLocation(shaderProgramId, "useAO"));
                }
                if (parentObject.textureRough != null)
                {
                    uniformLocations.Add("textureSamplerRough", GL.GetUniformLocation(shaderProgramId, "textureSamplerRough"));
                    uniformLocations.Add("useRough", GL.GetUniformLocation(shaderProgramId, "useRough"));
                }
                if (parentObject.textureMetal != null)
                {
                    uniformLocations.Add("textureSamplerMetal", GL.GetUniformLocation(shaderProgramId, "textureSamplerMetal"));
                    uniformLocations.Add("useMetal", GL.GetUniformLocation(shaderProgramId, "useMetal"));
                }
            }
        }

        protected override void SendUniforms()
        {
            projectionMatrix = camera.projectionMatrix;
            viewMatrix = camera.viewMatrix;

            GL.UniformMatrix4(uniformLocations["modelMatrix"], true, ref modelMatrix);
            GL.UniformMatrix4(uniformLocations["viewMatrix"], true, ref viewMatrix);
            GL.UniformMatrix4(uniformLocations["projectionMatrix"], true, ref projectionMatrix);
            GL.Uniform2(uniformLocations["windowSize"], windowSize);
            GL.Uniform3(uniformLocations["cameraPosition"], camera.GetPosition());
            GL.Uniform1(uniformLocations["useBillboarding"], useBillboarding);
            GL.Uniform1(uniformLocations["useTexture"], parentObject.texture != null ? 1 : 0);
            if (parentObject.texture != null)
            {
                GL.Uniform1(uniformLocations["textureSampler"], parentObject.texture.TextureUnit);
                if (parentObject.textureNormal != null)
                {
                    GL.Uniform1(uniformLocations["textureSamplerNormal"], parentObject.textureNormal.TextureUnit);
                    GL.Uniform1(uniformLocations["useNormal"], 1);
                }
                if (parentObject.textureHeight != null)
                {
                    GL.Uniform1(uniformLocations["textureSamplerHeight"], parentObject.textureHeight.TextureUnit);
                    GL.Uniform1(uniformLocations["useHeight"], 1);
                }
                if (parentObject.textureAO != null)
                {
                    GL.Uniform1(uniformLocations["textureSamplerAO"], parentObject.textureAO.TextureUnit);
                    GL.Uniform1(uniformLocations["useAO"], 1);
                }
                if (parentObject.textureRough != null)
                {
                    GL.Uniform1(uniformLocations["textureSamplerRough"], parentObject.textureRough.TextureUnit);
                    GL.Uniform1(uniformLocations["useRough"], 1);
                }
                if (parentObject.textureMetal != null)
                {
                    GL.Uniform1(uniformLocations["textureSamplerMetal"], parentObject.textureMetal.TextureUnit);
                    GL.Uniform1(uniformLocations["useMetal"], 1);
                }
            }
        }

        public void UpdateFrustumAndCamera(ref Camera camera)
        {
            this.camera = camera;
        }

        private void ConvertToNDC(ref List<float> vertices, triangle tri, int index)
        {
            vertices.AddRange(new float[]
                {
                    tri.p[index].X, tri.p[index].Y, tri.p[index].Z, 1.0f,
                    tri.n[index].X, tri.n[index].Y, tri.n[index].Z,
                    tri.t[index].u, tri.t[index].v,
                    tri.c[index].R, tri.c[index].G, tri.c[index].B, tri.c[index].A,
                    tri.tan[index].X, tri.tan[index].Y, tri.tan[index].Z
                });
        }

        private void ConvertToNDCInstance(ref List<float> vertices, InstancedMeshData data)
        {
            vertices.AddRange(new float[]
            {
                data.Position.X, data.Position.Y, data.Position.Z, 1.0f,
                data.Rotation.X, data.Rotation.Y, data.Rotation.Z, data.Rotation.W,
                data.Scale.X, data.Scale.Y, data.Scale.Z,
                data.Color.R, data.Color.G, data.Color.B, data.Color.A
            });
        }

        private void AddVertices(List<float> vertices, triangle tri)
        {
            lock (vertices) // Lock to ensure thread-safety when modifying the list
            {
                ConvertToNDC(ref vertices, tri, 0);
                ConvertToNDC(ref vertices, tri, 1);
                ConvertToNDC(ref vertices, tri, 2);
            }
        }

        public (List<float>, List<float>) Draw(GameState gameRunning)
        {
            if (!parentObject.isEnabled)
                return (new List<float>(), new List<float>());

            Vao.Bind();

            if (!recalculate)
            {
                if (gameRunning == GameState.Stopped && vertices.Count > 0 && instancedVertices.Count > 0)
                {
                    SendUniforms();

                    if (parentObject.texture != null)
                    {
                        parentObject.texture.Bind();
                        if (parentObject.textureNormal != null)
                            parentObject.textureNormal.Bind();
                        if (parentObject.textureHeight != null)
                            parentObject.textureHeight.Bind();
                        if (parentObject.textureAO != null)
                            parentObject.textureAO.Bind();
                        if (parentObject.textureRough != null)
                            parentObject.textureRough.Bind();
                        if (parentObject.textureMetal != null)
                            parentObject.textureMetal.Bind();
                    }

                    return (vertices, instancedVertices);
                }
            }
            else
            {
                recalculate = false;
                CalculateFrustumVisibility();
            }

            vertices = new List<float>();
            instancedVertices = new List<float>();

            ObjectType type = parentObject.GetObjectType();

            if (parentObject.BSPStruct != null)
            {
                tris = parentObject.BSPStruct.GetTrianglesFrontToBack(camera);
            }
            else if (parentObject.GridStructure != null)
            {
                tris = parentObject.GridStructure.GetTriangles(camera);
            }

            ParallelOptions parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = threadSize };
            Parallel.ForEach(tris, parallelOptions,
                 () => new List<float>(),
                 (tri, loopState, localVertices) =>
                 {
                     if (tri.visibile)
                     {
                         AddVertices(localVertices, tri);
                     }
                     return localVertices;
                 },
                 localVertices =>
                 {
                     lock (vertices)
                     {
                         vertices.AddRange(localVertices);
                     }
                 });

            SendUniforms();

            if (parentObject.texture != null)
            {
                parentObject.texture.Bind();
                if (parentObject.textureNormal != null)
                    parentObject.textureNormal.Bind();
                if (parentObject.textureHeight != null)
                    parentObject.textureHeight.Bind();
                if (parentObject.textureAO != null)
                    parentObject.textureAO.Bind();
                if (parentObject.textureRough != null)
                    parentObject.textureRough.Bind();
                if (parentObject.textureMetal != null)
                    parentObject.textureMetal.Bind();
            }

            foreach (InstancedMeshData meshData in instancedData)
            {
                ConvertToNDCInstance(ref instancedVertices, meshData);
            }

            return (vertices, instancedVertices);
        }

    }
}
