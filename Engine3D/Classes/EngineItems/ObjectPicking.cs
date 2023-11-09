﻿using OpenTK.Graphics.OpenGL4;
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
                if(!IsMouseInsideGizmoWindow() || editorData.gizmoWindowPos == Vector2.Zero)
                {
                    if (objectMovingAxis != null && MouseState.IsButtonDown(MouseButton.Left) && editorData.selectedItem != null)
                    {
                        GizmoObjectManipulating();
                    }
                    else if (objectMovingAxis != null && MouseState.IsButtonReleased(MouseButton.Left))
                    {
                        objectMovingAxis = null;
                    }
                    else if (IsMouseInGameWindow(MouseState) && MouseState.IsButtonPressed(MouseButton.Left) && objectMovingAxis == null)
                    {
                        #region Object selection Drawing
                        pickingTexture.EnableWriting();
                        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

                        pickingShader.Use();
                        foreach (Object o in _meshObjects)
                        {
                            Mesh mesh = (Mesh)o.GetMesh();

                            int objectIdLoc = GL.GetUniformLocation(pickingShader.id, "objectIndex");
                            GL.Uniform1(objectIdLoc, (uint)o.id);

                            vertices.Clear();
                            vertices = mesh.DrawOnlyPos(editorData.gameRunning, pickingShader, onlyPosVao);
                            onlyPosVbo.Buffer(vertices);
                            GL.DrawArrays(PrimitiveType.Triangles, 0, vertices.Count);
                        }

                        pickingInstancedShader.Use();
                        foreach (Object o in _instObjects)
                        {
                            InstancedMesh mesh = (InstancedMesh)o.GetMesh();

                            int objectIdLoc = GL.GetUniformLocation(pickingInstancedShader.id, "objectIndex");
                            GL.Uniform1(objectIdLoc, (uint)o.id);

                            List<float> instancedVertices = new List<float>();
                            (vertices, instancedVertices) = mesh.DrawOnlyPosAndNormal(editorData.gameRunning, pickingInstancedShader, instancedOnlyPosAndNormalVao);
                            instancedOnlyPosAndNormalVbo.Buffer(instancedVertices);
                            onlyPosAndNormalVbo.Buffer(vertices);
                            GL.DrawArraysInstanced(PrimitiveType.Triangles, 0, vertices.Count, mesh.instancedData.Count());
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

                            pickingShader.Use();
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

                        #region Object selection
                        if (!axisClicked && objectMovingAxis == null)
                        {
                            if (pixel.objectId != 0)
                            {
                                Object selectedObject = objects.Where(x => x.id == pixel.objectId).First();
                                int instIndex = editorData.gizmoManager.PerInstanceMove && selectedObject.meshType == typeof(InstancedMesh) ? pixel.instId : -1;

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
