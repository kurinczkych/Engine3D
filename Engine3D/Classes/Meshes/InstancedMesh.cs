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

        Matrix4 viewMatrix, projectionMatrix;

        private InstancedVAO Vao;
        private VBO Vbo;

        public List<InstancedMeshData> instancedData = new List<InstancedMeshData>();

        public InstancedMesh(InstancedVAO vao, VBO vbo, int shaderProgramId, string relativeModelPath, string texturePath, Vector2 windowSize, ref Camera camera, ref Object parentObject) : base(vao.id, vbo.id, shaderProgramId)
        {
            this.parentObject = parentObject;
            this.parentObject.name = Path.GetFileName(relativeModelPath);

            bool success = false;
            parentObject.texture = Engine.textureManager.AddTexture(texturePath, out success);
            if(!success)
                Engine.consoleManager.AddLog("Texture: " + texturePath + "was not found!", LogType.Warning);

            Vao = vao;
            Vbo = vbo;

            this.windowSize = windowSize;
            this.camera = camera;

            modelPath = relativeModelPath;
            modelName = Path.GetFileName(relativeModelPath);
            ProcessObj(relativeModelPath);

            if (uniqueVertices.Count() > 0 && !uniqueVertices[0].gotNormal)
            {
                ComputeVertexNormalsSpherical();
            }

            ComputeTangents();

            GetUniformLocations();
            SendUniforms();
        }

        public InstancedMesh(InstancedVAO vao, VBO vbo, int shaderProgramId, string modelName, MeshData meshData, string texturePath, Vector2 windowSize, ref Camera camera, ref Object parentObject) : base(vao.id, vbo.id, shaderProgramId)
        {
            this.parentObject = parentObject;

            bool success = false;
            parentObject.texture = Engine.textureManager.AddTexture(texturePath, out success);
            if(!success)
                Engine.consoleManager.AddLog("Texture: " + texturePath + "was not found!", LogType.Warning);

            Vao = vao;
            Vbo = vbo;

            this.windowSize = windowSize;
            this.camera = camera;

            this.modelName = modelName;

            uniqueVertices = meshData.vertices;
            visibleVerticesData = meshData.visibleVerticesData;
            indices = meshData.indices;
            Bounds = meshData.bounds;

            if (uniqueVertices.Count() > 0 && !uniqueVertices[0].gotNormal)
            {
                ComputeVertexNormalsSpherical();
            }
            else if (uniqueVertices.Count() > 0 && uniqueVertices[0].gotNormal)
                ComputeVertexNormals();

            ComputeTangents();

            GetUniformLocations();
            SendUniforms();
        }

        public InstancedMesh(InstancedVAO vao, VBO vbo, int shaderProgramId, string modelName, MeshData meshData, Vector2 windowSize, ref Camera camera, ref Object parentObject) : base(vao.id, vbo.id, shaderProgramId)
        {
            this.parentObject = parentObject;

            Vao = vao;
            Vbo = vbo;

            this.windowSize = windowSize;
            this.camera = camera;

            this.modelName = modelName;

            uniqueVertices = meshData.vertices;
            visibleVerticesData = meshData.visibleVerticesData;
            indices = meshData.indices;
            Bounds = meshData.bounds;

            if (uniqueVertices.Count() > 0 && !uniqueVertices[0].gotNormal)
            {
                ComputeVertexNormalsSpherical();
            }
            else if (uniqueVertices.Count() > 0 && uniqueVertices[0].gotNormal)
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
            if (parentObject.texture != null)
            {
                uniformLocations.Add("textureSampler", GL.GetUniformLocation(shaderProgramId, "textureSampler"));
                if (parentObject.textureNormal != null)
                {
                    uniformLocations.Add("textureSamplerNormal", GL.GetUniformLocation(shaderProgramId, "textureSamplerNormal"));
                }
                if (parentObject.textureHeight != null)
                {
                    uniformLocations.Add("textureSamplerHeight", GL.GetUniformLocation(shaderProgramId, "textureSamplerHeight"));
                }
                if (parentObject.textureAO != null)
                {
                    uniformLocations.Add("textureSamplerAO", GL.GetUniformLocation(shaderProgramId, "textureSamplerAO"));
                }
                if (parentObject.textureRough != null)
                {
                    uniformLocations.Add("textureSamplerRough", GL.GetUniformLocation(shaderProgramId, "textureSamplerRough"));
                }
                if (parentObject.textureMetal != null)
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
            if (parentObject.texture != null)
            {
                GL.Uniform1(uniformLocations["textureSampler"], parentObject.texture.TextureUnit);
                if (parentObject.textureNormal != null)
                {
                    GL.Uniform1(uniformLocations["textureSamplerNormal"], parentObject.textureNormal.TextureUnit);
                }
                if (parentObject.textureHeight != null)
                {
                    GL.Uniform1(uniformLocations["textureSamplerHeight"], parentObject.textureHeight.TextureUnit);
                }
                if (parentObject.textureAO != null)
                {
                    GL.Uniform1(uniformLocations["textureSamplerAO"], parentObject.textureAO.TextureUnit);
                }
                if (parentObject.textureRough != null)
                {
                    GL.Uniform1(uniformLocations["textureSamplerRough"], parentObject.textureRough.TextureUnit);
                }
                if (parentObject.textureMetal != null)
                {
                    GL.Uniform1(uniformLocations["textureSamplerMetal"], parentObject.textureMetal.TextureUnit);
                }
            }

            GL.Uniform1(uniformLocations["useTexture"], parentObject.texture != null ? 1 : 0);
            GL.Uniform1(uniformLocations["useNormal"], parentObject.textureNormal != null ? 1 : 0);
            GL.Uniform1(uniformLocations["useHeight"], parentObject.textureHeight != null ? 1 : 0);
            GL.Uniform1(uniformLocations["useAO"], parentObject.textureAO != null ? 1 : 0);
            GL.Uniform1(uniformLocations["useRough"], parentObject.textureRough != null ? 1 : 0);
            GL.Uniform1(uniformLocations["useMetal"], parentObject.textureMetal != null ? 1 : 0);
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

        //private void ConvertToNDC(ref List<float> vertices, triangle tri, int index)
        //{
        //    vertices.AddRange(new float[]
        //        {
        //            tri.p[index].X, tri.p[index].Y, tri.p[index].Z,
        //            tri.n[index].X, tri.n[index].Y, tri.n[index].Z,
        //            tri.t[index].u, tri.t[index].v,
        //            tri.c[index].R, tri.c[index].G, tri.c[index].B, tri.c[index].A,
        //            tri.tan[index].X, tri.tan[index].Y, tri.tan[index].Z
        //        });
        //}

        //private void ConvertToNDCOnlyPosAndNormal(ref List<float> vertices, triangle tri, int index)
        //{
        //    vertices.AddRange(new float[]
        //    {
        //        tri.p[index].X, tri.p[index].Y, tri.p[index].Z,
        //        tri.n[index].X, tri.n[index].Y, tri.n[index].Z
        //    });
        //}

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

        //private void AddVertices(List<float> vertices, triangle tri)
        //{
        //    lock (vertices) // Lock to ensure thread-safety when modifying the list
        //    {
        //        ConvertToNDC(ref vertices, tri, 0);
        //        ConvertToNDC(ref vertices, tri, 1);
        //        ConvertToNDC(ref vertices, tri, 2);
        //    }
        //}

        //private void AddVerticesOnlyPosAndNormal(List<float> vertices, triangle tri)
        //{
        //    lock (vertices) // Lock to ensure thread-safety when modifying the list
        //    {
        //        ConvertToNDCOnlyPosAndNormal(ref vertices, tri, 0);
        //        ConvertToNDCOnlyPosAndNormal(ref vertices, tri, 1);
        //        ConvertToNDCOnlyPosAndNormal(ref vertices, tri, 2);
        //    }
        //}

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

            //ParallelOptions parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = threadSize };
            //Parallel.ForEach(tris, parallelOptions,
            //     () => new List<float>(),
            //     (tri, loopState, localVertices) =>
            //     {
            //         if (tri.visibile)
            //         {
            //             AddVertices(localVertices, tri);
            //         }
            //         return localVertices;
            //     },
            //     localVertices =>
            //     {
            //         lock (vertices)
            //         {
            //             vertices.AddRange(localVertices);
            //         }
            //     });

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

            //Parallel.ForEach(instancedData, parallelOptions,
            //     () => new List<float>(),
            //     (instancedData, loopState, localVertices) =>
            //     {
            //         ConvertToNDCInstance(ref localVertices, instancedData);
            //         return localVertices;
            //     },
            //     localVertices =>
            //     {
            //         lock (instancedVertices)
            //         {
            //             instancedVertices.AddRange(localVertices);
            //         }
            //     });

            return (vertices, instancedVertices);
        }

        public (List<float>, List<float>) DrawOnlyPosAndNormal(GameState gameRunning, Shader shader, InstancedVAO _vao)
        {
            if (!parentObject.isEnabled)
                return (new List<float>(), new List<float>());

            _vao.Bind();

            if (!recalculateOnlyPosAndNormal)
            {
                if (gameRunning == GameState.Stopped && verticesOnlyPosAndNormal.Count > 0 && instancedVertices.Count > 0)
                {
                    SendUniformsOnlyPos(shader);

                    return (verticesOnlyPosAndNormal, instancedVertices);
                }
            }
            else
            {
                recalculateOnlyPosAndNormal = false;
                CalculateFrustumVisibility();
            }

            verticesOnlyPosAndNormal = new List<float>();
            instancedVertices = new List<float>();

            ObjectType type = parentObject.GetObjectType();

            //ParallelOptions parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = threadSize };
            //Parallel.ForEach(tris, parallelOptions,
            //     () => new List<float>(),
            //     (tri, loopState, localVertices) =>
            //     {
            //         if (tri.visibile)
            //         {
            //             AddVerticesOnlyPosAndNormal(localVertices, tri);
            //         }
            //         return localVertices;
            //     },
            //     localVertices =>
            //     {
            //         lock (verticesOnlyPosAndNormal)
            //         {
            //             verticesOnlyPosAndNormal.AddRange(localVertices);
            //         }
            //     });

            SendUniformsOnlyPos(shader);

            //Parallel.ForEach(instancedData, parallelOptions,
            //     () => new List<float>(),
            //     (instancedData, loopState, localVertices) =>
            //     {
            //         ConvertToNDCInstance(ref localVertices, instancedData);
            //         return localVertices;
            //     },
            //     localVertices =>
            //     {
            //         lock (instancedVertices)
            //         {
            //             instancedVertices.AddRange(localVertices);
            //         }
            //     });

            return (verticesOnlyPosAndNormal, instancedVertices);
        }
    }
}
