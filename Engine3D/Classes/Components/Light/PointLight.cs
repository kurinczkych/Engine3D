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

        public Shadow shadowTop;
        public Shadow shadowBottom;
        public Shadow shadowLeft;
        public Shadow shadowRight;
        public Shadow shadowFront;
        public Shadow shadowBack;
        
        public PointLight()
        {

        }

        public PointLight(Object parentObject, int id, VAO wireVao, VBO wireVbo, int wireShaderId, Vector2 windowSize, ref Camera mainCamera) :
            base(parentObject, id, wireVao, wireVbo, wireShaderId, windowSize, ref mainCamera)
        {
            properties.color = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);
            properties.ambient = new Vector4(2f, 2f, 2f, 1.0f);
            properties.diffuse = new Vector4(0.8f, 0.8f, 0.8f, 1.0f);
            properties.specular = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);
            properties.specularPow = 32f;

            properties.constant = 1.0f;
            properties.linear = 0.09f;
            properties.quadratic = 0.032f;

            properties.lightType = 0;

            range = AttenuationToRange(properties.constant, properties.linear, properties.quadratic);
        }

        #region Light
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
            shadowTop = new Shadow(1024, ShadowType.Top)
            {
                projection = Projection.ShadowFace
            };
            shadowBottom = new Shadow(1024, ShadowType.Bottom)
            {
                projection = Projection.ShadowFace
            };
            shadowLeft = new Shadow(1024, ShadowType.Left)
            {
                projection = Projection.ShadowFace
            };
            shadowRight = new Shadow(1024, ShadowType.Right)
            {
                projection = Projection.ShadowFace
            };
            shadowFront = new Shadow(1024, ShadowType.Front)
            {
                projection = Projection.ShadowFace
            };
            shadowBack = new Shadow(1024, ShadowType.Back)
            {
                projection = Projection.ShadowFace
            };
        }

        public override void SetupShadows()
        {
            shadowTop.shadowMap = Engine.textureManager.GetShadowTexture(shadowTop.size);
            GL.BindTexture(TextureTarget.Texture2D, shadowTop.shadowMap.TextureId);
            shadowTop.fbo = SetupFrameBuffer(shadowTop.shadowMap.TextureId);
            GL.BindTexture(TextureTarget.Texture2D, 0);

            shadowBottom.shadowMap = Engine.textureManager.GetShadowTexture(shadowBottom.size);
            GL.BindTexture(TextureTarget.Texture2D, shadowBottom.shadowMap.TextureId);
            shadowBottom.fbo = SetupFrameBuffer(shadowBottom.shadowMap.TextureId);
            GL.BindTexture(TextureTarget.Texture2D, 0);

            shadowLeft.shadowMap = Engine.textureManager.GetShadowTexture(shadowLeft.size);
            GL.BindTexture(TextureTarget.Texture2D, shadowLeft.shadowMap.TextureId);
            shadowLeft.fbo = SetupFrameBuffer(shadowLeft.shadowMap.TextureId);
            GL.BindTexture(TextureTarget.Texture2D, 0);

            shadowRight.shadowMap = Engine.textureManager.GetShadowTexture(shadowRight.size);
            GL.BindTexture(TextureTarget.Texture2D, shadowRight.shadowMap.TextureId);
            shadowRight.fbo = SetupFrameBuffer(shadowRight.shadowMap.TextureId);
            GL.BindTexture(TextureTarget.Texture2D, 0);

            shadowFront.shadowMap = Engine.textureManager.GetShadowTexture(shadowFront.size);
            GL.BindTexture(TextureTarget.Texture2D, shadowFront.shadowMap.TextureId);
            shadowFront.fbo = SetupFrameBuffer(shadowFront.shadowMap.TextureId);
            GL.BindTexture(TextureTarget.Texture2D, 0);

            shadowBack.shadowMap = Engine.textureManager.GetShadowTexture(shadowBack.size);
            GL.BindTexture(TextureTarget.Texture2D, shadowBack.shadowMap.TextureId);
            shadowBack.fbo = SetupFrameBuffer(shadowBack.shadowMap.TextureId);
            GL.BindTexture(TextureTarget.Texture2D, 0);
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
            if (!castShadows)
                return;

            for (int i = 0; i < 6; i++)
            {
                Matrix4 invCombinedMatrix = Matrix4.Identity;
                switch (i)
                {
                    case 0:
                        shadowTop.projectionMatrix = GetProjectionMatrix(ShadowType.Top + i);
                        properties.lightSpaceTopMatrix = GetLightViewMatrix(ShadowType.Top + i) * shadowTop.projectionMatrix;
                        invCombinedMatrix = Matrix4.Invert(shadowTop.projectionMatrix * GetLightViewMatrixForFrustum(shadowTop.shadowType));
                        break;
                    case 1:
                        shadowBottom.projectionMatrix = GetProjectionMatrix(ShadowType.Top + i);
                        properties.lightSpaceBottomMatrix = GetLightViewMatrix(ShadowType.Top + i) * shadowBottom.projectionMatrix;
                        invCombinedMatrix = Matrix4.Invert(shadowBottom.projectionMatrix * GetLightViewMatrixForFrustum(shadowBottom.shadowType));
                        break;
                    case 2:
                        shadowLeft.projectionMatrix = GetProjectionMatrix(ShadowType.Top + i);
                        properties.lightSpaceLeftMatrix = GetLightViewMatrix(ShadowType.Top + i) * shadowLeft.projectionMatrix;
                        invCombinedMatrix = Matrix4.Invert(shadowLeft.projectionMatrix * GetLightViewMatrixForFrustum(shadowLeft.shadowType));
                        break;
                    case 3:
                        shadowRight.projectionMatrix = GetProjectionMatrix(ShadowType.Top + i);
                        properties.lightSpaceRightMatrix = GetLightViewMatrix(ShadowType.Top + i) * shadowRight.projectionMatrix;
                        invCombinedMatrix = Matrix4.Invert(shadowRight.projectionMatrix * GetLightViewMatrixForFrustum(shadowRight.shadowType));
                        break;
                    case 4:
                        shadowFront.projectionMatrix = GetProjectionMatrix(ShadowType.Top + i);
                        properties.lightSpaceFrontMatrix = GetLightViewMatrix(ShadowType.Top + i) * shadowFront.projectionMatrix;
                        invCombinedMatrix = Matrix4.Invert(shadowFront.projectionMatrix * GetLightViewMatrixForFrustum(shadowFront.shadowType));
                        break;
                    case 5:
                        shadowBack.projectionMatrix = GetProjectionMatrix(ShadowType.Top + i);
                        properties.lightSpaceBackMatrix = GetLightViewMatrix(ShadowType.Top + i) * shadowBack.projectionMatrix;
                        invCombinedMatrix = Matrix4.Invert(shadowBack.projectionMatrix * GetLightViewMatrixForFrustum(shadowBack.shadowType));
                        break;
                }

                Frustum frustum = new Frustum();

                // Define the corners in normalized device coordinates for the near and far planes
                Vector4[] ndcCorners = new Vector4[]
                {
                    new Vector4(-1, 1, -1, 1), // Near top-left
                    new Vector4(1, 1, -1, 1),  // Near top-right
                    new Vector4(-1, -1, -1, 1), // Near bottom-left
                    new Vector4(1, -1, -1, 1),  // Near bottom-right
                    new Vector4(-1, 1, 1, 1),  // Far top-left
                    new Vector4(1, 1, 1, 1),   // Far top-right
                    new Vector4(-1, -1, 1, 1), // Far bottom-left
                    new Vector4(1, -1, 1, 1)   // Far bottom-right
                };

                frustum.ntl = Helper.Transform(invCombinedMatrix, ndcCorners[0]);
                frustum.ntr = Helper.Transform(invCombinedMatrix, ndcCorners[1]);
                frustum.nbl = Helper.Transform(invCombinedMatrix, ndcCorners[2]);
                frustum.nbr = Helper.Transform(invCombinedMatrix, ndcCorners[3]);
                frustum.ftl = Helper.Transform(invCombinedMatrix, ndcCorners[4]);
                frustum.ftr = Helper.Transform(invCombinedMatrix, ndcCorners[5]);
                frustum.fbl = Helper.Transform(invCombinedMatrix, ndcCorners[6]);
                frustum.fbr = Helper.Transform(invCombinedMatrix, ndcCorners[7]);

                if (!gizmos.ContainsKey("frustumGizmo" + i.ToString()))
                {
                    gizmos.Add("frustumGizmo" + i.ToString(), new Gizmo(wireVao, wireVbo, wireShaderId, windowSize, ref camera, ref parentObject));
                    gizmos["frustumGizmo" + i.ToString()].AddFrustumGizmo(frustum, Color4.Red);
                    gizmos["frustumGizmo" + i.ToString()].recalculate = true;
                    gizmos["frustumGizmo" + i.ToString()].RecalculateModelMatrix(new bool[] { true, false, false });
                }
                else
                {
                    gizmos["frustumGizmo" + i.ToString()].model.meshes.Clear();
                    gizmos["frustumGizmo" + i.ToString()].AddFrustumGizmo(frustum, Color4.Red);
                    gizmos["frustumGizmo" + i.ToString()].recalculate = true;
                    gizmos["frustumGizmo" + i.ToString()].RecalculateModelMatrix(new bool[] { true, false, false });
                }
                if (i == 0)
                {
                    if (!gizmos.ContainsKey("positionGizmo"))
                    {
                        gizmos.Add("positionGizmo", new Gizmo(wireVao, wireVbo, wireShaderId, windowSize, ref camera, ref parentObject));
                        gizmos["positionGizmo"].AddSphereGizmo(2, Color4.Green, parentObject.transformation.Position);
                        gizmos["positionGizmo"].recalculate = true;
                        gizmos["positionGizmo"].RecalculateModelMatrix(new bool[] { true, false, false });
                    }
                    else
                    {
                        gizmos["positionGizmo"].model.meshes.Clear();
                        gizmos["positionGizmo"].AddSphereGizmo(2, Color4.Green, parentObject.transformation.Position);
                        gizmos["positionGizmo"].recalculate = true;
                        gizmos["positionGizmo"].RecalculateModelMatrix(new bool[] { true, false, false });
                    }
                }
            }
        }

        public override Matrix4 GetLightViewMatrix(ShadowType type = default)
        {
            Matrix4 lightView = Matrix4.Identity;
            switch (type)
            {
                case ShadowType.Top:
                    lightView = Matrix4.LookAt(parentObject.transformation.Position, parentObject.transformation.Position + Vector3.UnitY, Vector3.UnitZ);
                    break;
                case ShadowType.Bottom:
                    lightView = Matrix4.LookAt(parentObject.transformation.Position, parentObject.transformation.Position - Vector3.UnitY, -Vector3.UnitZ);
                    break;
                case ShadowType.Left:
                    lightView = Matrix4.LookAt(parentObject.transformation.Position, parentObject.transformation.Position - Vector3.UnitX, Vector3.UnitY);
                    break;
                case ShadowType.Right:
                    lightView = Matrix4.LookAt(parentObject.transformation.Position, parentObject.transformation.Position + Vector3.UnitX, Vector3.UnitY);
                    break;
                case ShadowType.Front:
                    lightView = Matrix4.LookAt(parentObject.transformation.Position, parentObject.transformation.Position + Vector3.UnitZ, Vector3.UnitY);
                    break;
                case ShadowType.Back:
                    lightView = Matrix4.LookAt(parentObject.transformation.Position, parentObject.transformation.Position - Vector3.UnitZ, Vector3.UnitY);
                    break;
            }
            return lightView;
        }

        public override Matrix4 GetLightViewMatrixForFrustum(ShadowType type = default)
        {
            Matrix4 lightView = Matrix4.Identity;
            switch (type)
            {
                case ShadowType.Top:
                    lightView = Matrix4.LookAt(parentObject.transformation.Position, parentObject.transformation.Position + Vector3.UnitY, Vector3.UnitZ);
                    break;
                case ShadowType.Bottom:
                    lightView = Matrix4.LookAt(parentObject.transformation.Position, parentObject.transformation.Position - Vector3.UnitY, -Vector3.UnitZ);
                    break;
                case ShadowType.Left:
                    lightView = Matrix4.LookAt(parentObject.transformation.Position, parentObject.transformation.Position - Vector3.UnitX, Vector3.UnitY);
                    break;
                case ShadowType.Right:
                    lightView = Matrix4.LookAt(parentObject.transformation.Position, parentObject.transformation.Position + Vector3.UnitX, Vector3.UnitY);
                    break;
                case ShadowType.Front:
                    lightView = Matrix4.LookAt(parentObject.transformation.Position, parentObject.transformation.Position + Vector3.UnitZ, Vector3.UnitY);
                    break;
                case ShadowType.Back:
                    lightView = Matrix4.LookAt(parentObject.transformation.Position, parentObject.transformation.Position - Vector3.UnitZ, Vector3.UnitY);
                    break;
            }
            return lightView;
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
