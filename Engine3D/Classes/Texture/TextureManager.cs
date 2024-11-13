using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Drawing;
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

        public Texture? AddTexture(string path, out bool success, bool flipY = true, string textureFilter = "linear")
        {
            if (textures.ContainsKey(Path.GetFileName(path)))
            {
                success = true;
                return textures[Path.GetFileName(path)];
            }

            try
            {
                Texture texture = new Texture(textureCount, path, out bool successTexture, flipY, textureFilter);
                if(!successTexture)
                {
                    texture.Delete();
                    success = false;
                    // Console log: texture fail
                    return null;
                }

                textures.Add(Path.GetFileName(path), texture);
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

        public void AddUITexture(string path, out bool success, bool flipY = true, string textureFilter = "linear", AssetTypeEditor type = AssetTypeEditor.UI)
        {
            if (textures.ContainsKey("ui_" + Path.GetFileName(path)))
            {
                success = true;
                return;
            }

            try
            {
                Texture texture = new Texture(textureCount, path, out bool successTexture, flipY, textureFilter, type);
                if (!successTexture)
                {
                    texture.Delete();
                    success = false;
                    // Console log: texture fail
                    return;
                }

                textures.Add("ui_" + Path.GetFileName(path), texture);
                textureCount++;
                success = true;
            }
            catch
            {
                success = false;
            }
        }

        public int GetTextureUnit()
        {
            var unit = textureCount;
            textureCount++;
            return unit;
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
            GL.UseProgram(mesh.shaderProgramId);

            string use = "use";
            if (textureType == "")
                use = "useTexture";
            mesh.RemoveTexture("textureSampler" + textureType, use);

            textures.Remove(texture.TextureName);
        }

        public void DeleteTextures()
        {
            foreach(var texture in textures.Values)
            {
                texture.Delete();
            }
        }

        public void DeleteObjectTextures()
        {
            foreach(var texture in textures.Values)
            {
                if (texture.UITexture)
                    continue;
                texture.Delete();
            }
        }
    }
}
