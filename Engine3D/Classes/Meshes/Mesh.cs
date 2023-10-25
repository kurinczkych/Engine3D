﻿using OpenTK.Graphics.OpenGL4;
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
using System.Data;

#pragma warning disable CS8600
#pragma warning disable CA1416
#pragma warning disable CS8604
#pragma warning disable CS8603

namespace Engine3D
{

    public class Mesh : BaseMesh
    {
        public static int floatCount = 13;

        public bool drawNormals = false;
        public WireframeMesh normalMesh;

        public Texture texture;

        private List<float> vertices = new List<float>();
        private string? embeddedModelName;

        public BoundingBox BoundingBox;


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

        public Mesh(VAO vao, VBO vbo, int shaderProgramId, string embeddedModelName, string embeddedTextureName, Vector2 windowSize, ref Frustum frustum, ref Camera camera, ref int textureCount) : base(vao.id, vbo.id, shaderProgramId)
        {
            texture = new Texture(textureCount, embeddedTextureName);
            textureCount++;

            Vao = vao;
            Vbo = vbo;

            this.windowSize = windowSize;
            this.frustum = frustum;
            this.camera = camera;

            Scale = Vector3.One;

            this.embeddedModelName = embeddedModelName;
            ProcessObj(embeddedModelName);

            ComputeVertexNormals(ref tris);

            GetUniformLocations();
            SendUniforms();
        }

        public Mesh(VAO vao, VBO vbo, int shaderProgramId, string embeddedTextureName, Vector2 windowSize, ref Frustum frustum, ref Camera camera, ref int textureCount) : base(vao.id, vbo.id, shaderProgramId)
        {
            texture = new Texture(textureCount, embeddedTextureName);
            textureCount++;

            Vao = vao;
            Vbo = vbo;

            this.windowSize = windowSize;
            this.frustum = frustum;
            this.camera = camera;

            Scale = Vector3.One;
        }

        public Mesh(VAO vao, VBO vbo, int shaderProgramId, List<triangle> tris, string embeddedTextureName, Vector2 windowSize, ref Frustum frustum, ref Camera camera, ref int textureCount) : base(vao.id, vbo.id, shaderProgramId)
        {
            texture = new Texture(textureCount, embeddedTextureName);
            textureCount++;

            Vao = vao;
            Vbo = vbo;

            this.windowSize = windowSize;
            this.frustum = frustum;
            this.camera = camera;

            Scale = Vector3.One;

            this.tris = new List<triangle>(tris);

            ComputeVertexNormals(ref tris);

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

        private List<float> ConvertToNDC(triangle tri, int index, ref Matrix4 transformMatrix, bool isTransformed)
        {
            List<float> result = new List<float>();
            if (isTransformed)
            {
                Vector3 v = Vector3.TransformPosition(tri.p[index], transformMatrix);

                result = new List<float>()
                {
                    v.X, v.Y, v.Z, 1.0f,
                    tri.n[index].X, tri.n[index].Y, tri.n[index].Z,
                    tri.t[index].u, tri.t[index].v,
                    tri.c[index].R, tri.c[index].G, tri.c[index].B, tri.c[index].A
                };
            }
            else
            {
                result = new List<float>()
                {
                    tri.p[index].X, tri.p[index].Y, tri.p[index].Z, 1.0f,
                    tri.n[index].X, tri.n[index].Y, tri.n[index].Z,
                    tri.t[index].u, tri.t[index].v,
                    tri.c[index].R, tri.c[index].G, tri.c[index].B, tri.c[index].A
                };
            }
            return result;
        }

        private List<float> ConvertToNDCOnlyPos(triangle tri, int index, ref Matrix4 transformMatrix, bool isTransformed)
        {
            List<float> result = new List<float>();
            if (isTransformed)
            {
                Vector3 v = Vector3.TransformPosition(tri.p[index], transformMatrix);

                result = new List<float>()
                {
                    v.X, v.Y, v.Z
                };
            }
            else
            {
                result = new List<float>()
                {
                    tri.p[index].X, tri.p[index].Y, tri.p[index].Z,
                };
            }
            return result;
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
            GL.Uniform1(uniformLocations["textureSampler"], texture.unit);
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

            foreach (triangle tri in tris)
            {
                if (frustum.IsTriangleInside(tri) || camera.IsTriangleClose(tri))
                {
                    if (tri.gotPointNormals)
                    {
                        vertices.AddRange(ConvertToNDC(tri, 0, ref transformMatrix, isTransformed));
                        vertices.AddRange(ConvertToNDC(tri, 1, ref transformMatrix, isTransformed));
                        vertices.AddRange(ConvertToNDC(tri, 2, ref transformMatrix, isTransformed));
                    }
                    else
                    {
                        tri.ComputeTriangleNormal(ref transformMatrix);
                        vertices.AddRange(ConvertToNDC(tri, 0, ref transformMatrix, isTransformed));
                        vertices.AddRange(ConvertToNDC(tri, 1, ref transformMatrix, isTransformed));
                        vertices.AddRange(ConvertToNDC(tri, 2, ref transformMatrix, isTransformed));
                    }
                }
            }

            SendUniforms();
            texture.Bind();

            return vertices;
        }
        
        public List<float> DrawNotOccluded(List<triangle> notOccludedTris, out int trisDrew)
        {
            Vao.Bind();

            trisDrew = 0;
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

            foreach (triangle tri in notOccludedTris)
            {
                if (frustum.IsTriangleInside(tri) || camera.IsTriangleClose(tri))
                {
                    trisDrew++;
                    if (tri.gotPointNormals)
                    {
                        vertices.AddRange(ConvertToNDC(tri, 0, ref transformMatrix, isTransformed));
                        vertices.AddRange(ConvertToNDC(tri, 1, ref transformMatrix, isTransformed));
                        vertices.AddRange(ConvertToNDC(tri, 2, ref transformMatrix, isTransformed));
                    }
                    else
                    {
                        tri.ComputeTriangleNormal(ref transformMatrix);
                        vertices.AddRange(ConvertToNDC(tri, 0, ref transformMatrix, isTransformed));
                        vertices.AddRange(ConvertToNDC(tri, 1, ref transformMatrix, isTransformed));
                        vertices.AddRange(ConvertToNDC(tri, 2, ref transformMatrix, isTransformed));
                    }
                }
            }

            SendUniforms();
            texture.Bind();

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

            foreach (triangle tri in tris)
            {
                if (frustum.IsTriangleInside(tri) || camera.IsTriangleClose(tri))
                {
                    if (tri.gotPointNormals)
                    {
                        vertices.AddRange(ConvertToNDCOnlyPos(tri, 0, ref transformMatrix, isTransformed));
                        vertices.AddRange(ConvertToNDCOnlyPos(tri, 1, ref transformMatrix, isTransformed));
                        vertices.AddRange(ConvertToNDCOnlyPos(tri, 2, ref transformMatrix, isTransformed));
                    }
                    else
                    {
                        tri.ComputeTriangleNormal(ref transformMatrix);
                        vertices.AddRange(ConvertToNDCOnlyPos(tri, 0, ref transformMatrix, isTransformed));
                        vertices.AddRange(ConvertToNDCOnlyPos(tri, 1, ref transformMatrix, isTransformed));
                        vertices.AddRange(ConvertToNDCOnlyPos(tri, 2, ref transformMatrix, isTransformed));
                    }
                }
            }

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

        public void CalculateBoundingBox()
        {
            if (tris == null || tris.Count == 0)
            {
                throw new InvalidOperationException("Mesh contains no triangles.");
            }

            // Initialize with the first vertex of the first triangle
            Vector3 min = tris[0].p[0];
            Vector3 max = tris[0].p[0];

            foreach (var triangle in tris)
            {
                for (int i = 0; i < 3; i++)
                {
                    Vector3 vertex = triangle.p[i];

                    // Check for a new min
                    if (vertex.X < min.X) min.X = vertex.X;
                    if (vertex.Y < min.Y) min.Y = vertex.Y;
                    if (vertex.Z < min.Z) min.Z = vertex.Z;

                    // Check for a new max
                    if (vertex.X > max.X) max.X = vertex.X;
                    if (vertex.Y > max.Y) max.Y = vertex.Y;
                    if (vertex.Z > max.Z) max.Z = vertex.Z;
                }
            }

            BoundingBox = new BoundingBox(min, max);
        }

        public void ProcessObj(string filename)
        {
            tris = new List<triangle>();

            var assembly = Assembly.GetExecutingAssembly();
            string resourceName = assembly.GetManifestResourceNames()
                .Single(str => str.EndsWith(filename));

            string result;
            int fPerCount = -1;
            List<Vector3> verts = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<Vec2d> uvs = new List<Vec2d>();

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
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
