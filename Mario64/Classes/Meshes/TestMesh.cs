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
using static OpenTK.Graphics.OpenGL.GL;

#pragma warning disable CS8600
#pragma warning disable CA1416
#pragma warning disable CS8604
#pragma warning disable CS8603

namespace Mario64
{
    public class TestMesh : BaseMesh
    {
        public Texture texture;

        private List<float> vertices = new List<float>();
        private string? embeddedModelName;

        public Vector3 Position;
        public Vector3 Rotation;
        public Vector3 Scale;
        private bool IsTransformed
        {
            get
            {
                return !(Position == Vector3.Zero && Rotation == Vector3.Zero && Scale == Vector3.One);
            }
        }

        private Frustum frustum;
        private Camera camera;
        private Vector2 windowSize;

        Matrix4 modelMatrix, viewMatrix, projectionMatrix;

        private VAO Vao;
        private VBO Vbo;

        public TestMesh(VAO vao, VBO vbo, int shaderProgramId, string embeddedTextureName, Vector2 windowSize, ref Frustum frustum, ref Camera camera, ref int textureCount) : base(vao.id, vbo.id, shaderProgramId)
        {
            texture = new Texture(textureCount, embeddedTextureName);
            textureCount++;

            Vao = vao;
            Vbo = vbo;

            this.windowSize = windowSize;
            this.frustum = frustum;
            this.camera = camera;

            Position = Vector3.Zero;
            Rotation = Vector3.Zero;
            Scale = Vector3.One;

            ComputeVertexNormals(ref tris);

            SendUniforms();
        }

        private List<float> ConvertToNDC(triangle tri, int index, ref Matrix4 transformMatrix)
        {
            Vector3 v = Vector3.TransformPosition(tri.p[index], transformMatrix);

            List<float> result = new List<float>()
            {
                v.X, v.Y+1, v.Z, 1.0f,
                tri.n[index].X, tri.n[index].Y, tri.n[index].Z,
                tri.t[index].u, tri.t[index].v
            };

            return result;
        }

        public void UpdateFrustumAndCamera(ref Frustum frustum, ref Camera camera)
        {
            this.frustum = frustum;
            this.camera = camera;
        }

        protected override void SendUniforms()
        {
            int textureLocation = GL.GetUniformLocation(shaderProgramId, "textureSampler");
            int windowSizeLocation = GL.GetUniformLocation(shaderProgramId, "windowSize");
            int modelMatrixLocation = GL.GetUniformLocation(shaderProgramId, "modelMatrix");
            int viewMatrixLocation = GL.GetUniformLocation(shaderProgramId, "viewMatrix");
            int projectionMatrixLocation = GL.GetUniformLocation(shaderProgramId, "projectionMatrix");
            int cameraPositionLocation = GL.GetUniformLocation(shaderProgramId, "cameraPosition");

            modelMatrix = Matrix4.Identity;
            projectionMatrix = camera.GetProjectionMatrix();
            viewMatrix = camera.GetViewMatrix();

            GL.UniformMatrix4(modelMatrixLocation, true, ref modelMatrix);
            GL.UniformMatrix4(viewMatrixLocation, true, ref viewMatrix);
            GL.UniformMatrix4(projectionMatrixLocation, true, ref projectionMatrix);
            GL.Uniform2(windowSizeLocation, windowSize);
            GL.Uniform3(cameraPositionLocation, camera.position);
            GL.Uniform1(textureLocation, texture.unit);
        }

        public List<float> Draw()
        {
            Vao.Bind();

            vertices = new List<float>();

            Matrix4 s = Matrix4.CreateScale(Scale);
            Matrix4 rX = Matrix4.CreateRotationX(MathHelper.DegreesToRadians(Rotation.X));
            Matrix4 rY = Matrix4.CreateRotationY(MathHelper.DegreesToRadians(Rotation.Y));
            Matrix4 rZ = Matrix4.CreateRotationZ(MathHelper.DegreesToRadians(Rotation.Z));
            Matrix4 t = Matrix4.CreateTranslation(Position);

            Matrix4 transformMatrix = Matrix4.Identity;
            if (IsTransformed)
                transformMatrix = s * rX * rY * rZ * t;

            foreach (triangle tri in tris)
            {
                if (frustum.IsTriangleInside(tri) || camera.IsTriangleClose(tri))
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
            texture.Bind();

            return vertices;
        }
    }
}
