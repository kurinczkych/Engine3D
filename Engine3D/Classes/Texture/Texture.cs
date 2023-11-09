using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL4;
using System.Runtime.CompilerServices;
using System.IO;

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

        public bool isBinded = false;

        public bool flipY;

        public Texture(int unit, string texturePath, out bool success, bool flipY = true, string textureFilter = "linear", AssetTypeEditor type = AssetTypeEditor.UI)
        {
            TextureName = Path.GetFileName(texturePath);

            if (texturePath == TextureName)
            {
                if (type == AssetTypeEditor.UI)
                    TexturePath = FileManager.GetFilePath(texturePath, "Textures");
                else if (type == AssetTypeEditor.Store)
                    TexturePath = FileManager.GetFilePath(texturePath, "Temp");
            }
            else
                TexturePath = texturePath;

            if (TexturePath == "" || TexturePath == null)
                throw new Exception("Texture not found!");

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

            if(type == AssetTypeEditor.Store)
                success = LoadTexture(TexturePath, flipY, tminf, tmagf, "Temp");
            else
                success = LoadTexture(TexturePath, flipY, tminf, tmagf);

            Unbind();
        }

        public static bool LoadTexture(string filePath, bool flipY, TextureMinFilter tminf, TextureMagFilter tmagf, string folder = "Textures")
        {
            Stream? stream_t = FileManager.GetFileStream(filePath);
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
                return true;
            }
            else
            {
                // TODO Console log ("File '" + fileName + "' not found!");
                return false;
            }
        }

        public void Bind() 
        {
            if(Engine.GLState.currentTextureUnit != TextureUnit)
            {
                GL.ActiveTexture(OpenTK.Graphics.OpenGL4.TextureUnit.Texture0 + TextureUnit);
                Engine.GLState.currentTextureUnit = TextureUnit;
            }

            if (Engine.GLState.currentTextureId != TextureId)
            {
                GL.BindTexture(TextureTarget.Texture2D, TextureId);
                Engine.GLState.currentTextureId = TextureId;
            }
            isBinded = true;
        }
        public void Unbind()
        {
            Engine.GLState.currentTextureUnit = -1;
            Engine.GLState.currentTextureId = -1;

            GL.BindTexture(TextureTarget.Texture2D, 0);
            isBinded = false;
        }

        public void Delete()
        {
            Engine.GLState.currentTextureUnit = -1;
            Engine.GLState.currentTextureId = -1;

            GL.BindTexture(TextureTarget.Texture2D, 0);
            isBinded = false;
            GL.DeleteTexture(TextureId);
        }

        ~Texture()
        {
            //Delete();
        }

    }
}
