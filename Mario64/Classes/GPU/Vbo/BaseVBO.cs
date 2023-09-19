using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL4;

namespace Mario64
{
    public abstract class BaseVBO
    {
        public int id;

        public void Bind()
        {
            GL.BindBuffer(BufferTarget.ArrayBuffer, id);
        }

        public void Unbind()
        {
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        }

        public void Delete() 
        {
            GL.DeleteBuffer(id);
        }
    }
}
