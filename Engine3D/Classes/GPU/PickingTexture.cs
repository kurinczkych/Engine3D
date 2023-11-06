using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Engine3D
{
    public struct PixelInfo
    {
        public int objectId;
        public int drawId;
        public int triId;

        public PixelInfo()
        {
            objectId = 0;
            drawId = 0;
            triId = 0;
        }
    }

    public class PickingTexture
    {
        private Vector2 screenSize;

        private int fbo = 0;
        private int pickingTexture = 0;
        private int depthTexture = 0;

        public PickingTexture(Vector2 screenSize)
        {
            this.screenSize = screenSize;

            fbo = GL.GenFramebuffer();
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, fbo);

            GL.GenTextures(1, out pickingTexture);
            GL.BindTexture(TextureTarget.Texture2D, pickingTexture);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgb32ui, (int)screenSize.X, (int)screenSize.Y, 0, PixelFormat.RgbInteger, PixelType.UnsignedInt, IntPtr.Zero);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, pickingTexture, 0);

            GL.GenTextures(1, out depthTexture);
            GL.BindTexture(TextureTarget.Texture2D, depthTexture);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.DepthComponent, (int)screenSize.X, (int)screenSize.Y, 0, PixelFormat.DepthComponent, PixelType.Float, IntPtr.Zero);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, TextureTarget.Texture2D, depthTexture, 0);

            FramebufferErrorCode error = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
            if(error != FramebufferErrorCode.FramebufferComplete)
            {
                throw new Exception("FBO error! " + error.ToString());
            }

            GL.BindTexture(TextureTarget.Texture2D, 0);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        }

        public PixelInfo ReadPixel(int x, int y)
        {
            GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, fbo);
            GL.ReadBuffer(ReadBufferMode.ColorAttachment0);

            PixelInfo pixel = new PixelInfo();
            unsafe
            {
                GL.ReadPixels(x, y, 1, 1, PixelFormat.RgbInteger, PixelType.UnsignedInt, new IntPtr(&pixel));
            }

            GL.ReadBuffer(ReadBufferMode.None);

            GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, 0);

            return pixel;
        }

        public void EnableWriting()
        {
            GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, fbo);
        }
        
        public void DisableWriting()
        {
            GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, 0);
        }
    }
}
