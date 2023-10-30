using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Engine3D
{
    public class InstancedVAO
    {
        public int id;
        private int vertexSize;
        private int instanceSize;

        private int currentOffset;
        private int currentInstanceOffset;

        public InstancedVAO(int floatCount, int instancedFloatCount)
        {
            vertexSize = sizeof(float) * floatCount;
            instanceSize = sizeof(float) * instancedFloatCount;
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

        public void LinkToVAOInstanceData(int location, int size, int divisor, VBO vbo)
        {
            Bind();
            vbo.Bind();

            GL.EnableVertexArrayAttrib(id, location);
            GL.VertexAttribPointer(location, size, VertexAttribPointerType.Float, false, instanceSize, currentInstanceOffset * sizeof(float));
            GL.VertexAttribDivisor(location, divisor);
            currentInstanceOffset += size;

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
