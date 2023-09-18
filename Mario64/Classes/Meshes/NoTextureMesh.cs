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
    public struct VertexNoTexture
    {
        public Vector4 Position;
        public Color4 Color;
    }

    public class NoTextureMesh : BaseMesh
    {
        private List<VertexNoTexture> vertices = new List<VertexNoTexture>();
        private string? embeddedModelName;
        private int vertexSize;

        public Vector3 Position;
        public Vector3 Rotation;
        public Vector3 Scale;
        private bool IsTransformed
        {
            get
            {
                return !(Position == Vector3.Zero && Rotation == Vector3.Zero && Scale == Vector3.One);
            }
        }

        private Frustum frustum;
        private Camera camera;

        Matrix4 modelMatrix, viewMatrix, projectionMatrix;

        public NoTextureMesh(int vaoId, int shaderProgramId, string embeddedModelName, ref Frustum frustum, ref Camera camera, Color4 color) : base(vaoId, shaderProgramId)
        {
            this.frustum = frustum;
            this.camera = camera;

            Position = Vector3.Zero;
            Rotation = Vector3.Zero;
            Scale = Vector3.One;

            GL.GenBuffers(1, out vbo);

            // Texture -----------------------------------------------

            this.embeddedModelName = embeddedModelName;
            ProcessObj(embeddedModelName, color);

            foreach (triangle tri in tris)
            {
                tri.c[0] = color;
                tri.c[1] = color;
                tri.c[2] = color;
            }

            vertexSize = System.Runtime.InteropServices.Marshal.SizeOf(typeof(VertexNoTexture));

            // VAO creating
            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
            GL.BindVertexArray(vaoId);

            GL.VertexAttribPointer(0, 4, VertexAttribPointerType.Float, false, vertexSize, 0);
            GL.EnableVertexArrayAttrib(vaoId, 0);

            GL.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, vertexSize, 4 * sizeof(float));
            GL.EnableVertexArrayAttrib(vaoId, 1);

            GL.BindVertexArray(0); // Unbind the VAO
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0); // Unbind the VBO

            SendUniforms();
        }


        public void UpdateFrustumAndCamera(ref Frustum frustum, ref Camera camera)
        {
            this.frustum = frustum;
            this.camera = camera;
        }

        protected override void SendUniforms()
        {
            int modelMatrixLocation = GL.GetUniformLocation(shaderProgramId, "modelMatrix");
            int viewMatrixLocation = GL.GetUniformLocation(shaderProgramId, "viewMatrix");
            int projectionMatrixLocation = GL.GetUniformLocation(shaderProgramId, "projectionMatrix");

            modelMatrix = Matrix4.Identity;
            projectionMatrix = camera.GetProjectionMatrix();
            viewMatrix = camera.GetViewMatrix();

            GL.UniformMatrix4(modelMatrixLocation, true, ref modelMatrix);
            GL.UniformMatrix4(viewMatrixLocation, true, ref viewMatrix);
            GL.UniformMatrix4(projectionMatrixLocation, true, ref projectionMatrix);
        }
        private VertexNoTexture ConvertToNDC(triangle tri, int index, ref Matrix4 transformMatrix)
        {
            Vector3 v = Vector3.TransformPosition(tri.p[index], transformMatrix);

            return new VertexNoTexture()
            {
                Position = new Vector4(v.X, v.Y, v.Z, 1.0f),
                Color = tri.c[index]
            };
        }

        public override void Draw()
        {
            SendUniforms();

            vertices = new List<VertexNoTexture>();

            Matrix4 s = Matrix4.CreateScale(Scale);
            Matrix4 rX = Matrix4.CreateRotationX(MathHelper.DegreesToRadians(Rotation.X));
            Matrix4 rY = Matrix4.CreateRotationY(MathHelper.DegreesToRadians(Rotation.Y));
            Matrix4 rZ = Matrix4.CreateRotationZ(MathHelper.DegreesToRadians(Rotation.Z));
            Matrix4 t = Matrix4.CreateTranslation(Position);

            Matrix4 transformMatrix = Matrix4.Identity;
            if (IsTransformed)
                transformMatrix = s * rX * rY * rZ * t;

            foreach (triangle tri in tris)
            {
                if (frustum.IsTriangleInside(tri) || camera.IsTriangleClose(tri))
                {
                    if (tri.gotPointNormals)
                    {
                        vertices.Add(ConvertToNDC(tri, 0, ref transformMatrix));
                        vertices.Add(ConvertToNDC(tri, 1, ref transformMatrix));
                        vertices.Add(ConvertToNDC(tri, 2, ref transformMatrix));
                    }
                    else
                    {
                        Vector3 normal = tri.ComputeTriangleNormal();
                        vertices.Add(ConvertToNDC(tri, 0, ref transformMatrix));
                        vertices.Add(ConvertToNDC(tri, 1, ref transformMatrix));
                        vertices.Add(ConvertToNDC(tri, 2, ref transformMatrix));
                    }
                }
            }

            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
            GL.BindVertexArray(vaoId);
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Count * vertexSize, vertices.ToArray(), BufferUsageHint.DynamicDraw);

            GL.DrawArrays(PrimitiveType.Triangles, 0, vertices.Count);

            GL.BindVertexArray(0); // Unbind the VAO
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0); // Unbind the VBO
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

        public void OnlyCube()
        {
            tris = new List<triangle>
                {
                    new triangle(new Vector3[] { new Vector3(0.0f, 0.0f, 0.0f), new Vector3(0.0f, 1.0f, 0.0f), new Vector3(1.0f, 1.0f, 0.0f)  }),
                    new triangle(new Vector3[] { new Vector3(0.0f, 0.0f, 0.0f), new Vector3(1.0f, 1.0f, 0.0f), new Vector3(1.0f, 0.0f, 0.0f)  }),
                    new triangle(new Vector3[] { new Vector3(1.0f, 0.0f, 0.0f), new Vector3(1.0f, 1.0f, 0.0f), new Vector3(1.0f, 1.0f, 1.0f)  }),
                    new triangle(new Vector3[] { new Vector3(1.0f, 0.0f, 0.0f), new Vector3(1.0f, 1.0f, 1.0f), new Vector3(1.0f, 0.0f, 1.0f)  }),
                    new triangle(new Vector3[] { new Vector3(1.0f, 0.0f, 1.0f), new Vector3(1.0f, 1.0f, 1.0f), new Vector3(0.0f, 1.0f, 1.0f)  }),
                    new triangle(new Vector3[] { new Vector3(1.0f, 0.0f, 1.0f), new Vector3(0.0f, 1.0f, 1.0f), new Vector3(0.0f, 0.0f, 1.0f)  }),
                    new triangle(new Vector3[] { new Vector3(0.0f, 0.0f, 1.0f), new Vector3(0.0f, 1.0f, 1.0f), new Vector3(0.0f, 1.0f, 0.0f)  }),
                    new triangle(new Vector3[] { new Vector3(0.0f, 0.0f, 1.0f), new Vector3(0.0f, 1.0f, 0.0f), new Vector3(0.0f, 0.0f, 0.0f)  }),
                    new triangle(new Vector3[] { new Vector3(0.0f, 1.0f, 0.0f), new Vector3(0.0f, 1.0f, 1.0f), new Vector3(1.0f, 1.0f, 1.0f)  }),
                    new triangle(new Vector3[] { new Vector3(0.0f, 1.0f, 0.0f), new Vector3(1.0f, 1.0f, 1.0f), new Vector3(1.0f, 1.0f, 0.0f)  }),
                    new triangle(new Vector3[] { new Vector3(1.0f, 0.0f, 1.0f), new Vector3(0.0f, 0.0f, 1.0f), new Vector3(0.0f, 0.0f, 0.0f)  }),
                    new triangle(new Vector3[] { new Vector3(1.0f, 0.0f, 1.0f), new Vector3(0.0f, 0.0f, 0.0f), new Vector3(1.0f, 0.0f, 0.0f)  })
                };
        }

        public void ProcessObj(string filename, Color4 color)
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
                                    tris.Last().c[0] = color;
                                    tris.Last().c[1] = color;
                                    tris.Last().c[2] = color;
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
                                    tris.Last().c[0] = color;
                                    tris.Last().c[1] = color;
                                    tris.Last().c[2] = color;
                                }

                            }
                            else
                            {
                                string[] vStr = result.Substring(2).Split(" ");
                                int[] f = { int.Parse(vStr[0]), int.Parse(vStr[1]), int.Parse(vStr[2]) };

                                tris.Add(new triangle(new Vector3[] { verts[f[0] - 1], verts[f[1] - 1], verts[f[2] - 1] }));
                                tris.Last().c[0] = color;
                                tris.Last().c[1] = color;
                                tris.Last().c[2] = color;
                            }
                        }
                    }

                    if (result == null)
                        break;
                }
            }
        }
    }
}
