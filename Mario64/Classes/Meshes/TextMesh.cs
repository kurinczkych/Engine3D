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
        public Texture texture;

        private Vector2 windowSize;

        private List<TextVertex> vertices = new List<TextVertex>();

        // Text variables
        public Vector2 position;
        public Vector2 sizeScale;
        public Color4 color;

        public TextMesh(int vaoId, int vboId, int shaderProgramId, string embeddedTextureName, Vector2 windowSize, ref int textureCount) : base(vaoId, vboId, shaderProgramId)
        {
            texture = new Texture(textureCount, embeddedTextureName);
            textureCount++;

            this.windowSize = windowSize;

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
            int textureLocation = GL.GetUniformLocation(shaderProgramId, "textureSampler");
            int windowSizeLocation = GL.GetUniformLocation(shaderProgramId, "windowSize");
            GL.Uniform2(windowSizeLocation, windowSize);
            GL.Uniform1(textureLocation, texture.unit);
        }

        public List<TextVertex> Draw()
        {
            vertices = new List<TextVertex>();

            foreach (triangle tri in tris)
            {
                vertices.Add(ConvertToNDC(tri.p[0], tri.t[0], tri.c[0]));
                vertices.Add(ConvertToNDC(tri.p[1], tri.t[1], tri.c[0]));
                vertices.Add(ConvertToNDC(tri.p[2], tri.t[2], tri.c[0]));
            }

            SendUniforms();
            texture.Bind();

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
