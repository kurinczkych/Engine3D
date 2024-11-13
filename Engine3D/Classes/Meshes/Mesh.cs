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
using OpenTK.Windowing.Common;
using Node = Assimp.Node;
using System.Text.Json.Serialization;

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

        [JsonIgnore]
        private Matrix4 viewMatrix, projectionMatrix;
        [JsonIgnore]
        private Vector3 cameraPos;

        public AnimationClip? animation;

        private VAO Vao;
        private VBO Vbo;

        public Mesh() : base()
        {
            
        }

        // Custom Mesh With Texture
        public Mesh(VAO vao, VBO vbo, int shaderProgramId, string relativeModelPath, string texturePath, Vector2 windowSize, ref Camera camera, ref Object parentObject) : base(vao.id, vbo.id, shaderProgramId)
        {
            this.parentObject = parentObject;
            this.parentObject.name = Path.GetFileName(relativeModelPath);
            this.shaderProgramId = shaderProgramId;

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

            ComputeNormalsIfNeeded();
            //ComputeTangents();

            GetUniformLocations();
            SendUniforms();
        }

        // Custom Mesh Without Texture
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
            modelName_ = Path.GetFileName(relativeModelPath);
            ProcessObj(relativeModelPath);

            ComputeNormalsIfNeeded();
            //ComputeTangents();

            GetUniformLocations();
            SendUniforms();
        }

        // Uniform Mesh With Texture
        public Mesh(VAO vao, VBO vbo, int shaderProgramId, string modelName, ModelData model, string texturePath, Vector2 windowSize, ref Camera camera, ref Object parentObject) : base(vao.id, vbo.id, shaderProgramId)
        {
            this.parentObject = parentObject;
            this.parentObject.name = modelName;
            this.shaderProgramId = shaderProgramId;

            bool success = false;
            texture = Engine.textureManager.AddTexture(texturePath, out success);
            if(!success)
                Engine.consoleManager.AddLog("Texture: " + texturePath + "was not found!", LogType.Warning);

            Vao = vao;
            Vbo = vbo;

            this.windowSize = windowSize;
            this.camera = camera;

            this.modelName_ = modelName;
            this.model = model;

            ComputeNormalsIfNeeded();
            //ComputeTangents();

            GetUniformLocations();
            SendUniforms();
        }

        // Uniform Mesh Without Texture
        public Mesh(VAO vao, VBO vbo, int shaderProgramId, string modelName, ModelData model, Vector2 windowSize, ref Camera camera, ref Object parentObject) : base(vao.id, vbo.id, shaderProgramId)
        {
            this.parentObject = parentObject;
            this.parentObject.name = modelName;
            this.shaderProgramId = shaderProgramId;

            Vao = vao;
            Vbo = vbo;

            this.windowSize = windowSize;
            this.camera = camera;

            this.modelName_ = modelName;
            this.model = model;

            ComputeNormalsIfNeeded();
            //ComputeTangents();

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
            Vector3 v = Vector3.TransformPosition(AHelp.AssimpToOpenTK(model.meshes[meshIndex].mesh.Vertices[index]), modelMatrix);

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
            #endregion
        }

        public void GetUniformLocationsAnim(Shader shader)
        {
            shader.Use();

            uniformAnimLocations.Add("windowSize", GL.GetUniformLocation(shader.programId, "windowSize"));
            uniformAnimLocations.Add("modelMatrix", GL.GetUniformLocation(shader.programId, "modelMatrix"));
            uniformAnimLocations.Add("viewMatrix", GL.GetUniformLocation(shader.programId, "viewMatrix"));
            uniformAnimLocations.Add("nodeMatrix", GL.GetUniformLocation(shader.programId, "nodeMatrix"));
            uniformAnimLocations.Add("projectionMatrix", GL.GetUniformLocation(shader.programId, "projectionMatrix"));
            uniformAnimLocations.Add("cameraPosition", GL.GetUniformLocation(shader.programId, "cameraPosition"));
            uniformAnimLocations.Add("useBillboarding", GL.GetUniformLocation(shader.programId, "useBillboarding"));
            uniformAnimLocations.Add("useShading", GL.GetUniformLocation(shader.programId, "useShading"));
            uniformAnimLocations.Add("useAnimation", GL.GetUniformLocation(shader.programId, "useAnimation"));
            uniformAnimLocations.Add("boneMatrices", GL.GetUniformLocation(shader.programId, "boneMatrices"));

            #region TextureLocations
            uniformAnimLocations.Add("useTexture", GL.GetUniformLocation(shader.programId, "useTexture"));
            uniformAnimLocations.Add("useNormal", GL.GetUniformLocation(shader.programId, "useNormal"));
            uniformAnimLocations.Add("useHeight", GL.GetUniformLocation(shader.programId, "useHeight"));
            uniformAnimLocations.Add("useAO", GL.GetUniformLocation(shader.programId, "useAO"));
            uniformAnimLocations.Add("useRough", GL.GetUniformLocation(shader.programId, "useRough"));
            uniformAnimLocations.Add("useMetal", GL.GetUniformLocation(shader.programId, "useMetal"));
            if (texture != null)
            {
                uniformAnimLocations.Add("textureSampler", GL.GetUniformLocation(shader.programId, "textureSampler"));
                if (textureNormal != null)
                {
                    uniformAnimLocations.Add("textureSamplerNormal", GL.GetUniformLocation(shader.programId, "textureSamplerNormal"));
                }
                if (textureHeight != null)
                {
                    uniformAnimLocations.Add("textureSamplerHeight", GL.GetUniformLocation(shader.programId, "textureSamplerHeight"));
                }
                if (textureAO != null)
                {
                    uniformAnimLocations.Add("textureSamplerAO", GL.GetUniformLocation(shader.programId, "textureSamplerAO"));
                }
                if (textureRough != null)
                {
                    uniformAnimLocations.Add("textureSamplerRough", GL.GetUniformLocation(shader.programId, "textureSamplerRough"));
                }
                if (textureMetal != null)
                {
                    uniformAnimLocations.Add("textureSamplerMetal", GL.GetUniformLocation(shader.programId, "textureSamplerMetal"));
                }
            }
            #endregion
        }

        protected override void SendUniforms()
        {
            if(Engine.GLState.currentShaderId != shaderProgramId)
            {
                GL.UseProgram(shaderProgramId);
                Engine.GLState.currentShaderId = shaderProgramId;
            }

            projectionMatrix = camera.projectionMatrix;
            viewMatrix = camera.viewMatrix;

            if (!uniformLocations.ContainsKey("modelMatrix") || Engine.reloadUniformLocations)
            {
                uniformLocations.Clear();
                GetUniformLocations();
            }

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

            if (uniformLocations["useTexture"] != -1) GL.Uniform1(uniformLocations["useTexture"], texture != null ? 1 : 0);
            if(uniformLocations["useNormal"] != -1) GL.Uniform1(uniformLocations["useNormal"], textureNormal != null ? 1 : 0);
            if(uniformLocations["useHeight"] != -1) GL.Uniform1(uniformLocations["useHeight"], textureHeight != null ? 1 : 0);
            if(uniformLocations["useAO"] != -1) GL.Uniform1(uniformLocations["useAO"], textureAO != null ? 1 : 0);
            if(uniformLocations["useRough"] != -1) GL.Uniform1(uniformLocations["useRough"], textureRough != null ? 1 : 0);
            if (uniformLocations["useMetal"] != -1) GL.Uniform1(uniformLocations["useMetal"], textureMetal != null ? 1 : 0);
        }

        protected void SendAnimationUniforms(int meshId, double delta, Shader sh)
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
            if (texture != null)
            {
                GL.Uniform1(uniformAnimLocations["textureSampler"], texture.TextureUnit);
                if (textureNormal != null)
                {
                    GL.Uniform1(uniformAnimLocations["textureSamplerNormal"], textureNormal.TextureUnit);
                }
                if (textureHeight != null)
                {
                    GL.Uniform1(uniformAnimLocations["textureSamplerHeight"], textureHeight.TextureUnit);
                }
                if (textureAO != null)
                {
                    GL.Uniform1(uniformAnimLocations["textureSamplerAO"], textureAO.TextureUnit);
                }
                if (textureRough != null)
                {
                    GL.Uniform1(uniformAnimLocations["textureSamplerRough"], textureRough.TextureUnit);
                }
                if (textureMetal != null)
                {
                    GL.Uniform1(uniformAnimLocations["textureSamplerMetal"], textureMetal.TextureUnit);
                }
            }

            GL.Uniform1(uniformAnimLocations["useTexture"], texture != null ? 1 : 0);
            GL.Uniform1(uniformAnimLocations["useNormal"], textureNormal != null ? 1 : 0);
            GL.Uniform1(uniformAnimLocations["useHeight"], textureHeight != null ? 1 : 0);
            GL.Uniform1(uniformAnimLocations["useAO"], textureAO != null ? 1 : 0);
            GL.Uniform1(uniformAnimLocations["useRough"], textureRough != null ? 1 : 0);
            GL.Uniform1(uniformAnimLocations["useMetal"], textureMetal != null ? 1 : 0);
            GL.Uniform1(uniformAnimLocations["useAnimation"], 1);

            if (animation != null)
            {
                //animation.AnimateKeyFrames(model.skeleton.RootBone, animation.GetLocalTimer());
                //model.skeleton.UpdateSkeleton();
                //animation.Update(delta);
                //model.skeleton.UpdateSkeleton(animation:animation);
                //model.skeleton.UpdateBoneMatrices(ref model.boneMatrices);

                //List<int> indexes = new List<int>();
                //model.skeleton.SendToGpu(null, ref uniformAnimLocations, ref indexes);
                //string ab = "";
                //foreach(var a in indexes)
                //    ab += a.ToString() + "\n";
                //;

                //for (int i = 0; i < animation.AnimationMatrices.Count; i++)
                //{
                //    string boneName = animation.AnimationMatrices.Keys.ElementAt(i);
                //    //Matrix4 animMatrix = animation.AnimationMatrices[boneName];
                //    Bone bone = model.skeleton.GetBone(boneName);

                //    if(bone == null || bone.BoneIndex <= -1)
                //    {
                //        continue;
                //    }

                //    Matrix4 boneMatrix = bone.FinalTransform;
                //    //Matrix4 boneMatrix = model.boneMatrices[bone.BoneIndex];
                //    //for (int x = 0; x < 4; x++)
                //    //{
                //    //    for (int y = 0; y < 4; y++)
                //    //    {
                //    //        boneMatrix[x, y] = (float)Math.Round(boneMatrix[x, y], 5);
                //    //        animMatrix[x, y] = (float)Math.Round(animMatrix[x, y], 5);
                //    //        if (boneMatrix[x, y] == -0)
                //    //            boneMatrix[x, y] = 0;
                //    //        if (animMatrix[x, y] == -0)
                //    //            animMatrix[x, y] = 0;
                //    //    }
                //    //}
                //    //Matrix4 final = Matrix4.Transpose(boneAnim.Transformations[currentAnim]) * Matrix4.Transpose(boneMatrix);
                //    //Matrix4 final = Matrix4.Transpose(boneAnim.Transformations[currentAnim]) * boneMatrix;
                //    //Matrix4 final = boneMatrix * Matrix4.Transpose(boneAnim.Transformations[currentAnim]);
                //    //Matrix4 final = boneAnim.Transformations[currentAnim] * boneMatrix;
                //    Matrix4 final = boneMatrix;
                //    //Matrix4 final = boneMatrix * animMatrix;
                //    //Matrix4 final = animMatrix * boneMatrix;
                //    //var a1 = animMatrix.ExtractRotation();
                //    //var a2 = animMatrix.ExtractTranslation();
                //    //var a3 = animMatrix.ExtractScale();
                //    //string b1 = boneName + ": " + a1.W.ToString() + " " + a1.X.ToString() + " " + a1.Y.ToString() + " " + a1.Z.ToString();
                //    //string b2 = boneName + ": " + a2.X.ToString() + " " + a2.Y.ToString() + " " + a2.Z.ToString();
                //    //string b3 = boneName + ": " + a3.X.ToString() + " " + a3.Y.ToString() + " " + a3.Z.ToString();
                //    //Engine.consoleManager.AddLog(b1);
                //    //Engine.consoleManager.AddLog(b2);
                //    //Engine.consoleManager.AddLog(b3);
                //    //Engine.consoleManager.AddLog(animation.GetLocalTimer().ToString());
                //    //Matrix4 final = Matrix4.Transpose(boneMatrix);
                //    //Matrix4 final = Matrix4.Identity;
                //    //if (boneName != "Bone.001")
                //    //    final = Matrix4.Identity;

                //    GL.UniformMatrix4(uniformAnimLocations["boneMatrices"] + bone.BoneIndex, true, ref final);
                //}

                //for (int i = 0; i < model.meshes[meshId].boneMatrices.Count; i++)
                //{
                //    Bone bone = model.meshes[meshId].boneMatrices
                //    var boneName = model.meshes[meshId].boneMatrices.Keys.ElementAt(i);



                //    Matrix4 final = model.meshes[meshId].boneMatrices[boneName].FinalMatrix;

                //    // Send to GPU
                //    GL.UniformMatrix4(uniformAnimLocations["boneMatrices"] + i, true, ref final);
                //}



                //Engine.consoleManager.AddLog(currentAnim.ToString());


            }
            else
            {
                throw new NotImplementedException();

                //for (int i = 0; i < model.animationMatrices.Count; i++)
                //{
                //    Matrix4 boneMatrix = Matrix4.Identity;

                //    GL.UniformMatrix4(uniformAnimLocations["boneMatrices"] + i, true, ref boneMatrix);
                //}
            }
        }

        public void SendUniformsOnlyPos(Shader shader, Matrix4? otherProj, Matrix4? otherView)
        {
            if (otherProj != null)
                projectionMatrix = otherProj.Value;
            else
                projectionMatrix = camera.projectionMatrix;

            if (otherView != null)
                viewMatrix = otherView.Value;
            else
                viewMatrix = camera.viewMatrix;

            cameraPos = camera.GetPosition();

            GL.UniformMatrix4(GL.GetUniformLocation(shader.programId, "modelMatrix"), true, ref modelMatrix);
            GL.UniformMatrix4(GL.GetUniformLocation(shader.programId, "viewMatrix"), true, ref viewMatrix);
            GL.UniformMatrix4(GL.GetUniformLocation(shader.programId, "projectionMatrix"), true, ref projectionMatrix);
            //GL.Uniform3(GL.GetUniformLocation(shader.id, "cameraPos"), cameraPos);

            //GL.UniformMatrix4(GL.GetUniformLocation(shader.id, "_scaleMatrix"), true, ref scaleMatrix);
            //GL.UniformMatrix4(GL.GetUniformLocation(shader.id, "_rotMatrix"), true, ref rotationMatrix);
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
                        if (mesh.visibleIndices.Count == 0)
                            continue;

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

                if (mesh.visibleIndices.Count == 0)
                    continue;

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
                GL.DrawElements(PrimitiveType.Triangles, mesh.visibleIndices.Count, DrawElementsType.UnsignedInt, 0);
            }
        }
        
        public void DrawAnimated(GameState gameRunning, Shader shader, VAO vao_, VBO vbo_, IBO ibo_, double delta)
        {
            if (!parentObject.isEnabled)
                return;

            vao_.Bind();
            shader.Use();

            //for(int i = 0; i < model.meshes.Count; i++)
            for(int i = 0; i < 1; i++)
            {
                MeshData mesh = model.meshes[i];
                SendAnimationUniforms(i, delta, shader);

                if (!recalculate)
                {
                    if (gameRunning == GameState.Stopped && mesh.visibleIndices.Count > 0)
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
                vbo_.Buffer(mesh.visibleVerticesDataWithAnim);
                GL.DrawElements(PrimitiveType.Triangles, mesh.visibleIndices.Count, DrawElementsType.UnsignedInt, 0);
            }
        }

        public void DrawOnlyPos(GameState gameRunning, Shader shader, VAO vao_, VBO vbo_, IBO ibo_, Matrix4? otherProj = null, Matrix4? otherView = null)
        {
            if (!parentObject.isEnabled)
                return;

            vao_.Bind();
            shader.Use();
            SendUniformsOnlyPos(shader, otherProj, otherView);

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
        
        public void DrawOnlyPosAndNormal(GameState gameRunning, Shader shader, VAO vao_, VBO vbo_, IBO ibo_, Matrix4? otherProj = null, Matrix4? otherView = null)
        {
            if (!parentObject.isEnabled)
                return;

            vao_.Bind();
            shader.Use();
            SendUniformsOnlyPos(shader, otherProj, otherView);

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

        //    if (texture != null)
        //    {
        //        texture.Bind();
        //        if (textureNormal != null)
        //            textureNormal.Bind();
        //        if (textureHeight != null)
        //            textureHeight.Bind();
        //        if (textureAO != null)
        //            textureAO.Bind();
        //        if (textureRough != null)
        //            textureRough.Bind();
        //        if (textureMetal != null)
        //            textureMetal.Bind();
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

            // Loop through all meshes in the model
            for (int i = 0; i < model.meshes.Count; i++)
            {
                MeshData meshData = model.meshes[i];
                Assimp.Mesh mesh = meshData.mesh;

                // Calculate the largest index for adjusting indices later
                int largestIndex = mesh.Faces.Max(f => f.Indices.Max());

                // Convert vertices using the provided ConvertToNDCPxVec3 method
                for (int j = 0; j < mesh.Vertices.Count; j++)
                {
                    // Convert each vertex to PxVec3 and add to the list, using the index and mesh index
                    verts_.Add(ConvertToNDCPxVec3(j, i)); // Here we pass the vertex index (j) and the mesh index (i)
                }

                // Convert indices using pis to remap them correctly
                for (int j = 0; j < mesh.Faces.Count; j++)
                {
                    Assimp.Face face = mesh.Faces[j];

                    indicesOut_.Add(meshData.pis[face.Indices[0]] + lastIndex);
                    indicesOut_.Add(meshData.pis[face.Indices[1]] + lastIndex);
                    indicesOut_.Add(meshData.pis[face.Indices[2]] + lastIndex);
                }

                // Update lastIndex to adjust indices for the next mesh
                lastIndex += largestIndex + 1; // Add 1 to handle zero-based index properly
            }

            // Convert lists to arrays
            verts = verts_.ToArray();
            indicesOut = indicesOut_.ToArray();
        }
    }
}
