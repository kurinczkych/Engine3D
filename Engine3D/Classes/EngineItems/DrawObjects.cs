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
        private void DrawObjects(double delta)
        {
            GL.ClearColor(Color4.Cyan);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);
            vertices.Clear();
            Type? currentMeshType = null;
            foreach (Object o in objects)
            {
                BaseMesh? baseMesh = o.Mesh;
                if (baseMesh == null)
                {
                    throw new Exception("Can't draw the object, it doesn't have a mesh!");
                }

                ObjectType objectType = o.GetObjectType();
                if (objectType == ObjectType.Cube ||
                   objectType == ObjectType.Sphere ||
                   objectType == ObjectType.Capsule ||
                   objectType == ObjectType.TriangleMesh ||
                   objectType == ObjectType.TriangleMeshWithCollider)
                {

                    if (baseMesh.GetType() == typeof(Mesh))
                    {
                        Mesh mesh = (Mesh)baseMesh;
                        if (editorData.gameRunning == GameState.Running && useOcclusionCulling && objectType == ObjectType.TriangleMesh)
                        {
                            //List<triangle> notOccludedTris = new List<triangle>();
                            //OcclusionCulling.TraverseBVHNode(o.BVHStruct.Root, ref notOccludedTris, ref frustum);

                            //vertices.AddRange(mesh.DrawNotOccluded(notOccludedTris));
                            //currentMeshType = typeof(Mesh);

                            //if (vertices.Count > 0)
                            //{
                            //    meshVbo.Buffer(vertices);
                            //    GL.DrawArrays(PrimitiveType.Triangles, 0, vertices.Count);
                            //    vertices.Clear();
                            //}
                        }
                        else
                        {
                            currentMeshType = typeof(Mesh);

                            //cullingProgram.Use();

                            //visibilityVbo.Buffer(vertices);
                            //frustumVbo.Buffer(character.camera.frustum.GetData());
                            //GL.DispatchCompute(256, 1, 1);
                            //GL.MemoryBarrier(MemoryBarrierFlags.ShaderStorageBarrierBit); 

                            //GL.DrawArraysIndirect(PrimitiveType.Triangles, IntPtr.Zero);

                            if (o.isEnabled)
                            {
                                // OUTLINING
                                if (o.isSelected && o.isEnabled && editorData.gameRunning == GameState.Stopped)
                                {
                                    GL.Enable(EnableCap.StencilTest);
                                    GL.StencilOp(StencilOp.Keep, StencilOp.Keep, StencilOp.Replace);
                                    GL.StencilFunc(StencilFunction.Always, 1, 0xFF);
                                    GL.StencilMask(0xFF);
                                }

                                if(mesh.animation == null)
                                    mesh.Draw(editorData.gameRunning, shaderProgram, meshVbo, meshIbo);
                                else
                                    mesh.DrawAnimated(editorData.gameRunning, shaderAnimProgram, meshAnimVao, meshAnimVbo, meshAnimIbo, delta);

                                // OUTLINING
                                if (o.isSelected && o.isEnabled && editorData.gameRunning == GameState.Stopped)
                                {
                                    GL.StencilFunc(StencilFunction.Notequal, 1, 0xFF);
                                    GL.StencilMask(0x00);
                                    GL.Disable(EnableCap.DepthTest);

                                    mesh.DrawOnlyPosAndNormal(editorData.gameRunning, outlineShader, onlyPosAndNormalVao, onlyPosAndNormalVbo, onlyPosAndNormalIbo);

                                    GL.StencilMask(0xFF);
                                    GL.StencilFunc(StencilFunction.Always, 0, 0xFF);
                                    GL.Disable(EnableCap.StencilTest);
                                    GL.Enable(EnableCap.DepthTest);
                                }
                            }
                        }
                    }
                    else if (baseMesh.GetType() == typeof(InstancedMesh))
                    {
                        InstancedMesh mesh = (InstancedMesh)baseMesh;
                        if (currentMeshType == null || currentMeshType != mesh.GetType())
                        {
                            instancedShaderProgram.Use();
                        }

                        // OUTLINING
                        if (o.isEnabled)
                        {
                            if (o.isSelected && editorData.gameRunning == GameState.Stopped)
                            {
                                GL.Enable(EnableCap.StencilTest);
                                GL.StencilOp(StencilOp.Keep, StencilOp.Keep, StencilOp.Replace);
                                GL.StencilFunc(StencilFunction.Always, 1, 0xFF);
                                GL.StencilMask(0xFF);
                            }

                            mesh.Draw(editorData.gameRunning, instancedShaderProgram, meshVbo, instancedMeshVbo, meshIbo); 

                            currentMeshType = typeof(InstancedMesh);

                            // OUTLINING
                            if (o.isSelected && editorData.gameRunning == GameState.Stopped)
                            {
                                GL.StencilFunc(StencilFunction.Notequal, 1, 0xFF);
                                GL.StencilMask(0x00);
                                GL.Disable(EnableCap.DepthTest);

                                int instIndex = -1;
                                //if (editorData.gizmoManager.PerInstanceMove && editorData.instIndex != -1)
                                //    instIndex = editorData.instIndex;

                                mesh.DrawOnlyPosAndNormal(editorData.gameRunning, outlineInstancedShader, instancedOnlyPosAndNormalVao, onlyPosAndNormalVbo,
                                                          instancedOnlyPosAndNormalVbo, onlyPosAndNormalIbo, instIndex);

                                GL.StencilMask(0xFF);
                                GL.StencilFunc(StencilFunction.Always, 0, 0xFF);
                                GL.Disable(EnableCap.StencilTest);
                                GL.Enable(EnableCap.DepthTest);
                            }
                        }
                    }
                }
                else if (objectType == ObjectType.Wireframe && baseMesh is WireframeMesh)
                {
                    WireframeMesh mesh = (WireframeMesh)baseMesh;

                    if (DrawCorrectMesh(ref vertices, currentMeshType == null ? typeof(WireframeMesh) : currentMeshType, typeof(WireframeMesh), null))
                        vertices = new List<float>();

                    if (currentMeshType == null || currentMeshType != mesh.GetType())
                    {
                        onlyPosShaderProgram.Use();
                    }

                    vertices.AddRange(mesh.Draw(editorData.gameRunning));
                    currentMeshType = typeof(WireframeMesh);
                }
                else if (objectType == ObjectType.UIMesh && baseMesh is UITextureMesh)
                {
                    UITextureMesh mesh = (UITextureMesh)baseMesh;

                    //if (DrawCorrectMesh(ref vertices, currentMesh, typeof(UITextureMesh)))
                    //    vertices = new List<float>();

                    if (currentMeshType == null || currentMeshType != mesh.GetType())
                    {
                        GL.Disable(EnableCap.DepthTest);
                        posTexShader.Use();
                    }

                    vertices.AddRange(mesh.Draw(editorData.gameRunning));
                    currentMeshType = typeof(UITextureMesh);

                    uiTexVbo.Buffer(vertices);
                    GL.DrawArrays(PrimitiveType.Triangles, 0, vertices.Count);
                    vertices.Clear();
                }
                else if (objectType == ObjectType.TextMesh && baseMesh is TextMesh)
                {
                    TextMesh mesh = (TextMesh)baseMesh;

                    if (currentMeshType == null || currentMeshType != mesh.GetType())
                    {
                        GL.Disable(EnableCap.DepthTest);
                        posTexShader.Use();
                    }

                    if (DrawCorrectMesh(ref vertices, currentMeshType == null ? typeof(TextMesh) : currentMeshType, typeof(TextMesh), null))
                        vertices = new List<float>();

                    vertices.AddRange(mesh.Draw(editorData.gameRunning));
                    currentMeshType = typeof(TextMesh);
                }
            }


            if (DrawCorrectMesh(ref vertices, currentMeshType == null ? typeof(int) : currentMeshType, typeof(int), null))
                vertices = new List<float>();
        }
        private bool DrawCorrectMesh(ref List<float> vertices, Type prevMeshType, Type currentMeshType, BaseMesh? prevMesh)
        {
            if (prevMeshType == null || currentMeshType == null)
                return false;

            if (prevMeshType == typeof(Mesh) && prevMeshType != currentMeshType)
            {
                meshVbo.Buffer(vertices);
            }
            if (prevMeshType == typeof(InstancedMesh) && prevMeshType != currentMeshType)
            {
                instancedMeshVbo.Buffer(vertices);
            }
            else if (prevMeshType == typeof(WireframeMesh) && prevMeshType != currentMeshType)
            {
                wireVbo.Buffer(vertices);
            }
            else if (prevMeshType == typeof(UITextureMesh) && prevMeshType != currentMeshType)
            {
                uiTexVbo.Buffer(vertices);
            }
            else if (prevMeshType == typeof(TextMesh) && prevMeshType != currentMeshType)
            {
                textVbo.Buffer(vertices);
            }
            else
                return false;

            if (prevMeshType == typeof(WireframeMesh))
                GL.DrawArrays(PrimitiveType.Lines, 0, vertices.Count);
            else if (prevMeshType == typeof(InstancedMesh))
            {
                if (prevMesh != null)
                    GL.DrawArraysInstanced(PrimitiveType.Triangles, 0, vertices.Count, ((InstancedMesh)prevMesh).instancedData.Count());
            }
            else
                GL.DrawArrays(PrimitiveType.Triangles, 0, vertices.Count);
            return true;

        }

    }
}
