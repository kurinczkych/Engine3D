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
        private string? textureName;
        //private int vertexSize;

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

        public UITextureMesh(VAO vao, VBO vbo, int shaderProgramId, string textureName, Vector2 position, Vector2 size, Vector2 windowSize, ref int textureCount) : base(vao.id, vbo.id, shaderProgramId)
        {
            this.windowSize = windowSize;
            Position = new Vector2(position.X, position.Y);
            Size = new Vector3(size.X, size.Y, 0);
            rotation = Vector3.Zero;

            texture = new Texture(textureCount, textureName);
            textureCount += texture.textureDescriptor.count;

            Vao = vao;
            Vbo = vbo;

            OnlyQuad();

            this.textureName = textureName;
            TextureDescriptor td = Texture.GetTextureDescriptor(textureName);
            Texture.LoadTexture(td.Texture, false, TextureMinFilter.Nearest, TextureMagFilter.Nearest);

            GetUniformLocations();
            SendUniforms();
        }

        private void GetUniformLocations()
        {
            uniformLocations.Add("textureSampler", GL.GetUniformLocation(shaderProgramId, "textureSampler"));
            uniformLocations.Add("windowSize", GL.GetUniformLocation(shaderProgramId, "windowSize"));
        }

        protected override void SendUniforms()
        {
            GL.Uniform2(uniformLocations["windowSize"], windowSize);
            GL.Uniform1(uniformLocations["textureSampler"], texture.textureDescriptor.TextureUnit);
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
            texture.Bind(TextureType.Texture);

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
    }
}
