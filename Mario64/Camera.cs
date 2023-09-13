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
        private Vector3 front = -Vector3.UnitZ;
        private Vector3 right = Vector3.UnitX;

        private float yaw;
        private float pitch = -90.0f;

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

        public Matrix4 GetProjectionMatrix()
        {
            //projectionMatrix = Matrix_MakeProjection(fov, aspectRatio, near, far);
            return Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(fov), aspectRatio, near, far);
        }

        public Frustum GetFrustum()
        {
            Frustum frustum = new Frustum();

            float tanHalfFOV = (float)Math.Tan(MathHelper.DegreesToRadians(fov) / 2.0);
            float nearHeight = near * tanHalfFOV;
            float nearWidth = nearHeight * aspectRatio;

            float farHeight = far * tanHalfFOV;
            float farWidth = farHeight * aspectRatio;

            frustum.nearCenter = position + front * near;
            frustum.farCenter = position + front * far;

            frustum.ntl = frustum.nearCenter + (up * nearHeight) - (right * nearWidth);
            frustum.ntr = frustum.nearCenter + (up * nearHeight) + (right * nearWidth);
            frustum.nbl = frustum.nearCenter - (up * nearHeight) - (right * nearWidth);
            frustum.nbr = frustum.nearCenter - (up * nearHeight) + (right * nearWidth);

            frustum.ftl = frustum.farCenter + (up * farHeight) - (right * farWidth);
            frustum.ftr = frustum.farCenter + (up * farHeight) + (right * farWidth);
            frustum.fbl = frustum.farCenter - (up * farHeight) - (right * farWidth);
            frustum.fbr = frustum.farCenter - (up * farHeight) + (right * farWidth);

            // Near plane
            frustum.Near.normal = -front.Normalized();
            frustum.Near.distance = -Vector3.Dot(frustum.Near.normal, frustum.nbl);

            // Far plane
            frustum.Far.normal = front.Normalized();
            frustum.Far.distance = -Vector3.Dot(frustum.Far.normal, frustum.fbl);

            // Left plane
            Vector3 vecToPoint1l = frustum.nbl - position;
            Vector3 vecToPoint2l = frustum.ntl - position;
            frustum.Left.normal = Vector3.Cross(vecToPoint1l, vecToPoint2l).Normalized();
            frustum.Left.distance = -Vector3.Dot(frustum.Left.normal, frustum.nbl);

            // Right plane
            Vector3 vecToPoint1r = frustum.nbr - position;
            Vector3 vecToPoint2r = frustum.ntr - position;
            frustum.Right.normal = Vector3.Cross(vecToPoint2r, vecToPoint1r).Normalized();
            frustum.Right.distance = -Vector3.Dot(frustum.Right.normal, frustum.nbr);

            // Top plane
            Vector3 vecToPoint1t = frustum.ntl - position;
            Vector3 vecToPoint2t = frustum.ntr - position;
            frustum.Top.normal = Vector3.Cross(vecToPoint2t, vecToPoint1t).Normalized();
            frustum.Top.distance = -Vector3.Dot(frustum.Top.normal, frustum.ntl);

            // Bottom plane
            Vector3 vecToPoint1b = frustum.nbl - position;
            Vector3 vecToPoint2b = frustum.nbr - position;

            frustum.Bottom.normal = Vector3.Cross(vecToPoint1b, vecToPoint2b).Normalized();
            frustum.Bottom.distance = -Vector3.Dot(frustum.Bottom.normal, frustum.nbl);

            //Matrix4 viewProjectionMatrix = GetViewMatrix() * GetProjectionMatrix();

            //frustum.Left = new Plane(
            //new Vector3(viewProjectionMatrix.Row3 + viewProjectionMatrix.Row0).Normalized(),
            //(viewProjectionMatrix.Row3 + viewProjectionMatrix.Row0).W);

            //// Right plane
            //frustum.Right = new Plane(
            //    new Vector3(viewProjectionMatrix.Row3 - viewProjectionMatrix.Row0).Normalized(),
            //    (viewProjectionMatrix.Row3 - viewProjectionMatrix.Row0).W);

            //// Bottom plane
            //frustum.Bottom = new Plane(
            //    new Vector3(viewProjectionMatrix.Row3 + viewProjectionMatrix.Row1).Normalized(),
            //    (viewProjectionMatrix.Row3 + viewProjectionMatrix.Row1).W);

            //// Top plane
            //frustum.Top = new Plane(
            //    new Vector3(viewProjectionMatrix.Row3 - viewProjectionMatrix.Row1).Normalized(),
            //    (viewProjectionMatrix.Row3 - viewProjectionMatrix.Row1).W);

            //// Near plane
            //frustum.Near = new Plane(
            //    new Vector3(viewProjectionMatrix.Row3 + viewProjectionMatrix.Row2).Normalized(),
            //    (viewProjectionMatrix.Row3 + viewProjectionMatrix.Row2).W);

            //// Far plane
            //frustum.Far = new Plane(
            //    new Vector3(viewProjectionMatrix.Row3 - viewProjectionMatrix.Row2).Normalized(),
            //    (viewProjectionMatrix.Row3 - viewProjectionMatrix.Row2).W);
            return frustum;
        }

        private void UpdateVectors()
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
