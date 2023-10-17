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
    public class Texture
    {
        public int id;
        public int unit;

        private TextureMinFilter tminf;
        private TextureMagFilter tmagf;

        public Texture(int unit, string embeddedResourceName, bool flipY = true, string textureFilter = "linear")
        {
            id = GL.GenTexture();
            this.unit = unit;

            if(textureFilter == "linear")
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

            Bind();
            Helper.LoadTexture(embeddedResourceName, flipY, tminf, tmagf);

            Unbind();
        }

        public void Bind() 
        {
            GL.ActiveTexture(TextureUnit.Texture0 + unit);
            GL.BindTexture(TextureTarget.Texture2D, id); 
        }
        public void Unbind() { GL.BindTexture(TextureTarget.Texture2D, 0); }
        public void Delete() { GL.DeleteTexture(id); }

    }
}
