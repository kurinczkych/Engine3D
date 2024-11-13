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
        public bool drawDepthBuffers = false;

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

            //var smallShadowMapsLoc = GL.GetUniformLocation(shaderProgramId, "smallShadowMaps");
            //if (smallShadowMapsLoc != -1)
            //{
            //    GL.ActiveTexture(TextureUnit.Texture0 + Engine.shadowMapArray.smallShadowMapArrayUnit);
            //    GL.BindTexture(TextureTarget.Texture2DArray, Engine.shadowMapArray.smallShadowMapArrayId);
            //    GL.Uniform1(smallShadowMapsLoc, 0); // TextureUnit 0 for smallShadowMapArray
            //}

            //var mediumShadowMapsLoc = GL.GetUniformLocation(shaderProgramId, "mediumShadowMaps");
            //if (mediumShadowMapsLoc != -1)
            //{
            //    GL.ActiveTexture(TextureUnit.Texture0 + Engine.shadowMapArray.mediumShadowMapArrayUnit);
            //    GL.BindTexture(TextureTarget.Texture2DArray, Engine.shadowMapArray.mediumShadowMapArrayId);
            //    GL.Uniform1(mediumShadowMapsLoc, 1); // TextureUnit 0 for mediumShadowMapArrayId
            //}

            //var largeShadowMapsLoc = GL.GetUniformLocation(shaderProgramId, "largeShadowMaps");
            //if (largeShadowMapsLoc != -1)
            //{
            //    GL.ActiveTexture(TextureUnit.Texture0 + Engine.shadowMapArray.largeShadowMapArrayUnit);
            //    GL.BindTexture(TextureTarget.Texture2DArray, Engine.shadowMapArray.largeShadowMapArrayId);
            //    GL.Uniform1(largeShadowMapsLoc, 2); // TextureUnit 0 for largeShadowMapArrayId
            //}

            //var faceShadowMapsLoc = GL.GetUniformLocation(shaderProgramId, "faceShadowMaps");
            //if (faceShadowMapsLoc != -1)
            //{
            //    GL.ActiveTexture(TextureUnit.Texture0 + Engine.shadowMapArray.faceShadowMapArrayUnit);
            //    GL.BindTexture(TextureTarget.Texture2DArray, Engine.shadowMapArray.faceShadowMapArrayId);
            //    GL.Uniform1(faceShadowMapsLoc, 3); // TextureUnit 0 for faceShadowMapArrayId
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
        public static void ManageShadowArrays(List<Light> lights, ref ShadowMapArray shadowMapArray)
        {
            bool justCreated = false;
            if(shadowMapArray.smallShadowMapArrayId == -1)
                justCreated = true;

            int dirIndex = -1;
            int pointIndex = -1;
            for (int i = 0; i < lights.Count; i++)
            {
                if (lights[i] == null)
                    continue;

                if (lights[i] is DirectionalLight dl)
                {
                    dirIndex++;
                    lights[i].properties.shadowIndex = dirIndex;
                }
                else if (lights[i] is PointLight pl)
                {
                    pointIndex++;
                    lights[i].properties.shadowIndex = pointIndex;
                }
            }

            if(dirIndex != -1 && dirIndex != shadowMapArray.dirIndex)
            {
                shadowMapArray.dirIndex = dirIndex;
                shadowMapArray.CreateResizeDirArray();
            }

            if(pointIndex != -1 && pointIndex != shadowMapArray.pointIndex)
            {
                shadowMapArray.pointIndex = pointIndex * 6;
                shadowMapArray.CreateResizePointArray();
            }

            if (justCreated)
                Engine.CreateGlobalShadowFramebuffer();
        }

        public abstract void BindForWriting(ShadowType type);
        public static void BindForReading(int shaderProgramId)
        {
            var smallShadowMapsLoc = GL.GetUniformLocation(shaderProgramId, "smallShadowMaps");
            if (smallShadowMapsLoc != -1)
            {
                GL.ActiveTexture(TextureUnit.Texture0 + Engine.shadowMapArray.smallShadowMapArrayUnit);
                GL.BindTexture(TextureTarget.Texture2DArray, Engine.shadowMapArray.smallShadowMapArrayId);
                GL.Uniform1(smallShadowMapsLoc, 0); // TextureUnit 0 for smallShadowMapArray
            }

            var mediumShadowMapsLoc = GL.GetUniformLocation(shaderProgramId, "mediumShadowMaps");
            if (mediumShadowMapsLoc != -1)
            {
                GL.ActiveTexture(TextureUnit.Texture0 + Engine.shadowMapArray.mediumShadowMapArrayUnit);
                GL.BindTexture(TextureTarget.Texture2DArray, Engine.shadowMapArray.mediumShadowMapArrayId);
                GL.Uniform1(mediumShadowMapsLoc, 1); // TextureUnit 0 for mediumShadowMapArrayId
            }

            var largeShadowMapsLoc = GL.GetUniformLocation(shaderProgramId, "largeShadowMaps");
            if (largeShadowMapsLoc != -1)
            {
                GL.ActiveTexture(TextureUnit.Texture0 + Engine.shadowMapArray.largeShadowMapArrayUnit);
                GL.BindTexture(TextureTarget.Texture2DArray, Engine.shadowMapArray.largeShadowMapArrayId);
                GL.Uniform1(largeShadowMapsLoc, 2); // TextureUnit 0 for largeShadowMapArrayId
            }

            var faceShadowMapsLoc = GL.GetUniformLocation(shaderProgramId, "faceShadowMaps");
            if (faceShadowMapsLoc != -1)
            {
                GL.ActiveTexture(TextureUnit.Texture0 + Engine.shadowMapArray.faceShadowMapArrayUnit);
                GL.BindTexture(TextureTarget.Texture2DArray, Engine.shadowMapArray.faceShadowMapArrayId);
                GL.Uniform1(faceShadowMapsLoc, 3); // TextureUnit 0 for faceShadowMapArrayId
            }
            //if (Engine.shadowMapArray.smallShadowMapArrayId != -1)
            //{
            //    GL.ActiveTexture(TextureUnit.Texture0 + Engine.shadowMapArray.smallShadowMapArrayUnit);
            //    GL.BindTexture(TextureTarget.Texture2DArray, Engine.shadowMapArray.smallShadowMapArrayId);
            //}
            //if (Engine.shadowMapArray.mediumShadowMapArrayId != -1)
            //{
            //    GL.ActiveTexture(TextureUnit.Texture0 + Engine.shadowMapArray.mediumShadowMapArrayUnit);
            //    GL.BindTexture(TextureTarget.Texture2DArray, Engine.shadowMapArray.mediumShadowMapArrayId);
            //}
            //if (Engine.shadowMapArray.largeShadowMapArrayId != -1)
            //{
            //    GL.ActiveTexture(TextureUnit.Texture0 + Engine.shadowMapArray.largeShadowMapArrayUnit);
            //    GL.BindTexture(TextureTarget.Texture2DArray, Engine.shadowMapArray.largeShadowMapArrayId);
            //}
            //if (Engine.shadowMapArray.faceShadowMapArrayId != -1)
            //{
            //    GL.ActiveTexture(TextureUnit.Texture0 + Engine.shadowMapArray.faceShadowMapArrayUnit);
            //    GL.BindTexture(TextureTarget.Texture2DArray, Engine.shadowMapArray.faceShadowMapArrayId);
            //}
        }

        public abstract void InitShadows();

        public abstract void RecalculateShadows();
        public abstract Matrix4 GetLightViewMatrix(ShadowType type = default);
        public abstract Matrix4 GetLightViewMatrixForFrustum(ShadowType type = default);
        public abstract Matrix4 GetProjectionMatrix(ShadowType type);
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
