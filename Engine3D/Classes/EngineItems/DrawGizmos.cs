using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static OpenTK.Graphics.OpenGL.GL;


namespace Engine3D
{
    public partial class Engine
    {
        public void DrawGizmos()
        {
            List<Gizmo?> gizmos = lights.Where(x => x.frustumGizmo != null && x.showFrustum).Select(x => x.frustumGizmo).ToList();

            foreach (Gizmo? gizmo in gizmos)
            {
                if (gizmo == null)
                    continue;

                onlyPosShaderProgram.Use();

                gizmo.Draw(gameState, onlyPosShaderProgram, wireVbo, wireIbo, null);
            }
        }
    }
}
