using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Engine3D
{
    
    public class Shader
    {
        private bool shadersLoaded = false;
        private Dictionary<string, string> shaderPaths = new Dictionary<string, string>(); 

        public int id;

        private List<int> shaderIds = new List<int>();
        public List<string> shaderNames = new List<string>();

        public Shader() { LoadShaderPaths(); }

        public Shader(List<string> shaders)
        {
            LoadShaderPaths();

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

            //if (shaderNames[0].Contains("infinite"))
            //    ;

            //int linkStatus;
            //GL.GetProgram(id, GetProgramParameterName.LinkStatus, out linkStatus);
            //if (linkStatus == 0)
            //{
            //    // Program linking failed, get the info log for details
            //    string programInfoLog = GL.GetProgramInfoLog(id);
            //    Console.WriteLine("Shader program linking failed: " + programInfoLog);
            //}
            //;

            Use();

            
        }

        public void Use()
        {
            if (Engine.GLState.currentShaderId != id)
            {
                GL.UseProgram(id);
                Engine.GLState.currentShaderId = id;
            }
        }

        public void Unload()
        {
            Engine.GLState.currentShaderId = -1;

            for (int i = 0;i < shaderIds.Count();i++)
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

        private void LoadShaderPaths()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            var list = assembly.GetManifestResourceNames().Where(x => x.StartsWith("Engine3D.Shaders"));
            foreach (string resourceName in list)
            {
                var parts = resourceName.Split('.');
                string name = parts[parts.Length-2] + "." + parts[parts.Length-1];
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
