using MagicPhysX;
using static MagicPhysX.NativeMethods;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Runtime;

namespace Engine3D
{
    public unsafe class Physics : IComponent
    {
        private IntPtr dynamicColliderPtr;
        private IntPtr staticColliderPtr;

        private Physx physx;

        public int selectedColliderOption = 0;
        public bool HasCollider
        {
            get
            {
                return !(dynamicColliderPtr == IntPtr.Zero && staticColliderPtr == IntPtr.Zero);
            }
        }

        public ColliderType colliderType = ColliderType.None;
        public string colliderStaticType
        {
            get
            {
                if (dynamicColliderPtr == IntPtr.Zero && staticColliderPtr == IntPtr.Zero)
                    return "";
                else if (dynamicColliderPtr == IntPtr.Zero)
                    return "Static";
                else
                    return "Dynamic";
            }
        }
        public PxRigidDynamic* GetDynamicCollider() { return (PxRigidDynamic*)dynamicColliderPtr.ToPointer(); }
        public PxRigidStatic* GetStaticCollider() { return (PxRigidStatic*)staticColliderPtr.ToPointer(); }

        public float StaticFriction { get; private set; }
        public float DynamicFriction { get; private set; }
        public float Restitution { get; private set; }

        private PxVec3 linearVelocity;
        public Vector3 LinearVelocity
        {
            get
            {
                if (dynamicColliderPtr == IntPtr.Zero && staticColliderPtr != IntPtr.Zero)
                    throw new Exception("Cannot return linear velocity of a static collider!");
                if (dynamicColliderPtr == IntPtr.Zero)
                    throw new Exception("This object doesn't have a dynamic collider!");

                return new Vector3(linearVelocity.x, linearVelocity.y, linearVelocity.z);
            }
            protected set
            {
                if (dynamicColliderPtr == IntPtr.Zero && staticColliderPtr != IntPtr.Zero)
                    throw new Exception("Cannot set linear velocity of a static collider!");
                if (dynamicColliderPtr == IntPtr.Zero)
                    throw new Exception("This object doesn't have a dynamic collider!");

                linearVelocity = new PxVec3() { x = value.X, y = value.Y, z = value.Z };

                PxVec3 vec3 = new PxVec3() { x = linearVelocity.x, y = linearVelocity.y, z = linearVelocity.z };
                GetDynamicCollider()->SetLinearVelocityMut(&vec3, true);
            }
        }

        private PxVec3 angularVelocity;
        public Vector3 AngularVelocity
        {
            get
            {
                if (dynamicColliderPtr == IntPtr.Zero && staticColliderPtr != IntPtr.Zero)
                    throw new Exception("Cannot return angular velocity of a static collider!");
                if (dynamicColliderPtr == IntPtr.Zero)
                    throw new Exception("This object doesn't have a dynamic collider!");

                return new Vector3(angularVelocity.x, angularVelocity.y, angularVelocity.z);
            }
            protected set
            {
                if (dynamicColliderPtr == IntPtr.Zero && staticColliderPtr != IntPtr.Zero)
                    throw new Exception("Cannot set angular velocity of a static collider!");
                if (dynamicColliderPtr == IntPtr.Zero)
                    throw new Exception("This object doesn't have a dynamic collider!");

                angularVelocity = new PxVec3() { x = value.X, y = value.Y, z = value.Z };

                PxVec3 vec3 = new PxVec3() { x = angularVelocity.x, y = angularVelocity.y, z = angularVelocity.z };
                GetDynamicCollider()->SetAngularVelocityMut(&vec3, true);
            }
        }

        public void SetGravity(bool doesAffect)
        {
            if (dynamicColliderPtr == IntPtr.Zero)
                throw new Exception("This object doesn't have a dynamic collider!");
            if (dynamicColliderPtr == IntPtr.Zero && staticColliderPtr != IntPtr.Zero)
                throw new Exception("Cannot set gravity of a static collider!");

            PxActor_setActorFlag_mut((PxActor*)GetDynamicCollider(), PxActorFlag.DisableGravity, doesAffect);
        }

        public bool IsOnGround(Vector3 Position)
        {
            PxVec3 start = new PxVec3() { x = Position.X, y = Position.Y, z = Position.Z };
            PxVec3 dir = new PxVec3() { x = 0, y = -1, z = 0 };

            PxHitFlags hitFlag = PxHitFlags.Default;
            PxRaycastHit hit = new PxRaycastHit();
            PxQueryFilterData filterData = PxQueryFilterData_new();

            if (physx.GetScene()->QueryExtRaycastSingle(&start, &dir, 0.1f, hitFlag, &hit, &filterData, null, null))
            {
                return true;
            }

            return false;
        }

        public void Lock(PxRigidDynamicLockFlag flag, bool value)
        {
            if (dynamicColliderPtr == IntPtr.Zero)
                throw new Exception("This object doesn't have a dynamic collider!");

            GetDynamicCollider()->SetRigidDynamicLockFlagMut(flag, value);
        }

        public Tuple<Vector3, Quaternion>? CollisionResponse()
        {
            if (dynamicColliderPtr != IntPtr.Zero)
            {
                PxVec3 vec3lin = GetDynamicCollider()->GetLinearVelocity();
                PxVec3 vec3ang = GetDynamicCollider()->GetAngularVelocity();

                LinearVelocity = new Vector3(vec3lin.x, vec3lin.y, vec3lin.z);
                AngularVelocity = new Vector3(vec3ang.x, vec3ang.y, vec3ang.z);

                PxTransform transform = PxRigidActor_getGlobalPose((PxRigidActor*)GetDynamicCollider());

                Vector3 Position = new Vector3(transform.p.x, transform.p.y, transform.p.z);
                Quaternion Rotation = QuatHelper.PxToOpenTk(transform.q);

                return new Tuple<Vector3, Quaternion>(Position, Rotation);
            }
            return null;
        }

        #region Colliders
        public void RemoveCollider(bool deleteActorPointer = true)
        {
            if (GetDynamicCollider() != null)
            {
                physx.GetScene()->RemoveActorMut((PxActor*)GetDynamicCollider(), true);
                if (deleteActorPointer)
                    dynamicColliderPtr = IntPtr.Zero;
            }
            if (GetStaticCollider() != null)
            {
                physx.GetScene()->RemoveActorMut((PxActor*)GetStaticCollider(), true);
                if (deleteActorPointer)
                    staticColliderPtr = IntPtr.Zero;
            }
        }

        public void AddTriangleMeshCollider(BaseMesh mesh, Transformation trans, bool removeCollider = false)
        {
            List<Vector3> allVerts = new List<Vector3>();
            List<int> indices = new List<int>();
            bool hasIndices = true;

            int lastIndex = 0;

            for (int i = 0; i < mesh.model.meshes.Count; i++)
            {
                MeshData meshData = mesh.model.meshes[i];

                if (!meshData.hasIndices)
                    hasIndices = false;

                int largestIndex = (int)meshData.indices.Max();

                for (int j = 0; j < meshData.allVerts.Count(); j++)
                {
                    allVerts.Add(meshData.allVerts[j]);
                }

                for (int j = 0; j < meshData.indices.Count; j += 3)
                {
                    indices.Add(meshData.uniqueVertices[(int)meshData.indices[j]].pi + lastIndex);
                    indices.Add(meshData.uniqueVertices[(int)meshData.indices[j + 1]].pi + lastIndex);
                    indices.Add(meshData.uniqueVertices[(int)meshData.indices[j + 2]].pi + lastIndex);
                }

                lastIndex += largestIndex;
            }


            if (removeCollider)
                RemoveCollider();

            Type meshType = mesh.GetType();
            if (meshType != typeof(Mesh))
                throw new Exception("Only 'Mesh' type object can be a TriangleMesh");
            if (!hasIndices)
                throw new Exception("The mesh doesn't have triangle indices!");

            uint count = (uint)indices.Count;
            PxTriangleMeshDesc meshDesc = PxTriangleMeshDesc_new();
            meshDesc.points.count = count;
            meshDesc.points.stride = (uint)sizeof(PxVec3);
            PxVec3[] verts = new PxVec3[allVerts.Count()];
            int[] indices_ = new int[count];
            ((Mesh)mesh).GetCookedData(out verts, out indices_);
            GCHandle vertsHandle = GCHandle.Alloc(verts, GCHandleType.Pinned);
            GCHandle indicesHandle = GCHandle.Alloc(indices_, GCHandleType.Pinned);

            var tolerancesScale = new PxTolerancesScale { length = 1, speed = 10 };
            PxCookingParams cookingParams = PxCookingParams_new(&tolerancesScale);

            bool valid = mesh.isValidMesh((PxVec3*)vertsHandle.AddrOfPinnedObject().ToPointer(), (int)count, (int*)indicesHandle.AddrOfPinnedObject().ToPointer(), (int)count);
            if (!valid)
                throw new Exception("TriangleMesh cooking data is not right!");

            meshDesc.points.data = (PxVec3*)vertsHandle.AddrOfPinnedObject().ToPointer();

            meshDesc.triangles.count = (uint)indices.Count() / 3;
            meshDesc.triangles.stride = 3 * sizeof(int);
            meshDesc.triangles.data = (int*)indicesHandle.AddrOfPinnedObject().ToPointer();

            PxTriangleMeshCookingResult result;
            PxInsertionCallback* callback = PxPhysics_getPhysicsInsertionCallback_mut(physx.GetPhysics());

            PxTriangleMesh triMeshPtr = new PxTriangleMesh();
            PxTriangleMesh* triMesh = &triMeshPtr;
            try
            {
                // Establish a no GC region. The size parameter specifies how much memory
                // to reserve for the small object heap.
                if (GC.TryStartNoGCRegion(indices.Count() / 3 * 60))
                {
                    triMesh = phys_PxCreateTriangleMesh(&cookingParams, &meshDesc, callback, &result);

                }
            }
            finally
            {
                // Always make sure to end the no GC region.
                if (GCSettings.LatencyMode == GCLatencyMode.NoGCRegion)
                {
                    GC.EndNoGCRegion();
                }
            }
            ;
            if (triMesh == null || &triMesh == null)
            {
                throw new Exception("TriangleMesh cooking didn't work!");
            }

            PxVec3 scale = new PxVec3 { x = 1, y = 1, z = 1 };
            PxQuat quat = QuatHelper.OpenTkToPx(trans.Rotation);
            PxMeshScale meshScale = PxMeshScale_new_3(&scale, &quat);
            PxTriangleMeshGeometry meshGeo = PxTriangleMeshGeometry_new(triMesh, &meshScale, PxMeshGeometryFlags.DoubleSided);

            var material = physx.GetPhysics()->CreateMaterialMut(StaticFriction, DynamicFriction, Restitution);
            PxVec3 position = new PxVec3 { x = trans.Position.X, y = trans.Position.Y, z = trans.Position.Z };
            PxTransform transform = PxTransform_new_1(&position);
            var identity = PxTransform_new_2(PxIDENTITY.PxIdentity);
            PxRigidStatic* staticCollider = physx.GetPhysics()->PhysPxCreateStatic(&transform, (PxGeometry*)&meshGeo, material, &identity);
            physx.GetScene()->AddActorMut((PxActor*)staticCollider, null);
            staticColliderPtr = new IntPtr(staticCollider);
            colliderType = ColliderType.TriangleMesh;

            vertsHandle.Free();
            indicesHandle.Free();
        }

        public void AddCapsuleCollider(Transformation trans, bool isStatic, bool removeCollider = false)
        {
            if (removeCollider)
                RemoveCollider();

            if (GetDynamicCollider() != null || GetStaticCollider() != null)
                throw new Exception("You can only add one collider. Remove collider first");
            if (GetDynamicCollider() != null && isStatic)
                throw new Exception("You can only add one type of collider. This object has a dynamic collider");
            if (GetStaticCollider() != null && !isStatic)
                throw new Exception("You can only add one type of collider. This object has a static collider");

            float radius = Math.Min(trans.Scale.X, trans.Scale.Z) / 2.0f;
            float halfHeight = (trans.Scale.Y / 2.0f) - radius;
            PxCapsuleGeometry capsuleGeo = PxCapsuleGeometry_new(halfHeight, radius);

            PxVec3 vec3 = new PxVec3 { x = trans.Position.X, y = trans.Position.Y, z = trans.Position.Z };
            PxQuat quat = QuatHelper.OpenTkToPx(trans.Rotation);
            var transform = PxTransform_new_5(&vec3, &quat);
            var identity = PxTransform_new_2(PxIDENTITY.PxIdentity);
            var material = physx.GetPhysics()->CreateMaterialMut(StaticFriction, DynamicFriction, Restitution);

            if (isStatic)
            {
                staticColliderPtr = new IntPtr(physx.GetPhysics()->PhysPxCreateStatic(&transform, (PxGeometry*)&capsuleGeo, material, &identity));
                physx.GetScene()->AddActorMut((PxActor*)GetStaticCollider(), null);
            }
            else
            {
                dynamicColliderPtr = new IntPtr(physx.GetPhysics()->PhysPxCreateDynamic(&transform, (PxGeometry*)&capsuleGeo, material, 10.0f, &identity));
                PxRigidBody_setAngularDamping_mut((PxRigidBody*)GetDynamicCollider(), 0.5f);
                physx.GetScene()->AddActorMut((PxActor*)GetDynamicCollider(), null);
            }
            colliderType = ColliderType.Capsule;
        }

        public void AddCubeCollider(Transformation trans, bool isStatic, bool removeCollider = false)
        {
            if (removeCollider)
                RemoveCollider();

            if (GetDynamicCollider() != null || GetStaticCollider() != null)
                throw new Exception("You can only add one collider. Remove collider first");
            if (GetDynamicCollider() != null && isStatic)
                throw new Exception("You can only add one type of collider. This object has a dynamic collider");
            if (GetStaticCollider() != null && !isStatic)
                throw new Exception("You can only add one type of collider. This object has a static collider");

            PxBoxGeometry cubeGeo = PxBoxGeometry_new(trans.Scale.X / 2f, trans.Scale.Y / 2f, trans.Scale.Z / 2f);

            PxVec3 vec3 = new PxVec3 { x = trans.Position.X, y = trans.Position.Y, z = trans.Position.Z };
            PxQuat quat = QuatHelper.OpenTkToPx(trans.Rotation);
            var transform = PxTransform_new_5(&vec3, &quat);
            var identity = PxTransform_new_2(PxIDENTITY.PxIdentity);
            var material = physx.GetPhysics()->CreateMaterialMut(StaticFriction, DynamicFriction, Restitution);

            if (isStatic)
            {
                staticColliderPtr = new IntPtr(physx.GetPhysics()->PhysPxCreateStatic(&transform, (PxGeometry*)&cubeGeo, material, &identity));
                physx.GetScene()->AddActorMut((PxActor*)GetStaticCollider(), null);
            }
            else
            {
                dynamicColliderPtr = new IntPtr(physx.GetPhysics()->PhysPxCreateDynamic(&transform, (PxGeometry*)&cubeGeo, material, 10.0f, &identity));
                PxRigidBody_setAngularDamping_mut((PxRigidBody*)GetDynamicCollider(), 0.5f);
                physx.GetScene()->AddActorMut((PxActor*)GetDynamicCollider(), null);
            }
            colliderType = ColliderType.Cube;
        }

        public void AddSphereCollider(Transformation trans, bool isStatic, bool removeCollider = false)
        {
            if (removeCollider)
                RemoveCollider();

            if (GetDynamicCollider() != null || GetStaticCollider() != null)
                throw new Exception("You can only add one collider. Remove collider first");
            if (GetDynamicCollider() != null && isStatic)
                throw new Exception("You can only add one type of collider. This object has a dynamic collider");
            if (GetStaticCollider() != null && !isStatic)
                throw new Exception("You can only add one type of collider. This object has a static collider");

            float radius = Math.Min(trans.Scale.X, Math.Min(trans.Scale.Y, trans.Scale.Z));
            PxSphereGeometry sphereGeo = PxSphereGeometry_new(radius);

            PxVec3 vec3 = new PxVec3 { x = trans.Position.X, y = trans.Position.Y, z = trans.Position.Z };
            PxQuat quat = QuatHelper.OpenTkToPx(trans.Rotation);
            var transform = PxTransform_new_5(&vec3, &quat);
            var identity = PxTransform_new_2(PxIDENTITY.PxIdentity);
            var material = physx.GetPhysics()->CreateMaterialMut(StaticFriction, DynamicFriction, Restitution);

            if (isStatic)
            {
                staticColliderPtr = new IntPtr(physx.GetPhysics()->PhysPxCreateStatic(&transform, (PxGeometry*)&sphereGeo, material, &identity));
                physx.GetScene()->AddActorMut((PxActor*)GetStaticCollider(), null);
            }
            else
            {
                dynamicColliderPtr = new IntPtr(physx.GetPhysics()->PhysPxCreateDynamic(&transform, (PxGeometry*)&sphereGeo, material, 10.0f, &identity));
                PxRigidBody_setAngularDamping_mut((PxRigidBody*)GetDynamicCollider(), 0.5f);
                physx.GetScene()->AddActorMut((PxActor*)GetDynamicCollider(), null);
            }
            colliderType = ColliderType.Sphere;
        }
        #endregion

        #region Getters
        public Vector3 GetLinearVelocity()
        {
            if (dynamicColliderPtr == IntPtr.Zero && staticColliderPtr != IntPtr.Zero)
                throw new Exception("Cannot return velocity of a static collider");
            if (dynamicColliderPtr == IntPtr.Zero)
                throw new Exception("This object doesn't have a dynamic collider!");

            PxVec3 velocity = GetDynamicCollider()->GetLinearVelocity();
            return new Vector3(velocity.x, velocity.y, velocity.z);
        }

        public Vector3 GetAngularVelocity()
        {
            if (dynamicColliderPtr == IntPtr.Zero && staticColliderPtr != IntPtr.Zero)
                throw new Exception("Cannot return velocity of a static collider");
            if (dynamicColliderPtr == IntPtr.Zero)
                throw new Exception("This object doesn't have a dynamic collider!");

            PxVec3 velocity = GetDynamicCollider()->GetAngularVelocity();
            return new Vector3(velocity.x, velocity.y, velocity.z);
        }
        #endregion

        #region Setters
        public void SetLinearVelocity(Vector3 velocity)
        {
            if (dynamicColliderPtr == IntPtr.Zero && staticColliderPtr != IntPtr.Zero)
                throw new Exception("Cannot set velocity of a static collider");
            if (dynamicColliderPtr == IntPtr.Zero)
                throw new Exception("This object doesn't have a dynamic collider!");

            PxVec3 vec3 = new PxVec3() { x = velocity.X, y = velocity.Y, z = velocity.Z };
            GetDynamicCollider()->SetLinearVelocityMut(&vec3, true);
        }
        public void SetAngularVelocity(Vector3 velocity)
        {
            if (dynamicColliderPtr == IntPtr.Zero && staticColliderPtr != IntPtr.Zero)
                throw new Exception("Cannot set velocity of a static collider");
            if (dynamicColliderPtr == IntPtr.Zero)
                throw new Exception("This object doesn't have a dynamic collider!");

            PxVec3 vec3 = new PxVec3() { x = velocity.X, y = velocity.Y, z = velocity.Z };
            GetDynamicCollider()->SetAngularVelocityMut(&vec3, true);
        }
        public void SetLinearVelocity(float x, float y, float z)
        {
            if (dynamicColliderPtr == IntPtr.Zero && staticColliderPtr != IntPtr.Zero)
                throw new Exception("Cannot set velocity of a static collider");
            if (dynamicColliderPtr == IntPtr.Zero)
                throw new Exception("This object doesn't have a dynamic collider!");

            PxVec3 vec3 = new PxVec3() { x = x, y = y, z = z };
            GetDynamicCollider()->SetLinearVelocityMut(&vec3, true);
        }
        public void SetAngularVelocity(float x, float y, float z)
        {
            if (dynamicColliderPtr == IntPtr.Zero && staticColliderPtr != IntPtr.Zero)
                throw new Exception("Cannot set velocity of a static collider");
            if (dynamicColliderPtr == IntPtr.Zero)
                throw new Exception("This object doesn't have a dynamic collider!");

            PxVec3 vec3 = new PxVec3() { x = x, y = y, z = z };
            GetDynamicCollider()->SetAngularVelocityMut(&vec3, true);
        }
        public void AddLinearVelocity(Vector3 velocity)
        {
            if (dynamicColliderPtr == IntPtr.Zero && staticColliderPtr != IntPtr.Zero)
                throw new Exception("Cannot set velocity of a static collider");
            if (dynamicColliderPtr == IntPtr.Zero)
                throw new Exception("This object doesn't have a dynamic collider!");

            PxVec3 vec3 = new PxVec3() { x = LinearVelocity.X + velocity.X, y = LinearVelocity.Y + velocity.Y, z = LinearVelocity.Z + velocity.Z };
            GetDynamicCollider()->SetLinearVelocityMut(&vec3, true);
        }
        public void AddAngularVelocity(Vector3 velocity)
        {
            if (dynamicColliderPtr == IntPtr.Zero && staticColliderPtr != IntPtr.Zero)
                throw new Exception("Cannot set velocity of a static collider");
            if (dynamicColliderPtr == IntPtr.Zero)
                throw new Exception("This object doesn't have a dynamic collider!");

            PxVec3 vec3 = new PxVec3() { x = AngularVelocity.X + velocity.X, y = AngularVelocity.Y + velocity.Y, z = AngularVelocity.Z + velocity.Z };
            GetDynamicCollider()->SetAngularVelocityMut(&vec3, true);
        }
        public void AddLinearVelocity(float x, float y, float z)
        {
            if (dynamicColliderPtr == IntPtr.Zero && staticColliderPtr != IntPtr.Zero)
                throw new Exception("Cannot set velocity of a static collider");
            if (dynamicColliderPtr == IntPtr.Zero)
                throw new Exception("This object doesn't have a dynamic collider!");

            PxVec3 vec3 = new PxVec3() { x = LinearVelocity.X + x, y = LinearVelocity.Y + y, z = LinearVelocity.Z + z };
            GetDynamicCollider()->SetLinearVelocityMut(&vec3, true);
        }
        public void AddAngularVelocity(float x, float y, float z)
        {
            if (dynamicColliderPtr == IntPtr.Zero && staticColliderPtr != IntPtr.Zero)
                throw new Exception("Cannot set velocity of a static collider");
            if (dynamicColliderPtr == IntPtr.Zero)
                throw new Exception("This object doesn't have a dynamic collider!");

            PxVec3 vec3 = new PxVec3() { x = AngularVelocity.X + x, y = AngularVelocity.Y + y, z = AngularVelocity.Z + z };
            GetDynamicCollider()->SetAngularVelocityMut(&vec3, true);
        }

        public void UpdatePhysxPositionAndRotation(Transformation trans)
        {
            if (dynamicColliderPtr == IntPtr.Zero && staticColliderPtr == IntPtr.Zero)
                return;

            PxVec3 vec = PxVec3_new_3(trans.Position.X, trans.Position.Y, trans.Position.Z);
            PxQuat quat = QuatHelper.OpenTkToPx(trans.Rotation);

            PxTransform pose = PxTransform_new_5(&vec, &quat);

            if (dynamicColliderPtr != IntPtr.Zero)
                PxRigidActor_setGlobalPose_mut((PxRigidActor*)GetDynamicCollider(), &pose, true);
            else if (staticColliderPtr != IntPtr.Zero)
                PxRigidActor_setGlobalPose_mut((PxRigidActor*)GetStaticCollider(), &pose, true);
        }

        public void UpdatePhysxScale(BaseMesh mesh, Transformation trans)
        {
            if (dynamicColliderPtr == IntPtr.Zero && staticColliderPtr == IntPtr.Zero)
                return;

            bool isStatic = true;
            if (dynamicColliderPtr == IntPtr.Zero)
                isStatic = false;

            if (colliderType == ColliderType.TriangleMesh)
                AddTriangleMeshCollider(mesh, trans, true);
            if (colliderType == ColliderType.Cube)
                AddCubeCollider(trans, isStatic, true);
            if (colliderType == ColliderType.Sphere)
                AddSphereCollider(trans, isStatic, true);
            if (colliderType == ColliderType.Capsule)
                AddCapsuleCollider(trans, isStatic, true);
        }

        public void SetMaterial(ObjectType type, Transformation trans, float staticFriction, float dynamicFriction, float restiution)
        {
            StaticFriction = staticFriction;
            DynamicFriction = dynamicFriction;
            Restitution = restiution;

            bool isStatic = true;
            if (dynamicColliderPtr == IntPtr.Zero)
                isStatic = false;

            if (dynamicColliderPtr == IntPtr.Zero || staticColliderPtr == IntPtr.Zero)
            {
                if (type == ObjectType.Cube)
                    AddCubeCollider(trans, isStatic, true);
                if (type == ObjectType.Sphere)
                    AddSphereCollider(trans, isStatic, true);
                if (type == ObjectType.Capsule)
                    AddCapsuleCollider(trans, isStatic, true);
            }
        }
        #endregion
    }
}
