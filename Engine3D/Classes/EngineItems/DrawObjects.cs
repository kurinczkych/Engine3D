using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#pragma warning disable CS8602

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

            foreach(Light light in lights)
            {
                if (light is PointLight pl)
                {
                    light.BindForReading(pl.shadowTop.shadowType);
                    light.BindForReading(pl.shadowBottom.shadowType);
                    light.BindForReading(pl.shadowLeft.shadowType);
                    light.BindForReading(pl.shadowRight.shadowType);
                    light.BindForReading(pl.shadowFront.shadowType);
                    light.BindForReading(pl.shadowBack.shadowType);
                }
                else if (light is DirectionalLight dl)
                {
                    light.BindForReading(dl.shadowSmall.shadowType);
                    light.BindForReading(dl.shadowMedium.shadowType);
                    light.BindForReading(dl.shadowLarge.shadowType);
                }
            }

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
                                    mesh.Draw(gameState, shaderProgram, meshVbo, meshIbo);
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

                        mesh.Draw(gameState, onlyPosShaderProgram, wireVbo, wireIbo);
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

        private void DrawObjectsForShadow(double delta, Light light)
        {
            Matrix4 shadowView = Matrix4.Identity;
            Matrix4 shadowProj = Matrix4.Identity;

            if(light is PointLight pl)
            {
                for(int i = 0; i < 6; i++)
                {
                    switch (i)
                    {
                        case 0:
                            light.BindForWriting(pl.shadowTop.shadowType);
                            shadowProj = pl.shadowTop.projectionMatrix;
                            shadowView = light.GetLightViewMatrix(pl.shadowTop.shadowType);
                            break;
                        case 1:
                            light.BindForWriting(pl.shadowBottom.shadowType);
                            shadowProj = pl.shadowBottom.projectionMatrix;
                            shadowView = light.GetLightViewMatrix(pl.shadowBottom.shadowType);
                            break;
                        case 2:
                            light.BindForWriting(pl.shadowLeft.shadowType);
                            shadowProj = pl.shadowLeft.projectionMatrix;
                            shadowView = light.GetLightViewMatrix(pl.shadowLeft.shadowType);
                            break;
                        case 3:
                            light.BindForWriting(pl.shadowRight.shadowType);
                            shadowProj = pl.shadowRight.projectionMatrix;
                            shadowView = light.GetLightViewMatrix(pl.shadowRight.shadowType);
                            break;
                        case 4:
                            light.BindForWriting(pl.shadowFront.shadowType);
                            shadowProj = pl.shadowFront.projectionMatrix;
                            shadowView = light.GetLightViewMatrix(pl.shadowFront.shadowType);
                            break;
                        case 5:
                            light.BindForWriting(pl.shadowBack.shadowType);
                            shadowProj = pl.shadowBack.projectionMatrix;
                            shadowView = light.GetLightViewMatrix(pl.shadowBack.shadowType);
                            break;
                    }

                    DrawObjectsForShadow(shadowProj, shadowView);
                }
            }
            else if(light is DirectionalLight dl)
            {
                for (int i = 0; i < 3; i++)
                {
                    if (i == 0)
                    {
                        light.BindForWriting(dl.shadowSmall.shadowType);
                        shadowProj = dl.shadowSmall.projectionMatrix;
                        shadowView = light.GetLightViewMatrix(dl.shadowSmall.shadowType);
                    }
                    else if (i == 1)
                    {
                        light.BindForWriting(dl.shadowMedium.shadowType);
                        shadowProj = dl.shadowMedium.projectionMatrix;
                        shadowView = light.GetLightViewMatrix(dl.shadowMedium.shadowType);
                    }
                    else if (i == 2)
                    {
                        light.BindForWriting(dl.shadowLarge.shadowType);
                        shadowProj = dl.shadowLarge.projectionMatrix;
                        shadowView = light.GetLightViewMatrix(dl.shadowLarge.shadowType);
                    }

                    DrawObjectsForShadow(shadowProj, shadowView);
                }
            }
        }

        private void DrawObjectsForShadow(Matrix4 shadowProj, Matrix4 shadowView)
        {
            GL.Clear(ClearBufferMask.DepthBufferBit);

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
