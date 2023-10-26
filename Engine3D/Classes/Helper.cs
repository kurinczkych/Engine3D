using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using MagicPhysX;
using static Engine3D.TextGenerator;
using System.IO;
using Cyotek.Drawing.BitmapFont;
using System.Security.Cryptography;

namespace Engine3D
{
    public static class Helper
    {
        public static Stream GetResourceStreamByNameEnd(string nameEnd)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            foreach (string resourceName in assembly.GetManifestResourceNames())
            {
                if (resourceName.EndsWith(nameEnd, StringComparison.OrdinalIgnoreCase))
                {
                    Stream? s = assembly.GetManifestResourceStream(resourceName);
                    if (s == null)
                        return Stream.Null;

                    return s;
                }
            }
            return Stream.Null; // or throw an exception if the resource is not found
        }
        public static string GetResourceNameByNameEnd(string nameEnd)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            foreach (string resourceName in assembly.GetManifestResourceNames())
            {
                if (resourceName.EndsWith(nameEnd, StringComparison.OrdinalIgnoreCase))
                {
                    return resourceName;
                }
            }
            return ""; // or throw an exception if the resource is not found
        }

        public static TextureDescriptor GetTextureDescriptor(string embeddedResourceName)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            string withoutExtension = Path.GetFileNameWithoutExtension(embeddedResourceName);
            string extension = Path.GetExtension(embeddedResourceName);
            string[] resources = assembly.GetManifestResourceNames();

            string t = "";
            string n = "";
            string h = "";
            string ao = "";
            string r = "";

            string def_n = "";
            string def_h = "";
            string def_ao = "";
            string def_r = "";

            foreach (string resourceName in resources)
            {
                if (resourceName.EndsWith(withoutExtension + "_t" + extension, StringComparison.OrdinalIgnoreCase))
                    t = resourceName;
                else if (resourceName.EndsWith(withoutExtension + "_n" + extension, StringComparison.OrdinalIgnoreCase))
                    n = resourceName;
                else if (resourceName.EndsWith(withoutExtension + "_h" + extension, StringComparison.OrdinalIgnoreCase))
                    h = resourceName;
                else if (resourceName.EndsWith(withoutExtension + "_ao" + extension, StringComparison.OrdinalIgnoreCase))
                    ao = resourceName;
                else if (resourceName.EndsWith(withoutExtension + "_r" + extension, StringComparison.OrdinalIgnoreCase))
                    r = resourceName;
                else if (resourceName.EndsWith("default_normal.png", StringComparison.OrdinalIgnoreCase))
                    def_n = resourceName;
                else if (resourceName.EndsWith("default_height.png", StringComparison.OrdinalIgnoreCase))
                    def_h = resourceName;
                else if (resourceName.EndsWith("default_ao.png", StringComparison.OrdinalIgnoreCase))
                    def_ao = resourceName;
                else if (resourceName.EndsWith("default_roughness.png", StringComparison.OrdinalIgnoreCase))
                    def_r = resourceName;
            }

            if(t == "")
            {
                t = GetResourceNameByNameEnd(embeddedResourceName);
            }

            TextureDescriptor td = new TextureDescriptor()
            {
                Texture = t,
                count = 1
            };
            if (n != "")
            {
                td.Normal = n;
                td.NormalUse = 1;
                td.count++;
            }
            else
            {
                td.Normal = def_n;
                td.NormalUse = 0;
                td.count++;
            }
            if (h != "")
            {
                td.Height = h;
                td.HeightUse = 1;
                td.count++;
            }
            else
            {
                td.Height = def_h;
                td.HeightUse = 0;
                td.count++;
            }
            if (ao != "")
            {
                td.AO = ao;
                td.AOUse = 1;
                td.count++;
            }
            else
            {
                td.AO = def_ao;
                td.AOUse = 0;
                td.count++;
            }
            if (r != "")
            {
                td.Rough = r;
                td.RoughUse = 1;
                td.count++;
            }
            else
            {
                td.Rough = def_r;
                td.RoughUse = 0;
                td.count++;
            }

            //td.Normal = def_n;
            //td.Height = def_h;
            //td.AO = def_ao;
            //td.Rough = def_r;

            return td;
        }

        public static Stream GetResourceStream(string embeddedResourceName)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();

            Stream? s = assembly.GetManifestResourceStream(embeddedResourceName);
            if (s == null)
                return Stream.Null;

            return s;
        }

        public static void LoadTexture(string embeddedResourceName, bool flipY, TextureMinFilter tminf, TextureMagFilter tmagf)
        {
            // Load the image (using System.Drawing or another library)
            Stream stream_t = GetResourceStream(embeddedResourceName);
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

        public static Vector3 Vector3Min(Vector3 a, Vector3 b)
        {
            return new Vector3(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y), Math.Min(a.Z, b.Z));
        }

        public static Vector3 Vector3Max(Vector3 a, Vector3 b)
        {
            return new Vector3(Math.Max(a.X, b.X), Math.Max(a.Y, b.Y), Math.Max(a.Z, b.Z));
        }

    }
}
