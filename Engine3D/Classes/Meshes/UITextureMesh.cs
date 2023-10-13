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

namespace Engine3D
{

    public class UITextureMesh : BaseMesh
    {
        public static int floatCount = 10;

        private Texture texture;

        private Vector2 windowSize;

        private List<float> vertices = new List<float>();
        private string? embeddedTextureName;
        private int vertexSize;

        private Vector3 position;
        public Vector2 Position
        {
            get { return new Vector2(position.X, position.Y); }
            set { position = new Vector3(value.X, value.Y, 0); }
        }
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

        private VAO Vao;
        private VBO Vbo;

        public UITextureMesh(VAO vao, VBO vbo, int shaderProgramId, string embeddedTextureName, Vector2 position, Vector2 size, Vector2 windowSize, ref int textureCount) : base(vao.id, vbo.id, shaderProgramId)
        {
            this.windowSize = windowSize;
            Position = new Vector2(position.X, position.Y);
            Size = new Vector3(size.X, size.Y, 0);
            rotation = Vector3.Zero;

            texture = new Texture(textureCount, embeddedTextureName);
            textureCount++;

            Vao = vao;
            Vbo = vbo;

            OnlyQuad();

            this.embeddedTextureName = embeddedTextureName;
            LoadTexture(embeddedTextureName);

            SendUniforms();
        }

        protected override void SendUniforms()
        {
            int textureLocation = GL.GetUniformLocation(shaderProgramId, "textureSampler");
            int windowSizeLocation = GL.GetUniformLocation(shaderProgramId, "windowSize");
            GL.Uniform2(windowSizeLocation, windowSize);
            GL.Uniform1(textureLocation, texture.unit);
        }

        private List<float> ConvertToNDC(triangle tri, int index, ref Matrix4 transformMatrix)
        {
            Vector3 v = Vector3.TransformPosition(tri.p[index], transformMatrix);

            float x = (2.0f * v.X / windowSize.X) - 1.0f;
            float y = (2.0f * v.Y / windowSize.Y) - 1.0f;

            List<float> result = new List<float>()
            {
                x, y, -1.0f, 1.0f,
                tri.c[index].R, tri.c[index].G, tri.c[index].B, tri.c[index].A,
                tri.t[index].u, tri.t[index].v
            };

            return result;
        }

        public List<float> Draw()
        {
            Vao.Bind();
            vertices = new List<float>();

            Matrix4 s = Matrix4.CreateScale(Size);
            Matrix4 t = Matrix4.CreateTranslation(position);
            Matrix4 transformMatrix = s * t;

            if (rotation != Vector3.Zero)
            {
                Matrix4 toOrigin = Matrix4.CreateTranslation(-Size.X / 2, -Size.Y / 2, 0);
                Matrix4 fromOrigin = Matrix4.CreateTranslation(Size.X / 2, Size.Y / 2, 0);
                Matrix4 rZ = Matrix4.CreateRotationZ(MathHelper.DegreesToRadians(rotation.Z));
                transformMatrix = s * toOrigin * rZ * fromOrigin * t;
            }

            foreach (triangle tri in tris)
            {
                vertices.AddRange(ConvertToNDC(tri, 0, ref transformMatrix));
                vertices.AddRange(ConvertToNDC(tri, 1, ref transformMatrix));
                vertices.AddRange(ConvertToNDC(tri, 2, ref transformMatrix));
            }

            SendUniforms();
            texture.Bind();

            return vertices;
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
