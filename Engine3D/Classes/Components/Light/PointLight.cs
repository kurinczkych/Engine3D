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
            GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, Engine.globalShadowFrameBuffer);

            int face = (int)ShadowType.Right + (int)type - 6;

            // Bind the entire cube map layer for the specified light
            GL.FramebufferTextureLayer(FramebufferTarget.DrawFramebuffer,
                                       FramebufferAttachment.DepthAttachment,
                                       Engine.shadowMapArray.cubeShadowMapArrayId,
                                       0, properties.shadowIndex * 6 + face);

            // Set the viewport based on shadow map resolution (assuming all faces use the same resolution)
            GL.Viewport(0, 0, 4096, 4096);
        }

        public override void InitShadows()
        {
            shadowTop = new Shadow(4096, ShadowType.Top)
            {
                projection = Projection.ShadowFace
            };
            shadowBottom = new Shadow(4096, ShadowType.Bottom)
            {
                projection = Projection.ShadowFace
            };
            shadowLeft = new Shadow(4096, ShadowType.Left)
            {
                projection = Projection.ShadowFace  
            };
            shadowRight = new Shadow(4096, ShadowType.Right)
            {
                projection = Projection.ShadowFace
            };
            shadowFront = new Shadow(4096, ShadowType.Front)
            {
                projection = Projection.ShadowFace
            };
            shadowBack = new Shadow(4096, ShadowType.Back)
            {
                projection = Projection.ShadowFace
            };
        }

        public override void RecalculateShadows()
        {
            if (!castShadows)
                return;

            properties.position = new Vector4(parentObject.transformation.Position, 1.0f);
            for (int i = 0; i < 6; i++)
            {
                Matrix4 invCombinedMatrix = Matrix4.Identity;
                switch (i)
                {
                    case 0:
                        shadowRight.projectionMatrix = GetProjectionMatrix(ShadowType.Right + i);
                        properties.lightSpaceRightMatrix = GetLightViewMatrix(ShadowType.Right + i) * shadowRight.projectionMatrix;
                        properties.lightSpaceRightMatrix.Transpose();
                        invCombinedMatrix = GetLightViewMatrixForFrustum(ShadowType.Right + i) * shadowRight.projectionMatrix;
                        invCombinedMatrix.Transpose();
                        invCombinedMatrix = Matrix4.Invert(invCombinedMatrix);
                        break;
                    case 1:
                        shadowLeft.projectionMatrix = GetProjectionMatrix(ShadowType.Right + i);
                        properties.lightSpaceLeftMatrix = GetLightViewMatrix(ShadowType.Right + i) * shadowLeft.projectionMatrix;
                        properties.lightSpaceLeftMatrix.Transpose();
                        invCombinedMatrix = GetLightViewMatrixForFrustum(ShadowType.Right + i) * shadowLeft.projectionMatrix;
                        invCombinedMatrix.Transpose();
                        invCombinedMatrix = Matrix4.Invert(invCombinedMatrix);
                        break;
                    case 2:
                        shadowTop.projectionMatrix = GetProjectionMatrix(ShadowType.Right + i);
                        properties.lightSpaceTopMatrix = GetLightViewMatrix(ShadowType.Right + i) * shadowTop.projectionMatrix;
                        properties.lightSpaceTopMatrix.Transpose();
                        invCombinedMatrix = GetLightViewMatrixForFrustum(ShadowType.Right + i) * shadowTop.projectionMatrix;
                        invCombinedMatrix.Transpose();
                        invCombinedMatrix = Matrix4.Invert(invCombinedMatrix);
                        break;
                    case 3:
                        shadowBottom.projectionMatrix = GetProjectionMatrix(ShadowType.Right + i);
                        properties.lightSpaceBottomMatrix = GetLightViewMatrix(ShadowType.Right + i) * shadowBottom.projectionMatrix;
                        properties.lightSpaceBottomMatrix.Transpose();
                        invCombinedMatrix = GetLightViewMatrixForFrustum(ShadowType.Right + i) * shadowBottom.projectionMatrix;
                        invCombinedMatrix.Transpose();
                        invCombinedMatrix = Matrix4.Invert(invCombinedMatrix);
                        break;
                    case 4:
                        shadowFront.projectionMatrix = GetProjectionMatrix(ShadowType.Right + i);
                        properties.lightSpaceFrontMatrix = GetLightViewMatrix(ShadowType.Right + i) * shadowFront.projectionMatrix;
                        properties.lightSpaceFrontMatrix.Transpose();
                        invCombinedMatrix = GetLightViewMatrixForFrustum(ShadowType.Right + i) * shadowFront.projectionMatrix;
                        invCombinedMatrix.Transpose();
                        invCombinedMatrix = Matrix4.Invert(invCombinedMatrix);
                        break;
                    case 5:
                        shadowBack.projectionMatrix = GetProjectionMatrix(ShadowType.Right + i);
                        properties.lightSpaceBackMatrix = GetLightViewMatrix(ShadowType.Right + i) * shadowBack.projectionMatrix;
                        properties.lightSpaceBackMatrix.Transpose();
                        invCombinedMatrix = GetLightViewMatrixForFrustum(ShadowType.Right + i) * shadowBack.projectionMatrix;
                        invCombinedMatrix.Transpose();
                        invCombinedMatrix = Matrix4.Invert(invCombinedMatrix);
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

                frustum.ntl /= frustum.ntl.W;
                frustum.ntr /= frustum.ntr.W;
                frustum.nbl /= frustum.nbl.W;
                frustum.nbr /= frustum.nbr.W;
                frustum.ftl /= frustum.ftl.W;
                frustum.ftr /= frustum.ftr.W;
                frustum.fbl /= frustum.fbl.W;
                frustum.fbr /= frustum.fbr.W;

                if (!gizmos.ContainsKey("frustumGizmo" + i.ToString()))
                {
                    gizmos.Add("frustumGizmo" + i.ToString(), new Gizmo(wireVao, wireVbo, wireShaderId, windowSize, ref camera, ref parentObject));
                    gizmos["frustumGizmo" + i.ToString()].alwaysVisible = true;
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
                        gizmos["positionGizmo"].AddSphereGizmo(2, Color4.Green);
                        gizmos["positionGizmo"].recalculate = true;
                        gizmos["positionGizmo"].RecalculateModelMatrix(new bool[] { true, false, false });
                    }
                    else
                    {
                        gizmos["positionGizmo"].model.meshes.Clear();
                        gizmos["positionGizmo"].AddSphereGizmo(2, Color4.Green);
                        gizmos["positionGizmo"].recalculate = true;
                        gizmos["positionGizmo"].RecalculateModelMatrix(new bool[] { true, false, false });
                    }
                }
            }

            Engine.sendLightUBO = true;
        }

        public override Matrix4 GetLightViewMatrix(ShadowType type = default)
        {
            Matrix4 lightView = Matrix4.Identity;
            switch (type)
            {
                case ShadowType.Right:
                    lightView = Matrix4.LookAt(parentObject.transformation.Position, parentObject.transformation.Position + Vector3.UnitX, -Vector3.UnitY);
                    break;
                case ShadowType.Left:
                    lightView = Matrix4.LookAt(parentObject.transformation.Position, parentObject.transformation.Position - Vector3.UnitX, -Vector3.UnitY);
                    break;
                case ShadowType.Top:
                    lightView = Matrix4.LookAt(parentObject.transformation.Position, parentObject.transformation.Position + Vector3.UnitY, Vector3.UnitZ);
                    break;
                case ShadowType.Bottom:
                    lightView = Matrix4.LookAt(parentObject.transformation.Position, parentObject.transformation.Position - Vector3.UnitY, -Vector3.UnitZ);
                    break;
                case ShadowType.Front:
                    lightView = Matrix4.LookAt(parentObject.transformation.Position, parentObject.transformation.Position + Vector3.UnitZ, -Vector3.UnitY);
                    break;
                case ShadowType.Back:
                    lightView = Matrix4.LookAt(parentObject.transformation.Position, parentObject.transformation.Position - Vector3.UnitZ, -Vector3.UnitY);
                    break;
            }
            return lightView;
        }

        public override Matrix4 GetLightViewMatrixForFrustum(ShadowType type = default)
        {
            Matrix4 lightView = Matrix4.Identity;
            switch (type)
            {
                case ShadowType.Right:
                    lightView = Matrix4.LookAt(Vector3.Zero, Vector3.UnitX, -Vector3.UnitY);
                    break;
                case ShadowType.Left:
                    lightView = Matrix4.LookAt(Vector3.Zero, -Vector3.UnitX, -Vector3.UnitY);
                    break;
                case ShadowType.Top:
                    lightView = Matrix4.LookAt(Vector3.Zero, Vector3.UnitY, Vector3.UnitZ);
                    break;
                case ShadowType.Bottom:
                    lightView = Matrix4.LookAt(Vector3.Zero, -Vector3.UnitY, -Vector3.UnitZ);
                    break;
                case ShadowType.Front:
                    lightView = Matrix4.LookAt(Vector3.Zero, Vector3.UnitZ, -Vector3.UnitY);
                    break;
                case ShadowType.Back:
                    lightView = Matrix4.LookAt(Vector3.Zero, -Vector3.UnitZ, -Vector3.UnitY);
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
