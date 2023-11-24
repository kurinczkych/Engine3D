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

        public TextMesh(VAO vao, VBO vbo, int shaderProgramId, string texturePath, Vector2 windowSize, ref TextGenerator tg, ref Object parentObject) : base(vao.id, vbo.id, shaderProgramId)
        {
            this.parentObject = parentObject;

            bool success = false;
            parentObject.texture = Engine.textureManager.AddTexture(texturePath, out success, false, "nearest");
            if(!success)
                Engine.consoleManager.AddLog("Texture: " + texturePath + "was not found!", LogType.Warning);

            Vao = vao;
            Vbo = vbo;

            Position = Vector2.Zero;
            Rotation = 0;
            Scale = Vector2.One;

            model = new ModelData();

            this.windowSize = windowSize;
            this.textGenerator = tg;

            GetUniformLocations();
            SendUniforms();
        }

        public void ChangeText(string text)
        {
            currentText = text;

            MeshData meshData = textGenerator.GetTriangles(text);

            model.meshes = new List<MeshData>() { meshData };
        }

        private List<float> ConvertToNDC(triangle tri, int index, ref Matrix4 transformMatrix)
        {
            Vector3 v = Vector3.TransformPosition(tri.v[index].p, transformMatrix);

            float x = (2.0f * v.X / windowSize.X) - 1.0f;
            float y = (2.0f * v.Y / windowSize.Y) - 1.0f;

            List<float> result = new List<float>()
            {
                x, y, -1.0f, 1.0f,
                tri.v[index].c.R, tri.v[index].c.G, tri.v[index].c.B, tri.v[index].c.A,
                tri.v[index].t.u, tri.v[index].t.v
            };

            return result;
        }

        private void GetUniformLocations()
        {
            uniformLocations.Add("textureSampler", GL.GetUniformLocation(shaderProgramId, "textureSampler"));
            uniformLocations.Add("windowSize", GL.GetUniformLocation(shaderProgramId, "windowSize"));
        }

        protected override void SendUniforms()
        {
            GL.Uniform2(uniformLocations["windowSize"], windowSize);
            GL.Uniform1(uniformLocations["textureSampler"], parentObject.texture.TextureUnit);
        }

        public List<float> Draw(GameState gameRunning)
        {
            Vao.Bind();

            if (gameRunning == GameState.Stopped && vertices.Count > 0)
            {
                SendUniforms();

                if (parentObject.texture != null)
                {
                    parentObject.texture.Bind();
                }

                return vertices;
            }

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

            throw new NotImplementedException();

            //foreach (triangle tri in tris)
            //{
            //    vertices.AddRange(ConvertToNDC(tri, 0, ref transformMatrix));
            //    vertices.AddRange(ConvertToNDC(tri, 1, ref transformMatrix));
            //    vertices.AddRange(ConvertToNDC(tri, 2, ref transformMatrix));
            //}

            SendUniforms();
            parentObject.texture.Bind();

            return vertices;
        }
    }
}
