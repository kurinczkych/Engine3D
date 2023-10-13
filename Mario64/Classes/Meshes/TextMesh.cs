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

namespace Engine3D
{
    public class TextMesh : BaseMesh
    {
        public static int floatCount = 10;

        public Texture texture;

        private Vector2 windowSize;
        private TextGenerator textGenerator;

        private List<float> vertices = new List<float>();

        // Text variables
        public Vector2 Position;
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
        public Vector2 Scale;

        private VAO Vao;
        private VBO Vbo;

        public string currentText = "";

        public TextMesh(VAO vao, VBO vbo, int shaderProgramId, string embeddedTextureName, Vector2 windowSize, ref TextGenerator tg, ref int textureCount) : base(vao.id, vbo.id, shaderProgramId)
        {
            texture = new Texture(textureCount, embeddedTextureName, false, "nearest");
            textureCount++;

            Vao = vao;
            Vbo = vbo;

            Position = Vector2.Zero;
            Rotation = 0;
            Scale = Vector2.One;

            this.windowSize = windowSize;
            this.textGenerator = tg;

            SendUniforms();
        }

        public void ChangeText(string text)
        {
            currentText = text;
            tris = textGenerator.GetTriangles(text);
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

        protected override void SendUniforms()
        {
            int textureLocation = GL.GetUniformLocation(shaderProgramId, "textureSampler");
            int windowSizeLocation = GL.GetUniformLocation(shaderProgramId, "windowSize");
            GL.Uniform2(windowSizeLocation, windowSize);
            GL.Uniform1(textureLocation, texture.unit);
        }

        public List<float> Draw()
        {
            Vao.Bind();

            vertices = new List<float>();

            Matrix4 s = Matrix4.CreateScale(new Vector3(Scale.X, Scale.Y, 1.0f));
            Matrix4 t = Matrix4.CreateTranslation(new Vector3(Position.X, Position.Y, 0));
            Matrix4 transformMatrix = s * t;

            if (Rotation != 0)
            {
                Matrix4 toOrigin = Matrix4.CreateTranslation(-Scale.X / 2, -Scale.Y / 2, 0);
                Matrix4 fromOrigin = Matrix4.CreateTranslation(Scale.X / 2, Scale.Y / 2, 0);
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
    }
}
