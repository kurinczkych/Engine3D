using Newtonsoft.Json;
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
        public static int floatCount = 15;
        public static int instancedFloatCount = 14;

        private List<float> vertices = new List<float>();
        private List<float> instancedVertices = new List<float>();
        private List<float> verticesOnlyPosAndNormal = new List<float>();

        private Vector2 windowSize;

        [JsonConverter(typeof(Matrix4Converter))]
        Matrix4 viewMatrix;
        [JsonConverter(typeof(Matrix4Converter))]
        Matrix4 projectionMatrix;

        private InstancedVAO Vao;
        private VBO Vbo;

        public List<InstancedMeshData> instancedData = new List<InstancedMeshData>();

        public InstancedMesh(InstancedVAO vao, VBO vbo, int shaderProgramId, string relativeModelPath, string texturePath, Vector2 windowSize, ref Camera camera, ref Object parentObject) : base(vao.id, vbo.id, shaderProgramId)
        {
            this.parentObject = parentObject;
            this.parentObject.name = Path.GetFileName(relativeModelPath);

            bool success = false;
            texture = Engine.textureManager.AddTexture(texturePath, out success);
            if(!success)
                Engine.consoleManager.AddLog("Texture: " + texturePath + "was not found!", LogType.Warning);

            Vao = vao;
            Vbo = vbo;

            this.windowSize = windowSize;
            this.camera = camera;

            modelPath = relativeModelPath;
            modelName = Path.GetFileName(relativeModelPath);
            ProcessObj(relativeModelPath);

            if(model.meshes.Count > 0 && model.meshes[0].uniqueVertices.Count > 0 && !model.meshes[0].uniqueVertices[0].gotNormal)
            {
                ComputeVertexNormalsSpherical();
            }

            ComputeTangents();

            GetUniformLocations();
            SendUniforms();
        }

        public InstancedMesh(InstancedVAO vao, VBO vbo, int shaderProgramId, string modelName, ModelData model, string texturePath, Vector2 windowSize, ref Camera camera, ref Object parentObject) : base(vao.id, vbo.id, shaderProgramId)
        {
            this.parentObject = parentObject;

            bool success = false;
            texture = Engine.textureManager.AddTexture(texturePath, out success);
            if(!success)
                Engine.consoleManager.AddLog("Texture: " + texturePath + "was not found!", LogType.Warning);

            Vao = vao;
            Vbo = vbo;

            this.windowSize = windowSize;
            this.camera = camera;

            this.modelName = modelName;
            this.model = model;

            if (model.meshes.Count > 0 && model.meshes[0].uniqueVertices.Count > 0 && !model.meshes[0].uniqueVertices[0].gotNormal)
            {
                ComputeVertexNormalsSpherical();
            }
            else if (model.meshes.Count > 0 && model.meshes[0].uniqueVertices.Count > 0 && model.meshes[0].uniqueVertices[0].gotNormal)
                ComputeVertexNormals();

            ComputeTangents();

            GetUniformLocations();
            SendUniforms();
        }

        public InstancedMesh(InstancedVAO vao, VBO vbo, int shaderProgramId, string modelName, ModelData model, Vector2 windowSize, ref Camera camera, ref Object parentObject) : base(vao.id, vbo.id, shaderProgramId)
        {
            this.parentObject = parentObject;

            Vao = vao;
            Vbo = vbo;

            this.windowSize = windowSize;
            this.camera = camera;

            this.modelName = modelName;
            this.model = model;

            if (model.meshes.Count > 0 && model.meshes[0].uniqueVertices.Count > 0 && !model.meshes[0].uniqueVertices[0].gotNormal)
            {
                ComputeVertexNormalsSpherical();
            }
            else if (model.meshes.Count > 0 && model.meshes[0].uniqueVertices.Count > 0 && model.meshes[0].uniqueVertices[0].gotNormal)
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
            uniformLocations.Add("useShading", GL.GetUniformLocation(shaderProgramId, "useShading"));
            uniformLocations.Add("useTexture", GL.GetUniformLocation(shaderProgramId, "useTexture"));
            uniformLocations.Add("useNormal", GL.GetUniformLocation(shaderProgramId, "useNormal"));
            uniformLocations.Add("useHeight", GL.GetUniformLocation(shaderProgramId, "useHeight"));
            uniformLocations.Add("useAO", GL.GetUniformLocation(shaderProgramId, "useAO"));
            uniformLocations.Add("useRough", GL.GetUniformLocation(shaderProgramId, "useRough"));
            uniformLocations.Add("useMetal", GL.GetUniformLocation(shaderProgramId, "useMetal"));
            if (texture != null)
            {
                uniformLocations.Add("textureSampler", GL.GetUniformLocation(shaderProgramId, "textureSampler"));
                if (textureNormal != null)
                {
                    uniformLocations.Add("textureSamplerNormal", GL.GetUniformLocation(shaderProgramId, "textureSamplerNormal"));
                }
                if (textureHeight != null)
                {
                    uniformLocations.Add("textureSamplerHeight", GL.GetUniformLocation(shaderProgramId, "textureSamplerHeight"));
                }
                if (textureAO != null)
                {
                    uniformLocations.Add("textureSamplerAO", GL.GetUniformLocation(shaderProgramId, "textureSamplerAO"));
                }
                if (textureRough != null)
                {
                    uniformLocations.Add("textureSamplerRough", GL.GetUniformLocation(shaderProgramId, "textureSamplerRough"));
                }
                if (textureMetal != null)
                {
                    uniformLocations.Add("textureSamplerMetal", GL.GetUniformLocation(shaderProgramId, "textureSamplerMetal"));
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
            GL.Uniform1(uniformLocations["useShading"], useShading ? 1 : 0);
            if (texture != null)
            {
                GL.Uniform1(uniformLocations["textureSampler"], texture.TextureUnit);
                if (textureNormal != null)
                {
                    GL.Uniform1(uniformLocations["textureSamplerNormal"], textureNormal.TextureUnit);
                }
                if (textureHeight != null)
                {
                    GL.Uniform1(uniformLocations["textureSamplerHeight"], textureHeight.TextureUnit);
                }
                if (textureAO != null)
                {
                    GL.Uniform1(uniformLocations["textureSamplerAO"], textureAO.TextureUnit);
                }
                if (textureRough != null)
                {
                    GL.Uniform1(uniformLocations["textureSamplerRough"], textureRough.TextureUnit);
                }
                if (textureMetal != null)
                {
                    GL.Uniform1(uniformLocations["textureSamplerMetal"], textureMetal.TextureUnit);
                }
            }

            GL.Uniform1(uniformLocations["useTexture"], texture != null ? 1 : 0);
            GL.Uniform1(uniformLocations["useNormal"], textureNormal != null ? 1 : 0);
            GL.Uniform1(uniformLocations["useHeight"], textureHeight != null ? 1 : 0);
            GL.Uniform1(uniformLocations["useAO"], textureAO != null ? 1 : 0);
            GL.Uniform1(uniformLocations["useRough"], textureRough != null ? 1 : 0);
            GL.Uniform1(uniformLocations["useMetal"], textureMetal != null ? 1 : 0);
        }

        public void SendUniformsOnlyPos(Shader shader)
        {
            projectionMatrix = camera.projectionMatrix;
            viewMatrix = camera.viewMatrix;

            GL.UniformMatrix4(GL.GetUniformLocation(shader.id, "modelMatrix"), true, ref modelMatrix);
            GL.UniformMatrix4(GL.GetUniformLocation(shader.id, "viewMatrix"), true, ref viewMatrix);
            GL.UniformMatrix4(GL.GetUniformLocation(shader.id, "projectionMatrix"), true, ref projectionMatrix);

            GL.UniformMatrix4(GL.GetUniformLocation(shader.id, "_scaleMatrix"), true, ref scaleMatrix);
            GL.UniformMatrix4(GL.GetUniformLocation(shader.id, "_rotMatrix"), true, ref rotationMatrix);
        }

        private void ConvertToNDCInstance(ref List<float> vertices, InstancedMeshData data)
        {
            vertices.AddRange(new float[]
            {
                data.Position.X, data.Position.Y, data.Position.Z,
                data.Rotation.X, data.Rotation.Y, data.Rotation.Z, data.Rotation.W,
                data.Scale.X, data.Scale.Y, data.Scale.Z,
                data.Color.R, data.Color.G, data.Color.B, data.Color.A
            });
        }

        public void Draw(GameState gameRunning, Shader shader, VBO vbo_, VBO instVbo_, IBO ibo_)
        {
            if (!parentObject.isEnabled || model == null || model.meshes.Count == 0)
                return;

            Vao.Bind();
            shader.Use();
            SendUniforms();

            foreach (MeshData mesh in model.meshes)
            {
                if (!recalculate)
                {
                    if (gameRunning == GameState.Stopped && vertices.Count > 0 && instancedVertices.Count > 0)
                    {
                        if (texture != null)
                        {
                            texture.Bind();
                            if (textureNormal != null)
                                textureNormal.Bind();
                            if (textureHeight != null)
                                textureHeight.Bind();
                            if (textureAO != null)
                                textureAO.Bind();
                            if (textureRough != null)
                                textureRough.Bind();
                            if (textureMetal != null)
                                textureMetal.Bind();
                        }

                        ibo_.Buffer(mesh.visibleIndices);
                        vbo_.Buffer(mesh.visibleVerticesData);
                        instVbo_.Buffer(instancedVertices);
                        GL.DrawElementsInstanced(PrimitiveType.Triangles, mesh.visibleIndices.Count, DrawElementsType.UnsignedInt, IntPtr.Zero, instancedData.Count());
                        continue;
                    }
                }
                else
                {
                    recalculate = false;
                    CalculateFrustumVisibility();
                }

                if (texture != null)
                {
                    texture.Bind();
                    if (textureNormal != null)
                        textureNormal.Bind();
                    if (textureHeight != null)
                        textureHeight.Bind();
                    if (textureAO != null)
                        textureAO.Bind();
                    if (textureRough != null)
                        textureRough.Bind();
                    if (textureMetal != null)
                        textureMetal.Bind();
                }

                instancedVertices.Clear();
                ParallelOptions parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = threadSize };
                Parallel.ForEach(instancedData, parallelOptions,
                     () => new List<float>(),
                     (instancedData_, loopState, localVertices) =>
                     {
                         ConvertToNDCInstance(ref localVertices, instancedData_);
                         return localVertices;
                     },
                     localVertices =>
                     {
                         lock (instancedVertices)
                         {
                             instancedVertices.AddRange(localVertices);
                         }
                     });

                ibo_.Buffer(mesh.visibleIndices);
                vbo_.Buffer(mesh.visibleVerticesData);
                instVbo_.Buffer(instancedVertices);
                GL.DrawElementsInstanced(PrimitiveType.Triangles, mesh.visibleIndices.Count, DrawElementsType.UnsignedInt, IntPtr.Zero, instancedData.Count());
            }
        }

        public void DrawOnlyPosAndNormal(GameState gameRunning, Shader shader, InstancedVAO _vao, VBO vbo_, VBO instVbo_, IBO ibo_, int instIndex = -1)
        {
            if (!parentObject.isEnabled)
                return;

            _vao.Bind();
            shader.Use();
            SendUniformsOnlyPos(shader);

            foreach (MeshData mesh in model.meshes)
            {
                if (!recalculateOnlyPosAndNormal)
                {
                    if (gameRunning == GameState.Stopped && model.meshes[0].visibleVerticesDataOnlyPosAndNormal.Count > 0 && instancedVertices.Count > 0)
                    {
                        ibo_.Buffer(mesh.visibleIndices);
                        vbo_.Buffer(mesh.visibleVerticesDataOnlyPosAndNormal);
                        instVbo_.Buffer(instancedVertices);
                        GL.DrawElementsInstanced(PrimitiveType.Triangles, mesh.indices.Count, DrawElementsType.UnsignedInt, IntPtr.Zero, instancedData.Count());
                        continue;
                    }
                }
                else
                {
                    recalculateOnlyPosAndNormal = false;
                    CalculateFrustumVisibility();
                }

                ibo_.Buffer(mesh.visibleIndices);
                vbo_.Buffer(mesh.visibleVerticesDataOnlyPosAndNormal);
                instVbo_.Buffer(instancedVertices);
                GL.DrawElementsInstanced(PrimitiveType.Triangles, mesh.indices.Count, DrawElementsType.UnsignedInt, IntPtr.Zero, instancedData.Count());

                //if(instIndex == -1)
                //    GL.DrawElementsInstanced(PrimitiveType.Triangles, indices.Count, DrawElementsType.UnsignedInt, IntPtr.Zero, mesh.instancedData.Count());
                //else
                //    GL.DrawElementsInstanced(PrimitiveType.Triangles, indices.Count, DrawElementsType.UnsignedInt, IntPtr.Zero, 1);

                //if (instIndex == -1)
                //    return (new List<float>(visibleVerticesDataOnlyPosAndNormal), new List<uint>(visibleIndices), new List<float>(instancedVertices));
                //else
                //{
                //    List<float> instancedVertex = new List<float>();
                //    ConvertToNDCInstance(ref instancedVertex, instancedData[instIndex]);
                //    return (new List<float>(visibleVerticesDataOnlyPosAndNormal), new List<uint>(visibleIndices), instancedVertex);
                //}
            }
        }
    }
}
