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

        private float gizmoScale = 25;

        public GizmoType gizmoType = GizmoType.Move;
        public GizmoType lastGizmoType = GizmoType.Move;
        public bool PerInstanceMove = false;
        public bool AbsoluteMoving = true;
        public int instIndex;

        public GizmoManager(VAO vao, VBO vbo, Shader shader, ref Camera camera)
        {
            shader.Use();

            this.vao = vao;
            this.vbo = vbo;
            this.shader = shader;
            this.camera = camera;

            CreateMoverGizmos();
        }

        public void UpdateMoverGizmo(Vector3 position, Quaternion rotation)
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

            Vector3 directionToGizmo = position - cameraPos;
            float angle = Vector3.Dot(directionToGizmo.Normalized(), camera.front);
            float scaleFactor = directionToGizmo.Length / ((float)Math.Cos(angle) * gizmoScale);
            //float scaleFactor = (cameraPos - position).Length / gizmoScale;

            foreach (Object moverGizmo in moverGizmos)
            {
                if (scaleFactor < 0.1)
                {
                    Vector3 newPos = position + (camera.front * 2);
                    directionToGizmo = newPos - cameraPos;
                    angle = Vector3.Dot(directionToGizmo.Normalized(), camera.front);
                    scaleFactor = directionToGizmo.Length / ((float)Math.Cos(angle) * gizmoScale);
                    //scaleFactor = (cameraPos - newPos).Length / gizmoScale;
                    if (newPos != moverGizmo.transformation.Position)
                    {
                        moverGizmo.transformation.Position = newPos;
                        toUpdate[0] = true;
                    }
                }
                else
                {
                    if (position != moverGizmo.transformation.Position)
                    {
                        moverGizmo.transformation.Position = position;
                        toUpdate[0] = true;
                    }
                }

                if (scaleFactor < 1 / gizmoScale)
                    scaleFactor = 1 / gizmoScale;


                if (moverGizmo.transformation.Scale.X != scaleFactor)
                {
                    //moverGizmo.transformation.Scale = new Vector3(scaleFactor);
                    //toUpdate[2] = true;

                    moverGizmo.transformation.Scale = new Vector3(0.5004f);
                    moverGizmo.transformation.Scale = new Vector3(scaleFactor);
                    toUpdate[2] = true;
                }

                if(AbsoluteMoving && moverGizmo.transformation.Rotation != Quaternion.Identity)
                {
                    moverGizmo.transformation.Rotation = Quaternion.Identity;
                    toUpdate[1] = true;
                }
                else if(!AbsoluteMoving && moverGizmo.transformation.Rotation != rotation)
                {
                    moverGizmo.transformation.Rotation = rotation;
                    toUpdate[1] = true;
                }

                BaseMesh? mesh = (BaseMesh?)moverGizmo.GetComponent<BaseMesh>();
                if (mesh == null)
                {
                    throw new Exception("Can't draw the gizmo, because the object doesn't have a mesh!");
                }

                mesh.RecalculateModelMatrix(toUpdate);
                mesh.AllIndicesVisible();
            }
            lastGizmoType = gizmoType;
        }

        public void CreateMoverGizmos()
        {
            moverGizmos.Clear();

            float moverGizmoSize = 3;
            float otherAxisScale = 0.5f;

            ModelData modelX = BaseMesh.GetUnitCube(1, 0, 0, 1);
            Object moverGizmoX = new Object(ObjectType.Cube, 1);
            moverGizmoX.name = "xMover";
            Matrix4 xMat = Matrix4.CreateScale(new Vector3(moverGizmoSize, otherAxisScale, otherAxisScale)) * Matrix4.CreateTranslation(new Vector3(2, 0, 0));
            modelX.meshes[0].TransformMeshData(xMat);
            Mesh xMesh = new Mesh(vao, vbo, shader.id, "xMesh", modelX, camera.screenSize, ref camera, ref moverGizmoX);
            xMesh.useShading = false;
            moverGizmoX.AddMesh(xMesh);
            xMesh.AllIndicesVisible();
            moverGizmos.Add(moverGizmoX);

            ModelData modelY = BaseMesh.GetUnitCube(0, 1, 0, 1);
            Object moverGizmoY = new Object(ObjectType.Cube, 2);
            moverGizmoY.name = "yMover";
            Matrix4 yMat = Matrix4.CreateScale(new Vector3(otherAxisScale, moverGizmoSize, otherAxisScale)) * Matrix4.CreateTranslation(new Vector3(0, 2, 0));
            modelY.meshes[0].TransformMeshData(yMat);
            Mesh yMesh = new Mesh(vao, vbo, shader.id, "yMesh", modelY, camera.screenSize, ref camera, ref moverGizmoY);
            yMesh.useShading = false;
            moverGizmoY.AddMesh(yMesh);
            yMesh.AllIndicesVisible();
            moverGizmos.Add(moverGizmoY);

            ModelData modelZ = BaseMesh.GetUnitCube(0, 0, 1, 1);
            Object moverGizmoZ = new Object(ObjectType.Cube, 3);
            moverGizmoZ.name = "zMover";
            Matrix4 zMat = Matrix4.CreateScale(new Vector3(otherAxisScale, otherAxisScale, moverGizmoSize)) * Matrix4.CreateTranslation(new Vector3(0, 0, 2));
            modelZ.meshes[0].TransformMeshData(zMat);
            Mesh zMesh = new Mesh(vao, vbo, shader.id, "zMesh", modelZ, camera.screenSize, ref camera, ref moverGizmoZ);
            zMesh.useShading = false;
            moverGizmoZ.AddMesh(zMesh);
            zMesh.AllIndicesVisible();
            moverGizmos.Add(moverGizmoZ);
        }
    }
}
