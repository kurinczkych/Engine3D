using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Engine3D
{
    public class VAO
    {
        public int id;
        private int vertexSize;
        private int currentOffset = 0;

        public VAO(int floatCount)
        {
            vertexSize = sizeof(float)*floatCount;
            id = GL.GenVertexArray();
            Bind();
        } 

        public void LinkToVAO(int location, int size, VBO vbo)
        {
            Bind();
            vbo.Bind();

            GL.EnableVertexArrayAttrib(id, location);
            GL.VertexAttribPointer(location, size, VertexAttribPointerType.Float, false, vertexSize, currentOffset * sizeof(float));
            currentOffset += size;

            Unbind();
        }

        public void Bind()
        {
            if (Engine.GLState.vaoBound != id)
            {
                GL.BindVertexArray(id);
                Engine.GLState.vaoBound = id;
            }
        }

        public void Unbind()
        {
            Engine.GLState.vaoBound = -1;

            GL.BindVertexArray(0);
        }

        public void Delete()
        {
            Engine.GLState.vaoBound = -1;

            GL.DeleteVertexArray(id);
        }
    }
}
