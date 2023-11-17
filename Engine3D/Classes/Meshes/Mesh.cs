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

#pragma warning disable CS8600
#pragma warning disable CS8604
#pragma warning disable CS0728

namespace Engine3D
{

    public class Mesh : BaseMesh
    {
        public static int floatCount = 15;

        public bool drawNormals = false;
        public WireframeMesh normalMesh;

        private List<float> vertices = new List<float>();
        public List<float> verticesOnlyPos = new List<float>();
        private List<float> verticesOnlyPosAndNormal = new List<float>();

        private Vector2 windowSize;

        private Matrix4 viewMatrix, projectionMatrix;
        private Vector3 cameraPos;

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

            if (tris.Count() > 0 && !tris[0].gotPointNormals)
            {
                ComputeVertexNormalsSpherical();
            }
            else if (tris.Count() > 0 && tris[0].gotPointNormals)
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

            if (tris.Count() > 0 && !tris[0].gotPointNormals)
            {
                ComputeVertexNormalsSpherical();
            }
            else if (tris.Count() > 0 && tris[0].gotPointNormals)
                ComputeVertexNormals();

            ComputeTangents();

            GetUniformLocations();
            SendUniforms();
        }

        public Mesh(VAO vao, VBO vbo, int shaderProgramId, string modelName, List<triangle> tris, string texturePath, Vector2 windowSize, ref Camera camera, ref Object parentObject) : base(vao.id, vbo.id, shaderProgramId)
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
            this.tris = new List<triangle>(tris);

            if (tris.Count() > 0 && !tris[0].gotPointNormals)
            {
                ComputeVertexNormalsSpherical();
            }
            else if (tris.Count() > 0 && tris[0].gotPointNormals)
                ComputeVertexNormals();

            ComputeTangents();

            GetUniformLocations();
            SendUniforms();
        }

        public Mesh(VAO vao, VBO vbo, int shaderProgramId, string modelName, List<triangle> tris, Vector2 windowSize, ref Camera camera, ref Object parentObject) : base(vao.id, vbo.id, shaderProgramId)
        {
            this.parentObject = parentObject;
            this.parentObject.name = modelName;
            this.shaderProgramId = shaderProgramId;

            Vao = vao;
            Vbo = vbo;

            this.windowSize = windowSize;
            this.camera = camera;

            this.modelName = modelName;
            this.tris = new List<triangle>(tris);

            if (tris.Count() > 0 && !tris[0].gotPointNormals)
            {
                ComputeVertexNormalsSpherical();
            }
            else if (tris.Count() > 0 && tris[0].gotPointNormals)
                ComputeVertexNormals();

            ComputeTangents();

            GetUniformLocations();
            SendUniforms();
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

        private PxVec3 ConvertToNDCPxVec3(int index)
        {
            Vector3 v = Vector3.TransformPosition(allVerts[index], modelMatrix);

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
            cameraPos = camera.GetPosition();

            GL.UniformMatrix4(GL.GetUniformLocation(shader.id, "modelMatrix"), true, ref modelMatrix);
            GL.UniformMatrix4(GL.GetUniformLocation(shader.id, "viewMatrix"), true, ref viewMatrix);
            GL.UniformMatrix4(GL.GetUniformLocation(shader.id, "projectionMatrix"), true, ref projectionMatrix);
            GL.Uniform3(GL.GetUniformLocation(shader.id, "cameraPos"), cameraPos);

            GL.UniformMatrix4(GL.GetUniformLocation(shader.id, "_scaleMatrix"), true, ref scaleMatrix);
            GL.UniformMatrix4(GL.GetUniformLocation(shader.id, "_rotMatrix"), true, ref rotationMatrix);
        }

        private void AddVertices(List<float> vertices, Vertex v)
        {
            lock (vertices) // Lock to ensure thread-safety when modifying the list
            {
                vertices.AddRange(v.GetData());
            }
        }

        private void AddVerticesOnlyPos(List<float> vertices, triangle tri)
        {
            lock (vertices) // Lock to ensure thread-safety when modifying the list
            {
                ConvertToNDCOnlyPos(ref vertices, tri, 0);
                ConvertToNDCOnlyPos(ref vertices, tri, 1);
                ConvertToNDCOnlyPos(ref vertices, tri, 2);
            }
        }

        private void AddVerticesOnlyPosAndNormal(List<float> vertices, triangle tri)
        {
            lock (vertices) // Lock to ensure thread-safety when modifying the list
            {
                ConvertToNDCOnlyPosAndNormal(ref vertices, tri, 0);
                ConvertToNDCOnlyPosAndNormal(ref vertices, tri, 1);
                ConvertToNDCOnlyPosAndNormal(ref vertices, tri, 2);
            }
        }

        public (List<float>, List<uint>) Draw(GameState gameRunning)
        {
            if (!parentObject.isEnabled)
                return (new List<float>(), new List<uint>());

            Vao.Bind();

            if (!recalculate)
            {
                if (gameRunning == GameState.Stopped && visibleIndices.Count > 0)
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

                    return (visibleVerticesData, visibleIndices);
                }
            }
            else
            {
                recalculate = false;
                CalculateFrustumVisibility();
            }

            if (parentObject.isSelected)
                verticesOnlyPos.Clear();

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

            return (visibleVerticesData, visibleIndices);
        }

        public List<float> DrawOnlyPos(GameState gameRunning, Shader shader, VAO _vao)
        {
            if (!parentObject.isEnabled)
                return new List<float>();

            _vao.Bind();

            if (!recalculateOnlyPos)
            {
                if (gameRunning == GameState.Stopped && verticesOnlyPos.Count > 0)
                {
                    SendUniformsOnlyPos(shader);

                    return verticesOnlyPos;
                }
            }
            else
            {
                recalculateOnlyPos = false;
                CalculateFrustumVisibility();
            }

            verticesOnlyPos = new List<float>();

            ParallelOptions parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = threadSize };
            Parallel.ForEach(tris, parallelOptions,
                () => new List<float>(),
                 (tri, loopState, localVertices) =>
                 {
                     if (tri.visibile)
                     {
                         AddVerticesOnlyPos(localVertices, tri);
                     }
                     return localVertices;
                 },
                 localVertices =>
                 {
                     lock (verticesOnlyPos)
                     {
                         verticesOnlyPos.AddRange(localVertices);
                     }
                 });

            SendUniformsOnlyPos(shader);

            return verticesOnlyPos;
        }
        
        public List<float> DrawOnlyPosAndNormal(GameState gameRunning, Shader shader, VAO _vao)
        {
            if (!parentObject.isEnabled)
                return new List<float>();

            _vao.Bind();

            if (!recalculateOnlyPosAndNormal)
            {
                if (gameRunning == GameState.Stopped && verticesOnlyPosAndNormal.Count > 0)
                {
                    SendUniformsOnlyPos(shader);

                    return verticesOnlyPosAndNormal;
                }
            }
            else
            {
                recalculateOnlyPosAndNormal = false;
                CalculateFrustumVisibility();
            }

            verticesOnlyPosAndNormal = new List<float>();

            ParallelOptions parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = threadSize };
            Parallel.ForEach(tris, parallelOptions,
                () => new List<float>(),
                 (tri, loopState, localVertices) =>
                 {
                     if (tri.visibile)
                     {
                         AddVerticesOnlyPosAndNormal(localVertices, tri);
                     }
                     return localVertices;
                 },
                 localVertices =>
                 {
                     lock (verticesOnlyPosAndNormal)
                     {
                         verticesOnlyPosAndNormal.AddRange(localVertices);
                     }
                 });

            SendUniformsOnlyPos(shader);

            return verticesOnlyPosAndNormal;
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

        public void GetCookedData(out PxVec3[] verts, out int[] indices)
        {
            int vertCount = tris.Count() * 3;
            int index = 0;
            verts = new PxVec3[allVerts.Count()];
            indices = new int[vertCount];

            for (int i = 0; i < allVerts.Count(); i++)
            {
                verts[i] = ConvertToNDCPxVec3(i);
            }

            foreach (triangle tri in tris)
            {
                indices[index] = tri.v[0].pi;
                index++;
                indices[index] = tri.v[1].pi;
                index++;
                indices[index] = tri.v[2].pi;
                index++;
            }
        }

        public void OnlyTriangle()
        {
            tris = new List<triangle>
                {
                    new triangle(new Vector3[] { new Vector3(0.0f, 0.0f, 0.0f), new Vector3(0.0f, 1.0f, 0.0f), new Vector3(1.0f, 1.0f, 0.0f) }, new Vec2d[] { new Vec2d(0.0f, 1.0f), new Vec2d(0.0f, 0.0f), new Vec2d(1.0f, 0.0f) })
                };
        }
    }
}
