using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#pragma warning disable CS8602

namespace Engine3D
{
    public class PointLight : Light
    {
        public float range;

        public int constantLoc;
        public float constant;

        public int linearLoc;
        public float linear;

        public int quadraticLoc;
        public float quadratic;

        public Shadow shadowTop;
        public Shadow shadowBottom;
        public Shadow shadowLeft;
        public Shadow shadowRight;
        public Shadow shadowFront;
        public Shadow shadowBack;

        public List<Shadow?> shadowFaces = new List<Shadow?>();
        
        public PointLight()
        {
            shadowFaces = new List<Shadow?>()
            {
                shadowTop,
                shadowBottom,
                shadowLeft,
                shadowRight,
                shadowFront,
                shadowBack
            };
        }

        public PointLight(Object parentObject, int id, VAO wireVao, VBO wireVbo, int wireShaderId, Vector2 windowSize, ref Camera mainCamera) :
            base(parentObject, id, wireVao, wireVbo, wireShaderId, windowSize, ref mainCamera)
        {
            color = Color4.White;
            ambient = new Vector3(2f, 2f, 2f);
            diffuse = new Vector3(0.8f, 0.8f, 0.8f);
            specular = new Vector3(1.0f, 1.0f, 1.0f);
            specularPow = 32f;

            constant = 1.0f;
            linear = 0.09f;
            quadratic = 0.032f;

            range = AttenuationToRange(constant, linear, quadratic);

            shadowFaces = new List<Shadow?>()
            {
                shadowTop,
                shadowBottom,
                shadowLeft,
                shadowRight,
                shadowFront,
                shadowBack
            };
        }

        #region Light
        protected override void GetUniformLocations(int spi)
        {
            if (shaderProgramId == spi)
                return;
            shaderProgramId = spi;

            uniforms = new Dictionary<string, int>
            {
                { "lightTypeLoc", GL.GetUniformLocation(shaderProgramId, "lights[" + id + "].lightType") },
                { "positionLoc", GL.GetUniformLocation(shaderProgramId, "lights[" + id + "].position") },
                { "colorLoc", GL.GetUniformLocation(shaderProgramId, "lights[" + id + "].color") },

                { "ambientLoc", GL.GetUniformLocation(shaderProgramId, "lights[" + id + "].ambient") },
                { "diffuseLoc", GL.GetUniformLocation(shaderProgramId, "lights[" + id + "].diffuse") },
                { "specularLoc", GL.GetUniformLocation(shaderProgramId, "lights[" + id + "].specular") },

                { "specularPowLoc", GL.GetUniformLocation(shaderProgramId, "lights[" + id + "].specularPow") },
                { "constantLoc", GL.GetUniformLocation(shaderProgramId, "lights[" + id + "].constant") },
                { "linearLoc", GL.GetUniformLocation(shaderProgramId, "lights[" + id + "].linear") },
                { "quadraticLoc", GL.GetUniformLocation(shaderProgramId, "lights[" + id + "].quadratic") }
            };
        }

        public static float AttenuationToRange(float constant, float linear, float quadratic)
        {
            if (linear == 0.7f && quadratic == 1.8f)
            {
                return 7.0f;
            }
            else if (linear == 0.09f && quadratic == 0.032f)
            {
                return 50.0f;
            }
            else if (linear == 0.022f && quadratic == 0.0019f)
            {
                return 100.0f;
            }

            return (4.5f / linear + (float)Math.Sqrt(75.0f / quadratic)) / 2.0f;
        }

        public static float[] RangeToAttenuation(float range)
        {
            // Default constant
            float constantStat = 1.0f;
            float linearStat = 1.0f;
            float quadraticStat = 1.0f;

            // Set linear and quadratic based on the range
            if (range <= 7.0f)
            {
                linearStat = 0.7f;
                quadraticStat = 1.8f;
            }
            else if (range <= 50.0f)
            {
                linearStat = 0.09f;
                quadraticStat = 0.032f;
            }
            else
            {
                linearStat = 0.022f;
                quadraticStat = 0.0019f;
            }

            return new float[] { constantStat, linearStat, quadraticStat };
        }
        #endregion

        #region Shadow

        public override void BindForWriting(ShadowType type)
        {
            switch(type)
            {
                case ShadowType.Top:
                    GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, shadowTop.fbo);
                    GL.Viewport(0, 0, shadowTop.size, shadowTop.size);
                    break;
                case ShadowType.Bottom:
                    GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, shadowBottom.fbo);
                    GL.Viewport(0, 0, shadowBottom.size, shadowBottom.size);
                    break;
                case ShadowType.Left:
                    GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, shadowLeft.fbo);
                    GL.Viewport(0, 0, shadowLeft.size, shadowLeft.size);
                    break;
                case ShadowType.Right:
                    GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, shadowRight.fbo);
                    GL.Viewport(0, 0, shadowRight.size, shadowRight.size);
                    break;
                case ShadowType.Front:
                    GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, shadowFront.fbo);
                    GL.Viewport(0, 0, shadowFront.size, shadowFront.size);
                    break;
                case ShadowType.Back:
                    GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, shadowBack.fbo);
                    GL.Viewport(0, 0, shadowBack.size, shadowBack.size);
                    break;
            }
        }

        public override void BindForReading(ShadowType type)
        {
            foreach (Shadow? shadow in shadowFaces)
            {
                if (shadow == null)
                    continue;

                GL.ActiveTexture(TextureUnit.Texture0 + shadow.shadowMap.TextureUnit);
                GL.BindTexture(TextureTarget.Texture2D, shadow.shadowMap.TextureId);
            }
        }

        public override void InitShadows()
        {
            for (int i = 0; i < shadowFaces.Count; i++)
            {
                if (shadowFaces[i] == null)
                    continue;

                shadowFaces[i] = new Shadow(1024)
                {
                    projection = Projection.ShadowMedium
                };
            }
        }

        public override void SetupShadows()
        {
            for (int i = 0; i < shadowFaces.Count; i++)
            {
                if (shadowFaces[i] == null)
                    continue;

                shadowFaces[i].shadowMap = Engine.textureManager.GetShadowTexture(shadowFaces[i].size);
                GL.BindTexture(TextureTarget.Texture2D, shadowFaces[i].shadowMap.TextureId);
                shadowFaces[i].fbo = SetupFrameBuffer(shadowFaces[i].shadowMap.TextureId);
                GL.BindTexture(TextureTarget.Texture2D, 0);
            }
        }

        public override void ResizeShadowMap(ShadowType type, int size)
        {
            switch (type)
            {
                case ShadowType.Top:
                    shadowTop.size = size;
                    Engine.textureManager.ResizeShadowTexture(shadowTop.shadowMap, size);
                    break;
                case ShadowType.Bottom:
                    shadowBottom.size = size;
                    Engine.textureManager.ResizeShadowTexture(shadowBottom.shadowMap, size);
                    break;
                case ShadowType.Left:
                    shadowLeft.size = size;
                    Engine.textureManager.ResizeShadowTexture(shadowLeft.shadowMap, size);
                    break;
                case ShadowType.Right:
                    shadowRight.size = size;
                    Engine.textureManager.ResizeShadowTexture(shadowRight.shadowMap, size);
                    break;
                case ShadowType.Front:
                    shadowFront.size = size;
                    Engine.textureManager.ResizeShadowTexture(shadowFront.shadowMap, size);
                    break;
                case ShadowType.Back:
                    shadowBack.size = size;
                    Engine.textureManager.ResizeShadowTexture(shadowBack.shadowMap, size);
                    break;
            }
        }

        public override void RecalculateShadows()
        {
            throw new NotImplementedException();
        }

        public override Matrix4 GetLightViewMatrix()
        {
            throw new NotImplementedException();
        }

        public override Matrix4 GetLightViewMatrixForFrustum()
        {
            throw new NotImplementedException();
        }

        public override Matrix4 GetProjectionMatrix(ShadowType type)
        {
            Matrix4 projection = Matrix4.Identity;
            switch (type)
            {
                case ShadowType.Top:
                    projection = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(90), 1.0f, shadowTop.projection.near, shadowTop.projection.far);
                    break;
                case ShadowType.Bottom:
                    projection = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(90), 1.0f, shadowBottom.projection.near, shadowBottom.projection.far);
                    break;
                case ShadowType.Left:
                    projection = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(90), 1.0f, shadowLeft.projection.near, shadowLeft.projection.far);
                    break;
                case ShadowType.Right:
                    projection = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(90), 1.0f, shadowRight.projection.near, shadowRight.projection.far);
                    break;
                case ShadowType.Front:
                    projection = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(90), 1.0f, shadowFront.projection.near, shadowFront.projection.far);
                    break;
                case ShadowType.Back:
                    projection = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(90), 1.0f, shadowBack.projection.near, shadowBack.projection.far);
                    break;
            }
            return projection;
        }

        #endregion
    }
}
