using MagicPhysX;
using static MagicPhysX.NativeMethods;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Formats.Asn1.AsnWriter;

#pragma warning disable CS8767

namespace Mario64
{
    public enum ObjectType
    {
        Cube,
        Sphere,
        Capsule,
        TriangleMesh,
        TestMesh,
        NoTexture,
        Wireframe,
        TextMesh,
        UIMesh
    }

    public unsafe class Object : IComparable<Object>
    {
        private BaseMesh mesh;
        private ObjectType type;
        private Physx physx;

        PxRigidDynamic* dynamicCollider;
        PxRigidStatic* staticCollider;

        private Vector3 Position;
        public Quaternion Rotation { get; private set; }

        public Vector3 Size { get; private set; }
        public float Radius { get; private set; }
        public float HalfHeight { get; private set; }

        public float StaticFriction { get; private set; }
        public float DynamicFriction { get; private set; }
        public float Restitution { get; private set; }

        public Object(BaseMesh mesh, ObjectType type, ref Physx physx)
        {
            StaticFriction = 0.5f;
            DynamicFriction = 0.5f;
            Restitution = 0.1f;

            if (type == ObjectType.TriangleMesh)
            {
                Type meshType = mesh.GetType();
                if (meshType != typeof(Mesh))
                    throw new Exception("Only 'Mesh' type object can be a TriangleMesh");
                if(!mesh.hasIndices)
                    throw new Exception("The mesh doesn't have triangle indices!");
                

                PxTriangleMeshDesc meshDesc = PxTriangleMeshDesc_new();
                meshDesc.points.count = (uint)((Mesh)mesh).tris.Count()*3;
                meshDesc.points.stride = (uint)sizeof(PxVec3);
                PxVec3[] verts = new PxVec3[((Mesh)mesh).tris.Count() * 3];
                int[] indices = new int[((Mesh)mesh).tris.Count() * 3];
                ((Mesh)mesh).GetCookedData(out verts, out indices);

                var tolerancesScale = new PxTolerancesScale { length = 1, speed = 10 };
                PxCookingParams cookingParams = PxCookingParams_new(&tolerancesScale);

                fixed (PxVec3* vertsPointer = &verts[0])
                {
                    fixed (int* indicesPointer = &indices[0])
                    {
                        meshDesc.points.data = vertsPointer;

                        meshDesc.triangles.count = (uint)((Mesh)mesh).tris.Count();
                        meshDesc.triangles.stride = 3 * (sizeof(int));
                        meshDesc.triangles.data = indicesPointer;

                        PxInsertionCallback* callback = PxPhysics_getPhysicsInsertionCallback_mut(physx.physics);
                        PxTriangleMeshCookingResult result;
                        PxTriangleMesh* triMesh = phys_PxCreateTriangleMesh(&cookingParams, &meshDesc, callback, &result);

                        if(triMesh == null || &triMesh == null)
                        {
                            throw new Exception("TriangleMesh cooking didn't work!");
                        }

                        PxVec3 size = new PxVec3 { x = Size.X, y = Size.Y, z = Size.Z };
                        PxQuat quat = QuatHelper.OpenTkToPx(Rotation);

                        PxMeshScale scale = PxMeshScale_new_3(&size, &quat);
                        PxMeshGeometryFlags flags = PxMeshGeometryFlags.DoubleSided;

                        PxTriangleMeshGeometry meshGeo = PxTriangleMeshGeometry_new(triMesh, &scale, flags);
                        var material = physx.physics->CreateMaterialMut(StaticFriction, DynamicFriction, Restitution);
                        PxShape* shape = physx.physics->CreateShapeMut((PxGeometry*)&meshGeo, material, true, PxShapeFlags.SimulationShape);

                        PxVec3 position = new PxVec3 { x = Position.X, y = Position.Y, z = Position.Z };
                        PxTransform transform = PxTransform_new_1(&position);
                        //PxRigidStatic* actor = PxPhysics_createRigidStatic_mut(physx.physics, &transform);
                        var identity = PxTransform_new_2(PxIDENTITY.PxIdentity);
                        staticCollider = physx.physics->PhysPxCreateStatic(&transform, (PxGeometry*)&shape, material, &identity);

                        physx.scene->AddActorMut((PxActor*)staticCollider, null);
                    }
                }
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

            this.mesh = mesh;
            this.type = type;
            this.physx = physx;
        }

        public BaseMesh GetMesh()
        {
            return mesh;
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
            return thisOrderIndex.CompareTo(otherOrderIndex);
        }

        #region Collision management
        public void CollisionResponse()
        {
            if (dynamicCollider != null)
            {
                PxTransform transform = PxRigidActor_getGlobalPose((PxRigidActor*)dynamicCollider);

                Position.X = transform.p.x;
                Position.Y = transform.p.y;
                Position.Z = transform.p.z;

                Rotation = QuatHelper.PxToOpenTk(transform.q);
            }
        }
        #endregion

        #region Setters
        public void SetPosition(Vector3 position)
        {
            Position = position;

            PxVec3 vec = PxVec3_new_3(position.X, position.Y, position.Z);
            ;

            PxTransform pose = PxTransform_new_1(&vec);
            PxRigidActor_setGlobalPose_mut((PxRigidActor*)dynamicCollider, &pose, true);
        }
        public void SetRotation(Quaternion rotation)
        {
            Rotation = rotation;

            PxQuat quat = QuatHelper.OpenTkToPx(rotation);

            PxTransform pose = PxTransform_new_3(&quat);
            PxRigidActor_setGlobalPose_mut((PxRigidActor*)dynamicCollider, &pose, true);
        }
        public void SetPositionAndRotation(Vector3 position, Quaternion rotation)
        {
            Position = position;
            Rotation = rotation;

            PxVec3 vec = PxVec3_new_3(position.X, position.Y, position.Z);
            PxQuat quat = QuatHelper.OpenTkToPx(rotation);

            PxTransform pose = PxTransform_new_5(&vec, &quat);
            PxRigidActor_setGlobalPose_mut((PxRigidActor*)dynamicCollider, &pose, true);
        }

        public void SetSize(Vector3 size)
        {
            if (type != ObjectType.Cube)
                throw new Exception("Cannot change the size of a '" + type.ToString() + "'!");

            bool isStatic = true;
            if (dynamicCollider != null)
                isStatic = false;

            Size = size;
            AddCubeCollider(isStatic, true);
        }

        public void SetSize(float halfHeight, float radius)
        {
            if (type != ObjectType.Capsule)
                throw new Exception("Cannot change the half height and radius of a '" + type.ToString() + "'!");

            bool isStatic = true;
            if (dynamicCollider != null)
                isStatic = false;

            HalfHeight = halfHeight;
            Radius = radius;
            AddCapsuleCollider(isStatic, true);
        }

        public void SetSize(float radius)
        {
            if (type != ObjectType.Sphere)
                throw new Exception("Cannot change the radius of a '" + type.ToString() + "'!");

            bool isStatic = true;
            if (dynamicCollider != null)
                isStatic = false;

            Radius = radius;
            AddCapsuleCollider(isStatic, true);
        }

        public void SetMaterial(float staticFriction, float dynamicFriction, float restiution)
        {
            StaticFriction = staticFriction;
            DynamicFriction = dynamicFriction;
            Restitution = restiution;

            bool isStatic = true;
            if (dynamicCollider != null)
                isStatic = false;

            if (dynamicCollider != null || staticCollider != null)
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
            if(dynamicCollider != null)
            {
                physx.scene->RemoveActorMut((PxActor*)dynamicCollider, true);
                if(deleteActorPointer)
                    dynamicCollider = null;
            }
            if(staticCollider != null)
            {
                physx.scene->RemoveActorMut((PxActor*)staticCollider, true);
                if(deleteActorPointer)
                    staticCollider = null;
            }
        }

        public void AddCapsuleCollider(bool isStatic, bool removeCollider=false)
        {
            if (removeCollider)
                RemoveCollider();

            if(dynamicCollider != null || staticCollider != null)
                throw new Exception("You can only add one collider. Remove collider first");
            if(dynamicCollider != null && isStatic)
                throw new Exception("You can only add one type of collider. This object has a dynamic collider");
            if(staticCollider != null && !isStatic)
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
            var material = physx.physics->CreateMaterialMut(StaticFriction, DynamicFriction, Restitution);

            if (isStatic)
            {
                staticCollider = physx.physics->PhysPxCreateStatic(&transform, (PxGeometry*)&capsuleGeo, material, &identity);
                physx.scene->AddActorMut((PxActor*)staticCollider, null);
            }
            else
            {
                dynamicCollider = physx.physics->PhysPxCreateDynamic(&transform, (PxGeometry*)&capsuleGeo, material, 10.0f, &identity);
                PxRigidBody_setAngularDamping_mut((PxRigidBody*)dynamicCollider, 0.5f);
                physx.scene->AddActorMut((PxActor*)dynamicCollider, null);
            }
        }

        public void AddCubeCollider(bool isStatic, bool removeCollider = false)
        {
            if (removeCollider)
                RemoveCollider();

            if (dynamicCollider != null || staticCollider != null)
                throw new Exception("You can only add one collider. Remove collider first");
            if (dynamicCollider != null && isStatic)
                throw new Exception("You can only add one type of collider. This object has a dynamic collider");
            if (staticCollider != null && !isStatic)
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
            var material = physx.physics->CreateMaterialMut(StaticFriction, DynamicFriction, Restitution);

            if (isStatic)
            {
                staticCollider = physx.physics->PhysPxCreateStatic(&transform, (PxGeometry*)&cubeGeo, material, &identity);
                physx.scene->AddActorMut((PxActor*)staticCollider, null);
            }
            else
            {
                dynamicCollider = physx.physics->PhysPxCreateDynamic(&transform, (PxGeometry*)&cubeGeo, material, 10.0f, &identity);
                PxRigidBody_setAngularDamping_mut((PxRigidBody*)dynamicCollider, 0.5f);
                physx.scene->AddActorMut((PxActor*)dynamicCollider, null);
            }
        }

        public void AddSphereCollider(bool isStatic, bool removeCollider = false)
        {
            if (removeCollider)
                RemoveCollider();

            if (dynamicCollider != null || staticCollider != null)
                throw new Exception("You can only add one collider. Remove collider first");
            if (dynamicCollider != null && isStatic)
                throw new Exception("You can only add one type of collider. This object has a dynamic collider");
            if (staticCollider != null && !isStatic)
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
            var material = physx.physics->CreateMaterialMut(StaticFriction, DynamicFriction, Restitution);

            if (isStatic)
            {
                staticCollider = physx.physics->PhysPxCreateStatic(&transform, (PxGeometry*)&sphereGeo, material, &identity);
                physx.scene->AddActorMut((PxActor*)staticCollider, null);
            }
            else
            {
                dynamicCollider = physx.physics->PhysPxCreateDynamic(&transform, (PxGeometry*)&sphereGeo, material, 10.0f, &identity);
                PxRigidBody_setAngularDamping_mut((PxRigidBody*)dynamicCollider, 0.5f);
                physx.scene->AddActorMut((PxActor*)dynamicCollider, null);
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

            // Back face
            tris.Add(new triangle(p1, p2, p3));
            tris.Add(new triangle(p3, p4, p1));

            // Front face
            tris.Add(new triangle(p5, p6, p7));
            tris.Add(new triangle(p7, p8, p5));

            // Left face
            tris.Add(new triangle(p1, p4, p8));
            tris.Add(new triangle(p8, p5, p1));

            // Right face
            tris.Add(new triangle(p2, p3, p7));
            tris.Add(new triangle(p7, p6, p2));

            // Top face
            tris.Add(new triangle(p4, p3, p7));
            tris.Add(new triangle(p7, p8, p4));

            // Bottom face
            tris.Add(new triangle(p1, p2, p6));
            tris.Add(new triangle(p6, p5, p1));

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

                    tris.Add(new triangle(new Vector3[] { p1, p2, p3 }));
                    tris.Add(new triangle(new Vector3[] { p2, p4, p3 }));
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

            return tris;
        }
        #endregion
    }
}