using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine3D
{
    public partial class Engine
    {
        private void OcclusionCuller()
        {
            if (gameState == GameState.Running)
            {
                if (useOcclusionCulling)
                {
                    //Occlusion
                    GL.ColorMask(false, false, false, false);  // Disable writing to the color buffer
                    GL.Clear(ClearBufferMask.DepthBufferBit);
                    GL.ClearDepth(1.0);

                    List<Object> triangleMeshObjects = scene.objects.Where(x => x.GetObjectType() == ObjectType.TriangleMesh).ToList();
                    aabbShaderProgram.Use();
                    List<float> posVertices = new List<float>();
                    foreach (Object obj in triangleMeshObjects)
                    {
                        //posVertices.AddRange(((Mesh)obj.GetMesh()).DrawOnlyPos(aabbVao, aabbShaderProgram));
                    }
                    aabbVbo.Buffer(posVertices);
                    GL.DrawArrays(PrimitiveType.Triangles, 0, posVertices.Count);


                    //GL.ColorMask(true, true, true, true);
                    //GL.ClearColor(Color4.Cyan);
                    //GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

                    GL.Disable(EnableCap.CullFace);
                    if (pendingQueries.Count() == 0)
                    {
                        foreach (Object obj in triangleMeshObjects)
                        {
                            //OcclusionCulling.PerformOcclusionQueriesForBVH(obj.BVHStruct, aabbVbo, aabbVao, aabbShaderProgram, character.camera, ref queryPool, ref pendingQueries, true);
                        }
                    }
                    else
                    {
                        ;
                        foreach (Object obj in triangleMeshObjects)
                        {
                            //OcclusionCulling.PerformOcclusionQueriesForBVH(obj.BVHStruct, aabbVbo, aabbVao, aabbShaderProgram, character.camera, ref queryPool, ref pendingQueries, false);
                        }
                    }
                    GL.Enable(EnableCap.CullFace);


                    GL.ColorMask(true, true, true, true);
                }
            }
        }
    }
}
