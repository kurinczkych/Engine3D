using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public List<Shadow> shadowFaces = new List<Shadow>();
        
        public PointLight()
        {
            shadowFaces.Add(shadowTop);
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
            GL.ActiveTexture(TextureUnit.Texture0 + shadowTop.shadowMap.TextureUnit);
            GL.BindTexture(TextureTarget.Texture2D, shadowTop.shadowMap.TextureId);

            GL.ActiveTexture(TextureUnit.Texture0 + shadowBottom.shadowMap.TextureUnit);
            GL.BindTexture(TextureTarget.Texture2D, shadowBottom.shadowMap.TextureId);

            GL.ActiveTexture(TextureUnit.Texture0 + shadowLeft.shadowMap.TextureUnit);
            GL.BindTexture(TextureTarget.Texture2D, shadowLeft.shadowMap.TextureId);

            GL.ActiveTexture(TextureUnit.Texture0 + shadowRight.shadowMap.TextureUnit);
            GL.BindTexture(TextureTarget.Texture2D, shadowRight.shadowMap.TextureId);

            GL.ActiveTexture(TextureUnit.Texture0 + shadowFront.shadowMap.TextureUnit);
            GL.BindTexture(TextureTarget.Texture2D, shadowFront.shadowMap.TextureId);

            GL.ActiveTexture(TextureUnit.Texture0 + shadowBack.shadowMap.TextureUnit);
            GL.BindTexture(TextureTarget.Texture2D, shadowBack.shadowMap.TextureId);
        }

        public override void InitShadows()
        {
            shadowTop = new Shadow(1024);
            shadowTop.projection = Projection.ShadowMedium;

            shadowBottom = new Shadow(1024);
            shadowBottom.projection = Projection.ShadowMedium;

            shadowLeft = new Shadow(1024);
            shadowLeft.projection = Projection.ShadowMedium;

            shadowRight = new Shadow(1024);
            shadowRight.projection = Projection.ShadowMedium;

            shadowFront = new Shadow(1024);
            shadowFront.projection = Projection.ShadowMedium;

            shadowBack = new Shadow(1024);
            shadowBack.projection = Projection.ShadowMedium;
        }

        public override void SetupShadows()
        {
            shadowTop.shadowMap = Engine.textureManager.GetShadowTexture(shadowTop.size);
            GL.BindTexture(TextureTarget.Texture2D, shadowTop.shadowMap.TextureId);
            shadowTop.fbo = SetupFrameBuffer(shadowTop.shadowMap.TextureId);
            GL.BindTexture(TextureTarget.Texture2D, 0);

            shadowTop.shadowMap = Engine.textureManager.GetShadowTexture(shadowTop.size);
            GL.BindTexture(TextureTarget.Texture2D, shadowTop.shadowMap.TextureId);
            shadowTop.fbo = SetupFrameBuffer(shadowTop.shadowMap.TextureId);
            GL.BindTexture(TextureTarget.Texture2D, 0);
        }

        public override void ResizeShadowMap(ShadowType type, int size)
        {
            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }

        #endregion
    }
}
