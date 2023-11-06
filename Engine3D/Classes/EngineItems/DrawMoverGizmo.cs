using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine3D
{
    public partial class Engine
    {
        private void DrawMoverGizmo()
        {
            if (editorData.gameRunning == GameState.Stopped)
            {
                if (editorData.selectedItem != null && editorData.selectedItem is Object o)
                {
                    GL.Clear(ClearBufferMask.DepthBufferBit);
                    shaderProgram.Use();
                    editorData.gizmoManager.UpdateMoverGizmo(o.Position);
                    vertices.Clear();
                    foreach (Object moverGizmo in editorData.gizmoManager.moverGizmos)
                    {
                        vertices.AddRange(((Mesh)moverGizmo.GetMesh()).Draw(editorData.gameRunning));
                    }
                    meshVbo.Buffer(vertices);
                    GL.DrawArrays(PrimitiveType.Triangles, 0, vertices.Count);
                    vertices.Clear();
                }
            }
        }
    }
}
