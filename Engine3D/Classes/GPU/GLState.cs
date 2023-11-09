using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine3D
{
    public class GLState
    {
        // Shader
        public int currentShaderId = -1;

        // VAO
        public int vaoBound = -1;

        // VBO
        public BufferTarget? vboTarget;
        public int vboBound;

        //Texture
        public int currentTextureUnit = -1;
        public int currentTextureId = -1;

        public GLState() { }
    }
}
