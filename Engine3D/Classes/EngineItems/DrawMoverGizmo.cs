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
                        //editorData.gizmoManager.UpdateMoverGizmo(o.Position + ((InstancedMesh)o.GetMesh()).instancedData[editorData.instIndex].Position,
                        //                                         o.Rotation);

                        editorData.gizmoManager.UpdateMoverGizmo(o.Position + ((InstancedMesh)o.GetMesh()).instancedData[editorData.instIndex].Position,
                                                                 o.Rotation * ((InstancedMesh)o.GetMesh()).instancedData[editorData.instIndex].Rotation);
                        // TODO
                    }
                    else
                        editorData.gizmoManager.UpdateMoverGizmo(o.Position, o.Rotation);


                    foreach (Object moverGizmo in editorData.gizmoManager.moverGizmos)
                    {
                        verticesUnique.Clear();
                        indices.Clear();
                        (verticesUnique, indices) = ((Mesh)moverGizmo.GetMesh()).Draw(editorData.gameRunning);
                        meshIbo.Buffer(indices);
                        meshVbo.Buffer(verticesUnique);
                        GL.DrawElements(PrimitiveType.Triangles, indices.Count, DrawElementsType.UnsignedInt, 0);
                    }
                }
            }
        }
    }
}
