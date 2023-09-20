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
    public struct VertexLine
    {
        public Vector4 Position;
        public Color4 Color;
    }

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
        private List<VertexLine> vertices = new List<VertexLine>();

        private Frustum frustum;
        private Camera camera;

        Matrix4 modelMatrix, viewMatrix, projectionMatrix;

        private WireVAO wireVao;
        private WireVBO wireVbo;

        public List<Line> lines;
        private Color4 color;

        public WireframeMesh(WireVAO vao, WireVBO vbo, int shaderProgramId, ref Frustum frustum, ref Camera camera, Color4 color) : base(vao.id, vbo.id, shaderProgramId)
        {
            this.frustum = frustum;
            this.camera = camera;

            wireVao = vao;
            wireVbo = vbo;

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
        private VertexLine ConvertToNDC(Vector3 point)
        {
            return new VertexLine()
            {
                Position = new Vector4(point, 1.0f),
                Color = color
            };
        }

        public List<VertexLine> Draw()
        {
            wireVao.Bind();

            vertices = new List<VertexLine>();

            foreach (Line line in lines)
            {
                if (frustum.IsLineInside(line) || camera.IsLineClose(line))
                {
                    vertices.Add(ConvertToNDC(line.Start));
                    vertices.Add(ConvertToNDC(line.End));
                }
            }

            SendUniforms();

            return vertices;
        }
    }
}
