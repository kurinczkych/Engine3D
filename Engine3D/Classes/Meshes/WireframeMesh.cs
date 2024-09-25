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
using static System.Formats.Asn1.AsnWriter;

#pragma warning disable CS8600
#pragma warning disable CA1416
#pragma warning disable CS8604
#pragma warning disable CS8603
#pragma warning disable CS0728

namespace Engine3D
{

    public class WireframeMesh : BaseMesh
    {
        public static int floatCount = 7;

        private List<float> vertices = new List<float>();

        Matrix4 viewMatrix, projectionMatrix;

        public List<Line> lines;

        public Vector3 Position;
        public Quaternion Rotation;

        private VAO Vao;
        private VBO Vbo;

        public WireframeMesh(VAO vao, VBO vbo, int shaderProgramId, ref Camera camera) : base(vao.id, vbo.id, shaderProgramId)
        {
            throw new NotImplementedException();
            this.camera = camera;

            Vao = vao;
            Vbo = vbo;

            Position = Vector3.Zero;
            Rotation = Quaternion.Identity;

            lines = new List<Line>();

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
            projectionMatrix = camera.projectionMatrix;
            viewMatrix = camera.viewMatrix;

            GL.UniformMatrix4(uniformLocations["modelMatrix"], true, ref modelMatrix);
            GL.UniformMatrix4(uniformLocations["viewMatrix"], true, ref viewMatrix);
            GL.UniformMatrix4(uniformLocations["projectionMatrix"], true, ref projectionMatrix);
        }
        private void ConvertToNDC(ref List<float> vertices, Vector3 point, Color4 color)
        {
            vertices.AddRange(new float[]
            {
                point.X, point.Y, point.Z,
                color.R, color.G, color.B, color.A
            });
        }

        private void AddVertices(List<float> vertices, Line line)
        {
            lock (vertices) // Lock to ensure thread-safety when modifying the list
            {
                ConvertToNDC(ref vertices, line.Start, line.StartColor);
                ConvertToNDC(ref vertices, line.End, line.EndColor);
            }
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

            ParallelOptions parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = threadSize };
            Parallel.ForEach(lines, parallelOptions,
                () => new List<float>(),
                 (line, loopState, localVertices) =>
                 {
                     AddVertices(localVertices, line);

                     return localVertices;
                 },
                 localVertices =>
                 {
                     lock (vertices)
                     {
                         vertices.AddRange(localVertices);
                     }
                 });

            SendUniforms();

            return vertices;
        }
    }
}
