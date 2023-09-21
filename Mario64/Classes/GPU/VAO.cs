using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Mario64
{
    public class VAO
    {
        public int id;
        private int vertexSize;

        public VAO(int floatCount)
        {
            vertexSize = sizeof(float)*floatCount;
            id = GL.GenVertexArray();
            Bind();
        } 

        public void LinkToVAO(int location, int size, int offset, VBO vbo)
        {
            Bind();
            vbo.Bind();

            GL.VertexAttribPointer(location, size, VertexAttribPointerType.Float, false, vertexSize, offset * sizeof(float));
            GL.EnableVertexArrayAttrib(id, location);

            Unbind();
        }

        public void Bind()
        {
            GL.BindVertexArray(id);
        }

        public void Unbind()
        {
            GL.BindVertexArray(0);
        }

        public void Delete()
        {
            GL.DeleteVertexArray(id);
        }
    }
}
