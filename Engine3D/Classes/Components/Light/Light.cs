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
    public enum ShadowType { Small, Medium, Large,
                             Top, Bottom, Left, Right, Front, Back}

    public abstract class Light : IComponent
    {
        public enum LightType
        {
            PointLight = 0,
            DirectionalLight = 1
        }

        public string name = "Light";
        protected int shaderProgramId;
        protected int id;

        #region LightVars
        public int colorLoc;
        protected Color4 color;

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
        protected abstract void GetUniformLocations(int spi);

        public static void SendToGPU(List<Light> lights, int shaderProgramId)
        {
            GL.Uniform1(GL.GetUniformLocation(shaderProgramId, "actualNumOfLights"), lights.Count);

            for (int i = 0; i < lights.Count; i++)
            {
                lights[i].GetUniformLocations(shaderProgramId);
                if (lights[i] is PointLight pl)
                {
                    GL.Uniform1(lights[i].uniforms["lightTypeLoc"], 0);
                    Vector3 c = new Vector3(lights[i].color.R, lights[i].color.G, lights[i].color.B);
                    GL.Uniform3(lights[i].uniforms["positionLoc"], lights[i].parentObject.transformation.Position);
                    GL.Uniform3(lights[i].uniforms["colorLoc"], c);
                    GL.Uniform3(lights[i].uniforms["ambientLoc"], lights[i].ambient);
                    GL.Uniform3(lights[i].uniforms["diffuseLoc"], lights[i].diffuse);
                    GL.Uniform3(lights[i].uniforms["specularLoc"], lights[i].specular);
                    GL.Uniform1(lights[i].uniforms["specularPowLoc"], lights[i].specularPow);
                    GL.Uniform1(lights[i].uniforms["constantLoc"], pl.constant);
                    GL.Uniform1(lights[i].uniforms["linearLoc"], pl.linear);
                }
                else if (lights[i] is DirectionalLight)
                {
                    GL.Uniform1(lights[i].uniforms["lightTypeLoc"], 1);
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
        public abstract Matrix4 GetLightViewMatrix();
        public abstract Matrix4 GetLightViewMatrixForFrustum();
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
