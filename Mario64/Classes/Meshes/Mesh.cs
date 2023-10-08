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

#pragma warning disable CS8600
#pragma warning disable CA1416
#pragma warning disable CS8604
#pragma warning disable CS8603

namespace Mario64
{

    public class Mesh : BaseMesh
    {
        public static int floatCount = 9;

        public bool drawNormals = false;
        public WireframeMesh normalMesh;

        public Texture texture;

        private List<float> vertices = new List<float>();
        private string? embeddedModelName;

        public BoundingBox BoundingBox;
        public Octree Octree;

        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 Scale;
        private bool IsTransformed
        {
            get
            {
                return !(Position == Vector3.Zero && Rotation == Quaternion.Identity && Scale == Vector3.One);
            }
        }

        private Frustum frustum;
        private Camera camera;
        private Vector2 windowSize;

        Matrix4 modelMatrix, viewMatrix, projectionMatrix;

        private VAO Vao;
        private VBO Vbo;

        public Mesh(VAO vao, VBO vbo, int shaderProgramId, string embeddedModelName, string embeddedTextureName, int ocTreeDepth, Vector2 windowSize, ref Frustum frustum, ref Camera camera, ref int textureCount) : base(vao.id, vbo.id, shaderProgramId)
        {
            texture = new Texture(textureCount, embeddedTextureName);
            textureCount++;

            Vao = vao;
            Vbo = vbo;

            this.windowSize = windowSize;
            this.frustum = frustum;
            this.camera = camera;

            Position = Vector3.Zero;
            Rotation = Quaternion.Identity;
            Scale = Vector3.One;

            this.embeddedModelName = embeddedModelName;
            ProcessObj(embeddedModelName);

            if (ocTreeDepth != -1)
            {
                CalculateBoundingBox();
                Octree = new Octree(new List<triangle>(tris), BoundingBox, ocTreeDepth);
            }

            ComputeVertexNormals(ref tris);

            SendUniforms();
        }

        public Mesh(VAO vao, VBO vbo, int shaderProgramId, string embeddedTextureName, int ocTreeDepth, Vector2 windowSize, ref Frustum frustum, ref Camera camera, ref int textureCount) : base(vao.id, vbo.id, shaderProgramId)
        {
            texture = new Texture(textureCount, embeddedTextureName);
            textureCount++;

            Vao = vao;
            Vbo = vbo;

            this.windowSize = windowSize;
            this.frustum = frustum;
            this.camera = camera;

            Position = Vector3.Zero;
            Rotation = Quaternion.Identity;
            Scale = Vector3.One;
        }

        public Mesh(VAO vao, VBO vbo, int shaderProgramId, List<triangle> tris, string embeddedTextureName, int ocTreeDepth, Vector2 windowSize, ref Frustum frustum, ref Camera camera, ref int textureCount) : base(vao.id, vbo.id, shaderProgramId)
        {
            texture = new Texture(textureCount, embeddedTextureName);
            textureCount++;

            Vao = vao;
            Vbo = vbo;

            this.windowSize = windowSize;
            this.frustum = frustum;
            this.camera = camera;

            Position = Vector3.Zero;
            Rotation = Quaternion.Identity;
            Scale = Vector3.One;

            this.tris = new List<triangle>(tris);

            if (ocTreeDepth != -1)
            {
                CalculateBoundingBox();
                Octree = new Octree(new List<triangle>(tris), BoundingBox, ocTreeDepth);
            }

            ComputeVertexNormals(ref tris);

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

        private List<float> ConvertToNDC(triangle tri, int index, ref Matrix4 transformMatrix)
        {
            Vector3 v = Vector3.TransformPosition(tri.p[index], transformMatrix);

            List<float> result = new List<float>()
            {
                v.X, v.Y, v.Z, 1.0f,
                tri.n[index].X, tri.n[index].Y, tri.n[index].Z,
                tri.t[index].u, tri.t[index].v
            };
            return result;
        }

        public void UpdateFrustumAndCamera(ref Frustum frustum, ref Camera camera)
        {
            this.frustum = frustum;
            this.camera = camera;
        }

        protected override void SendUniforms()
        {
            int textureLocation = GL.GetUniformLocation(shaderProgramId, "textureSampler");
            int windowSizeLocation = GL.GetUniformLocation(shaderProgramId, "windowSize");
            int modelMatrixLocation = GL.GetUniformLocation(shaderProgramId, "modelMatrix");
            int viewMatrixLocation = GL.GetUniformLocation(shaderProgramId, "viewMatrix");
            int projectionMatrixLocation = GL.GetUniformLocation(shaderProgramId, "projectionMatrix");
            int cameraPositionLocation = GL.GetUniformLocation(shaderProgramId, "cameraPosition");

            modelMatrix = Matrix4.Identity;
            projectionMatrix = camera.GetProjectionMatrix();
            viewMatrix = camera.GetViewMatrix();

            GL.UniformMatrix4(modelMatrixLocation, true, ref modelMatrix);
            GL.UniformMatrix4(viewMatrixLocation, true, ref viewMatrix);
            GL.UniformMatrix4(projectionMatrixLocation, true, ref projectionMatrix);
            GL.Uniform2(windowSizeLocation, windowSize);
            GL.Uniform3(cameraPositionLocation, camera.position);
            GL.Uniform1(textureLocation, texture.unit);
        }

        public List<float> Draw()
        {
            Vao.Bind();

            vertices = new List<float>();

            Matrix4 s = Matrix4.CreateScale(Scale);
            Matrix4 r = Matrix4.CreateFromQuaternion(Rotation);
            Matrix4 t = Matrix4.CreateTranslation(Position);
            Matrix4 offsetTo = Matrix4.CreateTranslation(-Scale/2f);
            Matrix4 offsetFrom = Matrix4.CreateTranslation(Scale/2f);

            Matrix4 transformMatrix = Matrix4.Identity;
            if (IsTransformed)
            {
                if (this is SphereCollider)
                    transformMatrix = s * r * t;
                else if (this is CapsuleCollider)
                    transformMatrix = r * t;
                else
                    transformMatrix = s * offsetTo * r * offsetFrom * t;
            }

            foreach (triangle tri in tris)
            {
                if (frustum.IsTriangleInside(tri) || camera.IsTriangleClose(tri))
                {
                    if (tri.gotPointNormals)
                    {
                        tri.ComputeTriangleNormal(ref transformMatrix);
                        vertices.AddRange(ConvertToNDC(tri, 0, ref transformMatrix));
                        vertices.AddRange(ConvertToNDC(tri, 1, ref transformMatrix));
                        vertices.AddRange(ConvertToNDC(tri, 2, ref transformMatrix));
                    }
                    else
                    {
                        tri.ComputeTriangleNormal(ref transformMatrix);
                        vertices.AddRange(ConvertToNDC(tri, 0, ref transformMatrix));
                        vertices.AddRange(ConvertToNDC(tri, 1, ref transformMatrix));
                        vertices.AddRange(ConvertToNDC(tri, 2, ref transformMatrix));
                    }
                }
            }

            SendUniforms();
            texture.Bind();

            return vertices;
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

        private Stream GetResourceStreamByNameEnd(string nameEnd)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            foreach (string resourceName in assembly.GetManifestResourceNames())
            {
                if (resourceName.EndsWith(nameEnd, StringComparison.OrdinalIgnoreCase))
                {
                    return assembly.GetManifestResourceStream(resourceName);
                }
            }
            return null; // or throw an exception if the resource is not found
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
                                }

                            }
                            else
                            {
                                string[] vStr = result.Substring(2).Split(" ");
                                int[] f = { int.Parse(vStr[0]), int.Parse(vStr[1]), int.Parse(vStr[2]) };

                                tris.Add(new triangle(new Vector3[] { verts[f[0] - 1], verts[f[1] - 1], verts[f[2] - 1] }));
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
