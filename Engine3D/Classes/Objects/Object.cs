using MagicPhysX;
using static MagicPhysX.NativeMethods;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Formats.Asn1.AsnWriter;
using Newtonsoft.Json.Linq;
using System.Runtime.InteropServices;
using System.Runtime;
using System.Runtime.CompilerServices;

#pragma warning disable CS8767

namespace Engine3D
{
    public enum ObjectType
    {
        Cube,
        Sphere,
        Capsule,
        TriangleMesh,
        TriangleMeshWithCollider,
        TestMesh,
        NoTexture,
        Wireframe,
        TextMesh,
        UIMesh
    }

    public unsafe class Object : IComparable<Object>
    {
        public string name = "";

        public bool isEnabled = true;

        public Type meshType;
        private BaseMesh mesh;
        private ObjectType type;
        private Physx physx;

        private IntPtr dynamicColliderPtr;
        private IntPtr staticColliderPtr;

        public BSP BSPStruct { get; private set; }
        public Octree Octree { get; private set; }
        public GridStructure GridStructure { get; private set; }

        public PxRigidDynamic* GetDynamicCollider() { return (PxRigidDynamic*)dynamicColliderPtr.ToPointer(); }
        public PxRigidStatic* GetStaticCollider() { return (PxRigidStatic*)staticColliderPtr.ToPointer(); }

        public Vector3 Position;
        public Quaternion Rotation { get; set; }
        public Vector3 Scale = Vector3.One;

        public string PStr
        {
            get { return Math.Round(Position.X, 2).ToString() + "," + Math.Round(Position.Y, 2).ToString() + "," + Math.Round(Position.Z, 2).ToString(); }
        }

        public Vector3 Size { get; private set; }
        public float Radius { get; private set; }
        public float HalfHeight { get; private set; }

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

        public Object(BaseMesh mesh, ObjectType type, ref Physx physx)
        {
            StaticFriction = 0.5f;
            DynamicFriction = 0.5f;
            Restitution = 0.1f;

            this.mesh = mesh;
            this.type = type;
            this.physx = physx;
            meshType = mesh.GetType();

            BuildBVH();
            mesh.CalculateFrustumVisibility();

            Position = Vector3.Zero;
            Rotation = Quaternion.Identity;

            mesh.parentObject = this;

            if (type == ObjectType.TriangleMeshWithCollider)
            {
                Type meshType = mesh.GetType();
                if (meshType != typeof(Mesh))
                    throw new Exception("Only 'Mesh' type object can be a TriangleMesh");
                if(!mesh.hasIndices)
                    throw new Exception("The mesh doesn't have triangle indices!");

                uint count = (uint)((Mesh)mesh).tris.Count() * 3;
                PxTriangleMeshDesc meshDesc = PxTriangleMeshDesc_new();
                meshDesc.points.count = count;
                meshDesc.points.stride = (uint)sizeof(PxVec3);
                PxVec3[] verts = new PxVec3[mesh.allVerts.Count()];
                int[] indices = new int[count];
                ((Mesh)mesh).GetCookedData(out verts, out indices);
                GCHandle vertsHandle = GCHandle.Alloc(verts, GCHandleType.Pinned);
                GCHandle indicesHandle = GCHandle.Alloc(indices, GCHandleType.Pinned);

                var tolerancesScale = new PxTolerancesScale { length = 1, speed = 10 };
                PxCookingParams cookingParams = PxCookingParams_new(&tolerancesScale);
                //cookingParams.suppressTriangleMeshRemapTable = true;
                //cookingParams.buildTriangleAdjacencies = false;
                //cookingParams.meshPreprocessParams = PxMeshPreprocessingFlags.WeldVertices;

                //fixed (PxVec3* vertsPointer = &verts[0])

                bool valid = isValidMesh((PxVec3*)vertsHandle.AddrOfPinnedObject().ToPointer(), (int)count, (int*)indicesHandle.AddrOfPinnedObject().ToPointer(), (int)count);
                if(!valid)
                    throw new Exception("TriangleMesh cooking data is not right!");

                meshDesc.points.data = (PxVec3*)vertsHandle.AddrOfPinnedObject().ToPointer();

                meshDesc.triangles.count = (uint)((Mesh)mesh).tris.Count();
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
                    if (GC.TryStartNoGCRegion(((Mesh)mesh).tris.Count() * 60))
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


                if(triMesh == null || &triMesh == null)
                {
                    throw new Exception("TriangleMesh cooking didn't work!");
                }

                PxVec3 scale = new PxVec3 { x = 1, y = 1, z = 1 };
                PxQuat quat = QuatHelper.OpenTkToPx(Rotation);
                PxMeshScale meshScale = PxMeshScale_new_3(&scale, &quat);
                PxTriangleMeshGeometry meshGeo = PxTriangleMeshGeometry_new(triMesh, &meshScale, PxMeshGeometryFlags.DoubleSided);

                var material = physx.GetPhysics()->CreateMaterialMut(StaticFriction, DynamicFriction, Restitution);
                PxVec3 position = new PxVec3 { x = Position.X, y = Position.Y, z = Position.Z };
                PxTransform transform = PxTransform_new_1(&position);
                var identity = PxTransform_new_2(PxIDENTITY.PxIdentity);
                PxRigidStatic* staticCollider = physx.GetPhysics()->PhysPxCreateStatic(&transform, (PxGeometry*)&meshGeo, material, &identity);
                physx.GetScene()->AddActorMut((PxActor*)staticCollider, null);
                staticColliderPtr = new IntPtr(staticCollider);

                vertsHandle.Free();
                indicesHandle.Free();
            }

            if(type == ObjectType.Cube)
            {
                Size = new Vector3(5, 5, 5);
            }
            if (type == ObjectType.Sphere)
            {
                Radius = 5;
            }
            if (type == ObjectType.Capsule)
            {
                HalfHeight = 5;
                Radius = 5;
            }
        }

        public void BuildBVH()
        {
            //Calculating bounding volume hiearchy
            if (meshType == typeof(Mesh) ||
                meshType == typeof(InstancedMesh) ||
                meshType == typeof(NoTextureMesh))
            {
                mesh.BVHStruct = new BVH(mesh.tris);
            }
        }

        public void BuildBSP()
        {
            BSPStruct = new BSP(((Mesh)mesh).tris);
        }

        public void BuildOctree()
        {
            Octree = new Octree();
            Octree.Build(((Mesh)mesh).tris, ((Mesh)mesh).Bounds);
        }

        public void BuildGrid(Shader currentShader, Shader GridShader)
        {
            GridShader.Use();
            GridStructure = new GridStructure(((Mesh)mesh).tris, ((Mesh)mesh).Bounds, 20, GridShader.id);
            currentShader.Use();
        }

        public void SetBillboarding(bool useBillboarding)
        {
            Type meshType = mesh.GetType();
            if (meshType != typeof(Mesh) && meshType != typeof(InstancedMesh))
                throw new Exception("Billboarding can only be used on type 'Mesh' and 'InstancedMesh'!");

            mesh.useBillboarding = useBillboarding ? 1 : 0;
        }

        private bool isValidMesh(PxVec3* vertices, int numVertices, int* indices, int numIndices)
        {
            if(numVertices == 0 || numIndices == 0 || numIndices % 3 != 0)
                return false;

            for(int i = 0; i<numIndices; ++i)
            {
                if(indices[i] >= numVertices)
                    return false;
            }

            // Additional checks (e.g., degenerate triangles, manifoldness, etc.) would go here.

            return true;
        }

        public BaseMesh GetMesh()
        {
            return mesh;
        }

        public void SetInstancedData(List<InstancedMeshData> data)
        {
            if(mesh.GetType() != typeof(InstancedMesh))
                throw new Exception("Cannot set instanced mesh data for '" + mesh.GetType().ToString() + "'!");

            ((InstancedMesh)mesh).instancedData = data;
        }

        public ObjectType GetObjectType()
        {
            return type;
        }

        public int CompareTo(Object other)
        {
            // A static array defining the custom order.
            ObjectType[] customOrder =
            {
                ObjectType.Cube,
                ObjectType.Sphere,
                ObjectType.Capsule,
                ObjectType.TriangleMesh,
                ObjectType.TestMesh,
                ObjectType.NoTexture,
                ObjectType.Wireframe,
                ObjectType.TextMesh,
                ObjectType.UIMesh
            };

            // Finding the indices of 'this' and 'other' ObjectTypes in customOrder.
            int thisOrderIndex = Array.IndexOf(customOrder, type);
            int otherOrderIndex = Array.IndexOf(customOrder, other.type);

            // Comparing the indices.
            int orderComparison = thisOrderIndex.CompareTo(otherOrderIndex);

            if (orderComparison == 0 && type == ObjectType.TriangleMesh)
            {
                // Assuming the camera (or any reference point) is at a fixed position.
                Vector3 cameraPosition = new Vector3(0, 0, 0); // Modify this as needed.

                float thisDistance = (Position - cameraPosition).LengthSquared;
                float otherDistance = (other.Position - cameraPosition).LengthSquared;

                // Comparing the distances (squared) for TriangleMesh objects.
                return thisDistance.CompareTo(otherDistance);
            }

            return orderComparison;
        }

        public void SetGravity(bool doesAffect)
        {
            if(dynamicColliderPtr == IntPtr.Zero)
                throw new Exception("This object doesn't have a dynamic collider!");
            if(dynamicColliderPtr == IntPtr.Zero && staticColliderPtr != IntPtr.Zero)
                throw new Exception("Cannot set gravity of a static collider!");

            PxActor_setActorFlag_mut((PxActor*)GetDynamicCollider(), PxActorFlag.DisableGravity, doesAffect);
        }
        
        public bool IsOnGround()
        {
            PxVec3 start = new PxVec3() { x = Position.X, y = Position.Y, z = Position.Z };
            PxVec3 dir = new PxVec3() { x = 0, y = -1, z = 0 };

            PxHitFlags hitFlag = PxHitFlags.Default;
            PxRaycastHit hit = new PxRaycastHit();
            PxQueryFilterData filterData = PxQueryFilterData_new();

            if (physx.GetScene()->QueryExtRaycastSingle(&start,&dir,0.1f,hitFlag,&hit,&filterData,null,null))
            {
                return true;
            }

            return false;
        } 

        public void Lock(PxRigidDynamicLockFlag flag, bool value)
        {
            if(dynamicColliderPtr == IntPtr.Zero)
                throw new Exception("This object doesn't have a dynamic collider!");

            GetDynamicCollider()->SetRigidDynamicLockFlagMut(flag, value);
        }

        #region Collision management
        public void CollisionResponse()
        {
            if (dynamicColliderPtr != IntPtr.Zero)
            {
                PxVec3 vec3lin = GetDynamicCollider()->GetLinearVelocity();
                PxVec3 vec3ang = GetDynamicCollider()->GetAngularVelocity();

                LinearVelocity = new Vector3(vec3lin.x, vec3lin.y, vec3lin.z);
                AngularVelocity = new Vector3(vec3ang.x, vec3ang.y, vec3ang.z);

                PxTransform transform = PxRigidActor_getGlobalPose((PxRigidActor*)GetDynamicCollider());

                Position.X = transform.p.x;
                Position.Y = transform.p.y;
                Position.Z = transform.p.z;

                Rotation = QuatHelper.PxToOpenTk(transform.q);
            }
        }
        #endregion

        #region Getters
        public Vector3 GetPosition()
        {
            return Position;
        }

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

        public void SetPosition(Vector3 position)
        {
            Position = position;
            

            if (dynamicColliderPtr == IntPtr.Zero && staticColliderPtr == IntPtr.Zero)
                return;

            PxVec3 vec = PxVec3_new_3(position.X, position.Y, position.Z);
            PxTransform pose = PxTransform_new_1(&vec);

            if(dynamicColliderPtr != IntPtr.Zero)
                PxRigidActor_setGlobalPose_mut((PxRigidActor*)GetDynamicCollider(), &pose, true);
            else if(staticColliderPtr != IntPtr.Zero)
                PxRigidActor_setGlobalPose_mut((PxRigidActor*)GetStaticCollider(), &pose, true);
        }

        public void SetPosition(float x, float y, float z)
        {
            Position = new Vector3(x,y,z);
            

            if (dynamicColliderPtr == IntPtr.Zero && staticColliderPtr == IntPtr.Zero)
                return;

            PxVec3 vec = PxVec3_new_3(x, y, z);
            PxTransform pose = PxTransform_new_1(&vec);

            if(dynamicColliderPtr != IntPtr.Zero)
                PxRigidActor_setGlobalPose_mut((PxRigidActor*)GetDynamicCollider(), &pose, true);
            else if(staticColliderPtr != IntPtr.Zero)
                PxRigidActor_setGlobalPose_mut((PxRigidActor*)GetStaticCollider(), &pose, true);
        }

        public void AddPosition(Vector3 position)
        {
            Position += position;
            

            if (dynamicColliderPtr == IntPtr.Zero && staticColliderPtr == IntPtr.Zero)
                return;

            PxVec3 vec = PxVec3_new_3(Position.X, Position.Y, Position.Z);
            PxTransform pose = PxTransform_new_1(&vec);

            if(dynamicColliderPtr != IntPtr.Zero)
                PxRigidActor_setGlobalPose_mut((PxRigidActor*)GetDynamicCollider(), &pose, true);
            else if(staticColliderPtr != IntPtr.Zero)
                PxRigidActor_setGlobalPose_mut((PxRigidActor*)GetStaticCollider(), &pose, true);
        }

        public void AddPosition(float x, float y, float z)
        {
            Position += new Vector3(x,y,z);
            

            if (dynamicColliderPtr == IntPtr.Zero && staticColliderPtr == IntPtr.Zero)
                return;

            PxVec3 vec = PxVec3_new_3(Position.X, Position.Y, Position.Z);
            PxTransform pose = PxTransform_new_1(&vec);

            if(dynamicColliderPtr != IntPtr.Zero)
                PxRigidActor_setGlobalPose_mut((PxRigidActor*)GetDynamicCollider(), &pose, true);
            else if(staticColliderPtr != IntPtr.Zero)
                PxRigidActor_setGlobalPose_mut((PxRigidActor*)GetStaticCollider(), &pose, true);
        }
        public void SetRotation(Quaternion rotation)
        {
            Rotation = rotation;

            if (dynamicColliderPtr == IntPtr.Zero && staticColliderPtr == IntPtr.Zero)
                return;

            PxQuat quat = QuatHelper.OpenTkToPx(rotation);

            PxTransform pose = PxTransform_new_3(&quat);

            if (dynamicColliderPtr != IntPtr.Zero)
                PxRigidActor_setGlobalPose_mut((PxRigidActor*)GetDynamicCollider(), &pose, true);
            else if (staticColliderPtr != IntPtr.Zero)
                PxRigidActor_setGlobalPose_mut((PxRigidActor*)GetStaticCollider(), &pose, true);
        }
        public void SetPositionAndRotation(Vector3 position, Quaternion rotation)
        {
            Position = position;
            Rotation = rotation;

            if (dynamicColliderPtr == IntPtr.Zero && staticColliderPtr == IntPtr.Zero)
                return;

            PxVec3 vec = PxVec3_new_3(position.X, position.Y, position.Z);
            PxQuat quat = QuatHelper.OpenTkToPx(rotation);

            PxTransform pose = PxTransform_new_5(&vec, &quat);

            if (dynamicColliderPtr != IntPtr.Zero)
                PxRigidActor_setGlobalPose_mut((PxRigidActor*)GetDynamicCollider(), &pose, true);
            else if (staticColliderPtr != IntPtr.Zero)
                PxRigidActor_setGlobalPose_mut((PxRigidActor*)GetStaticCollider(), &pose, true);
        }

        public void SetSize(Vector3 size)
        {
            if (type != ObjectType.Cube)
                throw new Exception("Cannot change the size of a '" + type.ToString() + "'!");

            Size = size;

            if (dynamicColliderPtr == IntPtr.Zero && staticColliderPtr == IntPtr.Zero)
                return;

            bool isStatic = true;
            if (dynamicColliderPtr == IntPtr.Zero)
                isStatic = false;

            AddCubeCollider(isStatic, true);
        }

        public void SetSize(float halfHeight, float radius)
        {
            if (type != ObjectType.Capsule)
                throw new Exception("Cannot change the half height and radius of a '" + type.ToString() + "'!");

            HalfHeight = halfHeight;
            Radius = radius;

            if (dynamicColliderPtr == IntPtr.Zero && staticColliderPtr == IntPtr.Zero)
                return;

            bool isStatic = true;
            if (dynamicColliderPtr == IntPtr.Zero)
                isStatic = false;

            AddCapsuleCollider(isStatic, true);
        }

        public void SetSize(float radius)
        {
            if (type != ObjectType.Sphere)
                throw new Exception("Cannot change the radius of a '" + type.ToString() + "'!");

            Radius = radius;

            if (dynamicColliderPtr == IntPtr.Zero && staticColliderPtr == IntPtr.Zero)
                return;

            bool isStatic = true;
            if (dynamicColliderPtr == IntPtr.Zero)
                isStatic = false;

            AddSphereCollider(isStatic, true);
        }

        public void SetMaterial(float staticFriction, float dynamicFriction, float restiution)
        {
            StaticFriction = staticFriction;
            DynamicFriction = dynamicFriction;
            Restitution = restiution;

            bool isStatic = true;
            if (dynamicColliderPtr == IntPtr.Zero)
                isStatic = false;

            if (dynamicColliderPtr == IntPtr.Zero || staticColliderPtr == IntPtr.Zero)
            {
                if(type == ObjectType.Cube)
                    AddCubeCollider(isStatic, true);
                if(type == ObjectType.Sphere)
                    AddSphereCollider(isStatic, true);
                if(type == ObjectType.Capsule)
                    AddCapsuleCollider(isStatic, true);
            }
        }
        #endregion

        #region Colliders
        public void RemoveCollider(bool deleteActorPointer=true)
        {
            if(GetDynamicCollider() != null)
            {
                physx.GetScene()->RemoveActorMut((PxActor*)GetDynamicCollider(), true);
                if(deleteActorPointer)
                    dynamicColliderPtr = IntPtr.Zero;
            }
            if(GetStaticCollider() != null)
            {
                physx.GetScene()->RemoveActorMut((PxActor*)GetStaticCollider(), true);
                if(deleteActorPointer)
                    staticColliderPtr = IntPtr.Zero;
            }
        }

        public void AddCapsuleCollider(bool isStatic, bool removeCollider=false)
        {
            if (removeCollider)
                RemoveCollider();

            if(GetDynamicCollider() != null || GetStaticCollider() != null)
                throw new Exception("You can only add one collider. Remove collider first");
            if(GetDynamicCollider() != null && isStatic)
                throw new Exception("You can only add one type of collider. This object has a dynamic collider");
            if(GetStaticCollider() != null && !isStatic)
                throw new Exception("You can only add one type of collider. This object has a static collider");

            PxCapsuleGeometry capsuleGeo = new PxCapsuleGeometry();

            if(type == ObjectType.Capsule)
                capsuleGeo = PxCapsuleGeometry_new(HalfHeight, Radius);
            else
                capsuleGeo = PxCapsuleGeometry_new(5, 5);

            PxVec3 vec3 = new PxVec3 { x = Position.X, y = Position.Y, z = Position.Z };
            PxQuat quat = QuatHelper.OpenTkToPx(Rotation);
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
        }

        public void AddCubeCollider(bool isStatic, bool removeCollider = false)
        {
            if (removeCollider)
                RemoveCollider();

            if (GetDynamicCollider() != null || GetStaticCollider() != null)
                throw new Exception("You can only add one collider. Remove collider first");
            if (GetDynamicCollider() != null && isStatic)
                throw new Exception("You can only add one type of collider. This object has a dynamic collider");
            if (GetStaticCollider() != null && !isStatic)
                throw new Exception("You can only add one type of collider. This object has a static collider");

            PxBoxGeometry cubeGeo = new PxBoxGeometry();
            if (type == ObjectType.Cube)
                cubeGeo = PxBoxGeometry_new(Size.X / 2f, Size.Y / 2f, Size.Z / 2f);
            else
                cubeGeo = PxBoxGeometry_new(10 / 2f, 10 / 2f, 10 / 2f);

            PxVec3 vec3 = new PxVec3 { x = Position.X, y = Position.Y, z = Position.Z };
            PxQuat quat = QuatHelper.OpenTkToPx(Rotation);
            var transform = PxTransform_new_5(&vec3, &quat);
            var identity = PxTransform_new_2(PxIDENTITY.PxIdentity);
            var material = physx.GetPhysics()->CreateMaterialMut(StaticFriction, DynamicFriction, Restitution);

            if (isStatic)
            {
                staticColliderPtr = new IntPtr(physx.GetPhysics()->PhysPxCreateStatic(&transform, (PxGeometry*)&cubeGeo, material, &identity));
                physx.GetScene()-> AddActorMut((PxActor*)GetStaticCollider(), null);
            }
            else
            {
                dynamicColliderPtr = new IntPtr(physx.GetPhysics()->PhysPxCreateDynamic(&transform, (PxGeometry*)&cubeGeo, material, 10.0f, &identity));
                PxRigidBody_setAngularDamping_mut((PxRigidBody*)GetDynamicCollider(), 0.5f);
                physx.GetScene()->AddActorMut((PxActor*)GetDynamicCollider(), null);
            }
        }

        public void AddSphereCollider(bool isStatic, bool removeCollider = false)
        {
            if (removeCollider)
                RemoveCollider();

            if (GetDynamicCollider() != null || GetStaticCollider() != null)
                throw new Exception("You can only add one collider. Remove collider first");
            if (GetDynamicCollider() != null && isStatic)
                throw new Exception("You can only add one type of collider. This object has a dynamic collider");
            if (GetStaticCollider() != null && !isStatic)
                throw new Exception("You can only add one type of collider. This object has a static collider");

            PxSphereGeometry sphereGeo = new PxSphereGeometry();
            if(type == ObjectType.Sphere)
                sphereGeo = PxSphereGeometry_new(Radius);
            else
                sphereGeo = PxSphereGeometry_new(5);

            PxVec3 vec3 = new PxVec3 { x = Position.X, y = Position.Y, z = Position.Z };
            PxQuat quat = QuatHelper.OpenTkToPx(Rotation);
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
        }
        #endregion

        #region SimpleMeshes
        public static List<triangle> GetUnitCube()
        {
            List<triangle> tris = new List<triangle>();

            float halfSize = 0.5f;

            // Define cube vertices
            Vector3 p1 = new Vector3(-halfSize, -halfSize, -halfSize);
            Vector3 p2 = new Vector3(halfSize, -halfSize, -halfSize);
            Vector3 p3 = new Vector3(halfSize, halfSize, -halfSize);
            Vector3 p4 = new Vector3(-halfSize, halfSize, -halfSize);
            Vector3 p5 = new Vector3(-halfSize, -halfSize, halfSize);
            Vector3 p6 = new Vector3(halfSize, -halfSize, halfSize);
            Vector3 p7 = new Vector3(halfSize, halfSize, halfSize);
            Vector3 p8 = new Vector3(-halfSize, halfSize, halfSize);

            Vec2d t1 = new Vec2d(0, 0);
            Vec2d t2 = new Vec2d(1, 0);
            Vec2d t3 = new Vec2d(1, 1);
            Vec2d t4 = new Vec2d(0, 1);

            // Back face
            tris.Add(new triangle(new Vector3[] { p1, p3, p2 }, new Vec2d[] { t1, t3, t2 }) { visibile = true });
            tris.Add(new triangle(new Vector3[] { p3, p1, p4 }, new Vec2d[] { t3, t1, t4 }) { visibile = true });

            // Front face
            tris.Add(new triangle(new Vector3[] { p5, p6, p7 }, new Vec2d[] { t1, t2, t3 }) { visibile = true });
            tris.Add(new triangle(new Vector3[] { p7, p8, p5 }, new Vec2d[] { t3, t4, t1 }) { visibile = true });

            // Left face
            tris.Add(new triangle(new Vector3[] { p1, p8, p4 }, new Vec2d[] { t1, t3, t2 }) { visibile = true });
            tris.Add(new triangle(new Vector3[] { p8, p1, p5 }, new Vec2d[] { t3, t1, t4 }) { visibile = true });

            // Right face
            tris.Add(new triangle(new Vector3[] { p2, p3, p7 }, new Vec2d[] { t1, t2, t3 }) { visibile = true });
            tris.Add(new triangle(new Vector3[] { p7, p6, p2 }, new Vec2d[] { t3, t4, t1 }) { visibile = true });

            // Top face
            tris.Add(new triangle(new Vector3[] { p4, p7, p3 }, new Vec2d[] { t1, t3, t2 }) { visibile = true });
            tris.Add(new triangle(new Vector3[] { p7, p4, p8 }, new Vec2d[] { t3, t1, t4 }) { visibile = true });

            // Bottom face
            tris.Add(new triangle(new Vector3[] { p1, p2, p6 }, new Vec2d[] { t1, t2, t3 }) { visibile = true });
            tris.Add(new triangle(new Vector3[] { p6, p5, p1 }, new Vec2d[] { t3, t4, t1 }) { visibile = true });

            return tris;
        }

        public static List<triangle> GetUnitFace()
        {
            List<triangle> tris = new List<triangle>();

            float halfSize = 0.5f;

            // Define cube vertices (only what's needed for the front face)
            Vector3 p5 = new Vector3(-halfSize, -halfSize, halfSize);
            Vector3 p6 = new Vector3(halfSize, -halfSize, halfSize);
            Vector3 p7 = new Vector3(halfSize, halfSize, halfSize);
            Vector3 p8 = new Vector3(-halfSize, halfSize, halfSize);

            Vec2d t1 = new Vec2d(0, 0);
            Vec2d t2 = new Vec2d(1, 0);
            Vec2d t3 = new Vec2d(1, 1);
            Vec2d t4 = new Vec2d(0, 1);

            // Front face
            tris.Add(new triangle(new Vector3[] { p5, p6, p7 }, new Vec2d[] { t1, t2, t3 }) { visibile = true });
            tris.Add(new triangle(new Vector3[] { p7, p8, p5 }, new Vec2d[] { t3, t4, t1 }) { visibile = true });

            return tris;
        }

        public static List<triangle> GetUnitSphere(float radius = 1, int resolution = 10)
        {
            List<triangle> tris = new List<triangle>();

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

                    tris.Add(new triangle(new Vector3[] { p1, p2, p3 }) { visibile = true });
                    tris.Add(new triangle(new Vector3[] { p2, p4, p3 }) { visibile = true });
                }
            }

            return tris;
        }

        public static List<triangle> GetUnitCapsule(float radius = 5, float halfHeight = 10, int resolution = 10)
        {
            List<triangle>  tris = new List<triangle>();

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

                    tris.Add(new triangle(p1b, p2b, p3b) { visibile = true });
                    tris.Add(new triangle(p2b, p4b, p3b) { visibile = true });
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

                tris.Add(new triangle(p1, p3, p2) { visibile = true });
                tris.Add(new triangle(p2, p3, p4) { visibile = true });
            }

            return tris;
        }
        #endregion
    }
}