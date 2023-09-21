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

namespace Mario64
{
    public class Line
    {
        public Line(Vector3 start, Vector3 end)
        {
            Start = start;
            End = end;
        }

        public Vector3 Start;
        public Vector3 End;
        public Color4 Color;
    }

    public class WireframeMesh : BaseMesh
    {
        public static int floatCount = 8;

        private List<float> vertices = new List<float>();

        private Frustum frustum;
        private Camera camera;

        Matrix4 modelMatrix, viewMatrix, projectionMatrix;


        public List<Line> lines;
        private Color4 color;

        private VAO Vao;
        private VBO Vbo;

        public WireframeMesh(VAO vao, VBO vbo, int shaderProgramId, ref Frustum frustum, ref Camera camera, Color4 color) : base(vao.id, vbo.id, shaderProgramId)
        {
            this.frustum = frustum;
            this.camera = camera;

            Vao = vao;
            Vbo = vbo;

            lines = new List<Line>();
            this.color = color;
        }


        public void UpdateFrustumAndCamera(ref Frustum frustum, ref Camera camera)
        {
            this.frustum = frustum;
            this.camera = camera;
        }

        protected override void SendUniforms()
        {
            int modelMatrixLocation = GL.GetUniformLocation(shaderProgramId, "modelMatrix");
            int viewMatrixLocation = GL.GetUniformLocation(shaderProgramId, "viewMatrix");
            int projectionMatrixLocation = GL.GetUniformLocation(shaderProgramId, "projectionMatrix");

            modelMatrix = Matrix4.Identity;
            projectionMatrix = camera.GetProjectionMatrix();
            viewMatrix = camera.GetViewMatrix();

            GL.UniformMatrix4(modelMatrixLocation, true, ref modelMatrix);
            GL.UniformMatrix4(viewMatrixLocation, true, ref viewMatrix);
            GL.UniformMatrix4(projectionMatrixLocation, true, ref projectionMatrix);
        }
        private List<float> ConvertToNDC(Vector3 point)
        {
            List<float> result = new List<float>()
            {
                point.X, point.Y, point.Z, 1.0f,
                color.R, color.G, color.B, color.A
            };

            return result;
        }

        public List<float> Draw()
        {
            Vao.Bind();

            vertices = new List<float>();

            foreach (Line line in lines)
            {
                if (frustum.IsLineInside(line) || camera.IsLineClose(line))
                {
                    vertices.AddRange(ConvertToNDC(line.Start));
                    vertices.AddRange(ConvertToNDC(line.End));
                }
            }

            SendUniforms();

            return vertices;
        }
    }
}
