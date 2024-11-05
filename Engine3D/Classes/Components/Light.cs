using Newtonsoft.Json;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Text;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine3D
{
    public enum ShadowType { Small, Medium, Large }

    public class Light : IComponent
    {
        public enum LightType
        {
            PointLight = 0,
            DirectionalLight = 1
        }

        public string name = "Light";
        private int shaderProgramId;
        private int id;

        private LightType lightType = LightType.DirectionalLight;

        #region LightVars
        public int colorLoc;
        private Color4 color;

        public float range;

        public int constantLoc;
        public float constant;

        public int linearLoc;
        public float linear;

        public int quadraticLoc;
        public float quadratic;

        public float ambientS = 0.1f;
        public int ambientLoc;
        public Vector3 ambient;

        public int diffuseLoc;
        public Vector3 diffuse;

        public int specularPowLoc;
        public float specularPow = 64f;
        public int specularLoc;
        public Vector3 specular;
        #endregion

        #region ShadowVars
        private VAO wireVao;
        private VBO wireVbo;
        private int wireShaderId;
        private Vector2 windowSize;
        [JsonIgnore]
        public Camera camera;

        [JsonIgnore]
        public Dictionary<string, Gizmo> gizmos = new Dictionary<string, Gizmo>();

        private bool showGizmos_ = true;
        [JsonIgnore]
        public bool showGizmos
        {
            get { return showGizmos_; }
            set
            {
                showGizmos_ = value;
            }
        }
        public Vector3 target = Vector3.Zero;
        public float distanceFromScene = 50;

        public Shadow shadowLarge;
        public Shadow shadowMedium;
        public Shadow shadowSmall;
        #endregion

        [JsonIgnore]
        public Object parentObject;
        [JsonIgnore]
        private Dictionary<string, int> uniforms;

        public Light()
        {
            
        }

        public Light(Object parentObject, int id, VAO wireVao, VBO wireVbo, int wireShaderId, Vector2 windowSize, ref Camera mainCamera)
        {
            this.parentObject = parentObject;
            this.id = id;
            this.wireVao = wireVao;
            this.wireVbo = wireVbo;
            this.wireShaderId = wireShaderId;
            this.windowSize = windowSize;
            camera = mainCamera;

            SetupShadows();

            SetLightType(LightType.DirectionalLight);
        }

        public Light(Object parentObject, int id, LightType lightType, VAO wireVao, VBO wireVbo, int wireShaderId, Vector2 windowSize, ref Camera mainCamera)
        {
            this.parentObject = parentObject;
            this.id = id;
            this.wireVao = wireVao;
            this.wireVbo = wireVbo;
            this.wireShaderId = wireShaderId;
            this.windowSize = windowSize;
            camera = mainCamera;

            SetupShadows();

            this.lightType = lightType;
            SetLightType(lightType);
        }

        #region Light
        private void GetUniformLocations(int spi)
        {
            if (shaderProgramId == spi)
                return;
            shaderProgramId = spi;

            uniforms = new Dictionary<string, int>();

            uniforms.Add("lightTypeLoc", GL.GetUniformLocation(shaderProgramId, "lights[" + id + "].lightType"));
            if (lightType == LightType.PointLight)
            {
                uniforms.Add("positionLoc", GL.GetUniformLocation(shaderProgramId, "lights[" + id + "].position"));
                uniforms.Add("colorLoc", GL.GetUniformLocation(shaderProgramId, "lights[" + id + "].color"));

                uniforms.Add("ambientLoc", GL.GetUniformLocation(shaderProgramId, "lights[" + id + "].ambient"));
                uniforms.Add("diffuseLoc", GL.GetUniformLocation(shaderProgramId, "lights[" + id + "].diffuse"));
                uniforms.Add("specularLoc", GL.GetUniformLocation(shaderProgramId, "lights[" + id + "].specular"));

                uniforms.Add("specularPowLoc", GL.GetUniformLocation(shaderProgramId, "lights[" + id + "].specularPow"));
                uniforms.Add("constantLoc", GL.GetUniformLocation(shaderProgramId, "lights[" + id + "].constant"));
                uniforms.Add("linearLoc", GL.GetUniformLocation(shaderProgramId, "lights[" + id + "].linear"));
                uniforms.Add("quadraticLoc", GL.GetUniformLocation(shaderProgramId, "lights[" + id + "].quadratic"));
            }
            else if (lightType == LightType.DirectionalLight)
            {
                uniforms.Add("directionLoc", GL.GetUniformLocation(shaderProgramId, "lights[" + id + "].direction"));
                uniforms.Add("colorLoc", GL.GetUniformLocation(shaderProgramId, "lights[" + id + "].color"));

                uniforms.Add("ambientLoc", GL.GetUniformLocation(shaderProgramId, "lights[" + id + "].ambient"));
                uniforms.Add("diffuseLoc", GL.GetUniformLocation(shaderProgramId, "lights[" + id + "].diffuse"));
                uniforms.Add("specularLoc", GL.GetUniformLocation(shaderProgramId, "lights[" + id + "].specular"));
                uniforms.Add("specularPowLoc", GL.GetUniformLocation(shaderProgramId, "lights[" + id + "].specularPow"));
            }
        }

        public LightType GetLightType()
        {
            return lightType;
        }

        public void SetLightType(LightType lightType)
        {
            this.lightType = lightType;
            color = Color4.White;
            if (lightType == LightType.PointLight)
            {
                ambient = new Vector3(2f, 2f, 2f);
                diffuse = new Vector3(0.8f, 0.8f, 0.8f);
                specular = new Vector3(1.0f, 1.0f, 1.0f);
                specularPow = 32f;

                constant = 1.0f;
                linear = 0.09f;
                quadratic = 0.032f;

                range = AttenuationToRange(constant, linear, quadratic);
            }
            else if (lightType == LightType.DirectionalLight)
            {
                parentObject.transformation.Rotation = Helper.QuaternionFromEuler(new Vector3(0.0f, -1.0f, 0.0f));
                ambient = new Vector3(0.1f, 0.1f, 0.1f);
                diffuse = new Vector3(1.0f, 1.0f, 1.0f);
                specular = new Vector3(1.0f, 1.0f, 1.0f);
                specularPow = 2.0f;
                RecalculateGizmos();
            }
        }

        public static void SendToGPU(List<Light> lights, int shaderProgramId)
        {
            GL.Uniform1(GL.GetUniformLocation(shaderProgramId, "actualNumOfLights"), lights.Count);

            for (int i = 0; i < lights.Count; i++)
            {
                lights[i].GetUniformLocations(shaderProgramId);
                GL.Uniform1(lights[i].uniforms["lightTypeLoc"], (int)lights[i].lightType);
                if (lights[i].lightType == LightType.PointLight)
                {
                    Vector3 c = new Vector3(lights[i].color.R, lights[i].color.G, lights[i].color.B);
                    GL.Uniform3(lights[i].uniforms["positionLoc"], lights[i].parentObject.transformation.Position);
                    GL.Uniform3(lights[i].uniforms["colorLoc"], c);
                    GL.Uniform3(lights[i].uniforms["ambientLoc"], lights[i].ambient);
                    GL.Uniform3(lights[i].uniforms["diffuseLoc"], lights[i].diffuse);
                    GL.Uniform3(lights[i].uniforms["specularLoc"], lights[i].specular);
                    GL.Uniform1(lights[i].uniforms["specularPowLoc"], lights[i].specularPow);
                    GL.Uniform1(lights[i].uniforms["constantLoc"], lights[i].constant);
                    GL.Uniform1(lights[i].uniforms["linearLoc"], lights[i].linear);
                }
                else if (lights[i].lightType == LightType.DirectionalLight)
                {
                    Vector3 c = new Vector3(lights[i].color.R, lights[i].color.G, lights[i].color.B);
                    Vector3 rot = GetLightDirectionFromEuler(lights[i].parentObject.transformation.Rotation);
                    GL.Uniform3(lights[i].uniforms["directionLoc"], rot);
                    GL.Uniform3(lights[i].uniforms["colorLoc"], c);

                    GL.Uniform3(lights[i].uniforms["ambientLoc"], lights[i].ambient);
                    GL.Uniform3(lights[i].uniforms["diffuseLoc"], lights[i].diffuse);
                    GL.Uniform3(lights[i].uniforms["specularLoc"], lights[i].specular);
                    GL.Uniform1(lights[i].uniforms["specularPowLoc"], lights[i].specularPow);
                }
            }
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

        public static Vector3 GetLightDirectionFromEuler(Quaternion rotation)
        {
            // Assuming the forward direction is along the negative Z-axis
            Vector3 forward = new Vector3(0, 0, -1);

            // Rotate the forward vector by the quaternion to get the light direction
            Vector3 lightDirection = Vector3.Transform(forward, rotation);
            lightDirection.X = (float)Math.Round(lightDirection.X, 3);
            lightDirection.Y = (float)Math.Round(lightDirection.Y, 3);
            lightDirection.Z = (float)Math.Round(lightDirection.Z, 3);

            return lightDirection.Normalized();  // Send normalized light direction
        }
        #endregion

        #region Shadow
        public void BindForWriting(ShadowType type)
        {
            if (type == ShadowType.Small)
            {
                GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, shadowSmall.fbo);
                GL.Viewport(0, 0, (int)shadowSmall.size.X, (int)shadowSmall.size.Y);
            }
            else if(type == ShadowType.Medium)
            {
                GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, shadowMedium.fbo);
                GL.Viewport(0, 0, (int)shadowMedium.size.X, (int)shadowMedium.size.Y);
            }
            else
            {
                GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, shadowLarge.fbo);
                GL.Viewport(0, 0, (int)shadowLarge.size.X, (int)shadowLarge.size.Y);
            }
        }

        public void BindForReading(ShadowType type)
        {
            int shadowMapId = shadowSmall.shadowMap.TextureId;
            int shadowMapUnit = shadowSmall.shadowMap.TextureUnit;
            if (type == ShadowType.Medium)
            {
                shadowMapId = shadowMedium.shadowMap.TextureId;
                shadowMapUnit = shadowMedium.shadowMap.TextureUnit;
            }
            else
            {
                shadowMapId = shadowLarge.shadowMap.TextureId;
                shadowMapUnit = shadowLarge.shadowMap.TextureUnit;
            }

            if (Engine.GLState.currentTextureUnit != shadowMapUnit)
            {
                GL.ActiveTexture(TextureUnit.Texture0 + shadowMapUnit);
                Engine.GLState.currentTextureUnit = shadowMapUnit;
            }

            if (Engine.GLState.currentTextureId != shadowMapId)
            {
                GL.BindTexture(TextureTarget.Texture2D, shadowMapId);
                Engine.GLState.currentTextureId = shadowMapId;
            }
        }

        public void SetupShadows()
        {
            shadowSmall = new Shadow(new Vector2(2048, 2048 / 1.6606f));
            shadowSmall.shadowMap = Engine.textureManager.GetShadowTexture(shadowSmall.size);
            GL.BindTexture(TextureTarget.Texture2D, shadowSmall.shadowMap.TextureId);
            shadowSmall.fbo = SetupFrameBuffer(shadowSmall.shadowMap.TextureId);
            GL.BindTexture(TextureTarget.Texture2D, 0);

            shadowMedium = new Shadow(new Vector2(2048, 2048 / 1.6606f));
            shadowMedium.projection = Projection.ShadowMedium;
            shadowMedium.shadowMap = Engine.textureManager.GetShadowTexture(shadowMedium.size);
            GL.BindTexture(TextureTarget.Texture2D, shadowMedium.shadowMap.TextureId);
            shadowMedium.fbo = SetupFrameBuffer(shadowMedium.shadowMap.TextureId);
            GL.BindTexture(TextureTarget.Texture2D, 0);

            shadowLarge = new Shadow(new Vector2(2048, 2048 / 1.6606f));
            shadowLarge.projection = Projection.ShadowLarge;
            shadowLarge.shadowMap = Engine.textureManager.GetShadowTexture(shadowLarge.size);
            GL.BindTexture(TextureTarget.Texture2D, shadowLarge.shadowMap.TextureId);
            shadowLarge.fbo = SetupFrameBuffer(shadowLarge.shadowMap.TextureId);
            GL.BindTexture(TextureTarget.Texture2D, 0);
        }

        private int SetupFrameBuffer(int shadowMapId)
        {
            int fbo = GL.GenFramebuffer();
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, fbo);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, TextureTarget.Texture2D, shadowMapId, 0);

            GL.DrawBuffer(DrawBufferMode.None);
            GL.ReadBuffer(ReadBufferMode.None);

            TextureManager.textureCount++;

            var status = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
            if (status != FramebufferErrorCode.FramebufferComplete)
            {
                throw new Exception($"Framebuffer is incomplete: {status}");
            }

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

            return fbo;
        }

        public void RecalculateGizmos()
        {
            for (int i = 0; i < 3; i++)
            {
                Matrix4 invCombinedMatrix = Matrix4.Invert(shadowSmall.projectionMatrixOrtho * GetLightViewMatrixForFrustum());
                shadowSmall.projectionMatrixOrtho = GetProjectionMatrixOrtho(ShadowType.Small);
                if (i == 1)
                {
                    shadowMedium.projectionMatrixOrtho = GetProjectionMatrixOrtho(ShadowType.Medium);
                    invCombinedMatrix = Matrix4.Invert(shadowMedium.projectionMatrixOrtho * GetLightViewMatrixForFrustum());
                }
                else if (i == 2)
                {
                    shadowLarge.projectionMatrixOrtho = GetProjectionMatrixOrtho(ShadowType.Large);
                    invCombinedMatrix = Matrix4.Invert(shadowLarge.projectionMatrixOrtho * GetLightViewMatrixForFrustum());
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
                Vector3 lightPosition = target - (lightDirection * distanceFromScene);

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
                        gizmos["targetGizmo"].AddSphereGizmo(2, Color4.Red, target);
                        gizmos["targetGizmo"].recalculate = true;
                        gizmos["targetGizmo"].RecalculateModelMatrix(new bool[] { true, false, false });
                    }
                    else
                    {
                        gizmos["targetGizmo"].model.meshes.Clear();
                        gizmos["targetGizmo"].AddSphereGizmo(2, Color4.Red, target);
                        gizmos["targetGizmo"].recalculate = true;
                        gizmos["targetGizmo"].RecalculateModelMatrix(new bool[] { true, false, false });
                    }
                    if (!gizmos.ContainsKey("directionGizmo"))
                    {
                        gizmos.Add("directionGizmo", new Gizmo(wireVao, wireVbo, wireShaderId, windowSize, ref camera, ref parentObject));
                        gizmos["directionGizmo"].AddDirectionGizmo(target, lightDirection, 10, Color4.Green);
                        gizmos["directionGizmo"].recalculate = true;
                        gizmos["directionGizmo"].RecalculateModelMatrix(new bool[] { true, false, false });
                    }
                    else
                    {
                        gizmos["directionGizmo"].model.meshes.Clear();
                        gizmos["directionGizmo"].AddDirectionGizmo(target, lightDirection, 10, Color4.Green);
                        gizmos["directionGizmo"].recalculate = true;
                        gizmos["directionGizmo"].RecalculateModelMatrix(new bool[] { true, false, false });
                    }
                }
            }
        }

        public Matrix4 GetLightViewMatrix()
        {
            Vector3 lightPosition = target - (GetDirection() * distanceFromScene);

            return Matrix4.LookAt(lightPosition, target, Vector3.UnitY);
        }

        public Matrix4 GetLightViewMatrixForFrustum()
        {
            Vector3 lightPosition = target - Vector3.Transform(new Vector3(0, 0, -1), parentObject.transformation.Rotation) * distanceFromScene;

            // Create a rotation matrix from the quaternion
            Matrix4 rotationMatrix = Matrix4.CreateFromQuaternion(parentObject.transformation.Rotation);

            // Create a translation matrix for the light's position
            Matrix4 translationMatrix = Matrix4.CreateTranslation(-lightPosition);

            // Combine rotation and translation to form the view matrix
            Matrix4 viewMatrix = translationMatrix * rotationMatrix;

            return viewMatrix;
        }

        public Matrix4 GetProjectionMatrixOrtho(ShadowType type)
        {
            Projection projection = shadowSmall.projection;
            if(type == ShadowType.Medium)
                projection = shadowMedium.projection;
            else if(type == ShadowType.Large)
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

        #region Getters/Setters
        public Vector3 GetDirection()
        {
            return GetLightDirectionFromEuler(parentObject.transformation.Rotation);
        }
        public void SetColor(Color4 c)
        {
            color = new Color4(c.R,c.G,c.B,c.A);
        }
        public void SetColor(System.Numerics.Vector3 c)
        {
            color = new Color4(c.X, c.Y, c.Z, 1.0f);
        }
        public System.Numerics.Vector3 GetColorV3()
        {
            return new System.Numerics.Vector3(color.R, color.G, color.B);
        }
        public Color4 GetColorC4()
        {
            return color;
        }
        #endregion
    }
}
