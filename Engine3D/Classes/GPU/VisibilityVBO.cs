using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine3D
{
    public class VisibilityVBO : VBO
    {
        public VisibilityVBO(bool DynamicCopy = false) : base(DynamicCopy) { }

        public override void Buffer(List<float> data)
        {
            Bind();
            GL.BufferData(BufferTarget.ArrayBuffer, (data.Count/Mesh.floatCount/3) * sizeof(uint), IntPtr.Zero, BufferUsageHint.DynamicCopy);
        }
    }
}
