using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using OpenTK.Mathematics;
using System.Diagnostics;
using System.Runtime.InteropServices;

#pragma warning disable CS8600
#pragma warning disable CA1416
#pragma warning disable CS8604

namespace Mario64
{
    public class Mesh
    {
        private int vaoId;
        private int vbo;
        private int shaderProgramId;

        private string embeddedModelName;
        private string embeddedTextureName;
        private int vertexSize;
        private int matrix4Size;

        private List<triangle> tris;
        private List<Vertex> vertices;

        private Matrix4 transformMatrix;

        public Mesh(int vaoId, int shaderProgramId, string embeddedModelName, string embeddedTextureName)
        {
            tris = new List<triangle>();
            vertices = new List<Vertex>();

            this.vaoId = vaoId;
            this.shaderProgramId = shaderProgramId;

            // generate a buffer
            GL.GenBuffers(1, out vbo);

            // Texture -----------------------------------------------
            int textureId = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, textureId);

            this.embeddedModelName = embeddedModelName;
            ProcessObj(embeddedModelName);

            this.embeddedTextureName = embeddedTextureName;
            LoadTexture(embeddedTextureName);

            vertexSize = System.Runtime.InteropServices.Marshal.SizeOf(typeof(Vertex));
            int sizeOfFloat = Marshal.SizeOf(typeof(float));
            matrix4Size = sizeOfFloat * 16;

            // VAO creating
            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
            GL.BindVertexArray(vaoId);

            GL.VertexAttribPointer(0, 4, VertexAttribPointerType.Float, false, vertexSize, 0);
            GL.EnableVertexArrayAttrib(vaoId, 0);

            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, vertexSize, 4 * sizeof(float));
            GL.EnableVertexArrayAttrib(vaoId, 1);

            GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, vertexSize, 7 * sizeof(float));
            GL.EnableVertexArrayAttrib(vaoId, 2);

            int textureLocation = GL.GetUniformLocation(shaderProgramId, "textureSampler");
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, textureId);
            GL.Uniform1(textureLocation, 0);  // 0 corresponds to TextureUnit.Texture0
        }

        public void TranslateRotateScale(Vector3 trans, Vector3 rotate, Vector3 scale)
        {
            Matrix4 s = Matrix4.CreateScale(scale);
            Matrix4 rX = Matrix4.CreateRotationX(MathHelper.DegreesToRadians(rotate.X));
            Matrix4 rY = Matrix4.CreateRotationY(MathHelper.DegreesToRadians(rotate.Y));
            Matrix4 rZ = Matrix4.CreateRotationZ(MathHelper.DegreesToRadians(rotate.Z));
            Matrix4 t = Matrix4.CreateTranslation(trans);

            transformMatrix = s * rX * rY * rZ * t;
        }

        Vertex ConvertToNDC(Vector3 screenPos, Vec2d tex, Vector3 normal, Matrix4 transform)
        {
            return new Vertex()
            {
                Position = new Vector4(screenPos.X, screenPos.Y, screenPos.Z, 1.0f),
                Normal = new Vector3(normal.X, normal.Y, normal.Z),
                Texture = new Vector2(tex.u, tex.v)
            };
        }

        public void UpdateVertexArray(ref Frustum frustum, ref Camera camera)
        {
            vertices = new List<Vertex>();

            int transformMatrixLocation = GL.GetUniformLocation(shaderProgramId, "transformMatrix");
            GL.UniformMatrix4(transformMatrixLocation, true, ref transformMatrix);

            foreach (triangle tri in tris)
            {
                if (frustum.IsTriangleInside(tri) || camera.IsTriangleClose(tri))
                {
                    Vector3 normal = tri.ComputeNormal();
                    vertices.Add(ConvertToNDC(tri.p[0], tri.t[0], normal, transformMatrix));
                    vertices.Add(ConvertToNDC(tri.p[1], tri.t[1], normal, transformMatrix));
                    vertices.Add(ConvertToNDC(tri.p[2], tri.t[2], normal, transformMatrix));
                }
            }

            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);

            GL.VertexAttribPointer(0, 4, VertexAttribPointerType.Float, false, vertexSize, 0);
            GL.EnableVertexArrayAttrib(vaoId, 0);

            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, vertexSize, 4 * sizeof(float));
            GL.EnableVertexArrayAttrib(vaoId, 1);

            GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, vertexSize, 7 * sizeof(float));
            GL.EnableVertexArrayAttrib(vaoId, 2);

            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Count * vertexSize, vertices.ToArray(), BufferUsageHint.DynamicDraw);
        }

        public void Draw()
        {
            GL.DrawArrays(PrimitiveType.Triangles, 0, vertices.Count);
        }

        private void LoadTexture(string embeddedResourceName)
        {
            // Load the image (using System.Drawing or another library)
            Stream stream = GetResourceStreamByNameEnd(embeddedResourceName);
            if (stream != null)
            {
                using (stream)
                {
                    Bitmap bitmap = new Bitmap(stream);
                    bitmap.RotateFlip(RotateFlipType.RotateNoneFlipY);
                    BitmapData data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

                    GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, data.Width, data.Height, 0, OpenTK.Graphics.OpenGL4.PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);

                    bitmap.UnlockBits(data);

                    // Texture settings
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
                }
            }
            else
            {
                throw new Exception("No texture was found");
            }
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
            List<Vector3> verts = new List<Vector3>();
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
                                //string[] vStr = result.Substring(3).Split(" ");
                                //var a = float.Parse(vStr[0]);
                                //var b = float.Parse(vStr[1]);
                                //Vec2d v = new Vec2d(a, b);
                                //uvs.Add(v);
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

        public void OnlyCube()
        {
            tris = new List<triangle>
                {
                    new triangle(new Vector3[] { new Vector3(0.0f, 0.0f, 0.0f), new Vector3(0.0f, 1.0f, 0.0f), new Vector3(1.0f, 1.0f, 0.0f) }, new Vec2d[] { new Vec2d(0.0f, 1.0f), new Vec2d(0.0f, 0.0f), new Vec2d(1.0f, 0.0f) }),
                    new triangle(new Vector3[] { new Vector3(0.0f, 0.0f, 0.0f), new Vector3(1.0f, 1.0f, 0.0f), new Vector3(1.0f, 0.0f, 0.0f) }, new Vec2d[] { new Vec2d(0.0f, 1.0f), new Vec2d(1.0f, 0.0f), new Vec2d(1.0f, 1.0f) }),
                    new triangle(new Vector3[] { new Vector3(1.0f, 0.0f, 0.0f), new Vector3(1.0f, 1.0f, 0.0f), new Vector3(1.0f, 1.0f, 1.0f) }, new Vec2d[] { new Vec2d(0.0f, 1.0f), new Vec2d(0.0f, 0.0f), new Vec2d(1.0f, 0.0f) }),
                    new triangle(new Vector3[] { new Vector3(1.0f, 0.0f, 0.0f), new Vector3(1.0f, 1.0f, 1.0f), new Vector3(1.0f, 0.0f, 1.0f) }, new Vec2d[] { new Vec2d(0.0f, 1.0f), new Vec2d(1.0f, 0.0f), new Vec2d(1.0f, 1.0f) }),
                    new triangle(new Vector3[] { new Vector3(1.0f, 0.0f, 1.0f), new Vector3(1.0f, 1.0f, 1.0f), new Vector3(0.0f, 1.0f, 1.0f) }, new Vec2d[] { new Vec2d(0.0f, 1.0f), new Vec2d(0.0f, 0.0f), new Vec2d(1.0f, 0.0f) }),
                    new triangle(new Vector3[] { new Vector3(1.0f, 0.0f, 1.0f), new Vector3(0.0f, 1.0f, 1.0f), new Vector3(0.0f, 0.0f, 1.0f) }, new Vec2d[] { new Vec2d(0.0f, 1.0f), new Vec2d(1.0f, 0.0f), new Vec2d(1.0f, 1.0f) }),
                    new triangle(new Vector3[] { new Vector3(0.0f, 0.0f, 1.0f), new Vector3(0.0f, 1.0f, 1.0f), new Vector3(0.0f, 1.0f, 0.0f) }, new Vec2d[] { new Vec2d(0.0f, 1.0f), new Vec2d(0.0f, 0.0f), new Vec2d(1.0f, 0.0f) }),
                    new triangle(new Vector3[] { new Vector3(0.0f, 0.0f, 1.0f), new Vector3(0.0f, 1.0f, 0.0f), new Vector3(0.0f, 0.0f, 0.0f) }, new Vec2d[] { new Vec2d(0.0f, 1.0f), new Vec2d(1.0f, 0.0f), new Vec2d(1.0f, 1.0f) }),
                    new triangle(new Vector3[] { new Vector3(0.0f, 1.0f, 0.0f), new Vector3(0.0f, 1.0f, 1.0f), new Vector3(1.0f, 1.0f, 1.0f) }, new Vec2d[] { new Vec2d(0.0f, 1.0f), new Vec2d(0.0f, 0.0f), new Vec2d(1.0f, 0.0f) }),
                    new triangle(new Vector3[] { new Vector3(0.0f, 1.0f, 0.0f), new Vector3(1.0f, 1.0f, 1.0f), new Vector3(1.0f, 1.0f, 0.0f) }, new Vec2d[] { new Vec2d(0.0f, 1.0f), new Vec2d(1.0f, 0.0f), new Vec2d(1.0f, 1.0f) }),
                    new triangle(new Vector3[] { new Vector3(1.0f, 0.0f, 1.0f), new Vector3(0.0f, 0.0f, 1.0f), new Vector3(0.0f, 0.0f, 0.0f) }, new Vec2d[] { new Vec2d(0.0f, 1.0f), new Vec2d(0.0f, 0.0f), new Vec2d(1.0f, 0.0f) }),
                    new triangle(new Vector3[] { new Vector3(1.0f, 0.0f, 1.0f), new Vector3(0.0f, 0.0f, 0.0f), new Vector3(1.0f, 0.0f, 0.0f) }, new Vec2d[] { new Vec2d(0.0f, 1.0f), new Vec2d(1.0f, 0.0f), new Vec2d(1.0f, 1.0f) })
                };
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
