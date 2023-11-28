using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Mathematics;
using static System.Net.Mime.MediaTypeNames;
using MagicPhysX;
using System.Threading.Tasks;
using System.Data;
using System.Diagnostics;
using System.Diagnostics.SymbolStore;
using Cyotek.Drawing.BitmapFont;
using System.Runtime.CompilerServices;
using static OpenTK.Graphics.OpenGL.GL;
using OpenTK.Windowing.Common;

#pragma warning disable CS8600
#pragma warning disable CS8604
#pragma warning disable CS0728

namespace Engine3D
{

    public class Mesh : BaseMesh
    {
        public static int floatCount = 15;
        public static int floatAnimCount = 24;

        public bool drawNormals = false;
        public WireframeMesh normalMesh;

        private Vector2 windowSize;

        private Matrix4 viewMatrix, projectionMatrix;
        private Vector3 cameraPos;

        public Animation? animation;
        protected double animDeltaTime = 0;
        protected int currentAnim = 0;

        private VAO Vao;
        private VBO Vbo;

        public Mesh(VAO vao, VBO vbo, int shaderProgramId, string relativeModelPath, string texturePath, Vector2 windowSize, ref Camera camera, ref Object parentObject) : base(vao.id, vbo.id, shaderProgramId)
        {
            this.parentObject = parentObject;
            this.parentObject.name = Path.GetFileName(relativeModelPath);
            this.shaderProgramId = shaderProgramId;

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

        public Mesh(VAO vao, VBO vbo, int shaderProgramId, string relativeModelPath, Vector2 windowSize, ref Camera camera, ref Object parentObject) : base(vao.id, vbo.id, shaderProgramId)
        {
            this.parentObject = parentObject;
            this.parentObject.name = Path.GetFileName(relativeModelPath);
            this.shaderProgramId = shaderProgramId;

            Vao = vao;
            Vbo = vbo;

            this.windowSize = windowSize;
            this.camera = camera;

            modelPath = relativeModelPath;
            modelName = Path.GetFileName(relativeModelPath);
            ProcessObj(relativeModelPath);

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

        public Mesh(VAO vao, VBO vbo, int shaderProgramId, string modelName, ModelData model, string texturePath, Vector2 windowSize, ref Camera camera, ref Object parentObject) : base(vao.id, vbo.id, shaderProgramId)
        {
            this.parentObject = parentObject;
            this.parentObject.name = modelName;
            this.shaderProgramId = shaderProgramId;

            bool success = false;
            parentObject.texture = Engine.textureManager.AddTexture(texturePath, out success);
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

        public Mesh(VAO vao, VBO vbo, int shaderProgramId, string modelName, ModelData model, Vector2 windowSize, ref Camera camera, ref Object parentObject) : base(vao.id, vbo.id, shaderProgramId)
        {
            this.parentObject = parentObject;
            this.parentObject.name = modelName;
            this.shaderProgramId = shaderProgramId;

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

        private void ConvertToNDCOnlyPos(ref List<float> vertices, triangle tri, int index)
        {
            vertices.AddRange(new float[]
            {
                tri.v[index].p.X, tri.v[index].p.Y, tri.v[index].p.Z,
            });
        }

        private void ConvertToNDCOnlyPosAndNormal(ref List<float> vertices, triangle tri, int index)
        {
            vertices.AddRange(new float[]
            {
                tri.v[index].p.X, tri.v[index].p.Y, tri.v[index].p.Z,
                tri.v[index].n.X, tri.v[index].n.Y, tri.v[index].n.Z
            });
        }

        private PxVec3 ConvertToNDCPxVec3(int index, int meshIndex)
        {
            Vector3 v = Vector3.TransformPosition(model.meshes[meshIndex].allVerts[index], modelMatrix);

            PxVec3 vec = new PxVec3();
            vec.x = v.X;
            vec.y = v.Y;
            vec.z = v.Z;

            return vec;
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

            #region TextureLocations
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
            #endregion
        }

        public void GetUniformLocationsAnim(Shader shader)
        {
            shader.Use();

            uniformAnimLocations.Add("windowSize", GL.GetUniformLocation(shader.id, "windowSize"));
            uniformAnimLocations.Add("modelMatrix", GL.GetUniformLocation(shader.id, "modelMatrix"));
            uniformAnimLocations.Add("viewMatrix", GL.GetUniformLocation(shader.id, "viewMatrix"));
            uniformAnimLocations.Add("projectionMatrix", GL.GetUniformLocation(shader.id, "projectionMatrix"));
            uniformAnimLocations.Add("cameraPosition", GL.GetUniformLocation(shader.id, "cameraPosition"));
            uniformAnimLocations.Add("useBillboarding", GL.GetUniformLocation(shader.id, "useBillboarding"));
            uniformAnimLocations.Add("useShading", GL.GetUniformLocation(shader.id, "useShading"));
            uniformAnimLocations.Add("useAnimation", GL.GetUniformLocation(shader.id, "useAnimation"));
            uniformAnimLocations.Add("boneMatrices", GL.GetUniformLocation(shader.id, "boneMatrices"));

            #region TextureLocations
            uniformAnimLocations.Add("useTexture", GL.GetUniformLocation(shader.id, "useTexture"));
            uniformAnimLocations.Add("useNormal", GL.GetUniformLocation(shader.id, "useNormal"));
            uniformAnimLocations.Add("useHeight", GL.GetUniformLocation(shader.id, "useHeight"));
            uniformAnimLocations.Add("useAO", GL.GetUniformLocation(shader.id, "useAO"));
            uniformAnimLocations.Add("useRough", GL.GetUniformLocation(shader.id, "useRough"));
            uniformAnimLocations.Add("useMetal", GL.GetUniformLocation(shader.id, "useMetal"));
            if (parentObject.texture != null)
            {
                uniformAnimLocations.Add("textureSampler", GL.GetUniformLocation(shader.id, "textureSampler"));
                if (parentObject.textureNormal != null)
                {
                    uniformAnimLocations.Add("textureSamplerNormal", GL.GetUniformLocation(shader.id, "textureSamplerNormal"));
                }
                if (parentObject.textureHeight != null)
                {
                    uniformAnimLocations.Add("textureSamplerHeight", GL.GetUniformLocation(shader.id, "textureSamplerHeight"));
                }
                if (parentObject.textureAO != null)
                {
                    uniformAnimLocations.Add("textureSamplerAO", GL.GetUniformLocation(shader.id, "textureSamplerAO"));
                }
                if (parentObject.textureRough != null)
                {
                    uniformAnimLocations.Add("textureSamplerRough", GL.GetUniformLocation(shader.id, "textureSamplerRough"));
                }
                if (parentObject.textureMetal != null)
                {
                    uniformAnimLocations.Add("textureSamplerMetal", GL.GetUniformLocation(shader.id, "textureSamplerMetal"));
                }
            }
            #endregion
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

        protected void SendAnimationUniforms(int meshId, double delta)
        {
            projectionMatrix = camera.projectionMatrix;
            viewMatrix = camera.viewMatrix;

            GL.UniformMatrix4(uniformAnimLocations["modelMatrix"], true, ref modelMatrix);
            GL.UniformMatrix4(uniformAnimLocations["viewMatrix"], true, ref viewMatrix);
            GL.UniformMatrix4(uniformAnimLocations["projectionMatrix"], true, ref projectionMatrix);
            GL.Uniform2(uniformAnimLocations["windowSize"], windowSize);
            GL.Uniform3(uniformAnimLocations["cameraPosition"], camera.GetPosition());
            GL.Uniform1(uniformAnimLocations["useBillboarding"], useBillboarding);
            GL.Uniform1(uniformAnimLocations["useShading"], useShading ? 1 : 0);
            if (parentObject.texture != null)
            {
                GL.Uniform1(uniformAnimLocations["textureSampler"], parentObject.texture.TextureUnit);
                if (parentObject.textureNormal != null)
                {
                    GL.Uniform1(uniformAnimLocations["textureSamplerNormal"], parentObject.textureNormal.TextureUnit);
                }
                if (parentObject.textureHeight != null)
                {
                    GL.Uniform1(uniformAnimLocations["textureSamplerHeight"], parentObject.textureHeight.TextureUnit);
                }
                if (parentObject.textureAO != null)
                {
                    GL.Uniform1(uniformAnimLocations["textureSamplerAO"], parentObject.textureAO.TextureUnit);
                }
                if (parentObject.textureRough != null)
                {
                    GL.Uniform1(uniformAnimLocations["textureSamplerRough"], parentObject.textureRough.TextureUnit);
                }
                if (parentObject.textureMetal != null)
                {
                    GL.Uniform1(uniformAnimLocations["textureSamplerMetal"], parentObject.textureMetal.TextureUnit);
                }
            }

            GL.Uniform1(uniformAnimLocations["useTexture"], parentObject.texture != null ? 1 : 0);
            GL.Uniform1(uniformAnimLocations["useNormal"], parentObject.textureNormal != null ? 1 : 0);
            GL.Uniform1(uniformAnimLocations["useHeight"], parentObject.textureHeight != null ? 1 : 0);
            GL.Uniform1(uniformAnimLocations["useAO"], parentObject.textureAO != null ? 1 : 0);
            GL.Uniform1(uniformAnimLocations["useRough"], parentObject.textureRough != null ? 1 : 0);
            GL.Uniform1(uniformAnimLocations["useMetal"], parentObject.textureMetal != null ? 1 : 0);
            GL.Uniform1(uniformAnimLocations["useAnimation"], 1);

            animDeltaTime += delta;

            if (animation != null)
            {
                //if (animDeltaTime >= 1.0/animation.TicksPerSecond)
                if (animDeltaTime >= 0.5)
                {
                    currentAnim++;
                    animDeltaTime = 0;
                }

                //TODO is the animation looping or just once?
                if (currentAnim > animation.DurationInTicks)
                    currentAnim = 0;

                Matrix4 boneMatrix  = model.meshes[meshId].boneMatrices[animation.boneAnimations[0].Name];

                GL.UniformMatrix4(uniformAnimLocations["boneMatrices"] + 0, true, ref boneMatrix);

                //int i = 0;
                //int j = 0;
                //while (i < animation.boneAnimations.Count)
                //{
                //    if (model.meshes[meshId].boneMatrices.ContainsKey(animation.boneAnimations[i].Name))
                //    {
                //        Matrix4 boneMatrix = Matrix4.Identity;
                //        if (currentAnim <= 2)
                //            boneMatrix = boneMatrix * Matrix4.CreateTranslation(currentAnim * 2, 0, 0);
                //        else
                //        {
                //            boneMatrix = model.meshes[meshId].boneMatrices[animation.boneAnimations[i].Name];
                //                //* animation.boneAnimations[i].Transformations[currentAnim];

                //            //boneMatrix = model.meshes[meshId].boneMatrices[animation.boneAnimations[i].Name] + animation.boneAnimations[i].Transformations[currentAnim];
                //        }

                //        GL.UniformMatrix4(uniformAnimLocations["boneMatrices"] + j, true, ref boneMatrix);
                //        j++;
                //    }

                //    i++;
                //}
            }
            else
            {
                for (int i = 0; i < model.meshes[meshId].boneMatrices.Count; i++)
                {
                    Matrix4 boneMatrix = Matrix4.Identity;

                    GL.UniformMatrix4(uniformAnimLocations["boneMatrices"] + i, true, ref boneMatrix);
                }
            }
        }



        public void SendUniformsOnlyPos(Shader shader)
        {
            projectionMatrix = camera.projectionMatrix;
            viewMatrix = camera.viewMatrix;
            cameraPos = camera.GetPosition();

            GL.UniformMatrix4(GL.GetUniformLocation(shader.id, "modelMatrix"), true, ref modelMatrix);
            GL.UniformMatrix4(GL.GetUniformLocation(shader.id, "viewMatrix"), true, ref viewMatrix);
            GL.UniformMatrix4(GL.GetUniformLocation(shader.id, "projectionMatrix"), true, ref projectionMatrix);
            GL.Uniform3(GL.GetUniformLocation(shader.id, "cameraPos"), cameraPos);

            GL.UniformMatrix4(GL.GetUniformLocation(shader.id, "_scaleMatrix"), true, ref scaleMatrix);
            GL.UniformMatrix4(GL.GetUniformLocation(shader.id, "_rotMatrix"), true, ref rotationMatrix);
        }

        public void Draw(GameState gameRunning, Shader shader, VBO vbo_, IBO ibo_)
        {
            if (!parentObject.isEnabled)
                return;

            Vao.Bind();
            shader.Use();

            SendUniforms();

            foreach (MeshData mesh in model.meshes)
            {
                if (!recalculate)
                {
                    if (gameRunning == GameState.Stopped && mesh.visibleIndices.Count > 0)
                    {
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

                        ibo_.Buffer(mesh.visibleIndices);
                        vbo_.Buffer(mesh.visibleVerticesData);
                        GL.DrawElements(PrimitiveType.Triangles, mesh.visibleIndices.Count, DrawElementsType.UnsignedInt, 0);
                        continue;
                    }
                }
                else
                {
                    recalculate = false;
                    CalculateFrustumVisibility();
                }

                #region not working parallelization
                //ParallelOptions parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = threadSize };
                //Parallel.ForEach(uniqueVertices, parallelOptions,
                //    () => new List<float>(),
                //     (v, loopState, localVertices) =>
                //     {
                //         AddVertices(localVertices, v);
                //         //if (tri.visibile)
                //         //{
                //         //    AddVertices(localVertices.LocalVertices1, tri);
                //         //    if (parentObject.isSelected)
                //         //    {
                //         //        AddVerticesOnlyPos(localVertices.LocalVertices2, tri);
                //         //    }
                //         //}
                //         return localVertices;
                //     },
                //     localVertices =>
                //     {
                //         lock (vertices)
                //         {
                //             vertices.AddRange(localVertices);
                //         }
                //         //if (parentObject.isSelected)
                //         //{
                //         //    lock (verticesOnlyPos)
                //         //    {
                //         //        verticesOnlyPos.AddRange(localVertices.LocalVertices2);
                //         //    }
                //         //}
                //     });
                #endregion


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

                ibo_.Buffer(mesh.visibleIndices);
                vbo_.Buffer(mesh.visibleVerticesData);
                GL.DrawElements(PrimitiveType.Triangles, mesh.visibleIndices.Count, DrawElementsType.UnsignedInt, 0);
            }
        }
        
        public void DrawAnimated(GameState gameRunning, Shader shader, VAO vao_, VBO vbo_, IBO ibo_, double delta)
        {
            if (!parentObject.isEnabled)
                return;

            vao_.Bind();
            shader.Use();

            for(int i = 0; i < model.meshes.Count; i++)
            {
                MeshData mesh = model.meshes[i];
                SendAnimationUniforms(i, delta);

                if (!recalculate)
                {
                    if (gameRunning == GameState.Stopped && mesh.visibleIndices.Count > 0)
                    {
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

                        ibo_.Buffer(mesh.visibleIndices);
                        vbo_.Buffer(mesh.visibleVerticesDataWithAnim);
                        GL.DrawElements(PrimitiveType.Triangles, mesh.visibleIndices.Count, DrawElementsType.UnsignedInt, 0);
                        continue;
                    }
                }
                else
                {
                    recalculate = false;
                    CalculateFrustumVisibility();
                }

                #region not working parallelization
                //ParallelOptions parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = threadSize };
                //Parallel.ForEach(uniqueVertices, parallelOptions,
                //    () => new List<float>(),
                //     (v, loopState, localVertices) =>
                //     {
                //         AddVertices(localVertices, v);
                //         //if (tri.visibile)
                //         //{
                //         //    AddVertices(localVertices.LocalVertices1, tri);
                //         //    if (parentObject.isSelected)
                //         //    {
                //         //        AddVerticesOnlyPos(localVertices.LocalVertices2, tri);
                //         //    }
                //         //}
                //         return localVertices;
                //     },
                //     localVertices =>
                //     {
                //         lock (vertices)
                //         {
                //             vertices.AddRange(localVertices);
                //         }
                //         //if (parentObject.isSelected)
                //         //{
                //         //    lock (verticesOnlyPos)
                //         //    {
                //         //        verticesOnlyPos.AddRange(localVertices.LocalVertices2);
                //         //    }
                //         //}
                //     });
                #endregion


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

                ibo_.Buffer(mesh.visibleIndices);
                vbo_.Buffer(mesh.visibleVerticesDataWithAnim);
                GL.DrawElements(PrimitiveType.Triangles, mesh.visibleIndices.Count, DrawElementsType.UnsignedInt, 0);
            }
        }

        public void DrawOnlyPos(GameState gameRunning, Shader shader, VAO vao_, VBO vbo_, IBO ibo_)
        {
            if (!parentObject.isEnabled)
                return;

            vao_.Bind();
            shader.Use();
            SendUniformsOnlyPos(shader);

            foreach (MeshData mesh in model.meshes)
            {
                if (!recalculateOnlyPos)
                {
                    if (gameRunning == GameState.Stopped && mesh.visibleVerticesDataOnlyPos.Count > 0)
                    {
                        ibo_.Buffer(mesh.visibleIndices);
                        vbo_.Buffer(mesh.visibleVerticesDataOnlyPos);
                        GL.DrawElements(PrimitiveType.Triangles, mesh.visibleIndices.Count, DrawElementsType.UnsignedInt, 0);
                        continue;
                    }
                }
                else
                {
                    recalculateOnlyPos = false;
                    CalculateFrustumVisibility();
                }

                ibo_.Buffer(mesh.visibleIndices);
                vbo_.Buffer(mesh.visibleVerticesDataOnlyPos);
                GL.DrawElements(PrimitiveType.Triangles, mesh.visibleIndices.Count, DrawElementsType.UnsignedInt, 0);
            }
        }
        
        public void DrawOnlyPosAndNormal(GameState gameRunning, Shader shader, VAO vao_, VBO vbo_, IBO ibo_)
        {
            if (!parentObject.isEnabled)
                return;

            vao_.Bind();
            shader.Use();
            SendUniformsOnlyPos(shader);

            foreach (MeshData mesh in model.meshes)
            {
                if (!recalculateOnlyPosAndNormal)
                {
                    if (gameRunning == GameState.Stopped && mesh.visibleVerticesDataOnlyPosAndNormal.Count > 0)
                    {
                        ibo_.Buffer(mesh.visibleIndices);
                        vbo_.Buffer(mesh.visibleVerticesDataOnlyPosAndNormal);
                        GL.DrawElements(PrimitiveType.Triangles, mesh.visibleIndices.Count, DrawElementsType.UnsignedInt, 0);
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
                GL.DrawElements(PrimitiveType.Triangles, mesh.visibleIndices.Count, DrawElementsType.UnsignedInt, 0);
            }
        }

        //public List<float> DrawNotOccluded(List<triangle> notOccludedTris)
        //{
        //    Vao.Bind();

        //    vertices = new List<float>();

        //    ObjectType type = parentObject.GetObjectType();

        //    ParallelOptions parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = threadSize };
        //    Parallel.ForEach(notOccludedTris, parallelOptions,
        //         () => new List<float>(),
        //         (tri, loopState, localVertices) =>
        //         {
        //             if (tri.visibile)
        //             {
        //                 AddVertices(localVertices, tri);
        //             }
        //             return localVertices;
        //         },
        //         localVertices =>
        //         {
        //             lock (vertices)
        //             {
        //                 vertices.AddRange(localVertices);
        //             }
        //         });

        //    SendUniforms();

        //    if (parentObject.texture != null)
        //    {
        //        parentObject.texture.Bind();
        //        if (parentObject.textureNormal != null)
        //            parentObject.textureNormal.Bind();
        //        if (parentObject.textureHeight != null)
        //            parentObject.textureHeight.Bind();
        //        if (parentObject.textureAO != null)
        //            parentObject.textureAO.Bind();
        //        if (parentObject.textureRough != null)
        //            parentObject.textureRough.Bind();
        //        if (parentObject.textureMetal != null)
        //            parentObject.textureMetal.Bind();
        //    }

        //    return vertices;
        //}

        
        //public List<float> DrawOnlyPos(VAO aabbVao, Shader shader)
        //{
        //    aabbVao.Bind();

        //    vertices = new List<float>();

        //    ObjectType type = parentObject.GetObjectType();

        //    Matrix4 s = Matrix4.CreateScale(parentObject.Scale);
        //    Matrix4 r = Matrix4.CreateFromQuaternion(parentObject.Rotation);
        //    Matrix4 t = Matrix4.CreateTranslation(parentObject.GetPosition());

        //    Matrix4 transformMatrix = Matrix4.Identity;
        //    transformMatrix = s * r * t;

        //    ParallelOptions parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = threadSize };
        //    Parallel.ForEach(tris, parallelOptions,
        //         () => new List<float>(),
        //         (tri, loopState, localVertices) =>
        //         {
        //             if (tri.visibile)
        //             {
        //                 AddVerticesOnlyPos(localVertices, tri, ref transformMatrix);
        //             }
        //             return localVertices;
        //         },
        //         localVertices =>
        //         {
        //             lock (vertices)
        //             {
        //                 vertices.AddRange(localVertices);
        //             }
        //         });

        //    SendUniformsOnlyPos(shader);

        //    return vertices;
        //}

        public void GetCookedData(out PxVec3[] verts, out int[] indicesOut)
        {
            List<PxVec3> verts_ = new List<PxVec3>();
            List<int> indicesOut_ = new List<int>();

            int lastIndex = 0;

            for (int i = 0; i < model.meshes.Count; i++)
            {
                MeshData mesh = model.meshes[i];

                int largestIndex = (int)mesh.indices.Max();

                for (int j = 0; j < mesh.allVerts.Count(); j++)
                {
                    verts_.Add(ConvertToNDCPxVec3(j, i));
                }

                for (int j = 0; j < mesh.indices.Count; j += 3)
                {
                    indicesOut_.Add(mesh.uniqueVertices[(int)mesh.indices[j]].pi + lastIndex);
                    indicesOut_.Add(mesh.uniqueVertices[(int)mesh.indices[j + 1]].pi + lastIndex);
                    indicesOut_.Add(mesh.uniqueVertices[(int)mesh.indices[j + 2]].pi + lastIndex);
                }

                lastIndex += largestIndex;
            }

            verts = verts_.ToArray();
            indicesOut = indicesOut_.ToArray();
        }
    }
}
