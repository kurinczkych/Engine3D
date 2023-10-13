using MagicPhysX;
using static MagicPhysX.NativeMethods;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Formats.Asn1.AsnWriter;
using System.Runtime.InteropServices;
using System.Net;

namespace Engine3D
{
    public unsafe class Physx
    {
        private IntPtr foundationPtr;
        private IntPtr pvdPtr;
        private IntPtr physicsPtr;
        private IntPtr scenePtr;
        private IntPtr dispatcherPtr;

        public PxFoundation* GetFoundation() { return (PxFoundation*)foundationPtr.ToPointer(); }
        public PxPvd* GetPvd() { return (PxPvd*)pvdPtr.ToPointer(); }
        public PxPhysics* GetPhysics() { return (PxPhysics*)physicsPtr.ToPointer(); }
        public PxScene* GetScene() { return (PxScene*)scenePtr.ToPointer(); }
        public PxDefaultCpuDispatcher* GetDispatcher() { return (PxDefaultCpuDispatcher*)dispatcherPtr.ToPointer(); }

        public Physx(bool usePvd=false)
        {
            unsafe
            {
                if (usePvd)
                {
                    foundationPtr = new IntPtr(physx_create_foundation());

                    pvdPtr = new IntPtr(phys_PxCreatePvd(GetFoundation()));

                    var tolerancesScale = new PxTolerancesScale { length = 1, speed = 10 };

                    uint PX_PHYSICS_VERSION_MAJOR = 5;
                    uint PX_PHYSICS_VERSION_MINOR = 1;
                    uint PX_PHYSICS_VERSION_BUGFIX = 3;
                    uint versionNumber = (PX_PHYSICS_VERSION_MAJOR << 24) + (PX_PHYSICS_VERSION_MINOR << 16) + (PX_PHYSICS_VERSION_BUGFIX << 8);

                    physicsPtr = new IntPtr(phys_PxCreatePhysics(versionNumber, GetFoundation(), &tolerancesScale, true, GetPvd(), null));
                    phys_PxInitExtensions(GetPhysics(), GetPvd());

                    string ipAddress = "0.0.0.0";
                    byte[] byteArray = IPAddress.Parse(ipAddress).GetAddressBytes();
                    fixed (byte* bytePointer = byteArray)
                    {
                        var transport = phys_PxDefaultPvdSocketTransportCreate(bytePointer, 5425, 100);
                        GetPvd()->ConnectMut(transport, PxPvdInstrumentationFlags.All);
                    }

                    var sceneDesc = PxSceneDesc_new(&tolerancesScale);
                    //sceneDesc.gravity = new PxVec3 { x = 0.0f, y = -9.81f, z = 0.0f };
                    sceneDesc.gravity = new PxVec3 { x = 0.0f, y = -30f, z = 0.0f };

                    dispatcherPtr = new IntPtr(phys_PxDefaultCpuDispatcherCreate(1, null, PxDefaultCpuDispatcherWaitForWorkMode.WaitForWork, 0));
                    sceneDesc.cpuDispatcher = (PxCpuDispatcher*)GetDispatcher();
                    sceneDesc.filterShader = get_default_simulation_filter_shader();

                    scenePtr = new IntPtr(GetPhysics()->CreateSceneMut(&sceneDesc));

                    var pvdClient = GetScene()->GetScenePvdClientMut();
                    if (pvdClient != null)
                    {
                        pvdClient->SetScenePvdFlagMut(PxPvdSceneFlag.TransmitConstraints, true);
                        pvdClient->SetScenePvdFlagMut(PxPvdSceneFlag.TransmitContacts, true);
                        pvdClient->SetScenePvdFlagMut(PxPvdSceneFlag.TransmitScenequeries, true);
                    }
                }
                else
                {
                    foundationPtr = new IntPtr(physx_create_foundation());
                    physicsPtr = new IntPtr(physx_create_physics(GetFoundation()));

                    var sceneDesc = PxSceneDesc_new(PxPhysics_getTolerancesScale(GetPhysics()));
                    sceneDesc.gravity = new PxVec3 { x = 0.0f, y = -9.81f, z = 0.0f };

                    dispatcherPtr = new IntPtr(phys_PxDefaultCpuDispatcherCreate(1, null, PxDefaultCpuDispatcherWaitForWorkMode.WaitForWork, 0));
                    sceneDesc.cpuDispatcher = (PxCpuDispatcher*)GetDispatcher();
                    sceneDesc.filterShader = get_default_simulation_filter_shader();

                    scenePtr = new IntPtr(GetPhysics()->CreateSceneMut(&sceneDesc));
                }
                

                //var material = physics->CreateMaterialMut(0.5f, 0.5f, 0.6f);

                //var plane = PxPlane_new_1(0.0f, 1.0f, 0.0f, 0.0f);
                //var groundPlane = physics->PhysPxCreatePlane(&plane, material);
                //scene->AddActorMut((PxActor*)groundPlane, null);

                //var sphereGeo = PxSphereGeometry_new(10.0f);
                //var vec3 = new PxVec3 { x = 0.0f, y = 40.0f, z = 100.0f };
                //var transform = PxTransform_new_1(&vec3);
                //var identity = PxTransform_new_2(PxIDENTITY.PxIdentity);
                //var sphere = physics->PhysPxCreateDynamic(&transform, (PxGeometry*)&sphereGeo, material, 10.0f, &identity);
                //PxRigidBody_setAngularDamping_mut((PxRigidBody*)sphere, 0.5f);
                //scene->AddActorMut((PxActor*)sphere, null);


            }
        }

        public void Simulate(float delta)
        {
            GetScene()->SimulateMut(delta, null, null, 0, true);
            uint error = 0;
            GetScene()->FetchResultsMut(true, &error);
        }

        ~Physx()
        {
            unsafe
            {
                PxScene_release_mut(GetScene());
                PxDefaultCpuDispatcher_release_mut(GetDispatcher());
                PxPhysics_release_mut(GetPhysics());
                if (pvdPtr == IntPtr.Zero)
                {
                    GetPvd()->DisconnectMut();
                    GetPvd()->ReleaseMut();
                }
            }
        }
    }
}
