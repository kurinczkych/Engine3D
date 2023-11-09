using MagicPhysX;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine3D
{
    public enum GizmoType
    {
        Move,
        Rotate,
        Scale
    }

    public class GizmoManager
    {
        public List<Object> moverGizmos = new List<Object>();

        private VAO vao;
        private VBO vbo;
        private Shader shader;
        private Camera camera;

        private float gizmoScale = 10;

        public GizmoType gizmoType = GizmoType.Move;
        public GizmoType lastGizmoType = GizmoType.Move;
        public bool PerInstanceMove = false;

        public GizmoManager(VAO vao, VBO vbo, Shader shader, ref Camera camera)
        {
            shader.Use();

            this.vao = vao;
            this.vbo = vbo;
            this.shader = shader;
            this.camera = camera;

            CreateMoverGizmos();
        }

        public void UpdateMoverGizmo(Vector3 position)
        {
            if(lastGizmoType != gizmoType)
            {
                if(gizmoType == GizmoType.Move)
                {
                    moverGizmos.Clear();
                    CreateMoverGizmos();
                }
                else if(gizmoType == GizmoType.Rotate)
                {
                    moverGizmos.Clear();
                    CreateMoverGizmos();
                }
                else if(gizmoType == GizmoType.Scale)
                {
                    moverGizmos.Clear();
                    CreateMoverGizmos();
                }
            }

            bool[] toUpdate = new bool[3] { false, false, false };
            Vector3 cameraPos = camera.GetPosition();

            float scaleFactor = (cameraPos - position).Length / gizmoScale;

            foreach (Object moverGizmo in moverGizmos)
            {
                if (scaleFactor < 0.1)
                {
                    Vector3 newPos = position + (camera.front * 2);
                    scaleFactor = (cameraPos - newPos).Length / gizmoScale;
                    if (newPos != moverGizmo.Position)
                    {
                        moverGizmo.Position = newPos;
                        toUpdate[0] = true;
                    }
                }
                else
                {
                    if (position != moverGizmo.Position)
                    {
                        moverGizmo.Position = position;
                        toUpdate[0] = true;
                    }
                }

                if (scaleFactor < 1 / gizmoScale)
                    scaleFactor = 1 / gizmoScale;


                if (moverGizmo.Scale.X != scaleFactor)
                {
                    moverGizmo.Scale = new Vector3(scaleFactor);
                    toUpdate[2] = true;
                }

                moverGizmo.GetMesh().RecalculateModelMatrix(toUpdate);
            }
            lastGizmoType = gizmoType;
        }

        private void CreateMoverGizmos()
        {
            float moverGizmoSize = 3;
            float otherAxisScale = 0.5f;

            List<triangle> xTris = Object.GetUnitCube(1,0,0,1);
            Object moverGizmoX = new Object(ObjectType.Cube, 1);
            Matrix4 xMat = Matrix4.CreateScale(new Vector3(moverGizmoSize, otherAxisScale, otherAxisScale)) * Matrix4.CreateTranslation(new Vector3(2, 0, 0));
            foreach (triangle tri in xTris)
            {
                for (int i = 0; i < 3; i++)
                {
                    tri.p[i] = Vector3.TransformPosition(tri.p[i], xMat);
                }
            }
            Mesh xMesh = new Mesh(vao, vbo, shader.id, "xMesh", xTris, camera.screenSize, ref camera, ref moverGizmoX);
            xMesh.useShading = false;
            moverGizmoX.AddMesh(xMesh);
            moverGizmos.Add(moverGizmoX);

            List<triangle> yTris = Object.GetUnitCube(0, 1, 0, 1);
            Object moverGizmoY = new Object(ObjectType.Cube, 2);
            Matrix4 yMat = Matrix4.CreateScale(new Vector3(otherAxisScale, moverGizmoSize, otherAxisScale)) * Matrix4.CreateTranslation(new Vector3(0, 2, 0));
            foreach (triangle tri in yTris)
            {
                for (int i = 0; i < 3; i++)
                {
                    tri.p[i] = Vector3.TransformPosition(tri.p[i], yMat);
                }
            }
            Mesh yMesh = new Mesh(vao, vbo, shader.id, "yMesh", yTris, camera.screenSize, ref camera, ref moverGizmoY);
            yMesh.useShading = false;
            moverGizmoY.AddMesh(yMesh);
            moverGizmos.Add(moverGizmoY);

            List<triangle> zTris = Object.GetUnitCube(0, 0, 1, 1);
            Object moverGizmoZ = new Object(ObjectType.Cube, 3);
            Matrix4 zMat = Matrix4.CreateScale(new Vector3(otherAxisScale, otherAxisScale, moverGizmoSize)) * Matrix4.CreateTranslation(new Vector3(0, 0, 2));
            foreach (triangle tri in zTris)
            {
                for (int i = 0; i < 3; i++)
                {
                    tri.p[i] = Vector3.TransformPosition(tri.p[i], zMat);
                }
            }
            Mesh zMesh = new Mesh(vao, vbo, shader.id, "zMesh", zTris, camera.screenSize, ref camera, ref moverGizmoZ);
            zMesh.useShading = false;
            moverGizmoZ.AddMesh(zMesh);
            moverGizmos.Add(moverGizmoZ);
        }
    }
}
