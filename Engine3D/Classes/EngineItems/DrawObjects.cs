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
        public enum DrawPass
        {
            ShadowMapPass,
            LightingPass
        }

        private void DrawObjects(double delta)
        {
            vertices.Clear();
            Type? currentMeshType = null;

            Light? light = lights.Where(x => x.GetLightType() == Light.LightType.DirectionalLight).FirstOrDefault();

            shadowMapFBO.BindForReading();

            foreach (Object o in objects)
            {
                BaseMesh? baseMesh = (BaseMesh?)o.GetComponent<BaseMesh>();
                if (baseMesh == null)
                {
                    //throw new Exception("Can't draw the object, it doesn't have a mesh!");
                    continue;
                }

                ObjectType objectType = o.GetObjectType();
                if (objectType == ObjectType.Cube ||
                   objectType == ObjectType.Sphere ||
                   objectType == ObjectType.Capsule ||
                   objectType == ObjectType.TriangleMesh ||
                   objectType == ObjectType.TriangleMeshWithCollider ||
                   objectType == ObjectType.Gizmo)
                {

                    if (baseMesh.GetType() == typeof(Mesh))
                    {
                        Mesh mesh = (Mesh)baseMesh;
                        if (gameState == GameState.Running && useOcclusionCulling && objectType == ObjectType.TriangleMesh)
                        {
                            
                        }
                        else
                        {
                            currentMeshType = typeof(Mesh);

                            if (o.isEnabled)
                            {
                                // OUTLINING
                                if (o.isSelected && o.isEnabled && gameState == GameState.Stopped)
                                {
                                    GL.Enable(EnableCap.StencilTest);
                                    GL.StencilOp(StencilOp.Keep, StencilOp.Keep, StencilOp.Replace);
                                    GL.StencilFunc(StencilFunction.Always, 1, 0xFF);
                                    GL.StencilMask(0xFF);
                                }

                                if(mesh.animation == null)
                                    mesh.Draw(gameState, shaderProgram, meshVbo, meshIbo, light);
                                else
                                    mesh.DrawAnimated(gameState, shaderAnimProgram, meshAnimVao, meshAnimVbo, meshAnimIbo, delta);

                                // OUTLINING
                                if (o.isSelected && o.isEnabled && gameState == GameState.Stopped)
                                {
                                    GL.StencilFunc(StencilFunction.Notequal, 1, 0xFF);
                                    GL.StencilMask(0x00);
                                    GL.Disable(EnableCap.DepthTest);

                                    mesh.DrawOnlyPosAndNormal(gameState, outlineShader, onlyPosAndNormalVao, onlyPosAndNormalVbo, onlyPosAndNormalIbo);

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
                            if (o.isSelected && gameState == GameState.Stopped)
                            {
                                GL.Enable(EnableCap.StencilTest);
                                GL.StencilOp(StencilOp.Keep, StencilOp.Keep, StencilOp.Replace);
                                GL.StencilFunc(StencilFunction.Always, 1, 0xFF);
                                GL.StencilMask(0xFF);
                            }

                            mesh.Draw(gameState, instancedShaderProgram, meshVbo, instancedMeshVbo, meshIbo); 

                            currentMeshType = typeof(InstancedMesh);

                            // OUTLINING
                            if (o.isSelected && gameState == GameState.Stopped)
                            {
                                GL.StencilFunc(StencilFunction.Notequal, 1, 0xFF);
                                GL.StencilMask(0x00);
                                GL.Disable(EnableCap.DepthTest);

                                int instIndex = -1;

                                mesh.DrawOnlyPosAndNormal(gameState, outlineInstancedShader, instancedOnlyPosAndNormalVao, onlyPosAndNormalVbo,
                                                          instancedOnlyPosAndNormalVbo, onlyPosAndNormalIbo, instIndex);

                                GL.StencilMask(0xFF);
                                GL.StencilFunc(StencilFunction.Always, 0, 0xFF);
                                GL.Disable(EnableCap.StencilTest);
                                GL.Enable(EnableCap.DepthTest);
                            }
                        }
                    }
                    else if (objectType == ObjectType.Gizmo && baseMesh is Gizmo)
                    {
                        Gizmo mesh = (Gizmo)baseMesh;

                        //if (DrawCorrectMesh(ref vertices, currentMeshType == null ? typeof(Gizmo) : currentMeshType, typeof(Gizmo), null))
                        //    vertices = new List<float>();

                        onlyPosShaderProgram.Use();

                        mesh.Draw(gameState, onlyPosShaderProgram, wireVbo, wireIbo, light);
                        currentMeshType = typeof(Gizmo);
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

                    vertices.AddRange(mesh.Draw(gameState));
                    currentMeshType = typeof(WireframeMesh);
                }
                else if (objectType == ObjectType.UIMesh && baseMesh is UITextureMesh)
                {
                    UITextureMesh mesh = (UITextureMesh)baseMesh;

                    if (currentMeshType == null || currentMeshType != mesh.GetType())
                    {
                        GL.Disable(EnableCap.DepthTest);
                        posTexShader.Use();
                    }

                    vertices.AddRange(mesh.Draw(gameState));
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

                    vertices.AddRange(mesh.Draw(gameState));
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
            else if (prevMeshType == typeof(Gizmo) && prevMeshType != currentMeshType)
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

        private void DrawObjectsForShadow(double delta)
        {
            Light? light = lights.Where(x => x.GetLightType() == Light.LightType.DirectionalLight).FirstOrDefault();
            if (light == null)
                return;

            shadowMapFBO.BindForWriting();

            GL.Clear(ClearBufferMask.DepthBufferBit);

            shadowShader.Use();

            Vector3 lightDir = light.GetDirection();
            Matrix4 shadowProj = light.projectionMatrixOrtho;
            Matrix4 shadowView = ShadowMapFBO.GetLightViewMatrix(light);

            vertices.Clear();
            Type? currentMeshType = null;
            foreach (Object o in objects)
            {
                BaseMesh? baseMesh = (BaseMesh?)o.GetComponent<BaseMesh>();
                if (baseMesh == null)
                {
                    //throw new Exception("Can't draw the object, it doesn't have a mesh!");
                    continue;
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
                        currentMeshType = typeof(Mesh);

                        if (o.isEnabled)
                        {
                            if (mesh.animation == null)
                                mesh.DrawOnlyPos(gameState, shadowShader, onlyPosVao, onlyPosVbo, onlyPosIbo, shadowProj, shadowView);  
                            //else
                            //    mesh.DrawAnimated(gameState, shaderAnimProgram, meshAnimVao, meshAnimVbo, meshAnimIbo, delta);
                        }
                    }
                    else if (baseMesh.GetType() == typeof(InstancedMesh))
                    {
                        throw new NotImplementedException();
                        //InstancedMesh mesh = (InstancedMesh)baseMesh;
                        //if (currentMeshType == null || currentMeshType != mesh.GetType())
                        //{
                        //    instancedShaderProgram.Use();
                        //}

                        //if (o.isEnabled)
                        //{
                        //    mesh.Draw(gameState, instancedShaderProgram, meshVbo, instancedMeshVbo, meshIbo);

                        //    currentMeshType = typeof(InstancedMesh);
                        //}
                    }
                }
            }


            if (DrawCorrectMesh(ref vertices, currentMeshType == null ? typeof(int) : currentMeshType, typeof(int), null))
                vertices = new List<float>();
        }
    }
}
