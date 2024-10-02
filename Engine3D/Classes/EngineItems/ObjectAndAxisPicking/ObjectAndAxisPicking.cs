using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
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
        private void ObjectAndAxisPicking()
        {
            if (editorData.gameRunning == GameState.Stopped)
            {
                if(!IsMouseInsideGizmoWindow() || editorData.gizmoWindowPos == Vector2.Zero)
                {
                    if (objectMovingAxis != null && MouseState.IsButtonDown(MouseButton.Left) && editorData.selectedItem != null)
                    {
                        GizmoObjectManipulating();
                    }
                    else if (objectMovingAxis != null && MouseState.IsButtonReleased(MouseButton.Left))
                    {
                        objectMovingAxis = null;
                        objectMovingPlane = null;
                    }
                    else if (IsMouseInGameWindow(MouseState) && !imGuiController.CursorInImGuiWindow(new Vector2(MouseState.X, MouseState.Y)) && MouseState.IsButtonPressed(MouseButton.Left) && objectMovingAxis == null)
                    {
                        #region Object selection Drawing
                        pickingTexture.EnableWriting();
                        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

                        pickingShader.Use();
                        foreach (Object o in _meshObjects)
                        {
                            BaseMesh? mesh = o.Mesh;
                            if(mesh == null)
                                continue;

                            int objectIdLoc = GL.GetUniformLocation(pickingShader.id, "objectIndex");
                            GL.Uniform1(objectIdLoc, (uint)o.id);

                            ((Mesh)mesh).DrawOnlyPos(editorData.gameRunning, pickingShader, onlyPosVao, onlyPosVbo, onlyPosIbo);
                        }

                        pickingInstancedShader.Use();
                        foreach (Object o in _instObjects)
                        {
                            BaseMesh? mesh = o.Mesh;
                            if (mesh == null)
                                continue;

                            int objectIdLoc = GL.GetUniformLocation(pickingInstancedShader.id, "objectIndex");
                            GL.Uniform1(objectIdLoc, (uint)o.id);

                            ((InstancedMesh)mesh).DrawOnlyPosAndNormal(editorData.gameRunning, pickingInstancedShader, instancedOnlyPosAndNormalVao, 
                                                       onlyPosAndNormalVbo, instancedOnlyPosAndNormalVbo, onlyPosAndNormalIbo);
                        }


                        pickingTexture.DisableWriting();

                        PixelInfo pixel = pickingTexture.ReadPixel((int)MouseState.X, (int)(windowSize.Y - MouseState.Y));
                        #endregion

                        #region Axis picking
                        bool axisClicked = false;
                        if (editorData.selectedItem != null && editorData.selectedItem is Object selectedO)
                        {
                            pickingTexture.EnableWriting();
                            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

                            pickingShader.Use();
                            foreach (Object moverGizmo in editorData.gizmoManager.moverGizmos)
                            {
                                int objectIdLoc = GL.GetUniformLocation(pickingShader.id, "objectIndex");
                                int drawIdLoc = GL.GetUniformLocation(pickingShader.id, "drawIndex");
                                GL.Uniform1(objectIdLoc, (uint)moverGizmo.id);
                                GL.Uniform1(drawIdLoc, (uint)0);

                                BaseMesh? moverMesh = moverGizmo.Mesh;
                                if (moverMesh == null)
                                    continue;

                                ((Mesh)moverMesh).DrawOnlyPos(editorData.gameRunning, pickingShader, onlyPosVao, onlyPosVbo, onlyPosIbo);
                            }

                            pickingTexture.DisableWriting();

                            PixelInfo pixel2 = pickingTexture.ReadPixel((int)MouseState.X, (int)(windowSize.Y - MouseState.Y));
                            CreateAxisPlane(pixel2, selectedO);
                        }

                        #endregion

                        #region Object selection
                        if (!axisClicked && objectMovingAxis == null)
                        {
                            if (pixel.objectId != 0 && objects.Count > 0)
                            {
                                var objs = objects.Where(x => x.id == pixel.objectId);

                                if (objs == null || objs.Count() == 0)
                                {
                                    imGuiController.SelectItem(null, editorData);
                                    return;
                                }

                                Object selectedObject = objs.First();
                                BaseMesh? selectedMesh = selectedObject.Mesh;
                                if (selectedMesh == null)
                                    return;

                                int instIndex = editorData.gizmoManager.PerInstanceMove && selectedMesh.GetType() == typeof(InstancedMesh) ? pixel.instId : -1;

                                imGuiController.SelectItem(selectedObject, editorData, instIndex);
                                imGuiController.shouldOpenTreeNodeMeshes = true;
                            }
                            else
                            {
                                imGuiController.SelectItem(null, editorData);
                            }
                        }
                        #endregion
                    }
                }
            }
        }

        private bool IsMouseInsideGizmoWindow()
        {
            Vector2 mousePosition = new Vector2(MouseState.X, MouseState.Y);
            float border = 10;

            bool isInside = (mousePosition.X >= (editorData.gizmoWindowPos.X - border)) &&
                            (mousePosition.Y >= (editorData.gizmoWindowPos.Y - border)) &&
                            (mousePosition.X <= (editorData.gizmoWindowPos.X + editorData.gizmoWindowSize.X + border)) &&
                            (mousePosition.Y <= (editorData.gizmoWindowPos.Y + editorData.gizmoWindowSize.Y + border));

            return isInside;
        }
    }
}
