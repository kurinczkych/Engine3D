using OpenTK.Graphics.OpenGL4;
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
                if (IsMouseInGameWindow(MouseState) && MouseState.IsButtonReleased(MouseButton.Left))
                {
                    GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
                    GL.DepthFunc(DepthFunction.Always);
                    #region Object Selection
                    pickingTexture.EnableWriting();

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
                        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
                        pickingTexture.EnableWriting();
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
                            axisClicked = true;
                            ;
                        }
                    }

                    #endregion
                    if (!axisClicked)
                    {
                        if (pixel.objectId != 0)
                        {
                            Object selectedObject = objects.Where(x => x.id == pixel.objectId).First();

                            imGuiController.SelectItem(selectedObject, editorData);
                        }
                        else
                        {
                            imGuiController.SelectItem(null, editorData);
                        }
                    }

                    GL.DepthFunc(DepthFunction.Less);
                }
            }
        }
    }
}
