using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Engine3D
{
    public class DirectionalLight : Light
    {
        public Shadow shadowLarge;
        public Shadow shadowMedium;
        public Shadow shadowSmall;

        public DirectionalLight()
        {
            
        }

        public DirectionalLight(Object parentObject, int id, VAO wireVao, VBO wireVbo, int wireShaderId, Vector2 windowSize, ref Camera mainCamera) :
            base(parentObject, id, wireVao, wireVbo, wireShaderId, windowSize, ref mainCamera)
        {
            color = Color4.White;
            parentObject.transformation.Rotation = Helper.QuaternionFromEuler(new Vector3(0.0f, -1.0f, 0.0f));
            ambient = new Vector3(0.1f, 0.1f, 0.1f);
            diffuse = new Vector3(1.0f, 1.0f, 1.0f);
            specular = new Vector3(1.0f, 1.0f, 1.0f);
            specularPow = 2.0f;
            RecalculateShadows();
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
                { "directionLoc", GL.GetUniformLocation(shaderProgramId, "lights[" + id + "].direction") },
                { "colorLoc", GL.GetUniformLocation(shaderProgramId, "lights[" + id + "].color") },

                { "ambientLoc", GL.GetUniformLocation(shaderProgramId, "lights[" + id + "].ambient") },
                { "diffuseLoc", GL.GetUniformLocation(shaderProgramId, "lights[" + id + "].diffuse") },
                { "specularLoc", GL.GetUniformLocation(shaderProgramId, "lights[" + id + "].specular") },
                { "specularPowLoc", GL.GetUniformLocation(shaderProgramId, "lights[" + id + "].specularPow") }
            };
        }
        #endregion

        #region Shadow
        public override void BindForWriting(ShadowType type)
        {
            if (type == ShadowType.Small)
            {
                GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, shadowSmall.fbo);
                GL.Viewport(0, 0, shadowSmall.size, shadowSmall.size);
            }
            else if (type == ShadowType.Medium)
            {
                GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, shadowMedium.fbo);
                GL.Viewport(0, 0, shadowMedium.size, shadowMedium.size);
            }
            else
            {
                GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, shadowLarge.fbo);
                GL.Viewport(0, 0, shadowLarge.size, shadowLarge.size);
            }
        }

        public override void BindForReading(ShadowType type)
        {
            //TODO: Dont bind every frame
            GL.ActiveTexture(TextureUnit.Texture0 + shadowSmall.shadowMap.TextureUnit);//12
            GL.BindTexture(TextureTarget.Texture2D, shadowSmall.shadowMap.TextureId);//17

            GL.ActiveTexture(TextureUnit.Texture0 + shadowMedium.shadowMap.TextureUnit);//14
            GL.BindTexture(TextureTarget.Texture2D, shadowMedium.shadowMap.TextureId);//18

            GL.ActiveTexture(TextureUnit.Texture0 + shadowLarge.shadowMap.TextureUnit);//16
            GL.BindTexture(TextureTarget.Texture2D, shadowLarge.shadowMap.TextureId);//19
        }

        public override void InitShadows()
        {
            shadowSmall = new Shadow(2048);
            shadowSmall.projection = Projection.ShadowSmall;

            shadowMedium = new Shadow(1024);
            shadowMedium.projection = Projection.ShadowMedium;

            shadowLarge = new Shadow(512);
            shadowLarge.projection = Projection.ShadowLarge;
        }

        public override void SetupShadows()
        {
            shadowSmall.shadowMap = Engine.textureManager.GetShadowTexture(shadowSmall.size);
            GL.BindTexture(TextureTarget.Texture2D, shadowSmall.shadowMap.TextureId);
            shadowSmall.fbo = SetupFrameBuffer(shadowSmall.shadowMap.TextureId);
            GL.BindTexture(TextureTarget.Texture2D, 0);

            shadowMedium.shadowMap = Engine.textureManager.GetShadowTexture(shadowMedium.size);
            GL.BindTexture(TextureTarget.Texture2D, shadowMedium.shadowMap.TextureId);
            shadowMedium.fbo = SetupFrameBuffer(shadowMedium.shadowMap.TextureId);
            GL.BindTexture(TextureTarget.Texture2D, 0);

            shadowLarge.shadowMap = Engine.textureManager.GetShadowTexture(shadowLarge.size);
            GL.BindTexture(TextureTarget.Texture2D, shadowLarge.shadowMap.TextureId);
            shadowLarge.fbo = SetupFrameBuffer(shadowLarge.shadowMap.TextureId);
            GL.BindTexture(TextureTarget.Texture2D, 0);
        }

        public override void ResizeShadowMap(ShadowType type, int size)
        {
            if (type == ShadowType.Small)
            {
                shadowSmall.size = size;
                Engine.textureManager.ResizeShadowTexture(shadowSmall.shadowMap, size);
            }
            else if (type == ShadowType.Medium)
            {
                shadowMedium.size = size;
                Engine.textureManager.ResizeShadowTexture(shadowMedium.shadowMap, size);
            }
            else if (type == ShadowType.Large)
            {
                shadowLarge.size = size;
                Engine.textureManager.ResizeShadowTexture(shadowLarge.shadowMap, size);
            }
        }

        public override void RecalculateShadows()
        {
            if (!castShadows)
                return;

            for (int i = 0; i < 3; i++)
            {
                Matrix4 invCombinedMatrix = Matrix4.Invert(shadowSmall.projectionMatrix * GetLightViewMatrixForFrustum());
                shadowSmall.projectionMatrix = GetProjectionMatrix(ShadowType.Small);
                if (i == 1)
                {
                    shadowMedium.projectionMatrix = GetProjectionMatrix(ShadowType.Medium);
                    invCombinedMatrix = Matrix4.Invert(shadowMedium.projectionMatrix * GetLightViewMatrixForFrustum());
                }
                else if (i == 2)
                {
                    shadowLarge.projectionMatrix = GetProjectionMatrix(ShadowType.Large);
                    invCombinedMatrix = Matrix4.Invert(shadowLarge.projectionMatrix * GetLightViewMatrixForFrustum());
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

                Vector3 lightDirection = GetDirection();
                Vector3 lightPosition = -(lightDirection * distanceFromScene);

                // Transform each corner from NDC to world space
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
                        gizmos["positionGizmo"].AddSphereGizmo(2, Color4.Green, lightPosition);
                        gizmos["positionGizmo"].recalculate = true;
                        gizmos["positionGizmo"].RecalculateModelMatrix(new bool[] { true, false, false });
                    }
                    else
                    {
                        gizmos["positionGizmo"].model.meshes.Clear();
                        gizmos["positionGizmo"].AddSphereGizmo(2, Color4.Green, lightPosition);
                        gizmos["positionGizmo"].recalculate = true;
                        gizmos["positionGizmo"].RecalculateModelMatrix(new bool[] { true, false, false });
                    }
                    if (!gizmos.ContainsKey("targetGizmo"))
                    {
                        gizmos.Add("targetGizmo", new Gizmo(wireVao, wireVbo, wireShaderId, windowSize, ref camera, ref parentObject));
                        gizmos["targetGizmo"].AddSphereGizmo(2, Color4.Red);
                        gizmos["targetGizmo"].recalculate = true;
                        gizmos["targetGizmo"].RecalculateModelMatrix(new bool[] { true, false, false });
                    }
                    else
                    {
                        gizmos["targetGizmo"].model.meshes.Clear();
                        gizmos["targetGizmo"].AddSphereGizmo(2, Color4.Red);
                        gizmos["targetGizmo"].recalculate = true;
                        gizmos["targetGizmo"].RecalculateModelMatrix(new bool[] { true, false, false });
                    }
                    if (!gizmos.ContainsKey("directionGizmo"))
                    {
                        gizmos.Add("directionGizmo", new Gizmo(wireVao, wireVbo, wireShaderId, windowSize, ref camera, ref parentObject));
                        gizmos["directionGizmo"].AddDirectionGizmo(Vector3.Zero, lightDirection, 10, Color4.Green);
                        gizmos["directionGizmo"].recalculate = true;
                        gizmos["directionGizmo"].RecalculateModelMatrix(new bool[] { true, false, false });
                    }
                    else
                    {
                        gizmos["directionGizmo"].model.meshes.Clear();
                        gizmos["directionGizmo"].AddDirectionGizmo(Vector3.Zero, lightDirection, 10, Color4.Green);
                        gizmos["directionGizmo"].recalculate = true;
                        gizmos["directionGizmo"].RecalculateModelMatrix(new bool[] { true, false, false });
                    }
                }
            }
        }

        public override Matrix4 GetLightViewMatrix()
        {
            Vector3 lightPosition = parentObject.transformation.Position - (GetDirection() * distanceFromScene);

            return Matrix4.LookAt(lightPosition, parentObject.transformation.Position, Vector3.UnitY);
        }

        public override Matrix4 GetLightViewMatrixForFrustum()
        {
            Vector3 lightPosition = parentObject.transformation.Position - Vector3.Transform(new Vector3(0, 0, -1), parentObject.transformation.Rotation) * distanceFromScene;

            // Create a rotation matrix from the quaternion
            Matrix4 rotationMatrix = Matrix4.CreateFromQuaternion(parentObject.transformation.Rotation);

            // Create a translation matrix for the light's position
            Matrix4 translationMatrix = Matrix4.CreateTranslation(-lightPosition);

            // Combine rotation and translation to form the view matrix
            Matrix4 viewMatrix = translationMatrix * rotationMatrix;

            return viewMatrix;
        }

        public override Matrix4 GetProjectionMatrix(ShadowType type)
        {
            Projection projection = shadowSmall.projection;
            if (type == ShadowType.Medium)
                projection = shadowMedium.projection;
            else if (type == ShadowType.Large)
                projection = shadowLarge.projection;

            float l = projection.left;
            float r = projection.right;
            float t = projection.top;
            float b = projection.bottom;
            float n = projection.near;
            float f = projection.far;

            Matrix4 m = Matrix4.CreateOrthographic(r - l, t - b, n, f);

            return m;
        }
        #endregion
    }
}
