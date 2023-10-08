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
    public unsafe class CapsuleCollider : Mesh
    {
        PxRigidDynamic* capsuleDynamicCollider;
        PxRigidStatic* capsuleStaticCollider;

        public CapsuleCollider(VAO vao, VBO vbo, int shaderProgramId, string embeddedTextureName, int ocTreeDepth, Vector2 windowSize, ref Frustum frustum, ref Camera camera, ref int textureCount) :
            base(vao, vbo, shaderProgramId, embeddedTextureName, ocTreeDepth, windowSize, ref frustum, ref camera, ref textureCount)
        {
            OnlyCapsule(2, 5, 10);

            if (ocTreeDepth != -1)
            {
                CalculateBoundingBox();
                Octree = new Octree(new List<triangle>(tris), BoundingBox, ocTreeDepth);
            }

            ComputeVertexNormals(ref tris);

            SendUniforms();
        }

        public void Init(Vector3 pos, float halfHeight, float radius, Vector3 rot)
        {
            Position = pos;
            Scale = new Vector3(halfHeight, radius, 1);

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
            if (capsuleDynamicCollider != null)
            {
                PxTransform transform = PxRigidActor_getGlobalPose((PxRigidActor*)capsuleDynamicCollider);

                // Centered origin to corner origin
                Position.X = transform.p.x;
                Position.Y = transform.p.y;
                Position.Z = transform.p.z;

                Rotation = QuatHelper.PxToOpenTk(transform.q);
            }
        }

        public void AddCapsuleCollider(bool isStatic, ref Physx physx)
        {
            var capsuleGeo = PxCapsuleGeometry_new(Scale.Y, Scale.X);
            PxVec3 vec3 = new PxVec3 { x = Position.X, y = Position.Y, z = Position.Z };
            PxQuat quat = QuatHelper.OpenTkToPx(Rotation);
            var transform = PxTransform_new_5(&vec3, &quat);
            var identity = PxTransform_new_2(PxIDENTITY.PxIdentity);
            var material = physx.physics->CreateMaterialMut(0.5f, 0.5f, 0.1f);

            if (isStatic)
            {
                capsuleStaticCollider = physx.physics->PhysPxCreateStatic(&transform, (PxGeometry*)&capsuleGeo, material, &identity);
                physx.scene->AddActorMut((PxActor*)capsuleStaticCollider, null);
            }
            else
            {
                capsuleDynamicCollider = physx.physics->PhysPxCreateDynamic(&transform, (PxGeometry*)&capsuleGeo, material, 10.0f, &identity);
                PxRigidBody_setAngularDamping_mut((PxRigidBody*)capsuleDynamicCollider, 0.5f);
                physx.scene->AddActorMut((PxActor*)capsuleDynamicCollider, null);
            }
        }

        public void OnlyCapsule(float radius, float halfHeight, int resolution)
        {
            tris = new List<triangle>();

            // Validate inputs
            if (radius <= 0 || resolution < 3)
                throw new ArgumentException("Invalid radius or resolution.");

            // Generate the top and bottom hemispheres
            for (int i = 0; i < resolution; i++)
            {
                for (int j = 0; j < resolution / 2; j++) // Only half the resolution for hemispheres
                {
                    float u1 = i / (float)resolution * MathF.PI * 2;
                    float u2 = (i + 1) / (float)resolution * MathF.PI * 2;
                    float v1 = j / (float)resolution * MathF.PI;
                    float v2 = (j + 1) / (float)resolution * MathF.PI;

                    Vector3 p1 = new Vector3(
                        halfHeight + radius * MathF.Cos(v1),
                        radius * MathF.Sin(v1) * MathF.Cos(u1),
                        radius * MathF.Sin(v1) * MathF.Sin(u1));

                    Vector3 p2 = new Vector3(
                        halfHeight + radius * MathF.Cos(v1),
                        radius * MathF.Sin(v1) * MathF.Cos(u2),
                        radius * MathF.Sin(v1) * MathF.Sin(u2));

                    Vector3 p3 = new Vector3(
                        halfHeight + radius * MathF.Cos(v2),
                        radius * MathF.Sin(v2) * MathF.Cos(u1),
                        radius * MathF.Sin(v2) * MathF.Sin(u1));

                    Vector3 p4 = new Vector3(
                        halfHeight + radius * MathF.Cos(v2),
                        radius * MathF.Sin(v2) * MathF.Cos(u2),
                        radius * MathF.Sin(v2) * MathF.Sin(u2));

                    tris.Add(new triangle(p1, p3, p2));
                    tris.Add(new triangle(p2, p3, p4));

                    // Back hemisphere (invert the x-coordinates)
                    Vector3 p1b = new Vector3(-p1.X, p1.Y, p1.Z);
                    Vector3 p2b = new Vector3(-p2.X, p2.Y, p2.Z);
                    Vector3 p3b = new Vector3(-p3.X, p3.Y, p3.Z);
                    Vector3 p4b = new Vector3(-p4.X, p4.Y, p4.Z);

                    tris.Add(new triangle(p1b, p2b, p3b));
                    tris.Add(new triangle(p2b, p4b, p3b));
                }
            }

            // Generate the cylindrical segment
            for (int i = 0; i < resolution; i++)
            {
                float u1 = i / (float)resolution * MathF.PI * 2;
                float u2 = (i + 1) / (float)resolution * MathF.PI * 2;

                // Creating vertices for the cylinder
                Vector3 p1 = new Vector3(halfHeight, radius * MathF.Cos(u1), radius * MathF.Sin(u1));
                Vector3 p2 = new Vector3(halfHeight, radius * MathF.Cos(u2), radius * MathF.Sin(u2));
                Vector3 p3 = new Vector3(-halfHeight, p1.Y, p1.Z);
                Vector3 p4 = new Vector3(-halfHeight, p2.Y, p2.Z);

                tris.Add(new triangle(p1, p3, p2));
                tris.Add(new triangle(p2, p3, p4));
            }
        }
    }
}
