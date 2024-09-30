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

    public class PointLight : Object
    {
        public string name = "";

        public bool isSelected { get; set; }
        public int positionLoc;

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

        public PointLight(Color4 color, int shaderProgramId, int i) : base(ObjectType.PointLight)
        {
            name = "Point Light";

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

        public static void SendToGPU(List<PointLight> pointLights, int shaderProgramId, GameState gameRunning)
        {
            if (gameRunning == GameState.Stopped)
            {
                GL.Uniform1(GL.GetUniformLocation(shaderProgramId, "actualNumOfPointLights"), 0);
                return;
            }

            GL.Uniform1(GL.GetUniformLocation(shaderProgramId, "actualNumOfPointLights"), pointLights.Count);

            for (int i = 0; i < pointLights.Count; i++)
            {
                Vector3 c = new Vector3(pointLights[i].color.R, pointLights[i].color.G, pointLights[i].color.B);
                GL.Uniform3(pointLights[i].positionLoc, pointLights[i].transformation.Position);
                GL.Uniform3(pointLights[i].colorLoc, c);

                GL.Uniform3(pointLights[i].ambientLoc, pointLights[i].ambient);
                GL.Uniform3(pointLights[i].diffuseLoc, pointLights[i].diffuse);
                GL.Uniform3(pointLights[i].specularLoc, pointLights[i].specular);
                            
                GL.Uniform1(pointLights[i].specularPowLoc, pointLights[i].specularPow);
                GL.Uniform1(pointLights[i].constantLoc, pointLights[i].constant);
                GL.Uniform1(pointLights[i].linearLoc, pointLights[i].linear);
            }
        }
    }
}
