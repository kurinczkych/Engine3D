using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL4;

namespace Mario64
{
    public class WireVBO : BaseVBO
    {
        int vertexSize;
        public WireVBO()
        {
            vertexSize = Marshal.SizeOf(typeof(VertexLine));
            id = GL.GenBuffer();

            Buffer(new List<VertexLine>());
        }

        public void Buffer(List<VertexLine> data)
        {
            Bind();
            GL.BufferData(BufferTarget.ArrayBuffer, data.Count * vertexSize, data.ToArray(), BufferUsageHint.DynamicDraw);
        }
    }
}
