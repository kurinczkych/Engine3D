using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Desktop;
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
            if (gameState == GameState.Stopped)
            {
                if(!IsMouseInsideGizmoWindow() || gizmoWindowPos == Vector2.Zero)
                {
                    float scaleX = windowSize.X / gameWindowProperty.gameWindowSize.X;
                    float scaleY = windowSize.Y / gameWindowProperty.gameWindowSize.Y;
                    float mouseXInFramebuffer = (MouseState.X - (gameWindowProperty.leftPanelPercent * windowSize.X + 5)) * scaleX;
                    float mouseYInFramebuffer = (MouseState.Y - (gameWindowProperty.topPanelSize)) * scaleY;

                    if (objectMovingAxis != null && MouseState.IsButtonDown(MouseButton.Left) && selectedObject != null)
                    {
                        GizmoObjectManipulating();
                    }
                    else if (objectMovingAxis != null && MouseState.IsButtonReleased(MouseButton.Left))
                    {
                        objectMovingAxis = null;
                        objectMovingPlane = null;
                    }
                    else if (IsMouseInGameWindow(MouseState) && !UIHasMouse && MouseState.IsButtonPressed(MouseButton.Left) && objectMovingAxis == null /*&& !imGuiController.CursorInImGuiWindow(new Vector2(mouseXInFramebuffer, mouseYInFramebuffer))*/)
                    {
                        #region Object selection Drawing
                        pickingTexture.EnableWriting();
                        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

                        pickingShader.Use();
                        foreach (Object o in _meshObjects)
                        {
                            if (!o.interactableInEditor)
                                continue;

                            BaseMesh? mesh = (BaseMesh?)o.GetComponent<BaseMesh>();
                            if(mesh == null)
                                continue;

                            int objectIdLoc = GL.GetUniformLocation(pickingShader.programId, "objectIndex");
                            GL.Uniform1(objectIdLoc, (uint)o.id);

                            ((Mesh)mesh).DrawOnlyPos(gameState, pickingShader, onlyPosVao, onlyPosVbo, onlyPosIbo);
                        }

                        pickingInstancedShader.Use();
                        foreach (Object o in _instObjects)
                        {
                            if (!o.interactableInEditor)
                                continue;

                            BaseMesh? mesh = (BaseMesh?)o.GetComponent<BaseMesh>();
                            if (mesh == null)
                                continue;

                            int objectIdLoc = GL.GetUniformLocation(pickingInstancedShader.programId, "objectIndex");
                            GL.Uniform1(objectIdLoc, (uint)o.id);

                            ((InstancedMesh)mesh).DrawOnlyPosAndNormal(gameState, pickingInstancedShader, instancedOnlyPosAndNormalVao, 
                                                       onlyPosAndNormalVbo, instancedOnlyPosAndNormalVbo, onlyPosAndNormalIbo);
                        }


                        pickingTexture.DisableWriting();

                        PixelInfo pixel = pickingTexture.ReadPixel((int)mouseXInFramebuffer, (int)(windowSize.Y - mouseYInFramebuffer));
                        #endregion

                        #region Axis picking
                        bool axisClicked = false;
                        if (selectedObject is Object selectedO)
                        {
                            pickingTexture.EnableWriting();
                            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

                            pickingShader.Use();
                            foreach (Object moverGizmo in gizmoManager.moverGizmos)
                            {
                                int objectIdLoc = GL.GetUniformLocation(pickingShader.programId, "objectIndex");
                                int drawIdLoc = GL.GetUniformLocation(pickingShader.programId, "drawIndex");
                                GL.Uniform1(objectIdLoc, (uint)moverGizmo.id);
                                GL.Uniform1(drawIdLoc, (uint)0);

                                BaseMesh? moverMesh = (BaseMesh?)moverGizmo.GetComponent<BaseMesh>();
                                if (moverMesh == null)
                                    continue;

                                ((Mesh)moverMesh).DrawOnlyPos(gameState, pickingShader, onlyPosVao, onlyPosVbo, onlyPosIbo);
                            }

                            pickingTexture.DisableWriting();

                            PixelInfo pixel2 = pickingTexture.ReadPixel((int)mouseXInFramebuffer, (int)(windowSize.Y - mouseYInFramebuffer));
                            CreateAxisPlane(pixel2, selectedO);
                        }

                        #endregion

                        #region Object selection
                        if (!axisClicked && objectMovingAxis == null)
                        {
                            if (pixel.objectId != 0 && objects.Count > 0)
                            {
                                var objs = objects.Where(x => x.id == pixel.objectId);

                                if (objs == null || objs.Count() == 0 || !objs.First().interactableInEditor)
                                {
                                    if (objectSelected != null)
                                        objectSelected.Invoke(null, -1);
                                    return;
                                }

                                Object selectedObject = objs.First();
                                BaseMesh? selectedMesh = (BaseMesh?)selectedObject.GetComponent<BaseMesh>();
                                if (selectedMesh == null)
                                    return;

                                int instIndex = gizmoManager.PerInstanceMove && selectedMesh.GetType() == typeof(InstancedMesh) ? pixel.instId : -1;

                                if (objectSelected != null)
                                    objectSelected.Invoke(selectedObject, instIndex);
                            }
                            else
                            {
                                if (objectSelected != null)
                                    objectSelected.Invoke(null, -1);
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

            bool isInside = (mousePosition.X >= (gizmoWindowPos.X - border)) &&
                            (mousePosition.Y >= (gizmoWindowPos.Y - border)) &&
                            (mousePosition.X <= (gizmoWindowPos.X + gizmoWindowSize.X + border)) &&
                            (mousePosition.Y <= (gizmoWindowPos.Y + gizmoWindowSize.Y + border));

            return isInside;
        }
    }
}
