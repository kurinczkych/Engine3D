using Cyotek.Drawing.BitmapFont;
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

namespace Mario64
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

    public class Character
    {
        private float sensitivity = 180f;
        private float speed = 2f;
        //private float gravity = 0.7f;
        public bool applyGravity = true;
        private float gravity = 120;
        private float jumpForce = 0.1f;
        private float terminalVelocity = -0.7f;
        private float characterHeight = 4f;
        private float characterWidth = 2f;

        private triangle groundTriangle;
        public float angleOfGround;
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

        private Vector3 OrigPosition;
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
            OrigPosition = position;
            Position = position;
            Velocity = Vector3.Zero;

            this.camera = camera;
            camera.position = position;
        }

        public void UpdatePosition(KeyboardState keyboardState, MouseState mouseState, ref Octree octree, FrameEventArgs args)
        {
            if(applyGravity)
                Velocity.Y = Velocity.Y - gravity * (float)Math.Pow(args.Time, 2) / 2;

            if (Velocity.Y < terminalVelocity)
                Velocity.Y = terminalVelocity;


            GetGround(ref octree);

            if (keyboardState.IsKeyDown(Keys.Space) && isOnGround)
            {
                Velocity.Y += jumpForce;
            }

            if (keyboardState.IsKeyDown(Keys.Enter))
            {
                Position = OrigPosition;
                Velocity = Vector3.Zero;
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

            angleOfGround = groundTriangle.GetAngleToNormal(new Vector3(0, -1, 0));

            // 3. Update Position
            //Position += Velocity;
            Capsule capsule = new Capsule(characterWidth, characterHeight * 2, Position - new Vector3(0, characterHeight, 0));

            int ccdMax = 5;
            bool intersection = false;
            Vector3 step = Velocity / ccdMax;
            bool disabledGravity = false;

            for (int i = 0; i < ccdMax; i++)
            {
                Position += step;
                foreach (triangle tri in octree.GetNearTriangles(Position))
                {
                    Vector3 penetration_normal = new Vector3();
                    float penetration_depth = 0.0f;
                    if (!tri.IsCapsuleInTriangle(capsule, out penetration_normal, out penetration_depth))
                        continue;

                    if (float.IsNaN(penetration_normal.X) || float.IsNaN(penetration_normal.Y) || float.IsNaN(penetration_normal.Z))
                        continue;

                    // Modify player velocity to slide on contact surface:
                    float velocity_length = Velocity.Length;
                    Vector3 velocity_normalized = Velocity.Normalized();
                    Vector3 undesired_motion = penetration_normal * Vector3.Dot(velocity_normalized, penetration_normal);
                    Vector3 desired_motion = velocity_normalized - undesired_motion;
                    Velocity = desired_motion * velocity_length;

                    // Remove penetration (penetration epsilon added to handle infinitely small penetration):
                    Position += (penetration_normal * (penetration_depth + 0.0001f));

                    intersection = true;

                    if (Math.Abs(tri.GetAngleToNormal(new Vector3(0, -1, 0))) < 45)
                    {
                        applyGravity = false;
                        Velocity.Y = 0;
                        Velocity *= new Vector3(0.9f, 1, 0.9f);
                        disabledGravity = true;
                    }
                    else if (!disabledGravity)
                    {
                        applyGravity = true;
                    }
                }

                //if (intersection)
                //    break;
            }

            if (!intersection)
                isOnGround = false;

            if (float.IsNaN(Position.X) || float.IsNaN(Position.Y) || float.IsNaN(Position.Z))
                ;

            ZeroSmallVelocity();
            //// 4. Collision Detection / Ground Check
            //if (Position.Y - characterHeight <= groundY)
            //{
            //    Position.Y = groundY + characterHeight;
            //    Velocity.Y = 0;
            //    isOnGround = true;
            //}
            //else
            //{
            //    isOnGround = false;
            //}

            camera.position = Position;
            camera.position.Y = 10;
            camera.position.X += 5;
            camera.position.Z += 5;

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
            groundTriangle = closest;
            distToGround = Position.Y - center.Y;
            groundY = center.Y;

            if (pointAbove != Vector3.NegativeInfinity)
            {
                distToGround = Position.Y - pointAbove.Y;
                groundY = pointAbove.Y;
            }
        }

        public List<Line> GetBoundLines()
        {
            Capsule c = new Capsule(characterWidth, characterHeight*2, Position - new Vector3(0, characterHeight, 0));
            List<Line> lines = c.GetWireframe(10);

            return lines;

            //Vector3 center = Position + new Vector3(0, -characterHeight/2, 0);

            //float halfWidth = characterWidth / 2;
            //float halfDepth = halfWidth;  // Assuming depth is same as width

            //Dictionary<string, Vector3> corners = new Dictionary<string, Vector3>
            //{
            //    { "LBL", new Vector3(-halfWidth, 0, -halfDepth) },                 // LBL  Left - Bottom Left
            //    { "LBR", new Vector3(halfWidth, 0, -halfDepth) },                  // LBR  Left - Bottom Right
            //    { "LTL", new Vector3(-halfWidth, characterHeight, -halfDepth) },   // LTL  Left - Top Left
            //    { "LTR", new Vector3(halfWidth, characterHeight, -halfDepth) },    // LTR  Left - Top Right
            //    { "RBR", new Vector3(-halfWidth, 0, halfDepth) },                  // RBR  Right - Bottom Left
            //    { "RBL", new Vector3(halfWidth, 0, halfDepth) },                   // RBL  Right - Bottom Right
            //    { "RTR", new Vector3(-halfWidth, characterHeight, halfDepth) },    // RTR  Right - Top Left
            //    { "RTL", new Vector3(halfWidth, characterHeight, halfDepth) }     // RTL  Right - Top Right
            //};

            //Matrix4 rotY = Matrix4.CreateRotationY(MathHelper.DegreesToRadians(-camera.yaw));
            //foreach(string key in corners.Keys)
            //{
            //    corners[key] = center + Vector3.TransformPosition(corners[key], rotY);
            //}

            //List<Line> lines = new List<Line>
            //{
            //    //FRONT
            //    new Line(corners["LBR"], corners["LTR"]),
            //    new Line(corners["LBR"], corners["RBL"]),
            //    new Line(corners["RTL"], corners["LTR"]),
            //    new Line(corners["RTL"], corners["RBL"]),

            //    //BACK
            //    new Line(corners["LBL"], corners["LTL"]),
            //    new Line(corners["LBL"], corners["RBR"]),
            //    new Line(corners["RTR"], corners["LTL"]),
            //    new Line(corners["RTR"], corners["RBR"]),

            //    //LEFT
            //    new Line(corners["LBL"], corners["LTL"]),
            //    new Line(corners["LBL"], corners["LBR"]),
            //    new Line(corners["LTR"], corners["LTL"]),
            //    new Line(corners["LTR"], corners["LBR"]),

            //    //LEFT
            //    new Line(corners["RBL"], corners["RTL"]),
            //    new Line(corners["RBL"], corners["RBR"]),
            //    new Line(corners["RTR"], corners["RTL"]),
            //    new Line(corners["RTR"], corners["RBR"])
            //};

            //return lines;
        }

        private List<Vector3> GetBoundRectangle(string side)
        {
            Vector3 center = Position + new Vector3(0, -characterHeight / 2, 0);

            float halfWidth = characterWidth / 2;
            float halfDepth = halfWidth;  // Assuming depth is same as width

            Dictionary<string, Vector3> corners = new Dictionary<string, Vector3>
            {
                { "LBL", new Vector3(-halfWidth, 0, -halfDepth) },                 // LBL  Left - Bottom Left
                { "LBR", new Vector3(halfWidth, 0, -halfDepth) },                  // LBR  Left - Bottom Right
                { "LTL", new Vector3(-halfWidth, characterHeight, -halfDepth) },   // LTL  Left - Top Left
                { "LTR", new Vector3(halfWidth, characterHeight, -halfDepth) },    // LTR  Left - Top Right
                { "RBR", new Vector3(-halfWidth, 0, halfDepth) },                  // RBR  Right - Bottom Left
                { "RBL", new Vector3(halfWidth, 0, halfDepth) },                   // RBL  Right - Bottom Right
                { "RTR", new Vector3(-halfWidth, characterHeight, halfDepth) },    // RTR  Right - Top Left
                { "RTL", new Vector3(halfWidth, characterHeight, halfDepth) }     // RTL  Right - Top Right
            };

            Matrix4 rotY = Matrix4.CreateRotationY(MathHelper.DegreesToRadians(-camera.yaw));
            foreach (string key in corners.Keys)
            {
                corners[key] = center + Vector3.TransformPosition(corners[key], rotY);
            }

            List<Vector3> rectCorners = new List<Vector3>();

            if (side == "left")
            {
                rectCorners = new List<Vector3>()
                {
                    corners["LTR"],corners["LBR"], corners["LBL"], corners["LTL"]
                };
            }
            else if (side == "right")
            {
                rectCorners = new List<Vector3>()
                {
                    corners["RTR"],corners["RBR"], corners["RBL"], corners["RTL"]
                };
            }
            else if (side == "front")
            {
                rectCorners = new List<Vector3>()
                {
                    corners["LTR"],corners["RTL"], corners["RBL"], corners["LBR"]
                };
            }
            else if (side == "back")
            {
                rectCorners = new List<Vector3>()
                {
                    corners["LTL"],corners["RTR"], corners["RBR"], corners["LBL"]
                };
            }

            return rectCorners;
        }

        public List<triangle> GetTrianglesColliding(ref Octree octree)
        {
            List<triangle> tris = octree.GetNearTriangles(Position);

            Capsule capsule = new Capsule(characterWidth, characterHeight * 2, Position - new Vector3(0, characterHeight, 0));
            List<TriangleCollision> collidedTris = new List<TriangleCollision>();

            foreach (triangle tri in octree.GetNearTriangles(Position))
            {
                Vector3 penetration_normal = new Vector3();
                float penetration_depth = 0.0f;
                if (tri.IsCapsuleInTriangle(capsule, out penetration_normal, out penetration_depth))
                    collidedTris.Add(new TriangleCollision(tri, penetration_normal, penetration_depth));
            }

            return collidedTris.Select(x => x.triangle).ToList();
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
