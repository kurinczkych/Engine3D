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
using System.Reflection.Metadata.Ecma335;
using OpenTK.Graphics.OpenGL;
using System.Data;

#pragma warning disable CS8767

namespace Engine3D
{
    public enum ObjectType
    {
        Empty,
        Cube,
        Sphere,
        Capsule,
        TriangleMesh,
        TriangleMeshWithCollider,
        TestMesh,
        Wireframe,
        TextMesh,
        UIMesh,
        Plane,
        AudioEmitter
    }

    public enum ColliderType
    {
        None,
        TriangleMesh,
        Cube,
        Capsule,
        Sphere
    }

    public class Transformation
    {
        public Vector3 LastPosition;
        private Vector3 position_;
        public Vector3 Position
        {
            get { return position_; }
            set
            {
                LastPosition = position_;
                position_ = value;
            }
        }

        public Quaternion LastRotation;
        private Quaternion rotation_;
        public Quaternion Rotation
        {
            get { return rotation_; }
            set
            {
                LastRotation = rotation_;
                rotation_ = value;
            }
        }

        public Vector3 Scale = Vector3.One;

        public Transformation(Vector3 position, Quaternion rotation, Vector3 scale)
        { 
            Position = position;
            Rotation = rotation;
            Scale = scale;
        }

        public Transformation(Vector3 position, Quaternion rotation)
        { 
            Position = position;
            Rotation = rotation;
            Scale = Vector3.One;
        }

        public Transformation()
        {
            Position = Vector3.Zero;
            Rotation = Quaternion.Identity;
            Scale = Vector3.One;
        }
    }

    public unsafe class Object : ISelectable, IComparable<Object>
    {
        public int id;
        public string name = "";
        public string displayName = "";

        public List<IComponent> components = new List<IComponent>();

        public bool isSelected { get; set; }
        public bool isEnabled = true;
        public List<Object>? moverGizmos;
        public AABB Bounds;

        public bool useBVH = false;

        //---------------------------------------------------------------

        private ObjectType type;

        private BaseMesh? mesh_;
        public BaseMesh? Mesh
        {
            get 
            { 
                if(mesh_ == null)
                {
                    mesh_ = components.OfType<BaseMesh>().FirstOrDefault();
                    return mesh_;
                }
                else
                    return mesh_;
            }
        }

        private Physics? physics_;
        public Physics? Physics
        {
            get
            {
                if (physics_ == null)
                {
                    physics_ = components.OfType<Physics>().FirstOrDefault();
                    return physics_;
                }
                else
                    return physics_;
            }
        }

        private ParticleSystem? particleSystem_;
        public ParticleSystem? ParticleSystem
        {
            get
            {
                if (particleSystem_ == null)
                {
                    particleSystem_ = components.OfType<ParticleSystem>().FirstOrDefault();
                    return particleSystem_;
                }
                else
                    return particleSystem_;
            }
        }

        public BSP BSPStruct { get; private set; }
        public Octree Octree { get; private set; }
        public GridStructure GridStructure { get; private set; }

        public Transformation transformation { get; set; }

        public string PStr
        {
            get { return Math.Round(transformation.Position.X, 2).ToString() + "," + 
                         Math.Round(transformation.Position.Y, 2).ToString() + "," + 
                         Math.Round(transformation.Position.Z, 2).ToString(); }
        }

        public Object(ObjectType type, int id = -1)
        {
            if (id == -1)
            {
                this.id = Engine.objectID;
                Engine.objectID++;
            }
            else
            {
                this.id = id;
            }

            this.type = type;

            transformation = new Transformation(Vector3.Zero, Quaternion.Identity);
        }

        public void AddMesh(BaseMesh mesh)
        {
            Bounds = new AABB();
            foreach (MeshData meshData in mesh.model.meshes)
            {
                if(meshData.Bounds.Min.X < Bounds.Min.X) Bounds.Min.X = meshData.Bounds.Min.X;
                if(meshData.Bounds.Min.Y < Bounds.Min.Y) Bounds.Min.Y = meshData.Bounds.Min.Y;
                if(meshData.Bounds.Min.Z < Bounds.Min.Z) Bounds.Min.Z = meshData.Bounds.Min.Z;

                if(meshData.Bounds.Max.X > Bounds.Max.X) Bounds.Max.X = meshData.Bounds.Max.X;
                if(meshData.Bounds.Max.Y > Bounds.Max.Y) Bounds.Max.Y = meshData.Bounds.Max.Y;
                if(meshData.Bounds.Max.Z > Bounds.Max.Z) Bounds.Max.Z = meshData.Bounds.Max.Z;
            }

            //StaticFriction = 0.5f;
            //DynamicFriction = 0.5f;
            //Restitution = 0.1f;

            mesh.CalculateFrustumVisibility();
            mesh.ComputeTangents();

            components.Add(mesh);
        }

        public void BuildBVH()
        {
            //Calculating bounding volume hiearchy
            //if (meshType == typeof(Mesh) ||
            //    meshType == typeof(InstancedMesh))
            //{
            //    mesh.BVHStruct = new BVH(mesh.tris);
            //}
            throw new NotImplementedException();
        }

        public void BuildBSP()
        {
            //BSPStruct = new BSP(((Mesh)mesh).tris);
            throw new NotImplementedException();
        }

        public void BuildOctree()
        {
            //Octree = new Octree();
            //Octree.Build(((Mesh)mesh).tris, ((Mesh)mesh).Bounds);
            throw new NotImplementedException();
        }

        public void BuildGrid(Shader currentShader, Shader GridShader)
        {
            //GridShader.Use();
            //GridStructure = new GridStructure(((Mesh)mesh).tris, ((Mesh)mesh).Bounds, 20, GridShader.id);
            //currentShader.Use();
            throw new NotImplementedException();
        }

        public void Delete(ref TextureManager textureManager)
        {
            for(int i = components.Count-1; i >= 0; i--)
            {
                IComponent c = components[i];
                if(c is BaseMesh cMesh)
                {
                    cMesh.Delete(ref textureManager);
                }
                else if(c is Physics cPhysics)
                {
                    cPhysics.RemoveCollider();
                }
                components.RemoveAt(i);
            }
        }

        public void DeleteComponent(IComponent component, ref TextureManager textureManager)
        {
            if (component is BaseMesh cMesh)
            {
                cMesh.Delete(ref textureManager);
                mesh_ = null;
            }
            else if (component is Physics cPhysics)
            {
                cPhysics.RemoveCollider();
                physics_ = null;
            }
            else if(component is Light cLight)
            {
                
            }
            components.Remove(component);
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

                float thisDistance = (transformation.Position - cameraPosition).LengthSquared;
                float otherDistance = (other.transformation.Position - cameraPosition).LengthSquared;

                // Comparing the distances (squared) for TriangleMesh objects.
                return thisDistance.CompareTo(otherDistance);
            }

            return orderComparison;
        }

        

        #region Getters
        public Vector3 GetPosition()
        {
            return transformation.Position;
        }
        #endregion
    }
}