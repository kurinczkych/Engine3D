using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using static System.Net.WebRequestMethods;

namespace Mario64
{
    public class Camera
    {
        private Vector2 screenSize;

        public float near;
        public float far;
        public float fov;
        public float aspectRatio;

        public Vector3 position;
        public Vector3 up = Vector3.UnitY;
        public Vector3 front = -Vector3.UnitZ;
        public Vector3 frontClamped = -Vector3.UnitZ;
        public Vector3 right = Vector3.UnitX;

        public float yaw;
        //private float pitch = -90.0f;
        public float pitch = 0f;

        public Camera() { }

        public Camera(Vector2 screenSize)
        {
            this.screenSize = screenSize;
            position = new Vector3();

            near = 0.1f;
            far = 1000.0f;
            fov = 90.0f;
            aspectRatio = screenSize.X / screenSize.Y;
        }

        public Matrix4 GetViewMatrix()
        {
            return Matrix4.LookAt(position, position + front, up);
        }

        public Matrix4 GetViewMatrixa()
        {
            Vector3 v = new Vector3(0, 0, -100) + position;
            return Matrix4.LookAt(v, v + front, up);
        }

        public Matrix4 GetViewMatrixb()
        {
            return Matrix4.LookAt(new Vector3(0,0,0), new Vector3(0,0,0) + front, up);
        }

        public Matrix4 GetProjectionMatrix()
        {
            return Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(fov), aspectRatio, near, far);
        }

        public Matrix4 GetProjectionMatrixBigger(float fovMult = 1.1f)
        {
            return Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(fov * fovMult), aspectRatio, near, far);
        }

        public bool IsTriangleClose(triangle tri)
        {
            float dist1 = (position - new Vector3(tri.p[0].X, tri.p[0].Y, tri.p[0].Z)).Length;
            float dist2 = (position - new Vector3(tri.p[1].X, tri.p[1].Y, tri.p[1].Z)).Length;
            float dist3 = (position - new Vector3(tri.p[2].X, tri.p[2].Y, tri.p[2].Z)).Length;
            float dist = float.PositiveInfinity;
            if (dist1 < dist) dist = dist1;
            if (dist2 < dist) dist = dist2;
            if (dist3 < dist) dist = dist3;

            return dist < 15.0f;

        }

        public Frustum GetFrustum()
        {
            Frustum frustum = new Frustum();
            Matrix4 m = GetViewMatrix();
            m = m * GetProjectionMatrixBigger(1.3f);

            //right
            frustum.planes[0].normal.X = m.Row0[3] - m.Row0[0];
            frustum.planes[0].normal.Y = m.Row1[3] - m.Row1[0];
            frustum.planes[0].normal.Z = m.Row2[3] - m.Row2[0];
            frustum.planes[0].distance = m.Row3[3] - m.Row3[0];
                   
            //left
            frustum.planes[1].normal.X = m.Row0[3] + m.Row0[0];
            frustum.planes[1].normal.Y = m.Row1[3] + m.Row1[0];
            frustum.planes[1].normal.Z = m.Row2[3] + m.Row2[0];
            frustum.planes[1].distance = m.Row3[3] + m.Row3[0];
                
            //down
            frustum.planes[2].normal.X = m.Row0[3] + m.Row0[1];
            frustum.planes[2].normal.Y = m.Row1[3] + m.Row1[1];
            frustum.planes[2].normal.Z = m.Row2[3] + m.Row2[1];
            frustum.planes[2].distance = m.Row3[3] + m.Row3[1];
                
            //up
            frustum.planes[3].normal.X = m.Row0[3] - m.Row0[1];
            frustum.planes[3].normal.Y = m.Row1[3] - m.Row1[1];
            frustum.planes[3].normal.Z = m.Row2[3] - m.Row2[1];
            frustum.planes[3].distance = m.Row3[3] - m.Row3[1];
              
            //far
            frustum.planes[4].normal.X = m.Row0[3] - m.Row0[2];
            frustum.planes[4].normal.Y = m.Row1[3] - m.Row1[2];
            frustum.planes[4].normal.Z = m.Row2[3] - m.Row2[2];
            frustum.planes[4].distance = m.Row3[3] - m.Row3[2];
                     
            //near
            frustum.planes[5].normal.X = m.Row0[3] + m.Row0[2];
            frustum.planes[5].normal.Y = m.Row1[3] + m.Row1[2];
            frustum.planes[5].normal.Z = m.Row2[3] + m.Row2[2];
            frustum.planes[5].distance = m.Row3[3] + m.Row3[2];

            for (int i = 0; i < 6; i++)
            {
                float magnitude = frustum.planes[i].normal.Length;
                frustum.planes[i].normal /= magnitude;
                frustum.planes[i].distance /= magnitude;
            }

            return frustum;
        }

        public void UpdatePositionToGround(List<triangle> groundTriangles)
        {
            float offsetHeight = 4f;

            foreach (var triangle in groundTriangles)
            {
                // Check if character is above this triangle.
                if (triangle.IsPointInTriangle(position, out float distanceToTriangle))
                {
                    // Compute the triangle's normal.
                    Vector3 normal = Vector3.Cross(triangle.p[1] - triangle.p[0], triangle.p[2] - triangle.p[0]).Normalized();

                    // Adjust the character's position based on the triangle's normal and the computed distance.
                    position -= normal * (distanceToTriangle - offsetHeight);

                    break; // Exit once the correct triangle is found.
                }
            }
        }

        public void UpdateVectors()
        {
            if (pitch > 89f)
                pitch = 89f;

            if (pitch < -89f)
                pitch = -89f;

            front.X = MathF.Cos(MathHelper.DegreesToRadians(pitch)) * MathF.Cos(MathHelper.DegreesToRadians(yaw));
            front.Y = MathF.Sin(MathHelper.DegreesToRadians(pitch));
            front.Z = MathF.Cos(MathHelper.DegreesToRadians(pitch)) * MathF.Sin(MathHelper.DegreesToRadians(yaw));

            frontClamped = new Vector3(front.X, 0, front.Z);

            front.Normalize();
            frontClamped.Normalize();

            right = Vector3.Normalize(Vector3.Cross(front, Vector3.UnitY));
            up = Vector3.Normalize(Vector3.Cross(right, front));
        }
    }
}
