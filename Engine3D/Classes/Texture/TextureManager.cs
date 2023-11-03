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
        private int textureCount = 0;

        public TextureManager() { }

        public Texture AddTexture(string name, bool flipY = true, string textureFilter = "linear")
        {
            if (textures.ContainsKey(name))
                return textures[name];

            Texture texture = new Texture(textureCount, name, flipY, textureFilter);
            textures.Add(name, texture);
            textureCount++;

            return texture;
        }

        private void AddUITexture(string name, bool flipY = true, string textureFilter = "linear")
        {
            if (textures.ContainsKey("ui_" + name))
                return;

            Texture texture = new Texture(textureCount, name, flipY, textureFilter);
            textures.Add("ui_" + name, texture);
            textureCount++;
        }

        public void GetAssetTextures(EditorData editorData)
        {
            editorData.AssetTextures = editorData.assets.Where(x => x.Type == AssetType.Texture).ToList();

            string name = editorData.AssetTextures[editorData.currentAssetTexture].Name;
            if (!textures.ContainsKey("ui_"+name))
                AddUITexture(name, flipY: false);

            editorData.currentAssetTexture++;
            textureCount++;
        }

        public void GetAssetTextureIfNeeded(ref EditorData editorData)
        {
            if (editorData.currentAssetTexture >= editorData.AssetTextures.Count())
                return;

            string name = editorData.AssetTextures[editorData.currentAssetTexture].Name;
            if (!textures.ContainsKey("ui_"+name))
                AddUITexture(name, flipY: false);

            editorData.currentAssetTexture++;
            textureCount++;
        }

        public void DeleteTexture(Texture texture)
        {
            texture.Delete();
            textures.Remove(texture.TextureName);
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
