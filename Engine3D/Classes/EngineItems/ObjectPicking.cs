using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine3D
{
    public partial class Engine
    {
        private void ObjectPicking()
        {
            if (editorData.gameRunning == GameState.Stopped)
            {
                if (objectMovingAxis != null && MouseState.IsButtonDown(MouseButton.Left) && editorData.selectedItem != null)
                {
                    // Moving
                    if (objectMovingAxis == Vector3.UnitX)
                    {
                        ((ISelectable)editorData.selectedItem).Position = ((ISelectable)editorData.selectedItem).Position - new Vector3(deltaX / 10, 0, 0);
                    }
                    else if (objectMovingAxis == Vector3.UnitY)
                    {
                        ((ISelectable)editorData.selectedItem).Position = ((ISelectable)editorData.selectedItem).Position - new Vector3(0, deltaY / 10, 0);
                    }
                    else if (objectMovingAxis == Vector3.UnitZ)
                    {
                        ((ISelectable)editorData.selectedItem).Position = ((ISelectable)editorData.selectedItem).Position + new Vector3(0, 0, deltaX / 10);
                    }

                    if (editorData.selectedItem is Object o)
                    {
                        o.GetMesh().recalculate = true;
                        o.GetMesh().RecalculateModelMatrix(new bool[] { true, false, false });
                        o.UpdatePhysxPositionAndRotation();
                    }
                }
                else if(objectMovingAxis != null && MouseState.IsButtonReleased(MouseButton.Left))
                {
                    objectMovingAxis = null;
                }
                else if (IsMouseInGameWindow(MouseState) && MouseState.IsButtonPressed(MouseButton.Left) && objectMovingAxis == null)
                {
                    #region Object Selection
                    pickingTexture.EnableWriting(); 
                    GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

                    pickingShader.Use();

                    foreach (Object o in objects.Where(x => x.meshType == typeof(Mesh)))
                    {
                        Mesh mesh = (Mesh)o.GetMesh();

                        int objectIdLoc = GL.GetUniformLocation(pickingShader.id, "objectIndex");
                        int drawIdLoc = GL.GetUniformLocation(pickingShader.id, "drawIndex");
                        GL.Uniform1(objectIdLoc, (uint)o.id);
                        GL.Uniform1(drawIdLoc, (uint)0);

                        vertices.Clear();
                        vertices = mesh.DrawOnlyPos(editorData.gameRunning, pickingShader, onlyPosVao);
                        onlyPosVbo.Buffer(vertices);
                        GL.DrawArrays(PrimitiveType.Triangles, 0, vertices.Count);
                    }

                    pickingTexture.DisableWriting();

                    PixelInfo pixel = pickingTexture.ReadPixel((int)MouseState.X, (int)(windowSize.Y - MouseState.Y));
                    #endregion

                    #region Object moving
                    bool axisClicked = false;
                    if (editorData.selectedItem != null && editorData.selectedItem is Object selectedO)
                    {
                        pickingTexture.EnableWriting();
                        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
                        //pickingShader.Use();

                        foreach (Object moverGizmo in editorData.gizmoManager.moverGizmos)
                        {
                            int objectIdLoc = GL.GetUniformLocation(pickingShader.id, "objectIndex");
                            int drawIdLoc = GL.GetUniformLocation(pickingShader.id, "drawIndex");
                            GL.Uniform1(objectIdLoc, (uint)moverGizmo.id);
                            GL.Uniform1(drawIdLoc, (uint)0);

                            vertices = ((Mesh)moverGizmo.GetMesh()).DrawOnlyPos(editorData.gameRunning, pickingShader, onlyPosVao);
                            onlyPosVbo.Buffer(vertices);
                            GL.DrawArrays(PrimitiveType.Triangles, 0, vertices.Count);
                        }

                        pickingTexture.DisableWriting();

                        PixelInfo pixel2 = pickingTexture.ReadPixel((int)MouseState.X, (int)(windowSize.Y - MouseState.Y));
                        if (pixel2.objectId != 0)
                        {
                            if (pixel2.objectId == 1)
                                objectMovingAxis = Vector3.UnitX;
                            else if (pixel2.objectId == 2)
                                objectMovingAxis = Vector3.UnitY;
                            else if (pixel2.objectId == 3)
                                objectMovingAxis = Vector3.UnitZ;
                        }
                    }

                    #endregion
                    if (!axisClicked && objectMovingAxis == null)
                    {
                        if (pixel.objectId != 0)
                        {
                            Object selectedObject = objects.Where(x => x.id == pixel.objectId).First();

                            imGuiController.SelectItem(selectedObject, editorData);
                            imGuiController.shouldOpenTreeNodeMeshes = true;
                        }
                        else
                        {
                            imGuiController.SelectItem(null, editorData);
                        }
                    }
                }
            }
        }
    }
}
