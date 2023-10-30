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

        public Texture texture;
        public int useTexture;

        private List<float> vertices = new List<float>();
        private List<float> instancedVertices = new List<float>();
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

        private InstancedVAO Vao;
        private VBO Vbo;

        public List<InstancedMeshData> instancedData = new List<InstancedMeshData>();

        public InstancedMesh(InstancedVAO vao, VBO vbo, int shaderProgramId, string modelName, string textureName, Vector2 windowSize, ref Camera camera, ref int textureCount) : base(vao.id, vbo.id, shaderProgramId)
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

        public InstancedMesh(InstancedVAO vao, VBO vbo, int shaderProgramId, List<triangle> tris, string textureName, Vector2 windowSize, ref Camera camera, ref int textureCount) : base(vao.id, vbo.id, shaderProgramId)
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

        public InstancedMesh(InstancedVAO vao, VBO vbo, int shaderProgramId, List<triangle> tris, Vector2 windowSize, ref Camera camera, ref int textureCount) : base(vao.id, vbo.id, shaderProgramId)
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

        public void UpdateFrustumAndCamera(ref Camera camera)
        {
            this.camera = camera;
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

        private void AddVertices(List<float> vertices, triangle tri, ref Matrix4 transformMatrix, bool isTransformed)
        {
            lock (vertices) // Lock to ensure thread-safety when modifying the list
            {
                ConvertToNDC(ref vertices, tri, 0, ref transformMatrix, isTransformed);
                ConvertToNDC(ref vertices, tri, 1, ref transformMatrix, isTransformed);
                ConvertToNDC(ref vertices, tri, 2, ref transformMatrix, isTransformed);
            }
        }

        public (List<float>, List<float>) Draw()
        {
            Vao.Bind();

            vertices = new List<float>();
            instancedVertices = new List<float>();

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

            foreach(InstancedMeshData meshData in instancedData)
            {
                ConvertToNDCInstance(ref instancedVertices, meshData);
            }

            return (vertices, instancedVertices);
        }

    }
}
