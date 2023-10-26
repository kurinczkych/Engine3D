using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL4;

namespace Engine3D
{
    public class TextureDescriptor
    {
        public string Texture;
        public int TextureId;
        public int TextureUnit;
        public string Normal;
        public int NormalId;
        public int NormalUnit;

        public int count;

        public TextureDescriptor()
        {
            Normal = "";
        }
    }

    public enum TextureType
    {
        Texture,
        Normal
    }

    public class Texture
    {
        private TextureMinFilter tminf;
        private TextureMagFilter tmagf;

        public TextureDescriptor textureDescriptor;

        public Texture(int unit, string embeddedResourceName, bool flipY = true, string textureFilter = "linear")
        {
            textureDescriptor = Helper.GetTextureDescriptor(embeddedResourceName);

            int currentUnit = unit;

            textureDescriptor.TextureId = GL.GenTexture();
            textureDescriptor.TextureUnit = currentUnit;
            currentUnit++;

            if (textureFilter == "linear")
            {
                tminf = TextureMinFilter.Linear;
                tmagf = TextureMagFilter.Linear;
            }
            else if(textureFilter == "nearest")
            {
                tminf = TextureMinFilter.Nearest;
                tmagf = TextureMagFilter.Nearest;
            }
            else
            {
                tminf = TextureMinFilter.Linear;
                tmagf = TextureMagFilter.Linear;
            }

            Bind(TextureType.Texture);
            Helper.LoadTexture(textureDescriptor.Texture, flipY, tminf, tmagf);

            Unbind();

            if(textureDescriptor.Normal != "")
            {
                textureDescriptor.NormalId = GL.GenTexture();
                textureDescriptor.NormalUnit = currentUnit;
                currentUnit++;

                Bind(TextureType.Normal);
                Helper.LoadTexture(textureDescriptor.Normal, flipY, tminf, tmagf);

                Unbind();
            }
        }

        public void Bind(TextureType tt) 
        {
            if (tt == TextureType.Texture)
            {
                GL.ActiveTexture(TextureUnit.Texture0 + textureDescriptor.TextureUnit);
                GL.BindTexture(TextureTarget.Texture2D, textureDescriptor.TextureId);
            }
            else if (tt == TextureType.Normal)
            {
                GL.ActiveTexture(TextureUnit.Texture0 + textureDescriptor.NormalUnit);
                GL.BindTexture(TextureTarget.Texture2D, textureDescriptor.NormalId);
            }
        }
        public void Unbind() { GL.BindTexture(TextureTarget.Texture2D, 0); }

        public void Delete()
        { 
            GL.DeleteTexture(textureDescriptor.TextureId);
            GL.DeleteTexture(textureDescriptor.NormalId);
        }

        ~Texture()
        {
            Delete();
        }

    }
}
