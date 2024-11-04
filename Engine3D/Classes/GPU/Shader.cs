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

        private List<ShaderResource> shaders = new List<ShaderResource>();

        public Shader() { }

        public Shader(List<string> shaderNames)
        {
            LoadShaderPaths(shaderNames);

            programId = GL.CreateProgram();

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
        }

        public void Reload()
        {
            foreach(var ss in shaders)
            {
                GL.DetachShader(programId, ss.id);

                GL.ShaderSource(ss.id, LoadShaderSource(ss.name));
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

        private void LoadShaderPaths(List<string> shaderNames)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            var list = assembly.GetManifestResourceNames().Where(x => x.StartsWith("Engine3D.Shaders"));
            foreach (string resourceName in list)
            {
                var parts = resourceName.Split('.');
                string name = parts[parts.Length-2] + "." + parts[parts.Length-1];
                if(shaderNames.Contains(name))
                    shaderPaths.Add(name, resourceName);
            }

            shadersLoaded = true;
        }

        private string LoadShaderSource(string shaderName)
        {
            string shaderSource = "";
            if (shadersLoaded && shaderPaths.ContainsKey(shaderName))
            {
                Assembly assembly = Assembly.GetExecutingAssembly();
                using (var stream = assembly.GetManifestResourceStream(shaderPaths[shaderName]))
                {
                    if(stream != null)
                    {
                        using (var reader = new StreamReader(stream))
                        {
                            shaderSource = reader.ReadToEnd();
                            return shaderSource;
                        }
                    }
                    else
                       throw new Exception("Shader not found!");
                }
            }
            else
            {
                throw new Exception("Shader not found!");
            }
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
