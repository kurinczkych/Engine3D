using MagicPhysX;
using OpenTK.Mathematics;
using static MagicPhysX.NativeMethods;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mario64
{
    public unsafe class SphereCollider : Mesh
    {
        PxRigidDynamic* sphereDynamicCollider;
        PxRigidStatic* sphereStaticCollider;

        public SphereCollider(VAO vao, VBO vbo, int shaderProgramId, string embeddedTextureName, int ocTreeDepth, Vector2 windowSize, ref Frustum frustum, ref Camera camera, ref int textureCount) :
    base(vao, vbo, shaderProgramId, embeddedTextureName, ocTreeDepth, windowSize, ref frustum, ref camera, ref textureCount)
        {
            OnlySphere(1f, 10);

            if (ocTreeDepth != -1)
            {
                CalculateBoundingBox();
                Octree = new Octree(new List<triangle>(tris), BoundingBox, ocTreeDepth);
            }

            ComputeVertexNormals(ref tris);

            SendUniforms();
        }

        public void Init(Vector3 pos, float radius, Vector3 rot)
        {
            Position = pos;
            Scale = new Vector3(radius, radius, radius);

            Vector3 radRot = new Vector3(MathHelper.DegreesToRadians(rot.X),
                                         MathHelper.DegreesToRadians(rot.Y),
                                         MathHelper.DegreesToRadians(rot.Z));

            Quaternion rotationX = Quaternion.FromAxisAngle(Vector3.UnitX, radRot.X);
            Quaternion rotationY = Quaternion.FromAxisAngle(Vector3.UnitY, radRot.Y);
            Quaternion rotationZ = Quaternion.FromAxisAngle(Vector3.UnitZ, radRot.Z);

            Rotation = rotationX * rotationY * rotationZ;
            Rotation.Normalize();
        }

        public void CollisionResponse()
        {
            if (sphereDynamicCollider != null)
            {
                PxTransform transform = PxRigidActor_getGlobalPose((PxRigidActor*)sphereDynamicCollider);

                // Centered origin to corner origin
                Position.X = transform.p.x;
                Position.Y = transform.p.y;
                Position.Z = transform.p.z;

                Rotation = QuatHelper.PxToOpenTk(transform.q);
            }
        }

        public void AddSphereCollider(bool isStatic, ref Physx physx)
        {
            var sphereGeo = PxSphereGeometry_new(Scale.X);
            PxVec3 vec3 = new PxVec3 { x = Position.X, y = Position.Y, z = Position.Z };
            PxQuat quat = QuatHelper.OpenTkToPx(Rotation);
            var transform = PxTransform_new_5(&vec3, &quat);
            var identity = PxTransform_new_2(PxIDENTITY.PxIdentity);
            var material = physx.physics->CreateMaterialMut(0.5f, 0.5f, 0.1f);

            if (isStatic)
            {
                sphereStaticCollider = physx.physics->PhysPxCreateStatic(&transform, (PxGeometry*)&sphereGeo, material, &identity);
                physx.scene->AddActorMut((PxActor*)sphereStaticCollider, null);
            }
            else
            {
                sphereDynamicCollider = physx.physics->PhysPxCreateDynamic(&transform, (PxGeometry*)&sphereGeo, material, 10.0f, &identity);
                PxRigidBody_setAngularDamping_mut((PxRigidBody*)sphereDynamicCollider, 0.5f);
                physx.scene->AddActorMut((PxActor*)sphereDynamicCollider, null);
            }
        }

        public void OnlySphere(float radius, int resolution)
        {
            tris = new List<triangle>();

            // Validate inputs
            if (radius <= 0 || resolution < 3)
                throw new ArgumentException("Invalid radius or resolution.");

            for (int i = 0; i < resolution; i++)
            {
                for (int j = 0; j < resolution; j++)
                {
                    float u1 = i / (float)resolution * MathF.PI * 2;
                    float u2 = (i + 1) / (float)resolution * MathF.PI * 2;
                    float v1 = j / (float)resolution * MathF.PI;
                    float v2 = (j + 1) / (float)resolution * MathF.PI;

                    Vector3 p1 = new Vector3(
                        radius * MathF.Sin(v1) * MathF.Cos(u1),
                        radius * MathF.Cos(v1),
                        radius * MathF.Sin(v1) * MathF.Sin(u1));

                    Vector3 p2 = new Vector3(
                        radius * MathF.Sin(v1) * MathF.Cos(u2),
                        radius * MathF.Cos(v1),
                        radius * MathF.Sin(v1) * MathF.Sin(u2));

                    Vector3 p3 = new Vector3(
                        radius * MathF.Sin(v2) * MathF.Cos(u1),
                        radius * MathF.Cos(v2),
                        radius * MathF.Sin(v2) * MathF.Sin(u1));

                    Vector3 p4 = new Vector3(
                        radius * MathF.Sin(v2) * MathF.Cos(u2),
                        radius * MathF.Cos(v2),
                        radius * MathF.Sin(v2) * MathF.Sin(u2));

                    tris.Add(new triangle(new Vector3[] { p1, p2, p3 }));
                    tris.Add(new triangle(new Vector3[] { p2, p4, p3 }));
                }
            }
        }
    }
}
