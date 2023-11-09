using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL4;

namespace Engine3D
{
    public class VBO
    {
        public int id;
        private bool dynamicCopy;

        public VBO(bool DynamicCopy = false)
        {
            id = GL.GenBuffer();

            dynamicCopy = DynamicCopy;

            Buffer(new List<float>());
        }

        public virtual void Buffer(List<float> data)
        {
            Bind();
            if(!dynamicCopy)
                GL.BufferData(BufferTarget.ArrayBuffer, data.Count * sizeof(float), data.ToArray(), BufferUsageHint.DynamicDraw);
            else
                GL.BufferData(BufferTarget.ArrayBuffer, data.Count * sizeof(float), data.ToArray(), BufferUsageHint.DynamicCopy);
        }

        public void Bind()
        {
            if (Engine.GLState.vboBound != id)
            {
                GL.BindBuffer(BufferTarget.ArrayBuffer, id);
                Engine.GLState.vboBound = id;
                Engine.GLState.vboTarget = BufferTarget.ArrayBuffer;
            }
        }

        public void Unbind()
        {
            Engine.GLState.vboBound = -1;
            Engine.GLState.vboTarget = null;

            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        }

        public void Delete()
        {
            Engine.GLState.vboBound = -1;
            Engine.GLState.vboTarget = null;

            GL.DeleteBuffer(id);
        }
    }
}
