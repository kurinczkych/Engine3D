using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
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

                    if (editorData.gizmoManager.PerInstanceMove && editorData.instIndex != -1 && o.meshType == typeof(InstancedMesh))
                    {
                        editorData.gizmoManager.UpdateMoverGizmo(o.Position + ((InstancedMesh)o.GetMesh()).instancedData[editorData.instIndex].Position);
                    }
                    else
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
