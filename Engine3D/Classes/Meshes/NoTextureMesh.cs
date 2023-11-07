using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Mathematics;
using static System.Net.Mime.MediaTypeNames;

#pragma warning disable CS8600
#pragma warning disable CA1416
#pragma warning disable CS8604
#pragma warning disable CS8603

namespace Engine3D
{

    public class NoTextureMesh : BaseMesh
    {
        public static int floatCount = 8;

        private List<float> vertices = new List<float>();
        private string? modelName;

        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 Scale;
        private bool IsTransformed
        {
            get
            {
                return !(Position == Vector3.Zero && Rotation == Quaternion.Identity && Scale == Vector3.One);
            }
        }


        Matrix4 viewMatrix, projectionMatrix;

        private VAO Vao;
        private VBO Vbo;

        public NoTextureMesh(VAO vao, VBO vbo, int shaderProgramId, string modelName, ref Camera camera, Color4 color, ref Object parentObject) : base(vao.id, vbo.id, shaderProgramId)
        {
            this.parentObject = parentObject;

            this.camera = camera;

            Vao = vao;
            Vbo = vbo;

            Position = Vector3.Zero;
            Rotation = Quaternion.Identity;
            Scale = Vector3.One;

            this.modelName = modelName;
            ProcessObj(modelName, color.R, color.G, color.B, color.A);

            GetUniformLocations();
            SendUniforms();
        }

        private void GetUniformLocations()
        {
            uniformLocations.Add("modelMatrix", GL.GetUniformLocation(shaderProgramId, "modelMatrix"));
            uniformLocations.Add("viewMatrix", GL.GetUniformLocation(shaderProgramId, "viewMatrix"));
            uniformLocations.Add("projectionMatrix", GL.GetUniformLocation(shaderProgramId, "projectionMatrix"));
        }

        protected override void SendUniforms()
        {
            modelMatrix = Matrix4.Identity;
            projectionMatrix = camera.projectionMatrix;
            viewMatrix = camera.viewMatrix;

            GL.UniformMatrix4(uniformLocations["modelMatrix"], true, ref modelMatrix);
            GL.UniformMatrix4(uniformLocations["viewMatrix"], true, ref viewMatrix);
            GL.UniformMatrix4(uniformLocations["projectionMatrix"], true, ref projectionMatrix);
        }
        private List<float> ConvertToNDC(triangle tri, int index, ref Matrix4 transformMatrix)
        {
            Vector3 v = Vector3.TransformPosition(tri.p[index], transformMatrix);

            List<float> result = new List<float>()
            {
                v.X, v.Y, v.Z, 1.0f,
                tri.c[index].R, tri.c[index].G, tri.c[index].B, tri.c[index].A
            };

            return result;
        }

        public List<float> Draw(GameState gameRunning)
        {
            Vao.Bind();

            if (gameRunning == GameState.Stopped && vertices.Count > 0)
            {
                SendUniforms();

                return vertices;
            }

            vertices = new List<float>();

            Matrix4 s = Matrix4.CreateScale(Scale);
            Matrix4 r = Matrix4.CreateFromQuaternion(Rotation);
            Matrix4 t = Matrix4.CreateTranslation(Position);
            Matrix4 offsetTo = Matrix4.CreateTranslation(-Scale / 2f);
            Matrix4 offsetFrom = Matrix4.CreateTranslation(Scale / 2f);

            Matrix4 transformMatrix = Matrix4.Identity;
            if (IsTransformed)
            {
                transformMatrix = s * offsetTo * r * offsetFrom * t;
            }

            foreach (triangle tri in tris)
            {
                if (tri.visibile)
                {
                    if (tri.gotPointNormals)
                    {
                        vertices.AddRange(ConvertToNDC(tri, 0, ref transformMatrix));
                        vertices.AddRange(ConvertToNDC(tri, 1, ref transformMatrix));
                        vertices.AddRange(ConvertToNDC(tri, 2, ref transformMatrix));
                    }
                    else
                    {
                        Vector3 normal = tri.ComputeTriangleNormal();
                        vertices.AddRange(ConvertToNDC(tri, 0, ref transformMatrix));
                        vertices.AddRange(ConvertToNDC(tri, 1, ref transformMatrix));
                        vertices.AddRange(ConvertToNDC(tri, 2, ref transformMatrix));
                    }
                }
            }

            SendUniforms();

            return vertices;
        }

        public void OnlyCube()
        {
            tris = new List<triangle>
                {
                    new triangle(new Vector3[] { new Vector3(0.0f, 0.0f, 0.0f), new Vector3(0.0f, 1.0f, 0.0f), new Vector3(1.0f, 1.0f, 0.0f)  }),
                    new triangle(new Vector3[] { new Vector3(0.0f, 0.0f, 0.0f), new Vector3(1.0f, 1.0f, 0.0f), new Vector3(1.0f, 0.0f, 0.0f)  }),
                    new triangle(new Vector3[] { new Vector3(1.0f, 0.0f, 0.0f), new Vector3(1.0f, 1.0f, 0.0f), new Vector3(1.0f, 1.0f, 1.0f)  }),
                    new triangle(new Vector3[] { new Vector3(1.0f, 0.0f, 0.0f), new Vector3(1.0f, 1.0f, 1.0f), new Vector3(1.0f, 0.0f, 1.0f)  }),
                    new triangle(new Vector3[] { new Vector3(1.0f, 0.0f, 1.0f), new Vector3(1.0f, 1.0f, 1.0f), new Vector3(0.0f, 1.0f, 1.0f)  }),
                    new triangle(new Vector3[] { new Vector3(1.0f, 0.0f, 1.0f), new Vector3(0.0f, 1.0f, 1.0f), new Vector3(0.0f, 0.0f, 1.0f)  }),
                    new triangle(new Vector3[] { new Vector3(0.0f, 0.0f, 1.0f), new Vector3(0.0f, 1.0f, 1.0f), new Vector3(0.0f, 1.0f, 0.0f)  }),
                    new triangle(new Vector3[] { new Vector3(0.0f, 0.0f, 1.0f), new Vector3(0.0f, 1.0f, 0.0f), new Vector3(0.0f, 0.0f, 0.0f)  }),
                    new triangle(new Vector3[] { new Vector3(0.0f, 1.0f, 0.0f), new Vector3(0.0f, 1.0f, 1.0f), new Vector3(1.0f, 1.0f, 1.0f)  }),
                    new triangle(new Vector3[] { new Vector3(0.0f, 1.0f, 0.0f), new Vector3(1.0f, 1.0f, 1.0f), new Vector3(1.0f, 1.0f, 0.0f)  }),
                    new triangle(new Vector3[] { new Vector3(1.0f, 0.0f, 1.0f), new Vector3(0.0f, 0.0f, 1.0f), new Vector3(0.0f, 0.0f, 0.0f)  }),
                    new triangle(new Vector3[] { new Vector3(1.0f, 0.0f, 1.0f), new Vector3(0.0f, 0.0f, 0.0f), new Vector3(1.0f, 0.0f, 0.0f)  })
                };
        }
    }
}
