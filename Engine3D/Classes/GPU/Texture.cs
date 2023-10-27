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
            textureDescriptor = GetTextureDescriptor(embeddedResourceName);

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
            LoadTexture(textureDescriptor.Texture, flipY, tminf, tmagf);

            Unbind();

            if(textureDescriptor.Normal != "")
            {
                textureDescriptor.NormalId = GL.GenTexture();
                textureDescriptor.NormalUnit = currentUnit;
                currentUnit++;

                Bind(TextureType.Normal);
                LoadTexture(textureDescriptor.Normal, flipY, tminf, tmagf);

                Unbind();
            }

            if(textureDescriptor.Height != "")
            {
                textureDescriptor.HeightId = GL.GenTexture();
                textureDescriptor.HeightUnit = currentUnit;
                currentUnit++;

                Bind(TextureType.Height);
                LoadTexture(textureDescriptor.Height, flipY, tminf, tmagf);

                Unbind();
            }

            if(textureDescriptor.AO != "")
            {
                textureDescriptor.AOId = GL.GenTexture();
                textureDescriptor.AOUnit = currentUnit;
                currentUnit++;

                Bind(TextureType.AO);
                LoadTexture(textureDescriptor.AO, flipY, tminf, tmagf);

                Unbind();
            }

            if(textureDescriptor.Rough != "")
            {
                textureDescriptor.RoughId = GL.GenTexture();
                textureDescriptor.RoughUnit = currentUnit;
                currentUnit++;

                Bind(TextureType.Rough);
                LoadTexture(textureDescriptor.Rough, flipY, tminf, tmagf);

                Unbind();
            }
        }

        public static TextureDescriptor GetTextureDescriptor(string fileName)
        {
            string withoutExtension = Path.GetFileNameWithoutExtension(fileName);
            string extension = Path.GetExtension(fileName);

            TextureDescriptor td = new TextureDescriptor();
            td.count = 1;
            string t = FileManager.GetFilePath(withoutExtension + "_t" + extension, FileType.Textures);
            if (t == "")
            {
                t = FileManager.GetFilePath(fileName, FileType.Textures);

                if(t == "")
                    throw new Exception("'" + withoutExtension + "_t" + extension + "' doesn't exist!");
            }
            td.Texture = t;

            string n = FileManager.GetFilePath(withoutExtension + "_n" + extension, FileType.Textures);
            if(n != "")
            {
                td.Normal = n;
                td.NormalUse = 1;
                td.count++;
            }
            else
                td.NormalUse = 0;

            string h = FileManager.GetFilePath(withoutExtension + "_h" + extension, FileType.Textures);
            if(h != "")
            {
                td.Height = h;
                td.HeightUse = 1;
                td.count++;
            }
            else
                td.HeightUse = 0;

            string ao = FileManager.GetFilePath(withoutExtension + "_ao" + extension, FileType.Textures);
            if(h != "")
            {
                td.AO = ao;
                td.AOUse = 1;
                td.count++;
            }
            else
                td.AOUse = 0;

            string r = FileManager.GetFilePath(withoutExtension + "_r" + extension, FileType.Textures);
            if(r != "")
            {
                td.Rough = r;
                td.RoughUse = 1;
                td.count++;
            }
            else
                td.RoughUse = 0;

            //td.Normal = def_n;
            //td.Height = def_h;
            //td.AO = def_ao;
            //td.Rough = def_r;

            return td;
        }

        public static void LoadTexture(string fileName, bool flipY, TextureMinFilter tminf, TextureMagFilter tmagf)
        {
            // Load the image (using System.Drawing or another library)
            Stream stream_t = FileManager.GetFileStream(Path.GetFileName(fileName), FileType.Textures);
            if (stream_t != Stream.Null)
            {
                using (stream_t)
                {
                    Bitmap bitmap = new Bitmap(stream_t);
                    if (flipY)
                        bitmap.RotateFlip(RotateFlipType.RotateNoneFlipY);
                    BitmapData data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)tminf);
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)tmagf);

                    GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, data.Width, data.Height, 0, OpenTK.Graphics.OpenGL4.PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);

                    bitmap.UnlockBits(data);

                    // Texture settings
                }
            }
            else
            {
                throw new Exception("No texture was found");
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
