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
    public enum TextureType
    {
        Texture,
        Normal,
        Height,
        AO,
        Rough,
        Metal
    }

    public class Texture
    {
        public TextureMinFilter tminf;
        public TextureMagFilter tmagf;

        public string TextureName;
        public string TexturePath;
        public int TextureId;
        public int TextureUnit;

        public bool flipY;

        public Texture(int unit, string textureName, bool flipY = true, string textureFilter = "linear")
        {
            TextureName = textureName;
            TexturePath = FileManager.GetFilePath(textureName, FileType.Textures);

            int currentUnit = unit;

            TextureId = GL.GenTexture();
            TextureUnit = currentUnit;

            this.flipY = flipY;

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

            Bind();
            LoadTexture(TexturePath, flipY, tminf, tmagf);

            Unbind();
        }

        public static void LoadTexture(string fileName, bool flipY, TextureMinFilter tminf, TextureMagFilter tmagf)
        {
            // Load the image (using System.Drawing or another library)
            Stream? stream_t = FileManager.GetFileStream(Path.GetFileName(fileName), FileType.Textures);
            if (stream_t != Stream.Null && stream_t != null)
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

        public void Bind() 
        {
            GL.ActiveTexture(OpenTK.Graphics.OpenGL4.TextureUnit.Texture0 + TextureUnit);
            GL.BindTexture(TextureTarget.Texture2D, TextureId);
        }
        public void Unbind() { GL.BindTexture(TextureTarget.Texture2D, 0); }

        public void Delete()
        { 
            GL.BindTexture(TextureTarget.Texture2D, 0);
            GL.DeleteTexture(TextureId);
        }

        ~Texture()
        {
            Delete();
        }

    }
}
