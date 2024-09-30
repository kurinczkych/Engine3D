using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine3D
{
    public class DirectionalLight : Object
    {
        public Vector3 direction;
        public Color4 color;
        
        public Vector3 ambient;
        public Vector3 diffuse;
        public Vector3 specular;
        public float specularPow;

        private Dictionary<string, int> uniforms = new Dictionary<string, int>();
        private int shaderProgramId;
        private int index;

        public DirectionalLight(int shaderProgramId, int index, Vector3 direction) : base(ObjectType.DirectionalLight)
        {
            name = "Directional Light";

            this.shaderProgramId = shaderProgramId;
            this.index = index;

            this.direction = direction;
            color = new Color4(1.0f, 1.0f, 1.0f, 1.0f);

            ambient = new Vector3(0.1f,0.1f,0.1f);
            diffuse = new Vector3(1.0f,1.0f,1.0f);
            specular = new Vector3(1.0f,1.0f,1.0f);
            specularPow = 2.0f;

            CreateUniforms();
        }

        public DirectionalLight(int shaderProgramId, int index, Vector3 direction, Color4 color) : base(ObjectType.DirectionalLight)
        {
            name = "Directional Light";

            this.shaderProgramId = shaderProgramId;
            this.index = index;

            this.direction = direction;
            this.color = color;

            ambient = new Vector3(0.1f,0.1f,0.1f);
            diffuse = new Vector3(1.0f,1.0f,1.0f);
            specular = new Vector3(1.0f,1.0f,1.0f);
            specularPow = 2.0f;

            CreateUniforms();
        }

        public DirectionalLight(int shaderProgramId, int index, Vector3 direction, Color4 color, 
                                Vector3 ambient, Vector3 diffuse, Vector3 specular, float specularPow) : base(ObjectType.DirectionalLight)
        {
            name = "Directional Light";

            this.shaderProgramId = shaderProgramId;
            this.index = index;

            this.direction = direction;
            this.color = color;

            this.ambient = ambient;
            this.diffuse = diffuse;
            this.specular = specular;
            this.specularPow = specularPow;

            CreateUniforms();
        }

        private void CreateUniforms()
        {
            uniforms.Add("directionLoc", GL.GetUniformLocation(shaderProgramId, "dirLights[" + index + "].direction"));
            uniforms.Add("colorLoc", GL.GetUniformLocation(shaderProgramId, "dirLights[" + index + "].color"));

            uniforms.Add("ambientLoc", GL.GetUniformLocation(shaderProgramId, "dirLights[" + index + "].ambient"));
            uniforms.Add("diffuseLoc", GL.GetUniformLocation(shaderProgramId, "dirLights[" + index + "].diffuse"));
            uniforms.Add("specularLoc", GL.GetUniformLocation(shaderProgramId, "dirLights[" + index + "].specular"));
            uniforms.Add("specularPowLoc", GL.GetUniformLocation(shaderProgramId, "dirLights[" + index + "].specularPow"));
        }

        public static void SendToGPU(List<DirectionalLight> dirLights, int shaderProgramId)
        {
            GL.Uniform1(GL.GetUniformLocation(shaderProgramId, "actualNumOfDirLights"), dirLights.Count);

            for (int i = 0; i < dirLights.Count; i++)
            {
                Vector3 c = new Vector3(dirLights[i].color.R, dirLights[i].color.G, dirLights[i].color.B);
                GL.Uniform3(dirLights[i].uniforms["directionLoc"], dirLights[i].direction);
                GL.Uniform3(dirLights[i].uniforms["colorLoc"], c);

                GL.Uniform3(dirLights[i].uniforms["ambientLoc"], dirLights[i].ambient);
                GL.Uniform3(dirLights[i].uniforms["diffuseLoc"], dirLights[i].diffuse);
                GL.Uniform3(dirLights[i].uniforms["specularLoc"], dirLights[i].specular);
                GL.Uniform1(dirLights[i].uniforms["specularPowLoc"], dirLights[i].specularPow);
            }
        }
    }
}
