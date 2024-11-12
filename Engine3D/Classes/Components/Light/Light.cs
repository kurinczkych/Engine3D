using Newtonsoft.Json;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Text;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Engine3D
{
    public enum ShadowType { Small, Medium, Large,
                             Top, Bottom, Left, Right, Front, Back}

    public abstract class Light : IComponent
    {
        public const int MAX_LIGHTS = 64;

        public enum LightType
        {
            PointLight = 0,
            DirectionalLight = 1
        }

        public string name = "Light";
        protected int shaderProgramId;
        protected int id;

        public LightStruct properties = new LightStruct();

        #region LightVars
        #endregion

        #region ShadowVars
        protected VAO wireVao;
        protected VBO wireVbo;
        protected int wireShaderId;
        protected Vector2 windowSize;
        [JsonIgnore]
        public Camera camera;

        [JsonIgnore]
        public Dictionary<string, Gizmo> gizmos = new Dictionary<string, Gizmo>();

        protected bool showGizmos_ = true;
        [JsonIgnore]
        public bool showGizmos
        {
            get { return showGizmos_; }
            set
            {
                showGizmos_ = value;
            }
        }
        public float distanceFromScene = 50;
        public bool freezeView = true;
        public bool castShadows = true;
        #endregion

        [JsonIgnore]
        public Object parentObject;
        [JsonIgnore]
        protected Dictionary<string, int> uniforms;

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

            InitShadows();
            SetupShadows();
        }

        #region Light

        public static void BindLightUBO(ref int lightUBO, int shaderId)
        {
            int blockIndex = GL.GetUniformBlockIndex(shaderId, "LightData");
            GL.UniformBlockBinding(shaderId, blockIndex, 0);
            GL.BindBufferBase(BufferRangeTarget.UniformBuffer, 0, lightUBO);
        }

        public static void SendUBOToGPU(List<Light> lights, int lightUBO)
        {
            //TODO: Optimalization, only send lights that can be seen
            LightStruct[] lightStructs = lights.Take(MAX_LIGHTS).Select(x => x.properties).ToArray();
            int structSize = Marshal.SizeOf(typeof(LightStruct));
            int size = structSize * MAX_LIGHTS;

            IntPtr ptr = Marshal.AllocHGlobal(size);

            try
            {
                // Copy each LightStruct to unmanaged memory
                for (int i = 0; i < lightStructs.Length; i++)
                {
                    IntPtr structPtr = IntPtr.Add(ptr, i * structSize);
                    Marshal.StructureToPtr(lightStructs[i], structPtr, false);
                }

                // Bind and upload data to the UBO
                GL.BindBuffer(BufferTarget.UniformBuffer, lightUBO);
                GL.BufferSubData(BufferTarget.UniformBuffer, IntPtr.Zero, size, ptr);
            }
            finally
            {
                // Free the unmanaged memory
                Marshal.FreeHGlobal(ptr);

                // Unbind the buffer
                GL.BindBuffer(BufferTarget.UniformBuffer, 0);
            }
        }

        public static void SendToGPU(List<Light> lights, int shaderProgramId)
        {
            GL.Uniform1(GL.GetUniformLocation(shaderProgramId, "actualNumOfLights"), lights.Count);

            //for (int i = 0; i < lights.Count; i++)
            //{
            //    lights[i].GetUniformLocations(shaderProgramId);
            //    if (lights[i] is PointLight pl)
            //    {
            //        GL.Uniform1(lights[i].uniforms["lightTypeLoc"], 0);
            //        Vector4 c = new Vector4(lights[i].properties.color.X, lights[i].properties.color.Y, lights[i].properties.color.X, 1.0f);
            //        GL.Uniform4(lights[i].uniforms["positionLoc"], lights[i].parentObject.transformation.Position.X, lights[i].parentObject.transformation.Position.Y, lights[i].parentObject.transformation.Position.Z, 1.0f);
            //        GL.Uniform4(lights[i].uniforms["colorLoc"], c);
            //        GL.Uniform4(lights[i].uniforms["ambientLoc"], lights[i].properties.ambient);
            //        GL.Uniform4(lights[i].uniforms["diffuseLoc"], lights[i].properties.diffuse);
            //        GL.Uniform4(lights[i].uniforms["specularLoc"], lights[i].properties.specular);
            //        GL.Uniform1(lights[i].uniforms["specularPowLoc"], lights[i].properties.specularPow);
            //        GL.Uniform1(lights[i].uniforms["constantLoc"], pl.properties.constant);
            //        GL.Uniform1(lights[i].uniforms["linearLoc"], pl.properties.linear);
            //    }
            //    else if (lights[i] is DirectionalLight dl)
            //    {
            //        GL.Uniform1(lights[i].uniforms["lightTypeLoc"], 1);
            //        Vector4 c = new Vector4(lights[i].properties.color.X, lights[i].properties.color.Y, lights[i].properties.color.X, 1.0f);
            //        Vector3 rot = GetLightDirectionFromEuler(lights[i].parentObject.transformation.Rotation);
            //        GL.Uniform4(lights[i].uniforms["directionLoc"], rot.X, rot.Y, rot.Z, 1.0f);
            //        GL.Uniform4(lights[i].uniforms["colorLoc"], c);

            //        GL.Uniform4(lights[i].uniforms["ambientLoc"], lights[i].properties.ambient);
            //        GL.Uniform4(lights[i].uniforms["diffuseLoc"], lights[i].properties.diffuse);
            //        GL.Uniform4(lights[i].uniforms["specularLoc"], lights[i].properties.specular);
            //        GL.Uniform1(lights[i].uniforms["specularPowLoc"], lights[i].properties.specularPow);

            //        Matrix4 lightview = lights[i].GetLightViewMatrix();
            //        dl.lightSpaceSmallMatrix = lightview * dl.shadowSmall.projectionMatrix;
            //        dl.lightSpaceMediumMatrix = lightview * dl.shadowMedium.projectionMatrix;
            //        dl.lightSpaceLargeMatrix = lightview * dl.shadowLarge.projectionMatrix;

            //        GL.UniformMatrix4(lights[i].uniforms["lightSpaceSmallMatrix"], true, ref dl.lightSpaceSmallMatrix);
            //        GL.UniformMatrix4(lights[i].uniforms["lightSpaceMediumMatrix"], true, ref dl.lightSpaceMediumMatrix);
            //        GL.UniformMatrix4(lights[i].uniforms["lightSpaceLargeMatrix"], true, ref dl.lightSpaceLargeMatrix);

            //        if (dl.shadowSmall.fbo != -1 && dl.shadowSmall.shadowMap.TextureId != -1)
            //            GL.Uniform1(lights[i].uniforms["shadowMapSmall"], dl.shadowSmall.shadowMap.TextureUnit);

            //        if (dl.shadowMedium.fbo != -1 && dl.shadowMedium.shadowMap.TextureId != -1)
            //            GL.Uniform1(lights[i].uniforms["shadowMapMedium"], dl.shadowMedium.shadowMap.TextureUnit);

            //        if (dl.shadowLarge.fbo != -1 && dl.shadowLarge.shadowMap.TextureId != -1)
            //            GL.Uniform1(lights[i].uniforms["shadowMapLarge"], dl.shadowLarge.shadowMap.TextureUnit);

            //        GL.Uniform1(lights[i].uniforms["cascadeFarPlaneSmall"], dl.shadowSmall.projection.distance);
            //        GL.Uniform1(lights[i].uniforms["cascadeFarPlaneMedium"], dl.shadowMedium.projection.distance);
            //        GL.Uniform1(lights[i].uniforms["cascadeFarPlaneLarge"], dl.shadowLarge.projection.distance);
            //    }
            //}
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
        public abstract void BindForWriting(ShadowType type);
        public abstract void BindForReading(ShadowType type);

        public abstract void InitShadows();
        public abstract void SetupShadows();
        public abstract void ResizeShadowMap(ShadowType type, int size);

        public abstract void RecalculateShadows();
        public abstract Matrix4 GetLightViewMatrix(ShadowType type = default);
        public abstract Matrix4 GetLightViewMatrixForFrustum(ShadowType type = default);
        public abstract Matrix4 GetProjectionMatrix(ShadowType type);

        protected int SetupFrameBuffer(int shadowMapId)
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
        #endregion

        #region Getters/Setters
        public Vector3 GetDirection()
        {
            return GetLightDirectionFromEuler(parentObject.transformation.Rotation);
        }
        public void SetColor(Color4 c)
        {
            properties.color = new Vector4(c.R,c.G,c.B,c.A);
        }
        public void SetColor(System.Numerics.Vector4 c)
        {
            properties.color = new Vector4(c.X, c.Y, c.Z, 1.0f);
        }
        public Color4 GetColorC4()
        {
            return new Color4(properties.color.X, properties.color.Y, properties.color.Z, properties.color.W);
        }
        #endregion
    }
}
