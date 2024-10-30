using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using PuppeteerSharp;
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
        public int shadowMap = -1;

        public static Vector3 minLightSpace;
        public static Vector3 maxLightSpace;

        public ShadowMapFBO(Vector2 size)
        {
            this.size = size;

            fbo = GL.GenFramebuffer();

            shadowMap = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, shadowMap);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.DepthComponent24, (int)size.X, (int)size.Y, 0, PixelFormat.DepthComponent, PixelType.UnsignedByte, IntPtr.Zero);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (float)TextureWrapMode.ClampToBorder);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (float)TextureWrapMode.ClampToBorder);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureBorderColor, new float[] { 1.0f, 1.0f, 1.0f, 1.0f });

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, fbo);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, TextureTarget.Texture2D, shadowMap, 0);

            GL.DrawBuffer(DrawBufferMode.None);
            GL.ReadBuffer(ReadBufferMode.None);

            TextureManager.textureCount++;

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

        public void BindForReading()
        {
            if (Engine.GLState.currentTextureUnit != 0)
            {
                GL.ActiveTexture(TextureUnit.Texture0);
                Engine.GLState.currentTextureUnit = 0;
            }

            if (Engine.GLState.currentTextureId != shadowMap)
            {
                GL.BindTexture(TextureTarget.Texture2D, shadowMap);
                Engine.GLState.currentTextureId = shadowMap;
            }
        }

        public static Matrix4 GetLightViewMatrix(Light light)
        {
            Vector3 lightPosition = light.target - (light.GetDirection() * light.distanceFromScene);

            return Matrix4.LookAt(lightPosition, light.target, Vector3.UnitY);
        }

        public static Vector3 CalculateDirectionFromEuler(float yaw, float pitch, float roll)
        {
            // Convert angles from degrees to radians
            float yawRad = MathHelper.DegreesToRadians(yaw);
            float pitchRad = MathHelper.DegreesToRadians(pitch);
            float rollRad = MathHelper.DegreesToRadians(roll);

            // Calculate direction vector using yaw and pitch
            float x = (float)(Math.Cos(yawRad) * Math.Cos(pitchRad));
            float y = (float)Math.Sin(pitchRad);  // Pitch affects the Y component
            float z = (float)(Math.Sin(yawRad) * Math.Cos(pitchRad));

            // Roll would apply rotation around the Z-axis (not needed for this case, roll = 0)

            return new Vector3(x, y, z).Normalized();
        }
    }
}
