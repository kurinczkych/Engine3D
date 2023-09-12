using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Mario64
{
    public class Camera
    {
        private float speed = 8f;
        private Vector2 screenSize;
        private float sensitivity = 180f;

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
        }

        public Matrix4 GetViewMatrix()
        {
            return Matrix4.LookAt(position, position + front, up);
        }

        public Matrix4 GetProjectionMatrix()
        {
            float near = 0.1f;
            float far = 1000.0f;
            float fov = 90.0f;
            float aspectRatio = screenSize.X / screenSize.Y;
            //projectionMatrix = Matrix_MakeProjection(fov, aspectRatio, near, far);
            return Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(fov), aspectRatio, near, far);
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
