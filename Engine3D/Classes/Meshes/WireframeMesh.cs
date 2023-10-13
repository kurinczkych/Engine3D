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

namespace Engine3D
{

    public class WireframeMesh : BaseMesh
    {
        public static int floatCount = 8;

        private List<float> vertices = new List<float>();

        private Frustum frustum;
        private Camera camera;

        Matrix4 modelMatrix, viewMatrix, projectionMatrix;

        private bool IsTransformed
        {
            get
            {
                if(parentObject == null)
                    return !(Position == Vector3.Zero && Rotation == Quaternion.Identity);
                else
                    return !(parentObject.Position == Vector3.Zero && parentObject.Rotation == Quaternion.Identity);
            }
        }

        public List<Line> lines;
        private Color4 color;

        public Vector3 Position;
        public Quaternion Rotation;

        private VAO Vao;
        private VBO Vbo;

        public WireframeMesh(VAO vao, VBO vbo, int shaderProgramId, ref Frustum frustum, ref Camera camera, Color4 color) : base(vao.id, vbo.id, shaderProgramId)
        {
            this.frustum = frustum;
            this.camera = camera;

            Vao = vao;
            Vbo = vbo;

            Position = Vector3.Zero;
            Rotation = Quaternion.Identity;

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
        private List<float> ConvertToNDC(Vector3 point, ref Matrix4 transformMatrix)
        {
            Vector3 v = Vector3.TransformPosition(point, transformMatrix);

            List<float> result = new List<float>()
            {
                v.X, v.Y, v.Z, 1.0f,
                color.R, color.G, color.B, color.A
            };

            return result;
        }

        public List<float> Draw()
        {
            Vao.Bind();

            vertices = new List<float>();

            Matrix4 r = Matrix4.Identity;
            Matrix4 t = Matrix4.Identity;

            if(parentObject == null)
            {
                r = Matrix4.CreateFromQuaternion(Rotation);
                t = Matrix4.CreateTranslation(Position);
            }
            else
            {
                r = Matrix4.CreateFromQuaternion(parentObject.Rotation);
                t = Matrix4.CreateTranslation(parentObject.Position);
            }

            Matrix4 transformMatrix = Matrix4.Identity;
            if (IsTransformed)
            {
                transformMatrix = r * t;
            }

            foreach (Line line in lines)
            {
                    vertices.AddRange(ConvertToNDC(line.Start, ref transformMatrix));
                    vertices.AddRange(ConvertToNDC(line.End, ref transformMatrix));
                if (frustum.IsLineInside(line) || camera.IsLineClose(line))
                {
                }
            }

            SendUniforms();

            return vertices;
        }
    }
}
