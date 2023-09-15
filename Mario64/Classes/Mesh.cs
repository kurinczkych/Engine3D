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

        private List<triangle> tris;
        private List<Vertex> vertices;

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

            // VAO creating
            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
            GL.BindVertexArray(vaoId);

            GL.VertexAttribPointer(0, 4, VertexAttribPointerType.Float, false, vertexSize, 0);
            GL.EnableVertexArrayAttrib(vaoId, 0);

            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, vertexSize, 4 * sizeof(float));
            GL.EnableVertexArrayAttrib(vaoId, 1);

            GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, vertexSize, 7 * sizeof(float));
            GL.EnableVertexArrayAttrib(vaoId, 2);

            GL.VertexAttribPointer(3, 3, VertexAttribPointerType.Float, false, vertexSize, 9 * sizeof(float));
            GL.EnableVertexArrayAttrib(vaoId, 3);

            int textureLocation = GL.GetUniformLocation(shaderProgramId, "textureSampler");
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, textureId);
            GL.Uniform1(textureLocation, 0);  // 0 corresponds to TextureUnit.Texture0
        }

        public void Transform(Vector3 trans)
        {
            foreach(triangle tri in tris)
            {
                
            }
        }

        public void Rotate(Vector3 rotate)
        {

        }

        public void Scale(Vector3 scale)
        {

        }

        Vertex ConvertToNDC(Vec3d screenPos, Vec2d tex, Vec3d normal)
        {
            return new Vertex()
            {
                Position = new Vector4(screenPos.X, screenPos.Y, screenPos.Z, screenPos.W),
                Normal = new Vector3(normal.X, normal.Y, normal.Z),
                Texture = new Vector2(tex.u, tex.v),
                Camera = new Vector3(0f, 0f, 0f)
            };
        }

        public List<Vertex> UpdateVertexArray(ref Frustum frustum, ref Camera camera)
        {
            vertices = new List<Vertex>();

            foreach (triangle tri in tris)
            {
                if (frustum.IsTriangleInside(tri) || camera.IsTriangleClose(tri))
                {
                    Vec3d normal = tri.ComputeNormal();
                    vertices.Add(ConvertToNDC(tri.p[0], tri.t[0], normal));
                    vertices.Add(ConvertToNDC(tri.p[1], tri.t[1], normal));
                    vertices.Add(ConvertToNDC(tri.p[2], tri.t[2], normal));
                }
            }

            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);

            GL.VertexAttribPointer(0, 4, VertexAttribPointerType.Float, false, vertexSize, 0);
            GL.EnableVertexArrayAttrib(vaoId, 0);

            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, vertexSize, 4 * sizeof(float));
            GL.EnableVertexArrayAttrib(vaoId, 1);

            GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, vertexSize, 7 * sizeof(float));
            GL.EnableVertexArrayAttrib(vaoId, 2);

            GL.VertexAttribPointer(3, 3, VertexAttribPointerType.Float, false, vertexSize, 9 * sizeof(float));
            GL.EnableVertexArrayAttrib(vaoId, 3);

            return vertices;
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
            List<Vec3d> verts = new List<Vec3d>();
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
                                Vec3d v = new Vec3d(a, b, c);
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

                                tris.Add(new triangle(new Vec3d[] { verts[v[0] - 1], verts[v[1] - 1], verts[v[2] - 1] },
                                                      new Vec2d[] { uvs[uv[0] - 1], uvs[uv[1] - 1], uvs[uv[2] - 1] }));
                            }
                            else
                            {
                                string[] vStr = result.Substring(2).Split(" ");
                                int[] f = { int.Parse(vStr[0]), int.Parse(vStr[1]), int.Parse(vStr[2]) };

                                tris.Add(new triangle(new Vec3d[] { verts[f[0] - 1], verts[f[1] - 1], verts[f[2] - 1] }));
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
                    new triangle(new Vec3d[] { new Vec3d(0.0f, 0.0f, 0.0f), new Vec3d(0.0f, 1.0f, 0.0f), new Vec3d(1.0f, 1.0f, 0.0f) }, new Vec2d[] { new Vec2d(0.0f, 1.0f), new Vec2d(0.0f, 0.0f), new Vec2d(1.0f, 0.0f) }),
                    new triangle(new Vec3d[] { new Vec3d(0.0f, 0.0f, 0.0f), new Vec3d(1.0f, 1.0f, 0.0f), new Vec3d(1.0f, 0.0f, 0.0f) }, new Vec2d[] { new Vec2d(0.0f, 1.0f), new Vec2d(1.0f, 0.0f), new Vec2d(1.0f, 1.0f) }),
                    new triangle(new Vec3d[] { new Vec3d(1.0f, 0.0f, 0.0f), new Vec3d(1.0f, 1.0f, 0.0f), new Vec3d(1.0f, 1.0f, 1.0f) }, new Vec2d[] { new Vec2d(0.0f, 1.0f), new Vec2d(0.0f, 0.0f), new Vec2d(1.0f, 0.0f) }),
                    new triangle(new Vec3d[] { new Vec3d(1.0f, 0.0f, 0.0f), new Vec3d(1.0f, 1.0f, 1.0f), new Vec3d(1.0f, 0.0f, 1.0f) }, new Vec2d[] { new Vec2d(0.0f, 1.0f), new Vec2d(1.0f, 0.0f), new Vec2d(1.0f, 1.0f) }),
                    new triangle(new Vec3d[] { new Vec3d(1.0f, 0.0f, 1.0f), new Vec3d(1.0f, 1.0f, 1.0f), new Vec3d(0.0f, 1.0f, 1.0f) }, new Vec2d[] { new Vec2d(0.0f, 1.0f), new Vec2d(0.0f, 0.0f), new Vec2d(1.0f, 0.0f) }),
                    new triangle(new Vec3d[] { new Vec3d(1.0f, 0.0f, 1.0f), new Vec3d(0.0f, 1.0f, 1.0f), new Vec3d(0.0f, 0.0f, 1.0f) }, new Vec2d[] { new Vec2d(0.0f, 1.0f), new Vec2d(1.0f, 0.0f), new Vec2d(1.0f, 1.0f) }),
                    new triangle(new Vec3d[] { new Vec3d(0.0f, 0.0f, 1.0f), new Vec3d(0.0f, 1.0f, 1.0f), new Vec3d(0.0f, 1.0f, 0.0f) }, new Vec2d[] { new Vec2d(0.0f, 1.0f), new Vec2d(0.0f, 0.0f), new Vec2d(1.0f, 0.0f) }),
                    new triangle(new Vec3d[] { new Vec3d(0.0f, 0.0f, 1.0f), new Vec3d(0.0f, 1.0f, 0.0f), new Vec3d(0.0f, 0.0f, 0.0f) }, new Vec2d[] { new Vec2d(0.0f, 1.0f), new Vec2d(1.0f, 0.0f), new Vec2d(1.0f, 1.0f) }),
                    new triangle(new Vec3d[] { new Vec3d(0.0f, 1.0f, 0.0f), new Vec3d(0.0f, 1.0f, 1.0f), new Vec3d(1.0f, 1.0f, 1.0f) }, new Vec2d[] { new Vec2d(0.0f, 1.0f), new Vec2d(0.0f, 0.0f), new Vec2d(1.0f, 0.0f) }),
                    new triangle(new Vec3d[] { new Vec3d(0.0f, 1.0f, 0.0f), new Vec3d(1.0f, 1.0f, 1.0f), new Vec3d(1.0f, 1.0f, 0.0f) }, new Vec2d[] { new Vec2d(0.0f, 1.0f), new Vec2d(1.0f, 0.0f), new Vec2d(1.0f, 1.0f) }),
                    new triangle(new Vec3d[] { new Vec3d(1.0f, 0.0f, 1.0f), new Vec3d(0.0f, 0.0f, 1.0f), new Vec3d(0.0f, 0.0f, 0.0f) }, new Vec2d[] { new Vec2d(0.0f, 1.0f), new Vec2d(0.0f, 0.0f), new Vec2d(1.0f, 0.0f) }),
                    new triangle(new Vec3d[] { new Vec3d(1.0f, 0.0f, 1.0f), new Vec3d(0.0f, 0.0f, 0.0f), new Vec3d(1.0f, 0.0f, 0.0f) }, new Vec2d[] { new Vec2d(0.0f, 1.0f), new Vec2d(1.0f, 0.0f), new Vec2d(1.0f, 1.0f) })
                };
        }

        public void OnlyTriangle()
        {
            tris = new List<triangle>
                {
                    new triangle(new Vec3d[] { new Vec3d(0.0f, 0.0f, 0.0f), new Vec3d(0.0f, 1.0f, 0.0f), new Vec3d(1.0f, 1.0f, 0.0f) }, new Vec2d[] { new Vec2d(0.0f, 1.0f), new Vec2d(0.0f, 0.0f), new Vec2d(1.0f, 0.0f) })
                };
        }
    }
}
