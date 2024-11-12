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
            if (selectedObject == null)
                return;

            Object o = selectedObject;

            if (gameState == GameState.Stopped)
            {
                GL.Clear(ClearBufferMask.DepthBufferBit);
                shaderProgram.Use();

                BaseMesh? mesh = (BaseMesh?)o.GetComponent<BaseMesh>();

                if (gizmoManager.PerInstanceMove && gizmoManager.instIndex != -1)
                {
                    if (mesh == null)
                        throw new Exception("Can't draw the gizmo, because the object doesn't have a mesh!");

                    if (mesh.GetType() == typeof(InstancedMesh))

                    {
                        //editorData.gizmoManager.UpdateMoverGizmo(o.Position + ((InstancedMesh)o.GetMesh()).instancedData[editorData.instIndex].Position,
                        //                                         o.Rotation);

                        gizmoManager.UpdateMoverGizmo(o.transformation.Position + ((InstancedMesh)mesh).instancedData[gizmoManager.instIndex].Position,
                                                                 o.transformation.Rotation * ((InstancedMesh)mesh).instancedData[gizmoManager.instIndex].Rotation);
                        // TODO
                    }
                }
                else
                    gizmoManager.UpdateMoverGizmo(o.transformation.Position, o.transformation.Rotation);


                foreach (Object moverGizmo in gizmoManager.moverGizmos)
                {
                    BaseMesh? moverMesh = (BaseMesh?)moverGizmo.GetComponent<BaseMesh>();
                    if (moverMesh == null)
                        continue;

                    ((Mesh)moverMesh).Draw(gameState, shaderProgram, meshVbo, meshIbo);
                }
            }
        }
    }
}
