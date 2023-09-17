using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mario64
{
    public class Shader
    {
        public int id;

        private int vertexShader;
        private int fragmentShader;

        private string? embeddedVertexShaderName;
        private string? embeddedFragmentShaderName;

        public Shader() { }

        public Shader(string embeddedVertexShaderName, string embeddedFragmentShaderName)
        {
            this.embeddedVertexShaderName = embeddedVertexShaderName;
            this.embeddedFragmentShaderName = embeddedFragmentShaderName;

            id = GL.CreateProgram();

            vertexShader = GL.CreateShader(ShaderType.VertexShader);
            // add the source code from "Default.vert" in the Shaders file
            GL.ShaderSource(vertexShader, LoadShaderSource(embeddedVertexShaderName));
            // Compile the Shader
            GL.CompileShader(vertexShader);

            // Same as vertex shader
            fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(fragmentShader, LoadShaderSource(embeddedFragmentShaderName));
            GL.CompileShader(fragmentShader);

            // Attach the shaders to the shader program
            GL.AttachShader(id, vertexShader);
            GL.AttachShader(id, fragmentShader);

            // Link the program to OpenGL
            GL.LinkProgram(id);
            GL.UseProgram(id); // bind vao
        }

        public void Use()
        {
            GL.UseProgram(id); // bind vao
        }

        public void Unload()
        {
            GL.DeleteProgram(vertexShader);
            GL.DeleteProgram(fragmentShader);
            GL.DeleteProgram(id);
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
