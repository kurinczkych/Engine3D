using System;
using System.Collections.Generic;
using MagicPhysX;
using OpenTK.Mathematics;
using static MagicPhysX.NativeMethods;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mario64
{
    public unsafe class Mesh_ : Mesh
    {
        PxRigidDynamic* meshDynamicCollider;
        PxRigidStatic* meshStaticCollider;

        public Mesh_(VAO vao, VBO vbo, int shaderProgramId, string embeddedTextureName, int ocTreeDepth, Vector2 windowSize, ref Frustum frustum, ref Camera camera, ref int textureCount) :
    base(vao, vbo, shaderProgramId, embeddedTextureName, ocTreeDepth, windowSize, ref frustum, ref camera, ref textureCount)
        {


            if (ocTreeDepth != -1)
            {
                CalculateBoundingBox();
                Octree = new Octree(new List<triangle>(tris), BoundingBox, ocTreeDepth);
            }

            ComputeVertexNormals(ref tris);

            SendUniforms();
        }

        public void AddMeshCollider(bool isStatic, ref Physx physx)
        {
            //var meshDesc = PxTriangleMeshDesc_new();
            //meshDesc.points.count = (uint)tris.Count() * 3;
            //meshDesc.points.stride = sizeof(PxVec3);
            //meshDesc.points.data = ConvertTrisToPxVec3();

            

            //var meshGeo = PxTriangleMeshGeometry_new();

            //var capsuleGeo = PxCapsuleGeometry_new(Scale.Y, Scale.X);
            //PxVec3 vec3 = new PxVec3 { x = Position.X, y = Position.Y, z = Position.Z };
            //PxQuat quat = QuatHelper.OpenTkToPx(Rotation);
            //var transform = PxTransform_new_5(&vec3, &quat);
            //var identity = PxTransform_new_2(PxIDENTITY.PxIdentity);
            //var material = physx.physics->CreateMaterialMut(0.5f, 0.5f, 0.1f);

            //if (isStatic)
            //{
            //    capsuleStaticCollider = physx.physics->PhysPxCreateStatic(&transform, (PxGeometry*)&capsuleGeo, material, &identity);
            //    physx.scene->AddActorMut((PxActor*)capsuleStaticCollider, null);
            //}
            //else
            //{
            //    capsuleDynamicCollider = physx.physics->PhysPxCreateDynamic(&transform, (PxGeometry*)&capsuleGeo, material, 10.0f, &identity);
            //    PxRigidBody_setAngularDamping_mut((PxRigidBody*)capsuleDynamicCollider, 0.5f);
            //    physx.scene->AddActorMut((PxActor*)capsuleDynamicCollider, null);
            //}
        }
    }
}
