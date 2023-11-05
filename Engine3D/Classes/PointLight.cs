using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Platform.Windows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine3D
{

    public class PointLight
    {
        public string name = "";
        public bool isSelected = false;

        public int positionLoc;

        private Vector3 _position;
        public Vector3 Position
        {
            set
            {
                _position = value;
                if(mesh != null)
                    mesh.Position = value;
            }
            get
            {
                return _position;
            }
        }

        public int colorLoc;
        public Color4 color;

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

        public NoTextureMesh mesh;

        public PointLight(Vector3 pos, Color4 color, int vaoId, int shaderProgramId, ref Camera camera, VAO meshVao, VBO meshVbo, int meshShaderProgramId, int i, ref Object parentObject)
        {
            Position = pos;
            this.color = color;

            ambient = new Vector3(color.R * ambientS, color.G * ambientS, color.B * ambientS);
            diffuse = new Vector3(color.R, color.G, color.B);
            specular = new Vector3(color.R, color.G, color.B);

            constant = 1.0f;
            //linear = 0.09f;
            //linear = 0.05f;
            linear = 0.01f;
            quadratic = 0.032f;

            positionLoc = GL.GetUniformLocation(shaderProgramId, "pointLights[" + i + "].position");
            colorLoc = GL.GetUniformLocation(shaderProgramId, "pointLights[" + i + "].color");
             
            ambientLoc = GL.GetUniformLocation(shaderProgramId, "pointLights[" + i + "].ambient");
            diffuseLoc = GL.GetUniformLocation(shaderProgramId, "pointLights[" + i + "].diffuse");
            specularLoc = GL.GetUniformLocation(shaderProgramId, "pointLights[" + i + "].specular");
                                                                        
            specularPowLoc = GL.GetUniformLocation(shaderProgramId, "pointLights[" + i + "].specularPow");
            constantLoc = GL.GetUniformLocation(shaderProgramId, "pointLights[" + i + "].constant");
            linearLoc = GL.GetUniformLocation(shaderProgramId, "pointLights[" + i + "].linear");
            quadraticLoc = GL.GetUniformLocation(shaderProgramId, "pointLights[" + i + "].quadratic");

            mesh = GetMesh(this, meshVao, meshVbo, meshShaderProgramId, ref camera, ref parentObject);
        }

        public static PointLight[] GetPointLights(ref List<PointLight> lights)
        {
            PointLight[] pl = new PointLight[lights.Count];
            for (int i = 0; i < lights.Count; i++)
            {
                pl[i] = lights[i];
            }
            return pl;
        }

        public static NoTextureMesh GetMesh(PointLight pointLight, VAO vao, VBO vbo, int shaderProgramId, ref Camera camera, ref Object parentObject)
        {
            NoTextureMesh mesh = new NoTextureMesh(vao, vbo, shaderProgramId, "sphereSmall.obj", ref camera, pointLight.color, ref parentObject);
            mesh.Position = pointLight.Position;
            mesh.Scale = new Vector3(0.5f,0.5f,0.5f);

            return mesh;
        }

        public static void SendToGPU(ref List<PointLight> pointLights, int shaderProgramId, GameState gameRunning)
        {
            if (gameRunning == GameState.Stopped)
            {
                GL.Uniform1(GL.GetUniformLocation(shaderProgramId, "actualNumOfLights"), 0);
                return;
            }

            GL.Uniform1(GL.GetUniformLocation(shaderProgramId, "actualNumOfLights"), pointLights.Count);

            for (int i = 0; i < pointLights.Count; i++)
            {
                Vector3 c = new Vector3(pointLights[i].color.R, pointLights[i].color.G, pointLights[i].color.B);
                GL.Uniform3(pointLights[i].positionLoc, pointLights[i].Position);
                GL.Uniform3(pointLights[i].colorLoc, c);

                GL.Uniform3(pointLights[i].ambientLoc, pointLights[i].ambient);
                GL.Uniform3(pointLights[i].diffuseLoc, pointLights[i].diffuse);
                GL.Uniform3(pointLights[i].specularLoc, pointLights[i].specular);
                            
                GL.Uniform1(pointLights[i].specularPowLoc, pointLights[i].specularPow);
                GL.Uniform1(pointLights[i].constantLoc, pointLights[i].constant);
                GL.Uniform1(pointLights[i].linearLoc, pointLights[i].linear);

                //GL.Uniform1(GL.GetUniformLocation(shaderProgramId, "pointLights[" + i + "].quadratic"), pointLights[i].quadratic);
                //GL.Uniform3(GL.GetUniformLocation(shaderProgramId, "pointLights[" + i + "].position"), pointLights[i].position);
                //GL.Uniform3(GL.GetUniformLocation(shaderProgramId, "pointLights[" + i + "].color"), c);

                //GL.Uniform3(GL.GetUniformLocation(shaderProgramId, "pointLights[" + i + "].ambient"), pointLights[i].ambient);
                //GL.Uniform3(GL.GetUniformLocation(shaderProgramId, "pointLights[" + i + "].diffuse"), pointLights[i].diffuse);
                //GL.Uniform3(GL.GetUniformLocation(shaderProgramId, "pointLights[" + i + "].specular"), pointLights[i].specular);

                //GL.Uniform1(GL.GetUniformLocation(shaderProgramId, "pointLights[" + i + "].specularPow"), pointLights[i].specularPow);
                //GL.Uniform1(GL.GetUniformLocation(shaderProgramId, "pointLights[" + i + "].constant"), pointLights[i].constant);
                //GL.Uniform1(GL.GetUniformLocation(shaderProgramId, "pointLights[" + i + "].linear"), pointLights[i].linear);
                //GL.Uniform1(GL.GetUniformLocation(shaderProgramId, "pointLights[" + i + "].quadratic"), pointLights[i].quadratic);
            }
        }

        public static Matrix4 GetDirLightSpaceMatrix()
        {
            float near = 1.0f;
            float far = 7.5f;
            Matrix4 lightProjection = Matrix4.CreateOrthographic(-10.0f, 10.0f, near, far);
            Matrix4 lightView = Matrix4.LookAt(new Vector3(0.0f, 10.0f, 0.0f),
                                               new Vector3(0.0f, -1.0f, 0.0f),
                                               new Vector3(0.0f, 1.0f, 0.0f));

            Matrix4 lightSpaceMatrix = lightProjection * lightView;
            return lightSpaceMatrix;
        }
    }
}
