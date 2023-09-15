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
        private float speed = 8f;
        private Vector2 screenSize;
        private float sensitivity = 180f;

        public float near;
        public float far;
        public float fov;
        public float aspectRatio;

        public Vector3 position;
        public Vec3d position_
        {
            get { return new Vec3d(position.X, position.Y, position.Z); }
        }
        private Vector3 up = Vector3.UnitY;
        public Vector3 front = -Vector3.UnitZ;
        private Vector3 right = Vector3.UnitX;

        private float yaw;
        //private float pitch = -90.0f;
        private float pitch = 0f;

        private bool firstMove = true;
        public Vector2 lastPos;

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

        public Frustum GetFrustum()
        {
            Frustum frustum = new Frustum();
            Matrix4 m = GetViewMatrix();
            m = m * GetProjectionMatrixBigger(0.7f);

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


            //Normalize all plane normals
            //for (int i = 0; i < 6; i++)
            //    frustum.planes[i].normal.Normalize();
            for (int i = 0; i < 6; i++)
            {
                float magnitude = frustum.planes[i].normal.Length;
                frustum.planes[i].normal /= magnitude;
                frustum.planes[i].distance /= magnitude;
            }
            //--------------------------------------------------------------------------------------

            //float tanHalfFOV = (float)Math.Tan(MathHelper.DegreesToRadians(fov) / 2.0);
            //float nearHeight = 2.0f * tanHalfFOV * near;
            //float nearWidth = nearHeight * aspectRatio;

            //float farHeight = 2.0f * tanHalfFOV * far;
            //float farWidth = farHeight * aspectRatio;

            //// Near plane corners
            //frustum.ntl = new Vector3(-nearWidth / 2.0f, nearHeight / 2.0f, -near);
            //frustum.ntr = new Vector3(nearWidth / 2.0f, nearHeight / 2.0f, -near);
            //frustum.nbl = new Vector3(-nearWidth / 2.0f, -nearHeight / 2.0f, -near);
            //frustum.nbr = new Vector3(nearWidth / 2.0f, -nearHeight / 2.0f, -near);

            //// Far plane corners
            //frustum.ftl = new Vector3(-farWidth / 2.0f, farHeight / 2.0f, -far);
            //frustum.ftr = new Vector3(farWidth / 2.0f, farHeight / 2.0f, -far);
            //frustum.fbl = new Vector3(-farWidth / 2.0f, -farHeight / 2.0f, -far);
            //frustum.fbr = new Vector3(farWidth / 2.0f, -farHeight / 2.0f, -far);

            //Matrix4 viewInverse = Matrix4.Invert(GetViewMatrix());

            //frustum.ntl = Vector3.TransformPosition(frustum.ntl, viewInverse);
            //frustum.ntr = Vector3.TransformPosition(frustum.ntr, viewInverse);
            //frustum.nbl = Vector3.TransformPosition(frustum.nbl, viewInverse);
            //frustum.nbr = Vector3.TransformPosition(frustum.nbr, viewInverse);

            //frustum.ftl = Vector3.TransformPosition(frustum.ftl, viewInverse);
            //frustum.ftr = Vector3.TransformPosition(frustum.ftr, viewInverse);
            //frustum.fbl = Vector3.TransformPosition(frustum.fbl, viewInverse);
            //frustum.fbr = Vector3.TransformPosition(frustum.fbr, viewInverse);

            return frustum;
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

            front.Normalize();

            right = Vector3.Normalize(Vector3.Cross(front, Vector3.UnitY));
            up = Vector3.Normalize(Vector3.Cross(right, front));
        }

        public void Update(KeyboardState keyboardState, MouseState mouseState, FrameEventArgs args)
        {
            if (keyboardState.IsKeyDown(Keys.Space))
            {
                position.Y += speed * (float)args.Time;
            }
            if (keyboardState.IsKeyDown(Keys.LeftShift))
            {
                position.Y -= speed * (float)args.Time;
            }

            if (keyboardState.IsKeyDown(Keys.W))
            {
                position += (front * speed) * (float)args.Time;
            }
            if (keyboardState.IsKeyDown(Keys.S))
            {
                position -= (front * speed) * (float)args.Time;
            }
            if (keyboardState.IsKeyDown(Keys.A))
            {
                position -= (right * speed) * (float)args.Time;
            }
            if (keyboardState.IsKeyDown(Keys.D))
            {
               position += (right * speed) * (float)args.Time;
            }

            if(firstMove)
            {
                lastPos = new Vector2(mouseState.X, mouseState.Y);
                firstMove = false;
            }
            else
            {
                float deltaX = mouseState.X - lastPos.X;
                float deltaY = mouseState.Y - lastPos.Y;
                lastPos = new Vector2(mouseState.X, mouseState.Y);

                yaw += deltaX * sensitivity * (float)args.Time;
                pitch -= deltaY * sensitivity * (float)args.Time;
            }
            UpdateVectors();
        }
    }
}
