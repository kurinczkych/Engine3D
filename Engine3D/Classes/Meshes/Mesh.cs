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

#pragma warning disable CS8600
#pragma warning disable CS8604
#pragma warning disable CS0728

namespace Engine3D
{

    public class Mesh : BaseMesh
    {
        public static int floatCount = 16;

        public bool drawNormals = false;
        public WireframeMesh normalMesh;

        public int useTexture;
        public Texture texture;

        private List<float> vertices = new List<float>();
        private string? modelName;


        public Vector3 Scale;
        private bool IsTransformed
        {
            get
            {
                return !(parentObject.Position == Vector3.Zero && parentObject.Rotation == Quaternion.Identity && Scale == Vector3.One);
            }
        }

        private Camera camera;
        private Vector2 windowSize;

        Matrix4 modelMatrix, viewMatrix, projectionMatrix;

        private VAO Vao;
        private VBO Vbo;

        public Mesh(VAO vao, VBO vbo, int shaderProgramId, string modelName, string textureName, Vector2 windowSize, ref Frustum frustum, ref Camera camera, ref int textureCount) : base(vao.id, vbo.id, shaderProgramId)
        {
            texture = new Texture(textureCount, textureName);
            textureCount += texture.textureDescriptor.count;
            useTexture = 1;

            Vao = vao;
            Vbo = vbo;

            this.windowSize = windowSize;
            this.camera = camera;

            Scale = Vector3.One;

            this.modelName = modelName;
            ProcessObj(modelName);

            ComputeVertexNormals(ref tris);
            ComputeTangents(ref tris);

            GetUniformLocations();
            SendUniforms();
        }

        public Mesh(VAO vao, VBO vbo, int shaderProgramId, string modelName, Vector2 windowSize, ref Frustum frustum, ref Camera camera, ref int textureCount) : base(vao.id, vbo.id, shaderProgramId)
        {
            useTexture = 0;

            Vao = vao;
            Vbo = vbo;

            this.windowSize = windowSize;
            this.camera = camera;

            Scale = Vector3.One;

            this.modelName = modelName;
            ProcessObj(modelName);

            ComputeVertexNormals(ref tris);
            ComputeTangents(ref tris);

            GetUniformLocations();
            SendUniforms();
        }

        //public Mesh(VAO vao, VBO vbo, int shaderProgramId, string textureName, Vector2 windowSize, ref Frustum frustum, ref Camera camera, ref int textureCount) : base(vao.id, vbo.id, shaderProgramId)
        //{
        //    texture = new Texture(textureCount, textureName);
        //    textureCount += texture.textureDescriptor.count;
        //    useTexture = 1;

        //    Vao = vao;
        //    Vbo = vbo;

        //    this.windowSize = windowSize;
        //    this.camera = camera;

        //    Scale = Vector3.One;
        //}

        public Mesh(VAO vao, VBO vbo, int shaderProgramId, List<triangle> tris, string textureName, Vector2 windowSize, ref Frustum frustum, ref Camera camera, ref int textureCount) : base(vao.id, vbo.id, shaderProgramId)
        {
            texture = new Texture(textureCount, textureName);
            textureCount += texture.textureDescriptor.count;
            useTexture = 1;

            Vao = vao;
            Vbo = vbo;

            this.windowSize = windowSize;
            this.camera = camera;

            Scale = Vector3.One;

            this.tris = new List<triangle>(tris);

            ComputeVertexNormals(ref tris);
            ComputeTangents(ref tris);

            GetUniformLocations();
            SendUniforms();
        }

        public Mesh(VAO vao, VBO vbo, int shaderProgramId, List<triangle> tris, Vector2 windowSize, ref Frustum frustum, ref Camera camera, ref int textureCount) : base(vao.id, vbo.id, shaderProgramId)
        {
            useTexture = 0;

            Vao = vao;
            Vbo = vbo;

            this.windowSize = windowSize;
            this.camera = camera;

            Scale = Vector3.One;

            this.tris = new List<triangle>(tris);

            ComputeVertexNormals(ref tris);
            ComputeTangents(ref tris);

            GetUniformLocations();
            SendUniforms();
        }

        private void ConvertToNDC(ref List<float> vertices, triangle tri, int index, ref Matrix4 transformMatrix, bool isTransformed)
        {
            if (isTransformed)
            {
                Vector3 v = Vector3.TransformPosition(tri.p[index], transformMatrix);

                vertices.AddRange(new float[]
                {
                    v.X, v.Y, v.Z, 1.0f,
                    tri.n[index].X, tri.n[index].Y, tri.n[index].Z,
                    tri.t[index].u, tri.t[index].v,
                    tri.c[index].R, tri.c[index].G, tri.c[index].B, tri.c[index].A,
                    tri.tan[index].X, tri.tan[index].Y, tri.tan[index].Z
                });
            }
            else
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
        }

        private void ConvertToNDCOnlyPos(ref List<float> vertices, triangle tri, int index, ref Matrix4 transformMatrix, bool isTransformed)
        {
            if (isTransformed)
            {
                Vector3 v = Vector3.TransformPosition(tri.p[index], transformMatrix);

                vertices.AddRange(new float[] // Setting initial capacity to 3
                {
                    v.X, v.Y, v.Z
                });
            }
            else
            {
                vertices.AddRange(new float[] // Setting initial capacity to 3
                {
                    tri.p[index].X, tri.p[index].Y, tri.p[index].Z,
                });
            }
        }

        private PxVec3 ConvertToNDCPxVec3(int index, ref Matrix4 transformMatrix)
        {
            Vector3 v = Vector3.TransformPosition(allVerts[index], transformMatrix);

            PxVec3 vec = new PxVec3();
            vec.x = v.X;
            vec.y = v.Y;
            vec.z = v.Z;

            return vec;
        }

        public void UpdateFrustumAndCamera(ref Camera camera)
        {
            this.camera = camera;
        }

        private void GetUniformLocations()
        {
            uniformLocations.Add("windowSize", GL.GetUniformLocation(shaderProgramId, "windowSize"));
            uniformLocations.Add("modelMatrix", GL.GetUniformLocation(shaderProgramId, "modelMatrix"));
            uniformLocations.Add("viewMatrix", GL.GetUniformLocation(shaderProgramId, "viewMatrix"));
            uniformLocations.Add("projectionMatrix", GL.GetUniformLocation(shaderProgramId, "projectionMatrix"));
            uniformLocations.Add("cameraPosition", GL.GetUniformLocation(shaderProgramId, "cameraPosition"));
            uniformLocations.Add("useTexture", GL.GetUniformLocation(shaderProgramId, "useTexture"));
            if (texture != null)
            {
                uniformLocations.Add("textureSampler", GL.GetUniformLocation(shaderProgramId, "textureSampler"));
                if (texture.textureDescriptor.Normal != "")
                {
                    uniformLocations.Add("textureSamplerNormal", GL.GetUniformLocation(shaderProgramId, "textureSamplerNormal"));
                    uniformLocations.Add("useNormal", GL.GetUniformLocation(shaderProgramId, "useNormal"));
                }
                if (texture.textureDescriptor.Height != "")
                {
                    uniformLocations.Add("textureSamplerHeight", GL.GetUniformLocation(shaderProgramId, "textureSamplerHeight"));
                    uniformLocations.Add("useHeight", GL.GetUniformLocation(shaderProgramId, "useHeight"));
                }
                if (texture.textureDescriptor.AO != "")
                {
                    uniformLocations.Add("textureSamplerAO", GL.GetUniformLocation(shaderProgramId, "textureSamplerAO"));
                    uniformLocations.Add("useAO", GL.GetUniformLocation(shaderProgramId, "useAO"));
                }
                if (texture.textureDescriptor.Rough != "")
                {
                    uniformLocations.Add("textureSamplerRough", GL.GetUniformLocation(shaderProgramId, "textureSamplerRough"));
                    uniformLocations.Add("useRough", GL.GetUniformLocation(shaderProgramId, "useRough"));
                }
                if (texture.textureDescriptor.Metal != "")
                {
                    uniformLocations.Add("textureSamplerMetal", GL.GetUniformLocation(shaderProgramId, "textureSamplerMetal"));
                    uniformLocations.Add("useMetal", GL.GetUniformLocation(shaderProgramId, "useMetal"));
                }
            }
        }

        protected override void SendUniforms()
        {
            modelMatrix = Matrix4.Identity;
            projectionMatrix = camera.projectionMatrix;
            viewMatrix = camera.viewMatrix;

            GL.UniformMatrix4(uniformLocations["modelMatrix"], true, ref modelMatrix);
            GL.UniformMatrix4(uniformLocations["viewMatrix"], true, ref viewMatrix);
            GL.UniformMatrix4(uniformLocations["projectionMatrix"], true, ref projectionMatrix);
            GL.Uniform2(uniformLocations["windowSize"], windowSize);
            GL.Uniform3(uniformLocations["cameraPosition"], camera.GetPosition());

            GL.Uniform1(uniformLocations["useTexture"], useTexture);
            if (texture != null)
            {
                GL.Uniform1(uniformLocations["textureSampler"], texture.textureDescriptor.TextureUnit);
                //texture.textureDescriptor.DisableMapUse();
                if (texture.textureDescriptor.NormalUse == 1)
                {
                    GL.Uniform1(uniformLocations["textureSamplerNormal"], texture.textureDescriptor.NormalUnit);
                    GL.Uniform1(uniformLocations["useNormal"], texture.textureDescriptor.NormalUse);
                }
                if (texture.textureDescriptor.HeightUse == 1)
                {
                    GL.Uniform1(uniformLocations["textureSamplerHeight"], texture.textureDescriptor.HeightUnit);
                    GL.Uniform1(uniformLocations["useHeight"], texture.textureDescriptor.HeightUse);
                }
                if (texture.textureDescriptor.AOUse == 1)
                {
                    GL.Uniform1(uniformLocations["textureSamplerAO"], texture.textureDescriptor.AOUnit);
                    GL.Uniform1(uniformLocations["useAO"], texture.textureDescriptor.AOUse);
                }
                if (texture.textureDescriptor.RoughUse == 1)
                {
                    GL.Uniform1(uniformLocations["textureSamplerRough"], texture.textureDescriptor.RoughUnit);
                    GL.Uniform1(uniformLocations["useRough"], texture.textureDescriptor.RoughUse);
                }
                if (texture.textureDescriptor.MetalUse == 1)
                {
                    GL.Uniform1(uniformLocations["textureSamplerMetal"], texture.textureDescriptor.MetalUnit);
                    GL.Uniform1(uniformLocations["useMetal"], texture.textureDescriptor.MetalUse);
                }
            }
        }

        protected void SendUniformsOnlyPos(Shader shader)
        {
            int modelLoc = GL.GetUniformLocation(shader.id, "modelMatrix");
            int viewLoc = GL.GetUniformLocation(shader.id, "viewMatrix");
            int projLoc = GL.GetUniformLocation(shader.id, "projectionMatrix");

            modelMatrix = Matrix4.Identity;
            projectionMatrix = camera.projectionMatrix;
            viewMatrix = camera.viewMatrix;

            GL.UniformMatrix4(modelLoc, true, ref modelMatrix);
            GL.UniformMatrix4(viewLoc, true, ref viewMatrix);
            GL.UniformMatrix4(projLoc, true, ref projectionMatrix);
        }

        private void AddVertices(List<float> vertices, triangle tri, ref Matrix4 transformMatrix, bool isTransformed)
        {
            lock (vertices) // Lock to ensure thread-safety when modifying the list
            {
                ConvertToNDC(ref vertices, tri, 0, ref transformMatrix, isTransformed);
                ConvertToNDC(ref vertices, tri, 1, ref transformMatrix, isTransformed);
                ConvertToNDC(ref vertices, tri, 2, ref transformMatrix, isTransformed);
            }
        }

        private void AddVerticesOnlyPos(List<float> vertices, triangle tri, ref Matrix4 transformMatrix, bool isTransformed)
        {
            lock (vertices) // Lock to ensure thread-safety when modifying the list
            {
                ConvertToNDCOnlyPos(ref vertices, tri, 0, ref transformMatrix, isTransformed);
                ConvertToNDCOnlyPos(ref vertices, tri, 1, ref transformMatrix, isTransformed);
                ConvertToNDCOnlyPos(ref vertices, tri, 2, ref transformMatrix, isTransformed);
            }
        }

        public List<float> Draw()
        {
            Vao.Bind();

            vertices = new List<float>();

            ObjectType type = parentObject.GetObjectType();
            if (type == ObjectType.Sphere)
            {
                Scale = new Vector3(parentObject.Radius);
            }
            else if (type == ObjectType.Cube)
            {
                Scale = new Vector3(parentObject.Size);
            }
            else if (type == ObjectType.Capsule)
            {
                Scale = new Vector3(parentObject.Radius, parentObject.HalfHeight + parentObject.Radius, parentObject.Radius);
            }

            Matrix4 s = Matrix4.CreateScale(Scale);
            Matrix4 r = Matrix4.CreateFromQuaternion(parentObject.Rotation);
            Matrix4 t = Matrix4.CreateTranslation(parentObject.GetPosition());

            Matrix4 transformMatrix = Matrix4.Identity;
            bool isTransformed = IsTransformed;
            if (isTransformed)
            {
                transformMatrix = s * r * t;
            }

            if (parentObject.BSPStruct != null)
            {
                tris = parentObject.BSPStruct.GetTrianglesFrontToBack(camera);
            }
            else if (parentObject.BVHStruct != null)
            {

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
                         AddVertices(localVertices, tri, ref transformMatrix, isTransformed);
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

            if (texture != null)
            {
                texture.Bind(TextureType.Texture);
                if (texture.textureDescriptor.Normal != "")
                    texture.Bind(TextureType.Normal);
                if (texture.textureDescriptor.Height != "")
                    texture.Bind(TextureType.Height);
                if (texture.textureDescriptor.AO != "")
                    texture.Bind(TextureType.AO);
                if (texture.textureDescriptor.Rough != "")
                    texture.Bind(TextureType.Rough);
            }

            return vertices;
        }

        public List<float> DrawNotOccluded(List<triangle> notOccludedTris)
        {
            Vao.Bind();

            vertices = new List<float>();

            ObjectType type = parentObject.GetObjectType();
            if (type == ObjectType.Sphere)
            {
                Scale = new Vector3(parentObject.Radius);
            }
            else if (type == ObjectType.Cube)
            {
                Scale = new Vector3(parentObject.Size);
            }
            else if (type == ObjectType.Capsule)
            {
                Scale = new Vector3(parentObject.Radius, parentObject.HalfHeight + parentObject.Radius, parentObject.Radius);
            }

            Matrix4 s = Matrix4.CreateScale(Scale);
            Matrix4 r = Matrix4.CreateFromQuaternion(parentObject.Rotation);
            Matrix4 t = Matrix4.CreateTranslation(parentObject.GetPosition());

            Matrix4 transformMatrix = Matrix4.Identity;
            bool isTransformed = IsTransformed;
            if (isTransformed)
            {
                transformMatrix = s * r * t;
            }

            ParallelOptions parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = threadSize };
            Parallel.ForEach(notOccludedTris, parallelOptions,
                 () => new List<float>(),
                 (tri, loopState, localVertices) =>
                 {
                     if (tri.visibile)
                     {
                         AddVertices(localVertices, tri, ref transformMatrix, isTransformed);
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

            if (texture != null)
            {
                texture.Bind(TextureType.Texture);
                if (texture.textureDescriptor.Normal != "")
                    texture.Bind(TextureType.Normal);
                if (texture.textureDescriptor.Height != "")
                    texture.Bind(TextureType.Height);
                if (texture.textureDescriptor.AO != "")
                    texture.Bind(TextureType.AO);
                if (texture.textureDescriptor.Rough != "")
                    texture.Bind(TextureType.Rough);
            }

            return vertices;
        }

        
        public List<float> DrawOnlyPos(VAO aabbVao, Shader shader)
        {
            aabbVao.Bind();

            vertices = new List<float>();

            ObjectType type = parentObject.GetObjectType();
            if (type == ObjectType.Sphere)
            {
                Scale = new Vector3(parentObject.Radius);
            }
            else if (type == ObjectType.Cube)
            {
                Scale = new Vector3(parentObject.Size);
            }
            else if (type == ObjectType.Capsule)
            {
                Scale = new Vector3(parentObject.Radius, parentObject.HalfHeight + parentObject.Radius, parentObject.Radius);
            }

            Matrix4 s = Matrix4.CreateScale(Scale);
            Matrix4 r = Matrix4.CreateFromQuaternion(parentObject.Rotation);
            Matrix4 t = Matrix4.CreateTranslation(parentObject.GetPosition());

            Matrix4 transformMatrix = Matrix4.Identity;
            bool isTransformed = IsTransformed;
            if (isTransformed)
            {
                transformMatrix = s * r * t;
            }

            ParallelOptions parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = threadSize };
            Parallel.ForEach(tris, parallelOptions,
                 () => new List<float>(),
                 (tri, loopState, localVertices) =>
                 {
                     if (tri.visibile)
                     {
                         AddVerticesOnlyPos(localVertices, tri, ref transformMatrix, isTransformed);
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

            SendUniformsOnlyPos(shader);

            return vertices;
        }

        public void GetCookedData(out PxVec3[] verts, out int[] indices)
        {
            int vertCount = tris.Count() * 3;
            int index = 0;
            verts = new PxVec3[allVerts.Count()];
            indices = new int[vertCount];

            ObjectType type = parentObject.GetObjectType();
            if (type == ObjectType.Sphere)
            {
                Scale = new Vector3(parentObject.Radius);
            }
            else if (type == ObjectType.Cube)
            {
                Scale = new Vector3(parentObject.Size);
            }
            else if (type == ObjectType.Capsule)
            {
                Scale = new Vector3(parentObject.Radius, parentObject.HalfHeight + parentObject.Radius, parentObject.Radius);
            }

            Matrix4 s = Matrix4.CreateScale(Scale);
            Matrix4 r = Matrix4.CreateFromQuaternion(parentObject.Rotation);
            Matrix4 t = Matrix4.CreateTranslation(parentObject.Position);
            Matrix4 offsetTo = Matrix4.CreateTranslation(-Scale / 2f);
            Matrix4 offsetFrom = Matrix4.CreateTranslation(Scale / 2f);

            Matrix4 transformMatrix = Matrix4.Identity;
            if (IsTransformed)
            {
                transformMatrix = s * r * t;
                //if (type == ObjectType.Sphere || type == ObjectType.Capsule)
                //    transformMatrix = s * r * t;
                //else
                //    transformMatrix = s * offsetTo * r * offsetFrom * t;
            }

            for (int i = 0; i < allVerts.Count(); i++)
            {
                verts[i] = ConvertToNDCPxVec3(i, ref transformMatrix);
            }

            foreach (triangle tri in tris)
            {
                indices[index] = tri.pi[0];
                index++;
                indices[index] = tri.pi[1];
                index++;
                indices[index] = tri.pi[2];
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
