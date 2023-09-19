using Cyotek.Drawing.BitmapFont;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mario64
{
    public class Character
    {
        private float sensitivity = 180f;
        private float speed = 2f;
        private float gravity = 1;

        private bool firstMove = true;
        public Vector2 lastPos;

        public Vector3 Position;
        public string PStr
        {
            get { return Position.X.ToString() + "," + Position.Y.ToString() + "," + Position.Z.ToString();}
        }

        public Vector3 Velocity;
        public string VStr
        {
            get { return Velocity.X.ToString() + "," + Velocity.Y.ToString() + "," + Velocity.Z.ToString(); }
        }

        public Camera camera;

        public Character(Vector3 position, Camera camera)
        {
            this.Position = position;
            Velocity = Vector3.Zero;

            this.camera = camera;
            camera.position = position;
        }

        public void UpdatePosition(KeyboardState keyboardState, MouseState mouseState, FrameEventArgs args)
        {
            //Velocity.Y -= gravity * (float)args.Time;

            //if (keyboardState.IsKeyDown(Keys.Space))
            //{
            //    Position.Y += speed * (float)args.Time;
            //}
            //if (keyboardState.IsKeyDown(Keys.LeftShift))
            //{
            //    Position.Y -= speed * (float)args.Time;
            //}

            float speed_ = speed;
            if (keyboardState.IsKeyDown(Keys.LeftShift))
                speed_ *= 2;

            if (keyboardState.IsKeyDown(Keys.W))
            {
                Velocity += (camera.frontClamped * speed_) * (float)args.Time;
            }
            if (keyboardState.IsKeyDown(Keys.S))
            {
                Velocity -= (camera.frontClamped * speed_) * (float)args.Time;
            }
            if (keyboardState.IsKeyDown(Keys.A))
            {
                Velocity -= (camera.right * speed_) * (float)args.Time;
            }
            if (keyboardState.IsKeyDown(Keys.D))
            {
                Velocity += (camera.right * speed_) * (float)args.Time;
            }

            Vector3 newPosition = Position + Velocity;
            if(newPosition.Y > 0)
            {
                Position += Velocity;
            }
            else
            {
                Position += new Vector3(Velocity.X, 0, Velocity.Z);
            }



            camera.position = Position;
            Velocity *= 0.9f;

            if (firstMove)
            {
                lastPos = new Vector2(mouseState.X, mouseState.Y);
                firstMove = false;
            }
            else
            {
                float deltaX = mouseState.X - lastPos.X;
                float deltaY = mouseState.Y - lastPos.Y;
                lastPos = new Vector2(mouseState.X, mouseState.Y);

                camera.yaw += deltaX * sensitivity * (float)args.Time;
                camera.pitch -= deltaY * sensitivity * (float)args.Time;
            }
            camera.UpdateVectors();
        }
    }
}
