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

        private Frustum frustum;
        private Camera camera;
        private Vector2 windowSize;

        Matrix4 modelMatrix, viewMatrix, projectionMatrix;

        private VAO Vao;
        private VBO Vbo;

        public Mesh(VAO vao, VBO vbo, int shaderProgramId, string modelName, string textureName, Vector2 windowSize, ref Frustum frustum, ref Camera camera, ref int textureCount) : base(vao.id, vbo.id, shaderProgramId)
        {
            texture = new Texture(textureCount, textureName);
            textureCount += texture.textureDescriptor.count;

            Vao = vao;
            Vbo = vbo;

            this.windowSize = windowSize;
            this.frustum = frustum;
            this.camera = camera;

            Scale = Vector3.One;

            this.modelName = modelName;
            ProcessObj(modelName);

            ComputeVertexNormals(ref tris);
            ComputeTangents(ref tris);

            GetUniformLocations();
            SendUniforms();
        }

        public Mesh(VAO vao, VBO vbo, int shaderProgramId, string textureName, Vector2 windowSize, ref Frustum frustum, ref Camera camera, ref int textureCount) : base(vao.id, vbo.id, shaderProgramId)
        {
            texture = new Texture(textureCount, textureName);
            textureCount += texture.textureDescriptor.count;

            Vao = vao;
            Vbo = vbo;

            this.windowSize = windowSize;
            this.frustum = frustum;
            this.camera = camera;

            Scale = Vector3.One;
        }

        public Mesh(VAO vao, VBO vbo, int shaderProgramId, List<triangle> tris, string textureName, Vector2 windowSize, ref Frustum frustum, ref Camera camera, ref int textureCount) : base(vao.id, vbo.id, shaderProgramId)
        {
            texture = new Texture(textureCount, textureName);
            textureCount += texture.textureDescriptor.count;

            Vao = vao;
            Vbo = vbo;

            this.windowSize = windowSize;
            this.frustum = frustum;
            this.camera = camera;

            Scale = Vector3.One;

            this.tris = new List<triangle>(tris);

            ComputeVertexNormals(ref tris);
            ComputeTangents(ref tris);

            GetUniformLocations();
            SendUniforms();
        }

        public void CalculateNormalWireframe(VAO vao, VBO vbo, int shaderProgramId, ref Frustum frustum, ref Camera camera)
        {
            List<Line> normalLines = new List<Line>();
            foreach (triangle tri in tris)
            {
                Vector3 n1 = tri.GetMiddle();
                Vector3 n2 = (tri.ComputeTriangleNormal() * 2.5f) + tri.GetMiddle();
                normalLines.Add(new Line(n1, n2));
            }
            normalMesh = new WireframeMesh(vao, vbo, shaderProgramId, ref frustum, ref camera, Color4.Red);
            normalMesh.lines = normalLines;
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

        public void UpdateFrustumAndCamera(ref Frustum frustum, ref Camera camera)
        {
            this.frustum = frustum;
            this.camera = camera;
        }

        private void GetUniformLocations()
        {
            uniformLocations.Add("textureSampler", GL.GetUniformLocation(shaderProgramId, "textureSampler"));
            uniformLocations.Add("windowSize", GL.GetUniformLocation(shaderProgramId, "windowSize"));
            uniformLocations.Add("modelMatrix", GL.GetUniformLocation(shaderProgramId, "modelMatrix"));
            uniformLocations.Add("viewMatrix", GL.GetUniformLocation(shaderProgramId, "viewMatrix"));
            uniformLocations.Add("projectionMatrix", GL.GetUniformLocation(shaderProgramId, "projectionMatrix"));
            uniformLocations.Add("cameraPosition", GL.GetUniformLocation(shaderProgramId, "cameraPosition"));
            if(texture.textureDescriptor.Normal != "")
            {
                uniformLocations.Add("textureSamplerNormal", GL.GetUniformLocation(shaderProgramId, "textureSamplerNormal"));
                uniformLocations.Add("useNormal", GL.GetUniformLocation(shaderProgramId, "useNormal"));
            }
            if(texture.textureDescriptor.Height != "")
            {
                uniformLocations.Add("textureSamplerHeight", GL.GetUniformLocation(shaderProgramId, "textureSamplerHeight"));
                uniformLocations.Add("useHeight", GL.GetUniformLocation(shaderProgramId, "useHeight"));
            }
            if(texture.textureDescriptor.AO != "")
            {
                uniformLocations.Add("textureSamplerAO", GL.GetUniformLocation(shaderProgramId, "textureSamplerAO"));
                uniformLocations.Add("useAO", GL.GetUniformLocation(shaderProgramId, "useAO"));
            }
            if(texture.textureDescriptor.Rough != "")
            {
                uniformLocations.Add("textureSamplerRough", GL.GetUniformLocation(shaderProgramId, "textureSamplerRough"));
                uniformLocations.Add("useRough", GL.GetUniformLocation(shaderProgramId, "useRough"));
            }
            if(texture.textureDescriptor.Metal != "")
            {
                uniformLocations.Add("textureSamplerMetal", GL.GetUniformLocation(shaderProgramId, "textureSamplerMetal"));
                uniformLocations.Add("useMetal", GL.GetUniformLocation(shaderProgramId, "useMetal"));
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
            GL.Uniform1(uniformLocations["textureSampler"], texture.textureDescriptor.TextureUnit);
            //texture.textureDescriptor.DisableMapUse();
            if(texture.textureDescriptor.NormalUse == 1)
            {
                GL.Uniform1(uniformLocations["textureSamplerNormal"], texture.textureDescriptor.NormalUnit);
                GL.Uniform1(uniformLocations["useNormal"], texture.textureDescriptor.NormalUse);
            }
            if(texture.textureDescriptor.HeightUse == 1)
            {
                GL.Uniform1(uniformLocations["textureSamplerHeight"], texture.textureDescriptor.HeightUnit);
                GL.Uniform1(uniformLocations["useHeight"], texture.textureDescriptor.HeightUse);
            }
            if(texture.textureDescriptor.AOUse == 1)
            {
                GL.Uniform1(uniformLocations["textureSamplerAO"], texture.textureDescriptor.AOUnit);
                GL.Uniform1(uniformLocations["useAO"], texture.textureDescriptor.AOUse);
            }
            if(texture.textureDescriptor.RoughUse == 1)
            {
                GL.Uniform1(uniformLocations["textureSamplerRough"], texture.textureDescriptor.RoughUnit);
                GL.Uniform1(uniformLocations["useRough"], texture.textureDescriptor.RoughUse);
            }
            if(texture.textureDescriptor.MetalUse == 1)
            {
                GL.Uniform1(uniformLocations["textureSamplerMetal"], texture.textureDescriptor.MetalUnit);
                GL.Uniform1(uniformLocations["useMetal"], texture.textureDescriptor.MetalUse);
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

            texture.Bind(TextureType.Texture);
            if(texture.textureDescriptor.Normal != "")
                texture.Bind(TextureType.Normal);
            if(texture.textureDescriptor.Height != "")
                texture.Bind(TextureType.Height);
            if(texture.textureDescriptor.AO != "")
                texture.Bind(TextureType.AO);
            if(texture.textureDescriptor.Rough != "")
                texture.Bind(TextureType.Rough);

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

            texture.Bind(TextureType.Texture);
            if (texture.textureDescriptor.Normal != "")
                texture.Bind(TextureType.Normal);
            if (texture.textureDescriptor.Height != "")
                texture.Bind(TextureType.Height);
            if (texture.textureDescriptor.AO != "")
                texture.Bind(TextureType.AO);
            if (texture.textureDescriptor.Rough != "")
                texture.Bind(TextureType.Rough);

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

        private void ComputeTangents(ref List<triangle> tris)
        {
            // Initialize tangent and bitangent lists with zeros
            Dictionary<Vector3, List<Vector3>> tangentSums = new Dictionary<Vector3, List<Vector3>>();
            Dictionary<Vector3, List<Vector3>> bitangentSums = new Dictionary<Vector3, List<Vector3>>();

            foreach (var tri in tris)
            {
                // Get the vertices of the triangle
                Vector3 p0 = tri.p[0];
                Vector3 p1 = tri.p[1];
                Vector3 p2 = tri.p[2];

                // Get UVs of the triangle
                Vector2 uv0 = new Vector2(tri.t[0].u, tri.t[0].v);
                Vector2 uv1 = new Vector2(tri.t[1].u, tri.t[1].v);
                Vector2 uv2 = new Vector2(tri.t[2].u, tri.t[2].v);

                // Compute the edges of the triangle in both object space and texture space
                Vector3 edge1 = p1 - p0;
                Vector3 edge2 = p2 - p0;

                Vector2 deltaUV1 = uv1 - uv0;
                Vector2 deltaUV2 = uv2 - uv0;

                float f = 1.0f / (deltaUV1.X * deltaUV2.Y - deltaUV2.X * deltaUV1.Y);

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

                // Accumulate the tangents and bitangents
                foreach (var vertex in tri.p)
                {
                    if (!tangentSums.ContainsKey(vertex))
                    {
                        tangentSums[vertex] = new List<Vector3>();
                        bitangentSums[vertex] = new List<Vector3>();
                    }

                    tangentSums[vertex].Add(tangent);
                    bitangentSums[vertex].Add(bitangent);
                }
            }

            // Average and normalize tangents and bitangents
            foreach (var tri in tris)
            {
                for (int i = 0; i < 3; i++)
                {
                    Vector3 vertex = tri.p[i];

                    Vector3 avgTangent = Average(tangentSums[vertex]).Normalized();
                    Vector3 avgBitangent = Average(bitangentSums[vertex]).Normalized();

                    tri.tan[i] = avgTangent;
                    tri.bitan[i] = avgBitangent;
                }
            }
        }

        public void ProcessObj(string filename)
        {
            tris = new List<triangle>();

            string result;
            int fPerCount = -1;
            List<Vector3> verts = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<Vec2d> uvs = new List<Vec2d>();

            using (Stream stream = FileManager.GetFileStream(filename, FileType.Models))
            using (StreamReader reader = new StreamReader(stream))
            {
                while (true)
                {
                    result = reader.ReadLine();
                    if (result != null && result.Length > 0)
                    {
                        if (result[0] == 'v')
                        {
                            if (result[1] == 't')
                            {
                                string[] vStr = result.Substring(3).Split(" ");
                                var a = float.Parse(vStr[0]);
                                var b = float.Parse(vStr[1]);
                                Vec2d v = new Vec2d(a, b);
                                uvs.Add(v);
                            }
                            else if (result[1] == 'n')
                            {
                                string[] vStr = result.Substring(3).Split(" ");
                                var a = float.Parse(vStr[0]);
                                var b = float.Parse(vStr[1]);
                                var c = float.Parse(vStr[2]);
                                Vector3 v = new Vector3(a, b, c);
                                normals.Add(v);
                            }
                            else
                            {
                                string[] vStr = result.Substring(2).Split(" ");
                                var a = float.Parse(vStr[0]);
                                var b = float.Parse(vStr[1]);
                                var c = float.Parse(vStr[2]);
                                Vector3 v = new Vector3(a, b, c);
                                verts.Add(v);
                                allVerts.Add(v);
                            }
                        }
                        else if (result[0] == 'f')
                        {
                            if (result.Contains("//"))
                            {

                            }
                            else if (result.Contains("/"))
                            {
                                string[] vStr = result.Substring(2).Split(" ");
                                if (vStr.Length > 3)
                                    throw new Exception();

                                if (fPerCount == -1)
                                    fPerCount = vStr[0].Count(x => x == '/');

                                if (fPerCount == 2)
                                {
                                    // 1/1/1, 2/2/2, 3/3/3
                                    int[] v = new int[3];
                                    int[] n = new int[3];
                                    int[] uv = new int[3];
                                    for (int i = 0; i < 3; i++)
                                    {
                                        string[] fStr = vStr[i].Split("/");
                                        v[i] = int.Parse(fStr[0]);
                                        uv[i] = int.Parse(fStr[1]);
                                        n[i] = int.Parse(fStr[2]);
                                    }

                                    tris.Add(new triangle(new Vector3[] { verts[v[0] - 1], verts[v[1] - 1], verts[v[2] - 1] },
                                                          new Vector3[] { normals[n[0] - 1], normals[n[1] - 1], normals[n[2] - 1] },
                                                          new Vec2d[] { uvs[uv[0] - 1], uvs[uv[1] - 1], uvs[uv[2] - 1] }));

                                    tris.Last().pi[0] = v[0] - 1;
                                    tris.Last().pi[1] = v[1] - 1;
                                    tris.Last().pi[2] = v[2] - 1;

                                    hasIndices = true;
                                }
                                else if (fPerCount == 1)
                                {
                                    // 1/1, 2/2, 3/3
                                    int[] v = new int[3];
                                    int[] uv = new int[3];
                                    for (int i = 0; i < 3; i++)
                                    {
                                        string[] fStr = vStr[i].Split("/");
                                        v[i] = int.Parse(fStr[0]);
                                        uv[i] = int.Parse(fStr[1]);
                                    }

                                    tris.Add(new triangle(new Vector3[] { verts[v[0] - 1], verts[v[1] - 1], verts[v[2] - 1] },
                                                          new Vec2d[] { uvs[uv[0] - 1], uvs[uv[1] - 1], uvs[uv[2] - 1] }));

                                    tris.Last().pi[0] = v[0] - 1;
                                    tris.Last().pi[1] = v[1] - 1;
                                    tris.Last().pi[2] = v[2] - 1;

                                    hasIndices = true;
                                }

                            }
                            else
                            {
                                string[] vStr = result.Substring(2).Split(" ");
                                int[] f = { int.Parse(vStr[0]), int.Parse(vStr[1]), int.Parse(vStr[2]) };

                                tris.Add(new triangle(new Vector3[] { verts[f[0] - 1], verts[f[1] - 1], verts[f[2] - 1] }));

                                tris.Last().pi[0] = f[0] - 1;
                                tris.Last().pi[1] = f[1] - 1;
                                tris.Last().pi[2] = f[2] - 1;

                                hasIndices = true;
                            }
                        }
                    }

                    if (result == null)
                        break;
                }
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
