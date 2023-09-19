using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static System.Formats.Asn1.AsnWriter;

namespace Mario64
{
    public struct UITextureVertex
    {
        public Vector4 Position;
        public Color4 Color;
        public Vector2 Texture;
    }

    public class UITextureMesh : BaseMesh
    {
        private int textureId;
        private int textureUnit;

        private Vector2 windowSize;

        private List<UITextureVertex> vertices = new List<UITextureVertex>();
        private string? embeddedTextureName;
        private int vertexSize;

        public Vector3 Position;
        public Vector3 Size;
        public float Rotation
        {
            set
            {
                rotation = new Vector3(0, 0, value);
            }
            get
            {
                return rotation.Z;
            }
        }
        private Vector3 rotation;

        public UITextureMesh(int vaoId, int shaderProgramId, string embeddedTextureName, Vector2 position, Vector2 size, Vector2 windowSize, ref int textureCount) : base(vaoId, shaderProgramId)
        {
            this.windowSize = windowSize;
            Position = new Vector3(position.X, position.Y, 0);
            Size = new Vector3(size.X, size.Y, 0);
            rotation = Vector3.Zero;

            textureUnit = textureCount;
            textureCount++;

            GL.GenBuffers(1, out vbo);

            // Texture -----------------------------------------------
            textureId = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, textureId);

            OnlyQuad();

            this.embeddedTextureName = embeddedTextureName;
            LoadTexture(embeddedTextureName);

            vertexSize = System.Runtime.InteropServices.Marshal.SizeOf(typeof(TextVertex));

            // VAO creating
            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
            GL.BindVertexArray(vaoId);

            GL.VertexAttribPointer(0, 4, VertexAttribPointerType.Float, false, vertexSize, 0);
            GL.EnableVertexArrayAttrib(vaoId, 0);

            GL.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, vertexSize, 4 * sizeof(float));
            GL.EnableVertexArrayAttrib(vaoId, 1);

            GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, vertexSize, 8 * sizeof(float));
            GL.EnableVertexArrayAttrib(vaoId, 2);

            int textureLocation = GL.GetUniformLocation(shaderProgramId, "textureSampler");
            GL.ActiveTexture(TextureUnit.Texture0 + textureUnit);
            GL.BindTexture(TextureTarget.Texture2D, textureId);
            GL.Uniform1(textureLocation, textureUnit);

            GL.BindVertexArray(0); // Unbind the VAO
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0); // Unbind the VBO

            SendUniforms();
        }

        protected override void SendUniforms()
        {
            int windowSizeLocation = GL.GetUniformLocation(shaderProgramId, "windowSize");
            GL.Uniform2(windowSizeLocation, windowSize);
        }

        private UITextureVertex ConvertToNDC(triangle tri, int index, ref Matrix4 transformMatrix)
        {
            Vector3 v = Vector3.TransformPosition(tri.p[index], transformMatrix);

            float x = (2.0f * v.X / windowSize.X) - 1.0f;
            float y = (2.0f * v.Y / windowSize.Y) - 1.0f;

            return new UITextureVertex()
            {
                Position = new Vector4(x, y, -1.0f, 1.0f),
                Color = tri.c[index],
                Texture = new Vector2(tri.t[index].u, tri.t[index].v)
            };
        }

        public override void Draw()
        {
            SendUniforms();
            vertices = new List<UITextureVertex>();

            Matrix4 s = Matrix4.CreateScale(Size);
            Matrix4 t = Matrix4.CreateTranslation(Position);
            Matrix4 transformMatrix = s * t;

            if (rotation != Vector3.Zero)
            {
                Matrix4 toOrigin = Matrix4.CreateTranslation(-Size.X / 2, -Size.Y / 2, 0);
                Matrix4 fromOrigin = Matrix4.CreateTranslation(Size.X / 2, Size.Y / 2, 0);
                Matrix4 rX = Matrix4.CreateRotationX(MathHelper.DegreesToRadians(rotation.X));
                Matrix4 rY = Matrix4.CreateRotationY(MathHelper.DegreesToRadians(rotation.Y));
                Matrix4 rZ = Matrix4.CreateRotationZ(MathHelper.DegreesToRadians(rotation.Z));
                transformMatrix = s * toOrigin * rX * rY * rZ * fromOrigin * t;
            }


            foreach (triangle tri in tris)
            {
                vertices.Add(ConvertToNDC(tri, 0, ref transformMatrix));
                vertices.Add(ConvertToNDC(tri, 1, ref transformMatrix));
                vertices.Add(ConvertToNDC(tri, 2, ref transformMatrix));
            }

            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
            GL.BindVertexArray(vaoId);
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Count * vertexSize, vertices.ToArray(), BufferUsageHint.DynamicDraw);

            int textureLocation = GL.GetUniformLocation(shaderProgramId, "textureSampler");
            GL.ActiveTexture(TextureUnit.Texture0 + textureUnit);
            GL.BindTexture(TextureTarget.Texture2D, textureId);
            GL.Uniform1(textureLocation, textureUnit);

            GL.DrawArrays(PrimitiveType.Triangles, 0, vertices.Count);

            GL.BindVertexArray(0); // Unbind the VAO
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0); // Unbind the VBO
        }

        private void OnlyQuad()
        {
            tris = new List<triangle>
            {
                new triangle(new Vector3[] { new Vector3(0, 0, 0), new Vector3(0, 1, 0), new Vector3(1, 0, 0) },
                                  new Vec2d[] { new Vec2d(0, 0), new Vec2d(0, 1), new Vec2d(1, 0) }),
                new triangle(new Vector3[] { new Vector3(1, 0, 0), new Vector3(0, 1, 0), new Vector3(1, 1, 0) },
                                  new Vec2d[] { new Vec2d(1, 0), new Vec2d(0, 1), new Vec2d(1, 1) })
            };
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
                    //bitmap.RotateFlip(RotateFlipType.RotateNoneFlipY);
                    BitmapData data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

                    GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, data.Width, data.Height, 0, OpenTK.Graphics.OpenGL4.PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);

                    bitmap.UnlockBits(data);

                    // Texture settings
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
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
    }
}
