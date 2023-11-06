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

                List<float> instancedVertices = new List<float>();
                (vertices, instancedVertices) = mesh.Draw(editorData.gameRunning);

                meshVbo.Buffer(vertices);
                instancedMeshVbo.Buffer(instancedVertices);
                GL.DrawArraysInstanced(PrimitiveType.Triangles, 0, vertices.Count, mesh.instancedData.Count());
                vertices.Clear();
            }
            //GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        }
    }
}
