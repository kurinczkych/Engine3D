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
#pragma warning disable IDE0044

namespace Engine3D
{
    public class TestMesh : BaseMesh
    {
        public Texture texture;

        private List<float> vertices = new List<float>();
        private string? modelName;

        public Vector3 Scale;
        private bool IsTransformed
        {
            get
            {
                return !(parentObject.Position == Vector3.Zero && parentObject.Rotation == Quaternion.Identity && Scale == Vector3.One);
            }
        }

        private Frustum frustum;
        private Camera camera;
        private Vector2 windowSize;

        Matrix4 modelMatrix, viewMatrix, projectionMatrix;

        private VAO Vao;
        private VBO Vbo;

        public TestMesh(VAO vao, VBO vbo, int shaderProgramId, string textureName, Vector2 windowSize, ref Frustum frustum, ref Camera camera, ref int textureCount) : base(vao.id, vbo.id, shaderProgramId)
        {
            texture = new Texture(textureCount, textureName);
            textureCount += texture.textureDescriptor.count;

            Vao = vao;
            Vbo = vbo;

            this.windowSize = windowSize;
            this.frustum = frustum;
            this.camera = camera;

            Scale = Vector3.One;

            ComputeVertexNormals(ref tris);

            GetUniformLocations();
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

        private void GetUniformLocations()
        {
            uniformLocations.Add("textureSampler", GL.GetUniformLocation(shaderProgramId, "textureSampler"));
            uniformLocations.Add("windowSize", GL.GetUniformLocation(shaderProgramId, "windowSize"));
            uniformLocations.Add("modelMatrix", GL.GetUniformLocation(shaderProgramId, "modelMatrix"));
            uniformLocations.Add("viewMatrix", GL.GetUniformLocation(shaderProgramId, "viewMatrix"));
            uniformLocations.Add("projectionMatrix", GL.GetUniformLocation(shaderProgramId, "projectionMatrix"));
            uniformLocations.Add("cameraPosition", GL.GetUniformLocation(shaderProgramId, "cameraPosition"));
            if (texture.textureDescriptor.Normal != "")
            {
                uniformLocations.Add("textureSamplerNormal", GL.GetUniformLocation(shaderProgramId, "textureSamplerNormal"));
                uniformLocations.Add("useNormal", GL.GetUniformLocation(shaderProgramId, "useNormal"));
            }
            if (texture.textureDescriptor.Height != "")
            {
                uniformLocations.Add("textureSamplerHeight", GL.GetUniformLocation(shaderProgramId, "textureSamplerHeight"));
                uniformLocations.Add("useHeight", GL.GetUniformLocation(shaderProgramId, "useHeight"));
            }
            if (texture.textureDescriptor.AO != "")
            {
                uniformLocations.Add("textureSamplerAO", GL.GetUniformLocation(shaderProgramId, "textureSamplerAO"));
                uniformLocations.Add("useAO", GL.GetUniformLocation(shaderProgramId, "useAO"));
            }
            if (texture.textureDescriptor.Rough != "")
            {
                uniformLocations.Add("textureSamplerRough", GL.GetUniformLocation(shaderProgramId, "textureSamplerRough"));
                uniformLocations.Add("useRough", GL.GetUniformLocation(shaderProgramId, "useRough"));
            }
            if (texture.textureDescriptor.Metal != "")
            {
                uniformLocations.Add("textureSamplerMetal", GL.GetUniformLocation(shaderProgramId, "textureSamplerMetal"));
                uniformLocations.Add("useMetal", GL.GetUniformLocation(shaderProgramId, "useMetal"));
            }
        }

        protected override void SendUniforms()
        {
            modelMatrix = Matrix4.Identity;
            projectionMatrix = camera.projectionMatrix;
            viewMatrix = camera.viewMatrix;

            GL.UniformMatrix4(uniformLocations["modelMatrix"], true, ref modelMatrix);
            GL.UniformMatrix4(uniformLocations["viewMatrix"], true, ref viewMatrix);
            GL.UniformMatrix4(uniformLocations["projectionMatrix"], true, ref projectionMatrix);
            GL.Uniform2(uniformLocations["windowSize"], windowSize);
            GL.Uniform3(uniformLocations["cameraPosition"], camera.GetPosition());
            GL.Uniform1(uniformLocations["textureSampler"], texture.textureDescriptor.TextureUnit);
            if (texture.textureDescriptor.NormalUse == 1)
            {
                GL.Uniform1(uniformLocations["textureSamplerNormal"], texture.textureDescriptor.NormalUnit);
                GL.Uniform1(uniformLocations["useNormal"], texture.textureDescriptor.NormalUse);
            }
            if (texture.textureDescriptor.HeightUse == 1)
            {
                GL.Uniform1(uniformLocations["textureSamplerHeight"], texture.textureDescriptor.HeightUnit);
                GL.Uniform1(uniformLocations["useHeight"], texture.textureDescriptor.HeightUse);
            }
            if (texture.textureDescriptor.AOUse == 1)
            {
                GL.Uniform1(uniformLocations["textureSamplerAO"], texture.textureDescriptor.AOUnit);
                GL.Uniform1(uniformLocations["useAO"], texture.textureDescriptor.AOUse);
            }
            if (texture.textureDescriptor.RoughUse == 1)
            {
                GL.Uniform1(uniformLocations["textureSamplerRough"], texture.textureDescriptor.RoughUnit);
                GL.Uniform1(uniformLocations["useRough"], texture.textureDescriptor.RoughUse);
            }
            if (texture.textureDescriptor.MetalUse == 1)
            {
                GL.Uniform1(uniformLocations["textureSamplerMetal"], texture.textureDescriptor.MetalUnit);
                GL.Uniform1(uniformLocations["useMetal"], texture.textureDescriptor.MetalUse);
            }
        }

        public List<float> Draw()
        {
            Vao.Bind();

            vertices = new List<float>();

            Matrix4 s = Matrix4.CreateScale(Scale);
            Matrix4 r = Matrix4.CreateFromQuaternion(parentObject.Rotation);
            Matrix4 t = Matrix4.CreateTranslation(parentObject.Position);
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

            texture.Bind(TextureType.Texture);
            if (texture.textureDescriptor.Normal != "")
                texture.Bind(TextureType.Normal);
            if (texture.textureDescriptor.Height != "")
                texture.Bind(TextureType.Height);
            if (texture.textureDescriptor.AO != "")
                texture.Bind(TextureType.AO);

            return vertices;
        }
    }
}
