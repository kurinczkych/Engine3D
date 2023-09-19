using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mario64
{
    public abstract class BaseVAO
    {
        public int id;

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
