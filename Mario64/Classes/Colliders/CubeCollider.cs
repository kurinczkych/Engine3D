//using MagicPhysX;
//using static MagicPhysX.NativeMethods;
//using OpenTK.Mathematics;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace Mario64
//{
//    public unsafe class CubeCollider : Mesh
//    {
//        PxRigidDynamic* cubeDynamicCollider;
//        PxRigidStatic* cubeStaticCollider;

//        public Vector3 Center
//        {
//            get
//            {
//                return new Vector3(Position.X + (Scale.X / 2f),
//                                   Position.Y + (Scale.Y / 2f),
//                                   Position.Z + (Scale.Z / 2f));
//            }
//            set
//            {
//                Vector3 pos = value;
//                Position.X = pos.X - (Scale.X / 2f);
//                Position.Y = pos.Y - (Scale.Y / 2f);
//                Position.Z = pos.Z - (Scale.Z / 2f);
//            }
//        }

//        public CubeCollider(VAO vao, VBO vbo, int shaderProgramId, string embeddedTextureName, int ocTreeDepth, Vector2 windowSize, ref Frustum frustum, ref Camera camera, ref int textureCount) :
//            base(vao, vbo, shaderProgramId, embeddedTextureName, ocTreeDepth, windowSize, ref frustum, ref camera, ref textureCount)
//        {
//            OnlyCube();

//            if (ocTreeDepth != -1)
//            {
//                CalculateBoundingBox();
//                Octree = new Octree(new List<triangle>(tris), BoundingBox, ocTreeDepth);
//            }

//            ComputeVertexNormals(ref tris);

//            SendUniforms();
//        }

//        public void Init(Vector3 pos, Vector3 scale, Vector3 rot)
//        {
//            Position = pos;
//            Scale = scale;

//            Vector3 radRot = new Vector3(MathHelper.DegreesToRadians(rot.X),
//                                         MathHelper.DegreesToRadians(rot.Y),
//                                         MathHelper.DegreesToRadians(rot.Z));

//            Quaternion rotationX = Quaternion.FromAxisAngle(Vector3.UnitX, radRot.X);
//            Quaternion rotationY = Quaternion.FromAxisAngle(Vector3.UnitY, radRot.Y);
//            Quaternion rotationZ = Quaternion.FromAxisAngle(Vector3.UnitZ, radRot.Z);

//            Rotation = rotationX * rotationY * rotationZ;
//            Rotation.Normalize();
//        }

//        public void CollisionResponse()
//        {
//            if (cubeDynamicCollider != null)
//            {
//                PxTransform transform = PxRigidActor_getGlobalPose((PxRigidActor*)cubeDynamicCollider);

//                // Centered origin to corner origin
//                Position.X = transform.p.x - (Scale.X / 2f);
//                Position.Y = transform.p.y - (Scale.Y / 2f);
//                Position.Z = transform.p.z - (Scale.Z / 2f);

//                Rotation = QuatHelper.PxToOpenTk(transform.q);
//            }
//        }

//        public void AddCubeCollider(bool isStatic, ref Physx physx)
//        {
//            var cubeGeo = PxBoxGeometry_new(Scale.X / 2f, Scale.Y / 2f , Scale.Z / 2f);
//            PxVec3 vec3 = new PxVec3 { x = Center.X, y = Center.Y, z = Center.Z };
//            PxQuat quat = QuatHelper.OpenTkToPx(Rotation);
//            var transform = PxTransform_new_5(&vec3, &quat);
//            var identity = PxTransform_new_2(PxIDENTITY.PxIdentity);
//            var material = physx.physics->CreateMaterialMut(0.5f, 0.5f, 0.1f);

//            if (isStatic)
//            {
//                cubeStaticCollider = physx.physics->PhysPxCreateStatic(&transform, (PxGeometry*)&cubeGeo, material, &identity);
//                physx.scene->AddActorMut((PxActor*)cubeStaticCollider, null);
//            }
//            else
//            {
//                cubeDynamicCollider = physx.physics->PhysPxCreateDynamic(&transform, (PxGeometry*)&cubeGeo, material, 10.0f, &identity);
//                PxRigidBody_setAngularDamping_mut((PxRigidBody*)cubeDynamicCollider, 0.5f);
//                physx.scene->AddActorMut((PxActor*)cubeDynamicCollider, null);
//            }
//        }


//        private void OnlyCube()
//        {
//            float halfSize = 0.5f;

//            // Define cube vertices
//            Vector3 p1 = new Vector3(-halfSize, -halfSize, -halfSize);
//            Vector3 p2 = new Vector3(halfSize, -halfSize, -halfSize);
//            Vector3 p3 = new Vector3(halfSize, halfSize, -halfSize);
//            Vector3 p4 = new Vector3(-halfSize, halfSize, -halfSize);
//            Vector3 p5 = new Vector3(-halfSize, -halfSize, halfSize);
//            Vector3 p6 = new Vector3(halfSize, -halfSize, halfSize);
//            Vector3 p7 = new Vector3(halfSize, halfSize, halfSize);
//            Vector3 p8 = new Vector3(-halfSize, halfSize, halfSize);

//            // Back face
//            tris.Add(new triangle(p1, p2, p3));
//            tris.Add(new triangle(p3, p4, p1));

//            // Front face
//            tris.Add(new triangle(p5, p6, p7));
//            tris.Add(new triangle(p7, p8, p5));

//            // Left face
//            tris.Add(new triangle(p1, p4, p8));
//            tris.Add(new triangle(p8, p5, p1));

//            // Right face
//            tris.Add(new triangle(p2, p3, p7));
//            tris.Add(new triangle(p7, p6, p2));

//            // Top face
//            tris.Add(new triangle(p4, p3, p7));
//            tris.Add(new triangle(p7, p8, p4));

//            // Bottom face
//            tris.Add(new triangle(p1, p2, p6));
//            tris.Add(new triangle(p6, p5, p1));
//        }
//    }
//}
