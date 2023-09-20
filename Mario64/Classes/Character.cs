using Cyotek.Drawing.BitmapFont;
using OpenTK.Audio.OpenAL;
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
        //private float gravity = 0.7f;
        private float gravity = 120;
        private float jumpForce = 0.1f;
        private float terminalVelocity = -0.7f;
        private float characterHeight = 4f;

        private float groundY;
        public string groundYStr
        {
            get { return Math.Round(groundY, 2).ToString(); }
        }

        private float distToGround;
        public string distToGroundStr
        {
            get { return Math.Round(distToGround, 2).ToString(); }
        }

        private bool isOnGround = false;
        public string isOnGroundStr
        {
            get { return isOnGround.ToString(); }
        }

        private bool firstMove = true;
        public Vector2 lastPos;

        public Vector3 Position;
        public string PStr
        {
            get { return Math.Round(Position.X, 2).ToString() + "," + Math.Round(Position.Y, 2).ToString() + "," + Math.Round(Position.Z, 2).ToString(); }
        }

        public Vector3 Velocity;
        public string VStr
        {
            get { return Math.Round(Velocity.X, 2).ToString() + "," + Math.Round(Velocity.Y, 2).ToString() + "," + Math.Round(Velocity.Z, 2).ToString(); }
        }

        public Camera camera;

        public Character(Vector3 position, Camera camera)
        {
            this.Position = position;
            Velocity = Vector3.Zero;

            this.camera = camera;
            camera.position = position;
        }

        public void UpdatePosition(KeyboardState keyboardState, MouseState mouseState, ref Octree octree, FrameEventArgs args)
        {
            if(!isOnGround)
                Velocity.Y = Velocity.Y - gravity * (float)Math.Pow(args.Time, 2) / 2;

            if (Velocity.Y < terminalVelocity)
                Velocity.Y = terminalVelocity;


            GetGround(ref octree);

            if (keyboardState.IsKeyDown(Keys.Space) && isOnGround)
            {
                Velocity.Y += jumpForce;
            }

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

            // 3. Update Position
            Position += Velocity;

            // 4. Collision Detection / Ground Check
            if (Position.Y - characterHeight <= groundY)
            {
                Position.Y = groundY + characterHeight;
                Velocity.Y = 0;
                isOnGround = true;
            }
            else
            {
                isOnGround = false;
            }

            camera.position = Position;
            Velocity.Xz *= 0.9f;
            ZeroSmallVelocity();

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

        private void GetGround(ref Octree octree)
        {
            List<triangle> tris = octree.GetNearTriangles(Position);
            triangle closest = new triangle();

            Vector3 rayDir = new Vector3(0, -1, 0);
            Vector3 intersect = new Vector3();

            foreach (triangle triangle in tris)
            {
                if(triangle.RayIntersects(Position, rayDir, out intersect))
                {
                    closest = triangle;
                    break;
                }    
            }

            Vector3 pointAbove = closest.GetPointAboveXZ(Position);

            Vector3 center = closest.GetMiddle();
            float distToCenter = Position.Y - center.Y;
            groundY = center.Y;

            if (pointAbove != Vector3.NegativeInfinity)
            {
                distToCenter = Position.Y - pointAbove.Y;
                groundY = pointAbove.Y;
            }
        }

        private void ZeroSmallVelocity()
        {
            if (Velocity.X < 0.0001f && Velocity.X > -0.0001f)
                Velocity.X = 0;

            if (Velocity.Y < 0.0001f && Velocity.Y > -0.0001f)
                Velocity.Y = 0;

            if (Velocity.Z < 0.0001f && Velocity.Z > -0.0001f)
                Velocity.Z = 0;
        }
    }
}
