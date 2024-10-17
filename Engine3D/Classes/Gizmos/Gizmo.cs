using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Engine3D
{
    public class Gizmo : BaseMesh
    {
        private VAO Vao;
        private VBO Vbo;

        [JsonIgnore]
        private Matrix4 viewMatrix, projectionMatrix;

        private ModelData modelData = new ModelData();

        public Gizmo(VAO vao, VBO vbo, int shaderProgramId, Vector2 windowSize, ref Camera camera, ref Object parentObject) : base(vao.id, vbo.id, shaderProgramId)
        {
            Vao = vao;
            Vbo = vbo;
            this.camera = camera;
            this.parentObject = parentObject;

            GetUniformLocations();
        }

        private void GetUniformLocations()
        {
            uniformLocations.Add("modelMatrix", GL.GetUniformLocation(shaderProgramId, "modelMatrix"));
            uniformLocations.Add("viewMatrix", GL.GetUniformLocation(shaderProgramId, "viewMatrix"));
            uniformLocations.Add("projectionMatrix", GL.GetUniformLocation(shaderProgramId, "projectionMatrix"));
        }

        protected override void SendUniforms(Vector3? lightDir)
        {
            projectionMatrix = camera.projectionMatrix;
            viewMatrix = camera.viewMatrix;

            GL.UniformMatrix4(uniformLocations["modelMatrix"], true, ref modelMatrix);
            GL.UniformMatrix4(uniformLocations["viewMatrix"], true, ref viewMatrix);
            GL.UniformMatrix4(uniformLocations["projectionMatrix"], true, ref projectionMatrix);
        }

        public void Draw(GameState gameRunning, Shader shader, VBO vbo_, IBO ibo_, Vector3? lightDir)
        {
            if (!parentObject.isEnabled)
                return;

            Vao.Bind();
            shader.Use();

            SendUniforms(lightDir);

            foreach (MeshData mesh in model.meshes)
            {
                if (!recalculate)
                {
                    if (gameRunning == GameState.Stopped && mesh.visibleIndices.Count > 0)
                    {
                        if (mesh.visibleIndices.Count == 0)
                            continue;
                        ibo_.Buffer(mesh.visibleIndices);
                        vbo_.Buffer(mesh.visibleVerticesDataOnlyPosAndColor);
                        GL.DrawElements(PrimitiveType.Lines, mesh.visibleIndices.Count, DrawElementsType.UnsignedInt, 0);
                        continue;
                    }
                }
                else
                {
                    recalculate = false;
                    CalculateFrustumVisibility();
                }

                if (mesh.visibleIndices.Count == 0)
                    continue;
                ibo_.Buffer(mesh.visibleIndices);
                vbo_.Buffer(mesh.visibleVerticesDataOnlyPosAndColor);
                GL.DrawElements(PrimitiveType.Lines, mesh.visibleIndices.Count, DrawElementsType.UnsignedInt, 0);
            }
        }

        public void AddFrustumGizmo(Frustum f, Color4 color)
        {
            Assimp.Mesh mesh = new Assimp.Mesh();

            // Near plane
            AddLine(f.ntl.Xyz, f.ntr.Xyz, color, color, ref mesh);  // ntl -> ntr
            AddLine(f.nbl.Xyz, f.nbr.Xyz, color, color, ref mesh);  // nbl -> nbr
            AddLine(f.ntl.Xyz, f.nbl.Xyz, color, color, ref mesh);  // ntl -> nbl
            AddLine(f.ntr.Xyz, f.nbr.Xyz, color, color, ref mesh);  // ntr -> nbr

            // Far plane
            AddLine(f.ftl.Xyz, f.ftr.Xyz, color, color, ref mesh);  // ftl -> ftr
            AddLine(f.fbl.Xyz, f.fbr.Xyz, color, color, ref mesh);  // fbl -> fbr
            AddLine(f.ftl.Xyz, f.fbl.Xyz, color, color, ref mesh);  // ftl -> fbl
            AddLine(f.ftr.Xyz, f.fbr.Xyz, color, color, ref mesh);  // ftr -> fbr

            // Connecting near and far planes
            AddLine(f.ntl.Xyz, f.ftl.Xyz, color, color, ref mesh);  // ntl -> ftl
            AddLine(f.ntr.Xyz, f.ftr.Xyz, color, color, ref mesh);  // ntr -> ftr
            AddLine(f.nbl.Xyz, f.fbl.Xyz, color, color, ref mesh);  // nbl -> fbl
            AddLine(f.nbr.Xyz, f.fbr.Xyz, color, color, ref mesh);  // nbr -> fbr

            MeshData meshData = new MeshData(mesh);
            model.meshes.Add(meshData);
        }

        private void AddLine(Vector3 a, Vector3 b, Color4 aC, Color4 bC, ref Assimp.Mesh mesh)
        {
            // Convert OpenTK Vector3 and Color4 to Assimp types
            Assimp.Vector3D assimpA = new Assimp.Vector3D(a.X, a.Y, a.Z);
            Assimp.Vector3D assimpB = new Assimp.Vector3D(b.X, b.Y, b.Z);
            Assimp.Color4D assimpAC = new Assimp.Color4D(aC.R, aC.G, aC.B, aC.A);
            Assimp.Color4D assimpBC = new Assimp.Color4D(bC.R, bC.G, bC.B, bC.A);

            // Add vertices (a and b) to the Assimp.Mesh vertex list
            mesh.Vertices.Add(assimpA);
            mesh.Vertices.Add(assimpB);

            // Add colors for the vertices
            if (mesh.HasVertexColors(0))
            {
                mesh.VertexColorChannels[0].Add(assimpAC);
                mesh.VertexColorChannels[0].Add(assimpBC);
            }
            else
            {
                mesh.VertexColorChannels[0] = new List<Assimp.Color4D> { assimpAC, assimpBC };
            }

            // Add indices for the line (each line consists of 2 points)
            int vertexCount = mesh.VertexCount; // Current count of vertices in the mesh
            mesh.Faces.Add(new Assimp.Face(new int[] { vertexCount - 2, vertexCount - 1 }));  // Line between last two vertices
        }
    }
}
