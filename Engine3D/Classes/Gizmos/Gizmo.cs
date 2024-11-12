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
        public bool globalPosition = false;

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

        protected override void SendUniforms()
        {
            projectionMatrix = camera.projectionMatrix;
            viewMatrix = camera.viewMatrix;

            if (Engine.reloadUniformLocations)
            {
                uniformLocations.Clear();
                GetUniformLocations();
            }

            GL.UniformMatrix4(uniformLocations["modelMatrix"], true, ref modelMatrix);
            GL.UniformMatrix4(uniformLocations["viewMatrix"], true, ref viewMatrix);
            GL.UniformMatrix4(uniformLocations["projectionMatrix"], true, ref projectionMatrix);
        }

        public void Draw(GameState gameRunning, Shader shader, VBO vbo_, IBO ibo_)
        {
            if (parentObject != null && !parentObject.isEnabled)
            {
                return;
            }

            Vao.Bind();
            shader.Use();

            SendUniforms();

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
                    CalculateFrustumVisibility(true, globalPosition);
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

        public void AddSphereGizmo(float radius, Color4 color, Vector3 positionOffset = default)
        {
            Assimp.Mesh mesh = new Assimp.Mesh();
            int segments = 24; // Number of segments for smoother appearance; adjust as needed

            // Circle in the X-Y plane
            for (int i = 0; i < segments; i++)
            {
                float theta = (float)(2.0 * Math.PI * i / segments);
                float nextTheta = (float)(2.0 * Math.PI * (i + 1) / segments);

                Vector3 pointA = new Vector3(radius * (float)Math.Cos(theta), radius * (float)Math.Sin(theta), 0) + positionOffset;
                Vector3 pointB = new Vector3(radius * (float)Math.Cos(nextTheta), radius * (float)Math.Sin(nextTheta), 0) + positionOffset;

                AddLine(pointA, pointB, color, color, ref mesh);
            }

            // Circle in the Y-Z plane
            for (int i = 0; i < segments; i++)
            {
                float theta = (float)(2.0 * Math.PI * i / segments);
                float nextTheta = (float)(2.0 * Math.PI * (i + 1) / segments);

                Vector3 pointA = new Vector3(0, radius * (float)Math.Cos(theta), radius * (float)Math.Sin(theta)) + positionOffset;
                Vector3 pointB = new Vector3(0, radius * (float)Math.Cos(nextTheta), radius * (float)Math.Sin(nextTheta)) + positionOffset;

                AddLine(pointA, pointB, color, color, ref mesh);
            }

            // Circle in the X-Z plane
            for (int i = 0; i < segments; i++)
            {
                float theta = (float)(2.0 * Math.PI * i / segments);
                float nextTheta = (float)(2.0 * Math.PI * (i + 1) / segments);

                Vector3 pointA = new Vector3(radius * (float)Math.Cos(theta), 0, radius * (float)Math.Sin(theta)) + positionOffset;
                Vector3 pointB = new Vector3(radius * (float)Math.Cos(nextTheta), 0, radius * (float)Math.Sin(nextTheta)) + positionOffset;

                AddLine(pointA, pointB, color, color, ref mesh);
            }

            MeshData meshData = new MeshData(mesh);
            model.meshes.Add(meshData);
        }

        public void AddDirectionGizmo(Vector3 origin, Vector3 direction, float arrowLength, Color4 color)
        {
            Assimp.Mesh mesh = new Assimp.Mesh();

            // Normalize the direction vector to ensure it's a unit vector
            direction = direction.Normalized();

            // Calculate the end point of the arrow
            Vector3 endPoint = origin + direction * arrowLength;

            // Arrow body (line from origin to endPoint)
            AddLine(origin, endPoint, color, color, ref mesh);

            // Arrowhead: Make a 3D cross-like structure at the end of the arrow
            float headLength = arrowLength * 0.2f; // Arrowhead is 20% of arrow length
            float headWidth = arrowLength * 0.1f;  // Arrowhead width

            // Calculate two vectors perpendicular to the direction to create the arrowhead planes
            Vector3 right = Vector3.Cross(direction, Vector3.UnitY);
            if (right.LengthSquared == 0) right = Vector3.Cross(direction, Vector3.UnitX);
            right.Normalize();
            Vector3 up = Vector3.Cross(right, direction).Normalized();

            // Create the four points for the 3D arrowhead structure
            Vector3 headBaseLeft = endPoint - direction * headLength + right * headWidth;
            Vector3 headBaseRight = endPoint - direction * headLength - right * headWidth;
            Vector3 headBaseUp = endPoint - direction * headLength + up * headWidth;
            Vector3 headBaseDown = endPoint - direction * headLength - up * headWidth;

            // Add the arrowhead lines (first arrowhead plane)
            AddLine(endPoint, headBaseLeft, color, color, ref mesh);    // Right side of arrowhead (first plane)
            AddLine(endPoint, headBaseRight, color, color, ref mesh);   // Left side of arrowhead (first plane)
            AddLine(headBaseLeft, headBaseRight, color, color, ref mesh); // Base of the arrowhead (first plane)

            // Add the arrowhead lines (second arrowhead plane, perpendicular to the first)
            AddLine(endPoint, headBaseUp, color, color, ref mesh);      // Up side of arrowhead (second plane)
            AddLine(endPoint, headBaseDown, color, color, ref mesh);    // Down side of arrowhead (second plane)
            AddLine(headBaseUp, headBaseDown, color, color, ref mesh);  // Base of the arrowhead (second plane)

            // Add mesh to the model
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
