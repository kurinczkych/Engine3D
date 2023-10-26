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
        public int NormalUse;

        public string Height;
        public int HeightId;
        public int HeightUnit;
        public int HeightUse;

        public string AO;
        public int AOId;
        public int AOUnit;
        public int AOUse;

        public string Rough;
        public int RoughId;
        public int RoughUnit;
        public int RoughUse;

        public int count;

        public TextureDescriptor()
        {
            Normal = "";
            Height = "";
            AO = "";
            Rough = "";
        }
    }

    public enum TextureType
    {
        Texture,
        Normal,
        Height,
        AO,
        Rough
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

            if(textureDescriptor.Height != "")
            {
                textureDescriptor.HeightId = GL.GenTexture();
                textureDescriptor.HeightUnit = currentUnit;
                currentUnit++;

                Bind(TextureType.Height);
                Helper.LoadTexture(textureDescriptor.Height, flipY, tminf, tmagf);

                Unbind();
            }

            if(textureDescriptor.AO != "")
            {
                textureDescriptor.AOId = GL.GenTexture();
                textureDescriptor.AOUnit = currentUnit;
                currentUnit++;

                Bind(TextureType.AO);
                Helper.LoadTexture(textureDescriptor.AO, flipY, tminf, tmagf);

                Unbind();
            }

            if(textureDescriptor.Rough != "")
            {
                textureDescriptor.RoughId = GL.GenTexture();
                textureDescriptor.RoughUnit = currentUnit;
                currentUnit++;

                Bind(TextureType.Rough);
                Helper.LoadTexture(textureDescriptor.Rough, flipY, tminf, tmagf);

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
            else if (tt == TextureType.Height)
            {
                GL.ActiveTexture(TextureUnit.Texture0 + textureDescriptor.HeightUnit);
                GL.BindTexture(TextureTarget.Texture2D, textureDescriptor.HeightId);
            }
            else if (tt == TextureType.AO)
            {
                GL.ActiveTexture(TextureUnit.Texture0 + textureDescriptor.AOUnit);
                GL.BindTexture(TextureTarget.Texture2D, textureDescriptor.AOId);
            }
            else if (tt == TextureType.Rough)
            {
                GL.ActiveTexture(TextureUnit.Texture0 + textureDescriptor.RoughUnit);
                GL.BindTexture(TextureTarget.Texture2D, textureDescriptor.RoughId);
            }
        }
        public void Unbind() { GL.BindTexture(TextureTarget.Texture2D, 0); }

        public void Delete()
        { 
            GL.DeleteTexture(textureDescriptor.TextureId);
            GL.DeleteTexture(textureDescriptor.NormalId);
            GL.DeleteTexture(textureDescriptor.HeightId);
            GL.DeleteTexture(textureDescriptor.AOId);
            GL.DeleteTexture(textureDescriptor.RoughId);
        }

        ~Texture()
        {
            Delete();
        }

    }
}
