using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Mario64
{
    public class UITexVAO : BaseVAO
    {
        private int vertexSize;

        public UITexVAO()
        {
            vertexSize = Marshal.SizeOf(typeof(UITextureVertex));
            id = GL.GenVertexArray();
            Bind();
        } 

        public void LinkToVAO(int location, int size, int offset, UITexVBO vbo)
        {
            Bind();
            vbo.Bind();

            GL.VertexAttribPointer(location, size, VertexAttribPointerType.Float, false, vertexSize, offset * sizeof(float));
            GL.EnableVertexArrayAttrib(id, location);

            Unbind();
        }

        
    }
}
