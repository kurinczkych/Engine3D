using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL4;

namespace Mario64
{
    public class NoTexVBO : BaseVBO
    {
        int vertexSize;
        public NoTexVBO()
        {
            vertexSize = Marshal.SizeOf(typeof(VertexNoTexture));
            id = GL.GenBuffer();

            Buffer(new List<VertexNoTexture>());
        }

        public void Buffer(List<VertexNoTexture> data)
        {
            Bind();
            GL.BufferData(BufferTarget.ArrayBuffer, data.Count * vertexSize, data.ToArray(), BufferUsageHint.DynamicDraw);
        }
    }
}
