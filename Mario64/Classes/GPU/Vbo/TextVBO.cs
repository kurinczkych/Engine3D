using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL4;

namespace Mario64
{
    public class TextVBO : BaseVBO
    {
        int vertexSize;
        public TextVBO()
        {
            vertexSize = Marshal.SizeOf(typeof(TextVertex));
            id = GL.GenBuffer();

            Buffer(new List<TextVertex>());
        }

        public void Buffer(List<TextVertex> data)
        {
            Bind();
            GL.BufferData(BufferTarget.ArrayBuffer, data.Count * vertexSize, data.ToArray(), BufferUsageHint.DynamicDraw);
        }
    }
}
