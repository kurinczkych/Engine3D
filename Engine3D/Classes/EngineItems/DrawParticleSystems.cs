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
        private void DrawParticleSystems()
        {

            //GL.BlendFunc(BlendingFactor.SrcColor, BlendingFactor.OneMinusSrcColor);
            instancedShaderProgram.Use();
            foreach (ParticleSystem ps in particleSystems)
            {
                Object psO = ps.GetObject();
                psO.GetMesh().CalculateFrustumVisibility();

                InstancedMesh mesh = (InstancedMesh)psO.GetMesh();

                indices.Clear();
                verticesUnique.Clear();
                List<float> instancedVertices = new List<float>();
                (verticesUnique, indices, instancedVertices) = mesh.Draw(editorData.gameRunning);
                meshIbo.Buffer(indices);
                meshVbo.Buffer(verticesUnique);
                instancedMeshVbo.Buffer(instancedVertices);

                GL.DrawElementsInstanced(PrimitiveType.Triangles, indices.Count, DrawElementsType.UnsignedInt, IntPtr.Zero, mesh.instancedData.Count());
                vertices.Clear();
            }
            //GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        }
    }
}
