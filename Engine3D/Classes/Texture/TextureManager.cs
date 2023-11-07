using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine3D
{
    public class TextureManager
    {
        public Dictionary<string, Texture> textures = new Dictionary<string, Texture>();
        public static int textureCount = 0;

        public TextureManager() { }

        public Texture? AddTexture(string name, out bool success, bool flipY = true, string textureFilter = "linear")
        {
            if (textures.ContainsKey(name))
            {
                success = true;
                return textures[name];
            }

            try
            {
                Texture texture = new Texture(textureCount, name, flipY, textureFilter);
                textures.Add(name, texture);
                textureCount++;
                success = true;
                return texture;
            }
            catch
            {
                success = false;
            }

            return null;
        }

        public void AddUITexture(string name, out bool success, bool flipY = true, string textureFilter = "linear", AssetTypeEditor type = AssetTypeEditor.UI)
        {
            if (textures.ContainsKey("ui_" + name))
            {
                success = true;
                return;
            }

            try
            {
                Texture texture = new Texture(textureCount, name, flipY, textureFilter, type);
                textures.Add("ui_" + name, texture);
                textureCount++;
                success = true;
            }
            catch
            {
                success = false;
            }
        }

        public void DeleteTexture(Texture texture)
        {
            try { texture.Delete(); }
            catch { }
            textures.Remove(texture.TextureName);
        }

        public void DeleteTexture(string name)
        {
            if (textures.ContainsKey(name))
            {
                try { textures[name].Delete(); }
                catch { }
                textures.Remove(textures[name].TextureName);
            }
        }

        public void DeleteTexture(Texture texture, BaseMesh mesh, string textureType)
        {
            //GL.UseProgram(mesh.shaderProgramId);

            //string use = "use";
            //if (textureType == "")
            //    use = "useTexture";
            //mesh.RemoveTexture("textureSampler" + textureType, use);

            //textures.Remove(texture.TextureName);
        }

        public void DeleteTextures()
        {
            foreach(var texture in textures.Values)
            {
                texture.Delete();
            }
        }
    }
}
