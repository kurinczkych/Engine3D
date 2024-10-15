using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine3D
{
    public class ShadowMapFBO
    {
        public Vector2 size;
        public int fbo;
        public int shadowMap;

        public ShadowMapFBO(Vector2 size)
        {
            this.size = size;

            fbo = GL.GenFramebuffer();

            shadowMap = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, shadowMap);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.DepthComponent, (int)size.X, (int)size.Y, 0, PixelFormat.DepthComponent, PixelType.Float, IntPtr.Zero);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (float)TextureWrapMode.ClampToBorder);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (float)TextureWrapMode.ClampToBorder);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureBorderColor, new float[] { 1.0f, 1.0f, 1.0f, 1.0f });

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, fbo);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, TextureTarget.Texture2D, shadowMap, 0);

            GL.DrawBuffer(DrawBufferMode.None);
            GL.ReadBuffer(ReadBufferMode.None);

            var status = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
            if (status != FramebufferErrorCode.FramebufferComplete)
            {
                throw new Exception($"Framebuffer is incomplete: {status}");
            }

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        }

        public void BindForWriting()
        {
            GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, fbo);
            GL.Viewport(0, 0, (int)size.X, (int)size.Y);
        }

        public void BindForReading(TextureUnit unit)
        {
            GL.ActiveTexture(unit);
            GL.BindTexture(TextureTarget.Texture2D, shadowMap);
        }

        private Matrix4 CreateCameraTransform(Vector3 target, Vector3 up)
        {
            Vector3 n = target;
            n.Normalize();

            Vector3 upNorm = up;
            upNorm.Normalize();

            Vector3 u = Vector3.Cross(upNorm, n);
            u.Normalize();

            Vector3 v = Vector3.Cross(n, u);

            Matrix4 m = new Matrix4();
            m[0,0] = u.X;  m[0,1] = u.Y;  m[0,2] = u.Z;  m[0,3] = 0.0f;
            m[1,0] = v.X;  m[1,1] = v.Y;  m[1,2] = v.Z;  m[1,3] = 0.0f;
            m[2,0] = n.X;  m[2,1] = n.Y;  m[2,2] = n.Z;  m[2,3] = 0.0f;
            m[3,0] = 0.0f; m[3,1] = 0.0f; m[3,2] = 0.0f; m[3,3] = 1.0f;

            return m;
        }

        public Matrix4 CreateCameraTransform(Vector3 pos, Vector3 target, Vector3 up)
        {
            Matrix4 cameraTrans = CreateTranslationTransform(-pos.X, -pos.Y, -pos.Z);
            Matrix4 rotateTrans = CreateCameraTransform(target, up);

            return rotateTrans * cameraTrans;
        }

        private Matrix4 CreateTranslationTransform(float x, float y, float z)
        {
            Matrix4 m = new Matrix4();
            m[0,0] = 1.0f; m[0,1] = 0.0f; m[0,2] = 0.0f; m[0,3] = x;
            m[1,0] = 0.0f; m[1,1] = 1.0f; m[1,2] = 0.0f; m[1,3] = y;
            m[2,0] = 0.0f; m[2,1] = 0.0f; m[2,2] = 1.0f; m[2,3] = z;
            m[3,0] = 0.0f; m[3,1] = 0.0f; m[3,2] = 0.0f; m[3,3] = 1.0f;
            return m;
        }

        public Matrix4 GetLightViewMatrix(Vector3 lightDir, Vector3 sceneCenter, float distanceFromScene)
        {
            // Ensure the lightDir is normalized
            lightDir = lightDir.Normalized();

            // Position the light far away in the direction opposite to the light direction
            Vector3 lightPos = sceneCenter - lightDir * distanceFromScene;

            // Choose an up vector (usually the global Y axis, but adjust if necessary)
            Vector3 up = Vector3.UnitY;

            // If the light direction is parallel to the up vector, change the up vector
            if (Vector3.Cross(lightDir, up) == Vector3.Zero)
            {
                up = Vector3.UnitZ;
            }

            // Create the view matrix by making the light "look at" the scene center
            Matrix4 lightViewMatrix = Matrix4.LookAt(lightPos, sceneCenter, up);

            return lightViewMatrix;
        }
    }
}
