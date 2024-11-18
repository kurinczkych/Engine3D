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
        public int cubeShadowMapArrayId = -1;
        public int cubeShadowMapArrayUnit = -1;
        public int dirIndex = -1;
        public int pointIndex = -1;

        public ShadowMapArray()
        {
        }

        public void GetTextureUnits()
        {
            if (smallShadowMapArrayUnit == -1)
            {
                smallShadowMapArrayUnit = Engine.textureManager.GetTextureUnit();
                mediumShadowMapArrayUnit = Engine.textureManager.GetTextureUnit();
                largeShadowMapArrayUnit = Engine.textureManager.GetTextureUnit();
                cubeShadowMapArrayUnit = Engine.textureManager.GetTextureUnit();
            }
        }

        public void CreateResizeDirArray()
        {
            if(smallShadowMapArrayId != -1) GL.DeleteTexture(smallShadowMapArrayId);
            if(mediumShadowMapArrayId != -1) GL.DeleteTexture(mediumShadowMapArrayId);
            if(largeShadowMapArrayId != -1) GL.DeleteTexture(largeShadowMapArrayId);

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
            GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureCompareMode, (int)TextureCompareMode.None);
            GL.BindTexture(TextureTarget.Texture2DArray, 0);

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
            GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureCompareMode, (int)TextureCompareMode.None);
            GL.BindTexture(TextureTarget.Texture2DArray, 0);

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
            GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureCompareMode, (int)TextureCompareMode.None);
            GL.BindTexture(TextureTarget.Texture2DArray, 0);
        }

        public void DeleteDirArray()
        {
            if (smallShadowMapArrayId != -1)
            {
                GL.DeleteTexture(smallShadowMapArrayId);
                smallShadowMapArrayId = -1;
            }
            if (mediumShadowMapArrayId != -1)
            {
                GL.DeleteTexture(mediumShadowMapArrayId);
                mediumShadowMapArrayId = -1;
            }
            if (largeShadowMapArrayId != -1)
            {
                GL.DeleteTexture(largeShadowMapArrayId);
                largeShadowMapArrayId = -1;
            }
        }

        public void CreateResizePointArray()
        {
            if (cubeShadowMapArrayId != -1) GL.DeleteTexture(cubeShadowMapArrayId);

            cubeShadowMapArrayId = GL.GenTexture();
            GL.BindTexture(TextureTarget.TextureCubeMapArray, cubeShadowMapArrayId);

            GL.TexImage3D(TextureTarget.TextureCubeMapArray, 0, PixelInternalFormat.DepthComponent,
                          1024, 1024, (pointIndex + 1) * 6, 0, PixelFormat.DepthComponent, PixelType.Float, IntPtr.Zero);

            GL.TexParameter(TextureTarget.TextureCubeMapArray, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.TextureCubeMapArray, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            GL.TexParameter(TextureTarget.TextureCubeMapArray, TextureParameterName.TextureWrapS, (float)TextureWrapMode.ClampToBorder);
            GL.TexParameter(TextureTarget.TextureCubeMapArray, TextureParameterName.TextureWrapT, (float)TextureWrapMode.ClampToBorder);
            GL.TexParameter(TextureTarget.TextureCubeMapArray, TextureParameterName.TextureBorderColor, new float[] { 1.0f, 1.0f, 1.0f, 1.0f });
            GL.BindTexture(TextureTarget.TextureCubeMapArray, cubeShadowMapArrayId);
        }

        public void DeletePointArray()
        {
            if (cubeShadowMapArrayId != -1)
            {
                GL.DeleteTexture(cubeShadowMapArrayId);
                cubeShadowMapArrayId = -1;
                cubeShadowMapArrayUnit = -1;
            }
        }
    }
}
