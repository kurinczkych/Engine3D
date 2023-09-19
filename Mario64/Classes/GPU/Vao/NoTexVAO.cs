using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Mario64
{
    public class NoTexVAO : BaseVAO
    {
        private int vertexSize;

        public NoTexVAO()
        {
            vertexSize = Marshal.SizeOf(typeof(VertexNoTexture));
            id = GL.GenVertexArray();
            Bind();
        } 

        public void LinkToVAO(int location, int size, int offset, NoTexVBO vbo)
        {
            Bind();
            vbo.Bind();

            GL.VertexAttribPointer(location, size, VertexAttribPointerType.Float, false, vertexSize, offset * sizeof(float));
            GL.EnableVertexArrayAttrib(id, location);

            Unbind();
        }

        
    }
}
