using Cyotek.Drawing.BitmapFont;
using MagicPhysX;
using OpenTK.Audio.OpenAL;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Engine3D
{
    public struct TriangleCollision
    {
        public TriangleCollision(triangle triangle, Vector3 penetration_normal, float penetration_depth)
        {
            this.triangle = triangle;
            this.penetration_normal = penetration_normal;
            this.penetration_depth = penetration_depth;
        }

        public triangle triangle;
        public Vector3 penetration_normal;
        public float penetration_depth;
    }

    public class Character : Object
    {
        private float sensitivity = 180f;
        private float speed = 2f;
        private float flySpeed = 10f;
        //private float gravity = 0.7f;
        public bool applyGravity = true;
        private float gravity = 120;
        private float jumpForce = 0.08f;
        private float terminalVelocity = -0.7f;
        private float characterHeight = 4f;
        private float characterWidth = 2f;

        private float thirdY = 10f;

        private bool noClip = true;

        private bool firstMove = true;
        public Vector2 lastPos;

        private Vector3 OrigPosition;

        public Camera camera;

        public Character(WireframeMesh mesh, ObjectType type, ref Physx physx, Vector3 position, Camera camera) : base(mesh, type, ref physx)
        {
            OrigPosition = position;
            Position = position;

            this.camera = camera;
            camera.position = position;

            SetPosition(Position);
            SetSize(characterHeight, characterWidth);
            AddCapsuleCollider(false);
        }

        public void UpdatePosition(KeyboardState keyboardState, MouseState mouseState, FrameEventArgs args)
        {
            float speed_ = speed;
            float flySpeed_ = flySpeed;
            if (keyboardState.IsKeyDown(Keys.LeftShift))
            {
                speed_ *= 2;
                flySpeed_ *= 2;
            }

            if(keyboardState.IsKeyReleased(Keys.N))
            {
                noClip = !noClip;

                SetGravity(noClip);
            }

            bool isOnGround = IsOnGround();

            if (!noClip)
            {
                if (keyboardState.IsKeyDown(Keys.Space) && isOnGround)
                {
                    if (!noClip)
                        AddLinearVelocity(0, jumpForce, 0);
                }
            }
            else
            {
                if (keyboardState.IsKeyDown(Keys.Space))
                {
                    AddPosition(0, flySpeed_ * 10 * (float)args.Time, 0);
                }
            }

            if (keyboardState.IsKeyDown(Keys.LeftControl))
            {
                if(noClip)
                    AddPosition(0, -1 * flySpeed_ * 10 * (float)args.Time, 0);
            }

            if (keyboardState.IsKeyDown(Keys.Enter) || keyboardState.IsKeyDown(Keys.KeyPadEnter))
            {
                SetPosition(OrigPosition);
            }


            if (keyboardState.IsKeyDown(Keys.W))
            {
                if(!noClip)
                    AddLinearVelocity((camera.frontClamped * speed_) * (float)args.Time);
                else
                    AddLinearVelocity((camera.frontClamped * flySpeed) * (float)args.Time);
            }
            if (keyboardState.IsKeyDown(Keys.S))
            {
                if (!noClip)
                    AddLinearVelocity(-1 * (camera.frontClamped * speed_) * (float)args.Time);
                else
                    AddLinearVelocity(-1 * (camera.frontClamped * flySpeed) * (float)args.Time);
            }
            if (keyboardState.IsKeyDown(Keys.A))
            {
                if (!noClip)
                    AddLinearVelocity(-1 * (camera.right * speed_) * (float)args.Time);
                else
                    AddLinearVelocity(-1 * (camera.right * flySpeed) * (float)args.Time);
            }
            if (keyboardState.IsKeyDown(Keys.D))
            {
                if (!noClip)
                    AddLinearVelocity((camera.right * speed_) * (float)args.Time);
                else
                    AddLinearVelocity((camera.right * flySpeed) * (float)args.Time);
            }

            //ZeroSmallVelocity();

            camera.position = Position;
            camera.position.Y += thirdY;
            camera.position.X -= (float)Math.Cos(MathHelper.DegreesToRadians(camera.yaw)) * thirdY;//-6.97959471
            camera.position.Z -= (float)Math.Sin(MathHelper.DegreesToRadians(camera.yaw)) * thirdY;//-7.161373

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

                camera.yaw += deltaX * sensitivity * (float)args.Time;//45.73648
                camera.pitch -= deltaY * sensitivity * (float)args.Time;//-18.75002
            }
            camera.UpdateVectors();
        }


        public List<Line> GetBoundLines()
        {
            Capsule c = new Capsule(characterWidth, characterHeight*2, Position - new Vector3(0, characterHeight, 0));
            List<Line> lines = c.GetWireframe(10);

            return lines;
        }
    }
}
