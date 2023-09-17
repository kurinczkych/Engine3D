using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL4;
using Newtonsoft.Json;
using System.Security.Principal;
using OpenTK.Mathematics;
using System.Security.Cryptography;
using System.Runtime.InteropServices;

namespace Mario64
{

    public struct TextVertex
    {
        public Vector4 Position;
        public Color4 Color;
        public Vector2 Texture;
    }

    public class TextGenerator
    {
        public class Root
        {
            public Config config { get; set; }
            public List<Symbol> symbols { get; set; }
        }
        public class Config
        {
            public int _base { get; set; }
            public int bold { get; set; }
            public int charHeight { get; set; }
            public int charSpacing { get; set; }
            public string face { get; set; }
            public int italic { get; set; }
            public int lineSpacing { get; set; }
            public int size { get; set; }
            public int smooth { get; set; }
            public string textureFile { get; set; }
            public int textureHeight { get; set; }
            public int textureWidth { get; set; }
        }
        public class Symbol
        {
            public int height { get; set; }
            public int id { get; set; }
            public char c { get { return (char)id; } }
            public int width { get; set; }
            public int x { get; set; }
            public int xadvance { get; set; }
            public int xoffset { get; set; }
            public int y { get; set; }
            public int yoffset { get; set; }
        }

        private SortedDictionary<char,  Symbol> symbols { get; set; }
        private Root font;

        public TextGenerator()
        {
            font = JsonConvert.DeserializeObject<Root>(GetFile("font.json"));

            symbols = new SortedDictionary<char, Symbol>();
            foreach(Symbol s in font.symbols)
            {
                symbols.Add(s.c, s);
            }
        }

        public Text Generate(int vaoId, int shaderProgramId, string t, Vector2 pos, Color4 color, Vector2 sizeScale, Vector2 windowSize, ref int textureCount)
        {
            Text textMesh = new Text(vaoId, shaderProgramId, "font.png", windowSize, ref textureCount);
            textMesh.position = pos;
            textMesh.color = color;
            textMesh.sizeScale = sizeScale;

            Vector2 start = pos;
            for (int i = 0; i < t.Length;i++)
            {
                Symbol s = symbols[t[i]];

                float width = s.width * sizeScale.X;
                float height = s.height * sizeScale.Y;

                Vector2 topleft = new Vector2(s.x / (float)font.config.textureWidth, s.y / (float)font.config.textureHeight);
                Vector2 topRight = new Vector2((s.x + s.width) / (float)font.config.textureWidth, s.y / (float)font.config.textureHeight);
                Vector2 bottomLeft = new Vector2(s.x / (float)font.config.textureWidth, (s.y + s.height) / (float)font.config.textureHeight);
                Vector2 bottomRight = new Vector2((s.x + s.width) / (float)font.config.textureWidth, (s.y + s.height) / (float)font.config.textureHeight);

                triangle t1 = new triangle();
                t1.p[0].X = start.X;
                t1.p[0].Y = start.Y;
                t1.t[0].u = topleft.X;
                t1.t[0].v = topleft.Y;
                t1.c[0] = color;
                t1.p[1].X = start.X;
                t1.p[1].Y = start.Y + height;
                t1.t[1].u = bottomLeft.X;
                t1.t[1].v = bottomLeft.Y;
                t1.c[1] = color;
                t1.p[2].X = start.X + width;
                t1.p[2].Y = start.Y;
                t1.t[2].u = topRight.X;
                t1.t[2].v = topRight.Y;
                t1.c[2] = color;

                triangle t2 = new triangle();
                t2.p[0].X = start.X + width;
                t2.p[0].Y = start.Y;
                t2.t[0].u = topRight.X;
                t2.t[0].v = topRight.Y;
                t2.c[0] = color;
                t2.p[1].X = start.X;
                t2.p[1].Y = start.Y + height;
                t2.t[1].u = bottomLeft.X;
                t2.t[1].v = bottomLeft.Y;
                t2.c[1] = color;
                t2.p[2].X = start.X + width;
                t2.p[2].Y = start.Y + height;
                t2.t[2].u = bottomRight.X;
                t2.t[2].v = bottomRight.Y;
                t2.c[2] = color;

                textMesh.AddTriangle(t1);
                textMesh.AddTriangle(t2);

                start.X += width;
            }

            return textMesh;
        }

        public Text Generate(int vaoId, int shaderProgramId, string t, Vector2 pos, Color4 color, Vector2 sizeScale, Vector2 windowSize, int textureCount)
        {
            Text textMesh = new Text(vaoId, shaderProgramId, "font.png", windowSize, ref textureCount);
            textMesh.position = pos;
            textMesh.color = color;
            textMesh.sizeScale = sizeScale;

            Vector2 start = pos;
            for (int i = 0; i < t.Length;i++)
            {
                Symbol s = symbols[t[i]];

                float width = s.width * sizeScale.X;
                float height = s.height * sizeScale.Y;

                Vector2 topleft = new Vector2(s.x / (float)font.config.textureWidth, s.y / (float)font.config.textureHeight);
                Vector2 topRight = new Vector2((s.x + s.width) / (float)font.config.textureWidth, s.y / (float)font.config.textureHeight);
                Vector2 bottomLeft = new Vector2(s.x / (float)font.config.textureWidth, (s.y + s.height) / (float)font.config.textureHeight);
                Vector2 bottomRight = new Vector2((s.x + s.width) / (float)font.config.textureWidth, (s.y + s.height) / (float)font.config.textureHeight);

                triangle t1 = new triangle();
                t1.p[0].X = start.X;
                t1.p[0].Y = start.Y;
                t1.t[0].u = topleft.X;
                t1.t[0].v = topleft.Y;
                t1.c[0] = color;
                t1.p[1].X = start.X;
                t1.p[1].Y = start.Y + height;
                t1.t[1].u = bottomLeft.X;
                t1.t[1].v = bottomLeft.Y;
                t1.c[1] = color;
                t1.p[2].X = start.X + width;
                t1.p[2].Y = start.Y;
                t1.t[2].u = topRight.X;
                t1.t[2].v = topRight.Y;
                t1.c[2] = color;

                triangle t2 = new triangle();
                t2.p[0].X = start.X + width;
                t2.p[0].Y = start.Y;
                t2.t[0].u = topRight.X;
                t2.t[0].v = topRight.Y;
                t2.c[0] = color;
                t2.p[1].X = start.X;
                t2.p[1].Y = start.Y + height;
                t2.t[1].u = bottomLeft.X;
                t2.t[1].v = bottomLeft.Y;
                t2.c[1] = color;
                t2.p[2].X = start.X + width;
                t2.p[2].Y = start.Y + height;
                t2.t[2].u = bottomRight.X;
                t2.t[2].v = bottomRight.Y;
                t2.c[2] = color;

                textMesh.AddTriangle(t1);
                textMesh.AddTriangle(t2);

                start.X += width;
            }

            return textMesh;
        }

        private string GetFile(string embeddedResourceName)
        {
            // Load the image (using System.Drawing or another library)
            Stream stream = GetResourceStreamByNameEnd(embeddedResourceName);
            if (stream != null)
            {
                using (stream)
                {
                    StreamReader sr = new StreamReader(stream);
                    return sr.ReadToEnd();
                }
            }
            else
            {
                throw new Exception("No file was found");
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

    public class Text
    {
        public string text;

        private int vaoId;
        public int vbo;
        private int shaderProgramId;
        private int textureId;
        private int textureUnit;

        private Vector2 windowSize;

        private string? embeddedTextureName;
        private int vertexSize;

        private List<triangle> tris;
        private List<TextVertex> vertices;

        // Text variables
        public Vector2 position;
        public Vector2 sizeScale;
        public Color4 color;

        public Text(int vaoId, int shaderProgramId, string embeddedTextureName, Vector2 windowSize, ref int textureCount)
        {
            tris = new List<triangle>();
            vertices = new List<TextVertex>();
            this.windowSize = windowSize;

            this.vaoId = vaoId;
            this.shaderProgramId = shaderProgramId;
            textureUnit = textureCount;
            textureCount++;

            // generate a buffer
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
        }

        public void ChangeText(string text, ref TextGenerator tg)
        {
            Text t = tg.Generate(vaoId, shaderProgramId, text, position, color, sizeScale, windowSize, textureUnit);
            tris = new List<triangle>(t.tris);
        }

        public void Draw()
        {
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

        TextVertex ConvertToNDC(Vector3 screenPos, Vec2d tex, Color4 color)
        {
            float x = (2.0f * screenPos.X / windowSize.X) - 1.0f;
            float y = (2.0f * screenPos.Y / windowSize.Y) - 1.0f;

            return new TextVertex()
            {
                //Position = new Vector4(screenPos.X, screenPos.Y, screenPos.Z, 1.0f),
                Position = new Vector4(x, y, screenPos.Z, 1.0f),
                Color = color,
                Texture = new Vector2(tex.u, tex.v)
            };
        }

        public void AddTriangle(triangle tri)
        {
            tris.Add(tri);
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
