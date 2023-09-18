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

namespace Mario64
{
    public struct TextVertex
    {
        public Vector4 Position;
        public Color4 Color;
        public Vector2 Texture;
    }

    public class TextMesh : BaseMesh
    {
        private int textureId;
        private int textureUnit;

        private Vector2 windowSize;

        private List<TextVertex> vertices = new List<TextVertex>();
        private string? embeddedTextureName;
        private int vertexSize;

        // Text variables
        public Vector2 position;
        public Vector2 sizeScale;
        public Color4 color;

        public TextMesh(int vaoId, int shaderProgramId, string embeddedTextureName, Vector2 windowSize, ref int textureCount) : base(vaoId, shaderProgramId)
        {
            this.windowSize = windowSize;

            textureUnit = textureCount;
            textureCount++;

            GL.GenBuffers(1, out vbo);

            // Texture -----------------------------------------------
            textureId = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, textureId);

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

        private TextVertex ConvertToNDC(Vector3 screenPos, Vec2d tex, Color4 color)
        {
            float x = (2.0f * screenPos.X / windowSize.X) - 1.0f;
            float y = (2.0f * screenPos.Y / windowSize.Y) - 1.0f;

            return new TextVertex()
            {
                Position = new Vector4(x, y, screenPos.Z, 1.0f),
                Color = color,
                Texture = new Vector2(tex.u, tex.v)
            };
        }

        protected override void SendUniforms()
        {
            int windowSizeLocation = GL.GetUniformLocation(shaderProgramId, "windowSize");
            GL.Uniform2(windowSizeLocation, windowSize);
        }

        public override void Draw()
        {
            SendUniforms();

            vertices = new List<TextVertex>();

            foreach (triangle tri in tris)
            {
                vertices.Add(ConvertToNDC(tri.p[0], tri.t[0], tri.c[0]));
                vertices.Add(ConvertToNDC(tri.p[1], tri.t[1], tri.c[0]));
                vertices.Add(ConvertToNDC(tri.p[2], tri.t[2], tri.c[0]));
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
