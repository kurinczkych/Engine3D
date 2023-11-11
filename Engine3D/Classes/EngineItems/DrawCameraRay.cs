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
        private void DrawCameraRay()
        {
            onlyPosShaderProgram.Use();

            Vector3 dir = character.camera.GetCameraRay(MouseState.Position);

            Vector3 cameraPos = character.camera.GetPosition() + character.camera.front;
            WireframeMesh wiremesh = new WireframeMesh(wireVao, wireVbo, onlyPosShaderProgram.id, ref character.camera);
            wiremesh.lines = new List<Line>() { new Line(cameraPos, cameraPos + dir * 1000, Color4.Red, Color4.White) };

            List<float> vertices = wiremesh.Draw(editorData.gameRunning);
            wireVbo.Buffer(vertices);
            GL.DrawArrays(PrimitiveType.Lines, 0, vertices.Count);

            Plane planeZAligned = new Plane
            {
                normal = new Vector3(0, 0, 1), // or new Vector3(0, -1, 0)
                distance = 0 // Distance along Y-axis from origin to plane
            };
            Vector3? pos_ = planeZAligned.RayPlaneIntersection(character.camera.GetPosition(), dir);

            if (pos_ != null)
            {
                Vector3 pos = (Vector3)pos_;
                pos.Y = 0;

                shaderProgram.Use();
                Object o = new Object(ObjectType.Sphere);
                Mesh mesh = new Mesh(meshVao, meshVbo, shaderProgram.id, "sphere", Object.GetUnitSphere(), "red_t.png", windowSize, ref character.camera, ref o);
                o.Position = pos;
                mesh.RecalculateModelMatrix(new bool[3] { true, false, false });

                vertices = mesh.Draw(editorData.gameRunning);
                meshVbo.Buffer(vertices);
                GL.DrawArrays(PrimitiveType.Triangles, 0, vertices.Count);
            }

        }
    }
}
