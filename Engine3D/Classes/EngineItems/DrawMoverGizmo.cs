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

                    BaseMesh? mesh = o.Mesh;
                    if(mesh == null)
                    {
                        throw new Exception("Can't draw the gizmo, because the object doesn't have a mesh!");
                    }

                    if (editorData.gizmoManager.PerInstanceMove && editorData.instIndex != -1 && mesh.GetType() == typeof(InstancedMesh))
                    {
                        //editorData.gizmoManager.UpdateMoverGizmo(o.Position + ((InstancedMesh)o.GetMesh()).instancedData[editorData.instIndex].Position,
                        //                                         o.Rotation);

                        editorData.gizmoManager.UpdateMoverGizmo(o.transformation.Position + ((InstancedMesh)mesh).instancedData[editorData.instIndex].Position,
                                                                 o.transformation.Rotation * ((InstancedMesh)mesh).instancedData[editorData.instIndex].Rotation);
                        // TODO
                    }
                    else
                        editorData.gizmoManager.UpdateMoverGizmo(o.transformation.Position, o.transformation.Rotation);


                    foreach (Object moverGizmo in editorData.gizmoManager.moverGizmos)
                    {
                        ((Mesh)mesh).Draw(editorData.gameRunning, shaderProgram, meshVbo, meshIbo);
                    }
                }
            }
        }
    }
}
