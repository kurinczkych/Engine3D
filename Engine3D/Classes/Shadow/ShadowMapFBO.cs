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
        public int shadowMap = -1;

        public static int distanceFromScene = 50;
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
                GL.ActiveTexture(TextureUnit.Texture0 );
                Engine.GLState.currentTextureUnit = 0;
            }

            if (Engine.GLState.currentTextureId != shadowMap)
            {
                GL.BindTexture(TextureTarget.Texture2D, shadowMap);
                Engine.GLState.currentTextureId = shadowMap;
            }
        }

        public static Matrix4 GetLightViewMatrix(Vector3 lightDir, Vector3 sceneCenter, float distanceFromScene)
        {
            //lightDir = lightDir.Normalized();

            //Vector3 lightPos = sceneCenter - lightDir * distanceFromScene;

            //Vector3 up = Vector3.UnitY;

            //if (Vector3.Cross(lightDir, up) == Vector3.Zero)
            //{
            //    up = Vector3.UnitZ;
            //}

            //Matrix4 lightViewMatrix = Matrix4.LookAt(lightPos, sceneCenter, up);

            //return lightViewMatrix;
            Vector3 lightPosition = sceneCenter - lightDir * distanceFromScene;

            // LookAt matrix for the light
            return Matrix4.LookAt(lightPosition, sceneCenter, Vector3.UnitY); // Assumi
        }

        public static Matrix4 GetLightViewMatrix(Vector3 lightDir)
        {
            return GetLightViewMatrix(lightDir, new Vector3(0, 0, 0), distanceFromScene);
        }

        public static Matrix4 GetLightViewMatrix(Vector3 lightDir, Vector3 pos)
        {
            return GetLightViewMatrix(lightDir, pos, distanceFromScene);
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

        public static Frustum CreateFrustumFromLightViewAndOrtho(Vector3 lightDir, Vector3 sceneCenter, Vector3 sceneExtents)
        {
            Vector3 dir = CalculateDirectionFromEuler(lightDir.X, lightDir.Y, lightDir.Z);

            // Step 1: Get light view matrix
            Matrix4 lightViewMatrix = GetLightViewMatrix(dir, sceneCenter, distanceFromScene);

            // Scene bounding box (AABB)
            Vector3 minBounds = sceneCenter - sceneExtents;
            Vector3 maxBounds = sceneCenter + sceneExtents;

            Vector3[] corners = new Vector3[8]
            {
                new Vector3(minBounds.X, minBounds.Y, minBounds.Z),
                new Vector3(minBounds.X, minBounds.Y, maxBounds.Z),
                new Vector3(minBounds.X, maxBounds.Y, minBounds.Z),
                new Vector3(minBounds.X, maxBounds.Y, maxBounds.Z),
                new Vector3(maxBounds.X, minBounds.Y, minBounds.Z),
                new Vector3(maxBounds.X, minBounds.Y, maxBounds.Z),
                new Vector3(maxBounds.X, maxBounds.Y, minBounds.Z),
                new Vector3(maxBounds.X, maxBounds.Y, maxBounds.Z)
            };

            // Transform scene corners into light space
            minLightSpace = new Vector3(float.MaxValue);
            maxLightSpace = new Vector3(float.MinValue);

            for (int i = 0; i < 8; i++)
            {
                // Manually multiply the Matrix4 with the Vector4 (this applies the transformation)
                Vector4 transformedCorner = lightViewMatrix * new Vector4(corners[i], 1.0f);

                // Update min/max in light space
                minLightSpace = Vector3.ComponentMin(minLightSpace, transformedCorner.Xyz);
                maxLightSpace = Vector3.ComponentMax(maxLightSpace, transformedCorner.Xyz);
            }

            // Step 2: Create the orthographic projection from light-space bounds
            Matrix4 orthoProjectionMatrix = Camera.GetProjectionMatrixOrthoShadow(minLightSpace, maxLightSpace);

            // Step 3: Combine the view and orthographic projection matrices
            Matrix4 lightSpaceMatrix = orthoProjectionMatrix * lightViewMatrix;

            // Step 4: Create the frustum using the bounds
            Frustum frustum = new Frustum();

            // Use the light-space bounds to calculate the frustum corners
            float left = minLightSpace.X;
            float right = maxLightSpace.X;
            float bottom = minLightSpace.Y;
            float top = maxLightSpace.Y;
            float near = -maxLightSpace.Z;
            float far = -minLightSpace.Z;

            // Near plane (in light space)
            frustum.ntl = new Vector4(left, top, near, 1.0f);
            frustum.ntr = new Vector4(right, top, near, 1.0f);
            frustum.nbl = new Vector4(left, bottom, near, 1.0f);
            frustum.nbr = new Vector4(right, bottom, near, 1.0f);

            // Far plane (in light space)
            frustum.ftl = new Vector4(left, top, far, 1.0f);
            frustum.ftr = new Vector4(right, top, far, 1.0f);
            frustum.fbl = new Vector4(left, bottom, far, 1.0f);
            frustum.fbr = new Vector4(right, bottom, far, 1.0f);

            return frustum;
        }
    }
}
