using Newtonsoft.Json;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Drawing.Text;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine3D
{
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

        private LightType lightType = LightType.DirectionalLight;
        [JsonIgnore]
        public Object parentObject;
        [JsonIgnore]
        private Dictionary<string, int> uniforms;

        public Light()
        {
            
        }

        public Light(Object parentObject, int shaderProgramId, int id)
        {
            this.parentObject = parentObject;
            this.shaderProgramId = shaderProgramId;
            this.id = id;

            SetLightType(LightType.DirectionalLight);
        }

        public Light(Object parentObject, int shaderProgramId, int id, LightType lightType)
        {
            this.parentObject = parentObject;
            this.shaderProgramId = shaderProgramId;
            this.id = id;

            this.lightType = lightType;
            SetLightType(lightType);
        }

        private void GetUniformLocations()
        {
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
            }

            GetUniformLocations();
        }

        public static void SendToGPU(List<Light> lights, int shaderProgramId)
        {
            GL.Uniform1(GL.GetUniformLocation(shaderProgramId, "actualNumOfLights"), lights.Count);

            for (int i = 0; i < lights.Count; i++)
            {
                if (lights[i].uniforms == null || lights[i].uniforms.Count == 0)
                    lights[i].GetUniformLocations();

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
                    Vector3 rot = QuaternionToDirection(lights[i].parentObject.transformation.Rotation);
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

            return new float[] {constantStat, linearStat, quadraticStat};
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

        public static Vector3 QuaternionToDirection(Quaternion rot)
        {
            Vector3 baseDirection = new Vector3(0, -1, 0);

            Vector3 sunDirection = Vector3.Transform(baseDirection, rot);

            sunDirection = Vector3.Normalize(sunDirection);

            return sunDirection;
        }

        #region Getters/Setters
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
