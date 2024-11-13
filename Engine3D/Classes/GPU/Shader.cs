using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static Engine3D.Shader;

namespace Engine3D
{
    public class Shader
    {
        public class ShaderResource
        {
            public int id;
            public string name;
            public int path;

            public ShaderResource(int shaderId, string shaderName, int shaderPath)
            {
                id = shaderId;
                name = shaderName;
                path = shaderPath;
            }

            public ShaderResource()
            {
                
            }
        }

        private bool shadersLoaded = false;
        private Dictionary<string, string> shaderPaths = new Dictionary<string, string>(); 

        public int programId;
        private string folder;

        private List<ShaderResource> shaders = new List<ShaderResource>();

        public Shader() { }

        public Shader(List<string> shaderNames, string folder = "")
        {
            programId = GL.CreateProgram();

            if (folder == "")
                this.folder = Path.GetFileNameWithoutExtension(shaderNames[0]);
            else
                this.folder = folder;

            for(int i = 0; i < shaderNames.Count(); i++)
            {
                ShaderResource ss = new ShaderResource();
                ss.name = shaderNames[i];

                ss.id = GL.CreateShader(GetShaderType(ss.name));
                GL.ShaderSource(ss.id, LoadShaderSource(ss.name));
                GL.CompileShader(ss.id);

                GL.GetShader(ss.id, ShaderParameter.CompileStatus, out int vertexCompiled);
                if (vertexCompiled == 0)
                {
                    string infoLog = GL.GetShaderInfoLog(ss.id);
                    Engine.consoleManager.AddLog($"{ss.name} - Shader Compile Error: {infoLog}", LogType.Error);
                }

                GL.AttachShader(programId, ss.id);

                shaders.Add(ss);
            }

            GL.LinkProgram(programId);

            GL.GetProgram(programId, GetProgramParameterName.LinkStatus, out int linked);
            if (linked == 0)
            {
                string infoLog = GL.GetProgramInfoLog(programId);
                Engine.consoleManager.AddLog($"Shader Program Link Error: {infoLog}", LogType.Error);
            }

            Use();
            shadersLoaded = true;
        }

        public void Reload()
        {
            foreach(var ss in shaders)
            {
                GL.DetachShader(programId, ss.id);
                GL.DeleteShader(ss.id);

                ss.id = GL.CreateShader(GetShaderType(ss.name));
                string code = LoadShaderSource(ss.name);
                if (code == "")
                    Engine.consoleManager.AddLog(ss.name + " file returned empty!", LogType.Error);
                GL.ShaderSource(ss.id, code);
                GL.CompileShader(ss.id);

                GL.GetShader(ss.id, ShaderParameter.CompileStatus, out int vertexCompiled);
                if (vertexCompiled == 0)
                {
                    string infoLog = GL.GetShaderInfoLog(ss.id);
                    Engine.consoleManager.AddLog($"{ss.name} - Shader Compile Error: {infoLog}", LogType.Error);
                }

                GL.AttachShader(programId, ss.id);
            }

            GL.LinkProgram(programId);

            GL.GetProgram(programId, GetProgramParameterName.LinkStatus, out int linked);
            if (linked == 0)
            {
                string infoLog = GL.GetProgramInfoLog(programId);
                Engine.consoleManager.AddLog($"Shader Program Link Error: {infoLog}", LogType.Error);
            }
        }

        public void Use()
        {
            if (Engine.GLState.currentShaderId != programId)
            {
                GL.UseProgram(programId);
                Engine.GLState.currentShaderId = programId;
            }
        }

        public void Unload()
        {
            Engine.GLState.currentShaderId = -1;

            for (int i = 0;i < shaders.Count();i++)
                GL.DeleteShader(shaders[i].id);
            GL.DeleteProgram(programId);
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

        private string LoadShaderSource(string shaderName)
        {
            // Get the path to the executable directory
            string exeDirectory = AppDomain.CurrentDomain.BaseDirectory;

            // Combine the executable directory with the shader path and filename
            string shaderPath = Path.Combine(exeDirectory, "Assets", "Shaders", folder, shaderName);

            // Check if the shader file exists
            if (!File.Exists(shaderPath))
            {
                Engine.consoleManager.AddLog($"Shader file '{shaderName}' not found at path '{shaderPath}'", LogType.Error);
                return "";
            }

            // Read and return the shader source code as a string
            return File.ReadAllText(shaderPath);
        }

        public static List<string> GetUniformNames(int shaderProgramId)
        {
            GL.GetProgram(shaderProgramId, GetProgramParameterName.ActiveUniforms, out int numberOfUniforms);
            GL.GetProgram(shaderProgramId, GetProgramParameterName.ActiveUniformMaxLength, out int maxNameLength);

            StringBuilder uniformNameBuffer = new StringBuilder(maxNameLength);

            // Loop over each uniform
            List<string> names = new List<string>();
            for (int i = 0; i < numberOfUniforms; i++)
            {
                GL.GetActiveUniform(shaderProgramId, i, maxNameLength, out int length, out int size, out ActiveUniformType type, out string uniformName);
                names.Add(uniformName);
            }

            return names;
        }
    }
}
