using MagicPhysX;
using static MagicPhysX.NativeMethods;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mario64
{
    public unsafe class Cube : Mesh
    {
        PxRigidDynamic* cubeDynamicCollider;
        PxRigidStatic* cubeStaticCollider;

        public Vector3 Center
        {
            get
            {
                return new Vector3(Position.X + (Scale.X / 2f),
                                   Position.Y + (Scale.Y / 2f),
                                   Position.Z + (Scale.Z / 2f));
            }
            set
            {
                Vector3 pos = value;
                Position.X = pos.X - (Scale.X / 2f);
                Position.Y = pos.Y - (Scale.Y / 2f);
                Position.Z = pos.Z - (Scale.Z / 2f);
            }
        }

        public Cube(VAO vao, VBO vbo, int shaderProgramId, string embeddedTextureName, int ocTreeDepth, Vector2 windowSize, ref Frustum frustum, ref Camera camera, ref int textureCount) :
            base(vao, vbo, shaderProgramId, embeddedTextureName, ocTreeDepth, windowSize, ref frustum, ref camera, ref textureCount)
        {
            OnlyCube();

            if (ocTreeDepth != -1)
            {
                CalculateBoundingBox();
                Octree = new Octree(new List<triangle>(tris), BoundingBox, ocTreeDepth);
            }

            ComputeVertexNormals(ref tris);

            SendUniforms();
        }

        public void Init(Vector3 pos, Vector3 scale, Vector3 rot)
        {
            Position = pos;
            Scale = scale;

            Vector3 radRot = new Vector3(MathHelper.DegreesToRadians(rot.X),
                                         MathHelper.DegreesToRadians(rot.Y),
                                         MathHelper.DegreesToRadians(rot.Z));

            Quaternion rotationX = Quaternion.FromAxisAngle(Vector3.UnitX, radRot.X);
            Quaternion rotationY = Quaternion.FromAxisAngle(Vector3.UnitY, radRot.Y);
            Quaternion rotationZ = Quaternion.FromAxisAngle(Vector3.UnitZ, radRot.Z);

            // Combine the rotations in a roll-pitch - yaw(x - y - z) order.
            //yaw-pitch-roll

            Rotation = rotationZ * rotationY * rotationX;
            Rotation.Normalize();

            //TODO: here rot (90,0,0) gets translated to quat and to euler back and becames (0,0,0)
            // this is not good
            Vector3 a = new Vector3();
            Rotation.ToEulerAngles(out a);
        }

        public void CollisionResponse()
        {
            if (cubeDynamicCollider != null)
            {
                PxTransform transform = PxRigidActor_getGlobalPose((PxRigidActor*)cubeDynamicCollider);

                // Centered origin to corner origin
                Position.X = transform.p.x - (Scale.X / 2f);
                Position.Y = transform.p.y - (Scale.Y / 2f);
                Position.Z = transform.p.z - (Scale.Z / 2f);

                Rotation = QuatHelper.PxToOpenTk(transform.q);
                Rotation.Normalize();
            }
        }

        public void AddCubeCollider(bool isStatic, ref Physx physx)
        {
            var cubeGeo = PxBoxGeometry_new(Scale.X / 2f, Scale.Y / 2f , Scale.Z / 2f);
            PxVec3 vec3 = new PxVec3 { x = Center.X, y = Center.Y, z = Center.Z };
            PxQuat quat = QuatHelper.OpenTkToPx(Rotation);
            var transform = PxTransform_new_5(&vec3, &quat);
            var identity = PxTransform_new_2(PxIDENTITY.PxIdentity);
            var material = physx.physics->CreateMaterialMut(0.5f, 0.5f, 0.1f);

            if (isStatic)
            {
                cubeStaticCollider = physx.physics->PhysPxCreateStatic(&transform, (PxGeometry*)&cubeGeo, material, &identity);
                physx.scene->AddActorMut((PxActor*)cubeStaticCollider, null);
            }
            else
            {
                cubeDynamicCollider = physx.physics->PhysPxCreateDynamic(&transform, (PxGeometry*)&cubeGeo, material, 10.0f, &identity);
                PxRigidBody_setAngularDamping_mut((PxRigidBody*)cubeDynamicCollider, 0.5f);
                physx.scene->AddActorMut((PxActor*)cubeDynamicCollider, null);
            }
        }


        private void OnlyCube()
        {
            tris = new List<triangle>
                {
                    new triangle(new Vector3[] { new Vector3(0.0f, 0.0f, 0.0f), new Vector3(0.0f, 1.0f, 0.0f), new Vector3(1.0f, 1.0f, 0.0f) }, new Vec2d[] { new Vec2d(0.0f, 1.0f), new Vec2d(0.0f, 0.0f), new Vec2d(1.0f, 0.0f) }),
                    new triangle(new Vector3[] { new Vector3(0.0f, 0.0f, 0.0f), new Vector3(1.0f, 1.0f, 0.0f), new Vector3(1.0f, 0.0f, 0.0f) }, new Vec2d[] { new Vec2d(0.0f, 1.0f), new Vec2d(1.0f, 0.0f), new Vec2d(1.0f, 1.0f) }),
                    new triangle(new Vector3[] { new Vector3(1.0f, 0.0f, 0.0f), new Vector3(1.0f, 1.0f, 0.0f), new Vector3(1.0f, 1.0f, 1.0f) }, new Vec2d[] { new Vec2d(0.0f, 1.0f), new Vec2d(0.0f, 0.0f), new Vec2d(1.0f, 0.0f) }),
                    new triangle(new Vector3[] { new Vector3(1.0f, 0.0f, 0.0f), new Vector3(1.0f, 1.0f, 1.0f), new Vector3(1.0f, 0.0f, 1.0f) }, new Vec2d[] { new Vec2d(0.0f, 1.0f), new Vec2d(1.0f, 0.0f), new Vec2d(1.0f, 1.0f) }),
                    new triangle(new Vector3[] { new Vector3(1.0f, 0.0f, 1.0f), new Vector3(1.0f, 1.0f, 1.0f), new Vector3(0.0f, 1.0f, 1.0f) }, new Vec2d[] { new Vec2d(0.0f, 1.0f), new Vec2d(0.0f, 0.0f), new Vec2d(1.0f, 0.0f) }),
                    new triangle(new Vector3[] { new Vector3(1.0f, 0.0f, 1.0f), new Vector3(0.0f, 1.0f, 1.0f), new Vector3(0.0f, 0.0f, 1.0f) }, new Vec2d[] { new Vec2d(0.0f, 1.0f), new Vec2d(1.0f, 0.0f), new Vec2d(1.0f, 1.0f) }),
                    new triangle(new Vector3[] { new Vector3(0.0f, 0.0f, 1.0f), new Vector3(0.0f, 1.0f, 1.0f), new Vector3(0.0f, 1.0f, 0.0f) }, new Vec2d[] { new Vec2d(0.0f, 1.0f), new Vec2d(0.0f, 0.0f), new Vec2d(1.0f, 0.0f) }),
                    new triangle(new Vector3[] { new Vector3(0.0f, 0.0f, 1.0f), new Vector3(0.0f, 1.0f, 0.0f), new Vector3(0.0f, 0.0f, 0.0f) }, new Vec2d[] { new Vec2d(0.0f, 1.0f), new Vec2d(1.0f, 0.0f), new Vec2d(1.0f, 1.0f) }),
                    new triangle(new Vector3[] { new Vector3(0.0f, 1.0f, 0.0f), new Vector3(0.0f, 1.0f, 1.0f), new Vector3(1.0f, 1.0f, 1.0f) }, new Vec2d[] { new Vec2d(0.0f, 1.0f), new Vec2d(0.0f, 0.0f), new Vec2d(1.0f, 0.0f) }),
                    new triangle(new Vector3[] { new Vector3(0.0f, 1.0f, 0.0f), new Vector3(1.0f, 1.0f, 1.0f), new Vector3(1.0f, 1.0f, 0.0f) }, new Vec2d[] { new Vec2d(0.0f, 1.0f), new Vec2d(1.0f, 0.0f), new Vec2d(1.0f, 1.0f) }),
                    new triangle(new Vector3[] { new Vector3(1.0f, 0.0f, 1.0f), new Vector3(0.0f, 0.0f, 1.0f), new Vector3(0.0f, 0.0f, 0.0f) }, new Vec2d[] { new Vec2d(0.0f, 1.0f), new Vec2d(0.0f, 0.0f), new Vec2d(1.0f, 0.0f) }),
                    new triangle(new Vector3[] { new Vector3(1.0f, 0.0f, 1.0f), new Vector3(0.0f, 0.0f, 0.0f), new Vector3(1.0f, 0.0f, 0.0f) }, new Vec2d[] { new Vec2d(0.0f, 1.0f), new Vec2d(1.0f, 0.0f), new Vec2d(1.0f, 1.0f) })
                };
        }
    }
}
