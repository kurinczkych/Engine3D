using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL4;

namespace Engine3D
{
    public class IBO
    {
        public int id;

        public IBO()
        {
            id = GL.GenBuffer();

            Buffer(new List<uint>());
        }

        public virtual void Buffer(List<uint> data)
        {
            Bind();
            var a = data.ToArray();
            GL.BufferData(BufferTarget.ElementArrayBuffer, data.Count * sizeof(uint), a, BufferUsageHint.DynamicDraw);
        }

        public void Bind()
        {
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, id);
        }

        public void Unbind()
        {
            Engine.GLState.iboBound = -1;

            GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
        }

        public void Delete()
        {
            Engine.GLState.iboBound = -1;

            GL.DeleteBuffer(id);
        }
    }
}
