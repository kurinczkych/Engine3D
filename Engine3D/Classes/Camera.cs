﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using static System.Net.WebRequestMethods;

namespace Engine3D
{
    public class Camera
    {
        public float near;
        public float far;
        public float fov;
        public float aspectRatio;

        private Vector3 position;

        public Vector3 up { get; private set; } = Vector3.UnitY;
        public Vector3 front { get; private set; } = -Vector3.UnitZ;
        public Vector3 frontClamped { get; private set; } = -Vector3.UnitZ;
        public Vector3 right { get; private set; } = Vector3.UnitX;

        private float yaw;
        //private float pitch = -90.0f;
        private float pitch = 0f;

        public Matrix4 viewMatrix;
        public Matrix4 projectionMatrix;
        public Matrix4 projectionMatrixBigger;
        public Frustum frustum;

        public Camera(Vector2 screenSize)
        {
            position = new Vector3();

            near = 0.1f;
            far = 1000.0f;
            fov = 90.0f;
            aspectRatio = screenSize.X / screenSize.Y;

            viewMatrix = GetViewMatrix();
            projectionMatrix = GetProjectionMatrix();
            projectionMatrixBigger = GetProjectionMatrixBigger(1.3f);
        }

        #region Setters and getters
        public Vector3 GetPosition()
        {
            return position;
        }
        public void SetPosition(Vector3 position)
        {
            if (this.position != position)
            {
                this.position = position;
                UpdateVectors();
            }
        }

        public float GetYaw()
        {
            return yaw;
        }
        public void SetYaw(float yaw)
        {
            if (this.yaw != yaw)
            {
                this.yaw = yaw;
                if (this.yaw > 360)
                    this.yaw = 0;
                if (this.yaw < 0)
                    this.yaw = 360;
                UpdateVectors();
            }
        }

        public float GetPitch()
        {
            return pitch;
        }
        public void SetPitch(float pitch)
        {
            if (this.pitch != pitch)
            {
                this.pitch = pitch;
                UpdateVectors();
            }
        }
        #endregion

        public Matrix4 GetViewMatrix()
        {
            return Matrix4.LookAt(position, position + front, up);
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
            float dist = float.PositiveInfinity;
            foreach (var point in tri.p)
            {
                float ddist = (position - point).Length;
                if (ddist < dist)
                    dist = ddist;
            }

            return dist < 15.0f;

        }

        public bool IsPointClose(Vector3 p)
        {
            float dist = (position - p).Length;

            return dist < 15.0f;

        }

        public bool IsAnyTriangleClose(List<triangle> tris)
        {
            float dist = float.PositiveInfinity;
            foreach(triangle triangle in tris)
            {
                foreach (var point in triangle.p)
                {
                    float ddist = (position - point).Length;
                    if (ddist < dist)
                        dist = ddist;
                }
            }

            return dist < 15.0f;

        }

        public bool IsAABBClose(AABB aabb)
        {
            Vector3[] corners = aabb.GetCorners();
            float dist = float.PositiveInfinity;
            foreach (var corner in corners)
            {
                float ddist = (position - corner).Length;
                if(ddist < dist) 
                    dist = ddist;
            }

            return dist < 15.0f;

        }

        public bool IsLineClose(Line line)
        {
            float dist1 = (position - line.Start).Length;
            float dist2 = (position - line.End).Length;
            float dist = float.PositiveInfinity;
            if (dist1 < dist) dist = dist1;
            if (dist2 < dist) dist = dist2;

            return dist < 15.0f;

        }

        private Frustum GetFrustum()
        {
            Frustum frustum = new Frustum();
            Matrix4 m = viewMatrix;
            m = m * projectionMatrixBigger;

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

            Vector3 front_ = new Vector3()
            {
                X = MathF.Cos(MathHelper.DegreesToRadians(pitch)) * MathF.Cos(MathHelper.DegreesToRadians(yaw)),
                Y = MathF.Sin(MathHelper.DegreesToRadians(pitch)),
                Z = MathF.Cos(MathHelper.DegreesToRadians(pitch)) * MathF.Sin(MathHelper.DegreesToRadians(yaw)),
            };
            front = front_;

            frontClamped = new Vector3(front.X, 0, front.Z);

            front.Normalize();
            frontClamped.Normalize();

            right = Vector3.Normalize(Vector3.Cross(front, Vector3.UnitY));
            up = Vector3.Normalize(Vector3.Cross(right, front));

            viewMatrix = GetViewMatrix();
            projectionMatrix = GetProjectionMatrix();
            projectionMatrixBigger = GetProjectionMatrixBigger(1.3f);
            frustum = GetFrustum();
        }
    }
}
