using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine3D
{
    public class ShadowMapArray
    {
        public int smallShadowMapArrayId = -1;
        public int smallShadowMapArrayUnit = -1;
        public int mediumShadowMapArrayId = -1;
        public int mediumShadowMapArrayUnit = -1;
        public int largeShadowMapArrayId = -1;
        public int largeShadowMapArrayUnit = -1;
        public int faceShadowMapArrayId = -1;
        public int faceShadowMapArrayUnit = -1;
        public int dirIndex = -1;
        public int pointIndex = -1;

        public ShadowMapArray()
        {
            
        }

        public void CreateResizeDirArray()
        {
            if(smallShadowMapArrayId != -1) GL.DeleteTexture(smallShadowMapArrayId);
            if(mediumShadowMapArrayId != -1) GL.DeleteTexture(mediumShadowMapArrayId);
            if(largeShadowMapArrayId != -1) GL.DeleteTexture(largeShadowMapArrayId);

            smallShadowMapArrayUnit = Engine.textureManager.GetTextureUnit();
            smallShadowMapArrayId = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2DArray, smallShadowMapArrayId);

            // is Float -> UnsignedByte? TODO
            GL.TexImage3D(TextureTarget.Texture2DArray, 0, PixelInternalFormat.DepthComponent,
                          2048, 2048, dirIndex + 1, 0, PixelFormat.DepthComponent, PixelType.Float, IntPtr.Zero);

            GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureWrapS, (float)TextureWrapMode.ClampToBorder);
            GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureWrapT, (float)TextureWrapMode.ClampToBorder);
            GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureBorderColor, new float[] { 1.0f, 1.0f, 1.0f, 1.0f });
            GL.BindTexture(TextureTarget.Texture2DArray, 0);

            mediumShadowMapArrayUnit = Engine.textureManager.GetTextureUnit();
            mediumShadowMapArrayId = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2DArray, mediumShadowMapArrayId);

            // is Float -> UnsignedByte? TODO
            GL.TexImage3D(TextureTarget.Texture2DArray, 0, PixelInternalFormat.DepthComponent,
                          1024, 1024, dirIndex + 1, 0, PixelFormat.DepthComponent, PixelType.Float, IntPtr.Zero);

            GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureWrapS, (float)TextureWrapMode.ClampToBorder);
            GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureWrapT, (float)TextureWrapMode.ClampToBorder);
            GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureBorderColor, new float[] { 1.0f, 1.0f, 1.0f, 1.0f });
            GL.BindTexture(TextureTarget.Texture2DArray, 0);

            largeShadowMapArrayUnit = Engine.textureManager.GetTextureUnit();
            largeShadowMapArrayId = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2DArray, largeShadowMapArrayId);

            // is Float -> UnsignedByte? TODO
            GL.TexImage3D(TextureTarget.Texture2DArray, 0, PixelInternalFormat.DepthComponent,
                          512, 512, dirIndex + 1, 0, PixelFormat.DepthComponent, PixelType.Float, IntPtr.Zero);

            GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureWrapS, (float)TextureWrapMode.ClampToBorder);
            GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureWrapT, (float)TextureWrapMode.ClampToBorder);
            GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureBorderColor, new float[] { 1.0f, 1.0f, 1.0f, 1.0f });
            GL.BindTexture(TextureTarget.Texture2DArray, 0);

        }

        public void CreateResizePointArray()
        {
            if (faceShadowMapArrayId != -1) GL.DeleteTexture(faceShadowMapArrayId);

            faceShadowMapArrayUnit = Engine.textureManager.GetTextureUnit();
            faceShadowMapArrayId = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2DArray, faceShadowMapArrayId);

            // is Float -> UnsignedByte? TODO
            GL.TexImage3D(TextureTarget.Texture2DArray, 0, PixelInternalFormat.DepthComponent,
                          1024, 1024, (pointIndex + 1) * 6, 0, PixelFormat.DepthComponent, PixelType.Float, IntPtr.Zero);

            GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureWrapS, (float)TextureWrapMode.ClampToBorder);
            GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureWrapT, (float)TextureWrapMode.ClampToBorder);
            GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureBorderColor, new float[] { 1.0f, 1.0f, 1.0f, 1.0f });
            GL.BindTexture(TextureTarget.Texture2DArray, faceShadowMapArrayId);
        }
    }
}
