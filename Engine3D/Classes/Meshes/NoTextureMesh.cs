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

namespace Engine3D
{

    public class NoTextureMesh : BaseMesh
    {
        public static int floatCount = 8;

        private List<float> vertices = new List<float>();
        private string? modelName;

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

        private Camera camera;

        Matrix4 modelMatrix, viewMatrix, projectionMatrix;

        private VAO Vao;
        private VBO Vbo;

        public NoTextureMesh(VAO vao, VBO vbo, int shaderProgramId, string modelName, ref Frustum frustum, ref Camera camera, Color4 color) : base(vao.id, vbo.id, shaderProgramId)
        {
            this.camera = camera;

            Vao = vao;
            Vbo = vbo;

            Position = Vector3.Zero;
            Rotation = Quaternion.Identity;
            Scale = Vector3.One;

            this.modelName = modelName;
            ProcessObj(modelName, color);

            foreach (triangle tri in tris)
            {
                tri.c[0] = color;
                tri.c[1] = color;
                tri.c[2] = color;
            }

            GetUniformLocations();
            SendUniforms();
        }

        private void GetUniformLocations()
        {
            uniformLocations.Add("modelMatrix", GL.GetUniformLocation(shaderProgramId, "modelMatrix"));
            uniformLocations.Add("viewMatrix", GL.GetUniformLocation(shaderProgramId, "viewMatrix"));
            uniformLocations.Add("projectionMatrix", GL.GetUniformLocation(shaderProgramId, "projectionMatrix"));
        }

        protected override void SendUniforms()
        {
            modelMatrix = Matrix4.Identity;
            projectionMatrix = camera.projectionMatrix;
            viewMatrix = camera.viewMatrix;

            GL.UniformMatrix4(uniformLocations["modelMatrix"], true, ref modelMatrix);
            GL.UniformMatrix4(uniformLocations["viewMatrix"], true, ref viewMatrix);
            GL.UniformMatrix4(uniformLocations["projectionMatrix"], true, ref projectionMatrix);
        }
        private List<float> ConvertToNDC(triangle tri, int index, ref Matrix4 transformMatrix)
        {
            Vector3 v = Vector3.TransformPosition(tri.p[index], transformMatrix);

            List<float> result = new List<float>()
            {
                v.X, v.Y, v.Z, 1.0f,
                tri.c[index].R, tri.c[index].G, tri.c[index].B, tri.c[index].A
            };

            return result;
        }

        public List<float> Draw(GameState gameRunning)
        {
            Vao.Bind();

            if (gameRunning == GameState.Stopped && vertices.Count > 0)
            {
                SendUniforms();

                return vertices;
            }

            vertices = new List<float>();

            Matrix4 s = Matrix4.CreateScale(Scale);
            Matrix4 r = Matrix4.CreateFromQuaternion(Rotation);
            Matrix4 t = Matrix4.CreateTranslation(Position);
            Matrix4 offsetTo = Matrix4.CreateTranslation(-Scale / 2f);
            Matrix4 offsetFrom = Matrix4.CreateTranslation(Scale / 2f);

            Matrix4 transformMatrix = Matrix4.Identity;
            if (IsTransformed)
            {
                transformMatrix = s * offsetTo * r * offsetFrom * t;
            }

            foreach (triangle tri in tris)
            {
                if (tri.visibile)
                {
                    if (tri.gotPointNormals)
                    {
                        vertices.AddRange(ConvertToNDC(tri, 0, ref transformMatrix));
                        vertices.AddRange(ConvertToNDC(tri, 1, ref transformMatrix));
                        vertices.AddRange(ConvertToNDC(tri, 2, ref transformMatrix));
                    }
                    else
                    {
                        Vector3 normal = tri.ComputeTriangleNormal();
                        vertices.AddRange(ConvertToNDC(tri, 0, ref transformMatrix));
                        vertices.AddRange(ConvertToNDC(tri, 1, ref transformMatrix));
                        vertices.AddRange(ConvertToNDC(tri, 2, ref transformMatrix));
                    }
                }
            }

            SendUniforms();

            return vertices;
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
