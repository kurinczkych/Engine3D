using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine3D
{
    public class DrawCommandVBO : VBO
    {
        public DrawCommandVBO(bool DynamicCopy = false) : base(DynamicCopy) { }

        public override void Buffer(List<float> data)
        {
            Bind();
            
            GL.BufferData(BufferTarget.ArrayBuffer, sizeof(uint) * 4, IntPtr.Zero, BufferUsageHint.DynamicCopy);
        }
    }
}
