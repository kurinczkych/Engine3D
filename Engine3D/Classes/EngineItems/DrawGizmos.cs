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
            List<Gizmo> gizmos = lights.Where(light => light.gizmos != null && light.showGizmos)
                                       .SelectMany(light => light.gizmos)
                                       .Where(gizmoPair => gizmoPair.Value != null && gizmoPair.Key != "directionGizmo")
                                       .Select(gizmoPair => gizmoPair.Value)
                                       .ToList();

            gizmos.AddRange(lights.Where(light => light.gizmos != null && light.parentObject != null && (light.parentObject.isSelected || light.showGizmos))
                            .SelectMany(light => light.gizmos)
                            .Where(gizmoPair => gizmoPair.Value != null && gizmoPair.Key == "directionGizmo")
                            .Select(gizmoPair => gizmoPair.Value)
                            .ToList());

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
