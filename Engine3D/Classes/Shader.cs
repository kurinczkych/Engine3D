using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine3D
{
    
    public class Shader
    {
        public int id;

        private List<int> shaderIds = new List<int>();

        public List<string> shaderNames = new List<string>();

        public Shader() { }

        public Shader(List<string> shaders)
        {
            id = GL.CreateProgram();
            shaderNames = new List<string>(shaders);

            for(int i = 0; i < shaderNames.Count(); i++)
            {
                int shader = GL.CreateShader(GetShaderType(shaderNames[i]));
                GL.ShaderSource(shader, LoadShaderSource(shaderNames[i]));
                GL.CompileShader(shader);
                shaderIds.Add(shader);
            }

            for (int i = 0; i < shaderIds.Count(); i++)
                GL.AttachShader(id, shaderIds[i]);

            GL.LinkProgram(id);
            GL.UseProgram(id);
        }

        public void Use()
        {
            GL.UseProgram(id); // bind vao
        }

        public void Unload()
        {
            for(int i = 0;i < shaderIds.Count();i++)
                GL.DeleteProgram(shaderIds[i]);
            GL.DeleteProgram(id);
        }

        private ShaderType GetShaderType(string shaderName)
        {
            string ext = Path.GetExtension(shaderName);
            switch (ext)
            {
                case ".vert":
                    return ShaderType.VertexShader;
                case ".geom":
                    return ShaderType.GeometryShader;
                case ".frag":
                    return ShaderType.FragmentShader;
                case ".comp":
                    return ShaderType.ComputeShader;
                default:
                    return ShaderType.VertexShader;
            }
        }

        public string LoadShaderSource(string filePath)
        {
            string shaderSource = "";

            try
            {
                using (StreamReader reader = new StreamReader("../../../Shaders/" + filePath))
                {
                    shaderSource = reader.ReadToEnd();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to load shader source file: " + e.Message);
            }

            return shaderSource;
        }
    }
}
