using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL4;

namespace Mario64
{
    public class UITexVBO : BaseVBO
    {
        int vertexSize;
        public UITexVBO()
        {
            vertexSize = Marshal.SizeOf(typeof(UITextureVertex));
            id = GL.GenBuffer();

            Buffer(new List<UITextureVertex>());
        }

        public void Buffer(List<UITextureVertex> data)
        {
            Bind();
            GL.BufferData(BufferTarget.ArrayBuffer, data.Count * vertexSize, data.ToArray(), BufferUsageHint.DynamicDraw);
        }
    }
}
