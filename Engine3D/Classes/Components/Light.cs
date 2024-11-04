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
        public Projection projection = Projection.DefaultShadow;
        public Matrix4 projectionMatrixOrtho = Matrix4.Identity;
        public Vector3 target = Vector3.Zero;
        public float distanceFromScene = 50;
        public Vector3? calculatedDir;
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

            this.lightType = lightType;
            SetLightType(lightType);
        }

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

        public void RecalculateGizmos()
        {
            projectionMatrixOrtho = GetProjectionMatrixOrtho();

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

            // Combine the projection and view matrices to transform corners from NDC to world space
            Matrix4 invCombinedMatrix = Matrix4.Invert(projectionMatrixOrtho * GetLightViewMatrix());

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

            if (!gizmos.ContainsKey("frustumGizmo"))
            {
                gizmos.Add("frustumGizmo", new Gizmo(wireVao, wireVbo, wireShaderId, windowSize, ref camera, ref parentObject));
                gizmos["frustumGizmo"].AddFrustumGizmo(frustum, Color4.Red);
                gizmos["frustumGizmo"].recalculate = true;
                gizmos["frustumGizmo"].RecalculateModelMatrix(new bool[] { true, false, false });
            }
            else
            {
                gizmos["frustumGizmo"].model.meshes.Clear();
                gizmos["frustumGizmo"].AddFrustumGizmo(frustum, Color4.Red);
                gizmos["frustumGizmo"].recalculate = true;
                gizmos["frustumGizmo"].RecalculateModelMatrix(new bool[] { true, false, false });
            }
            if (!gizmos.ContainsKey("positionGizmo"))
            {
                gizmos.Add("positionGizmo", new Gizmo(wireVao, wireVbo, wireShaderId, windowSize, ref camera, ref parentObject));
                gizmos["positionGizmo"].AddSphereGizmo(20, Color4.Green, lightPosition);
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
                gizmos["targetGizmo"].AddSphereGizmo(20, Color4.Red, target);
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

        //-----------------
        public static Vector3 NormalizeAngles2(Vector3 angles)
        {
            Vector3 n = new Vector3(angles);
            if (n.X < 0) n.X += 360;
            if (n.Y < 0) n.Y += 360;
            if (n.Z < 0) n.Z += 360;
            return n;
        }
        public static Vector3 todeg(Vector3 d)
        {
            return new Vector3(MathHelper.RadiansToDegrees(d.X), MathHelper.RadiansToDegrees(d.Y), MathHelper.RadiansToDegrees(d.Z));
        }

        public Vector3 NormalizeAngles(Vector3 angles)
        {
            angles.X = (angles.X > 180) ? angles.X - 360 : angles.X;
            angles.Y = (angles.Y > 180) ? angles.Y - 360 : angles.Y;
            angles.Z = (angles.Z > 180) ? angles.Z - 360 : angles.Z;
            return angles;
        }

        //--------------

        public Matrix4 GetLightViewMatrix()
        {
            Vector3 lightPosition = target - (GetDirection() * distanceFromScene);

            return Matrix4.LookAt(lightPosition, target, Vector3.UnitY);
        }

        public Matrix4 GetProjectionMatrixOrtho()
        {
            float l = projection.left;
            float r = projection.right;
            float t = projection.top;
            float b = projection.bottom;
            float n = projection.near;
            float f = projection.far;

            Matrix4 m = Matrix4.CreateOrthographic(r - l, t - b, n, f);

            return m;
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

        public static Vector3 GetLightDirectionFromEuler(Quaternion rotation)
        {
            // Assuming the forward direction is along the negative Z-axis
            Vector3 forward = new Vector3(0, 0, -1);

            // Rotate the forward vector by the quaternion to get the light direction
            Vector3 lightDirection = Vector3.Transform(forward, rotation);
            lightDirection.X = (float)Math.Round(lightDirection.X,3);
            lightDirection.Y = (float)Math.Round(lightDirection.Y,3);
            lightDirection.Z = (float)Math.Round(lightDirection.Z,3);

            return lightDirection.Normalized();  // Send normalized light direction
        }


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
