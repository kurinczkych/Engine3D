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
        AudioEmitter,
        LightSource,
        ParticleEmitter
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
        public Vector3 Position { get; set; }
        public Quaternion Rotation { get; set; }
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

        public List<IComponent> components = new List<IComponent>();

        public bool isSelected { get; set; }
        public bool isEnabled = true;
        public List<Object>? moverGizmos;
        public AABB Bounds;

        //public string meshName
        //{
        //    get
        //    {
        //        if (mesh != null)
        //            return mesh.modelName;

        //        return "";
        //    }
        //    set
        //    {
        //        string relativePath = AssetManager.GetRelativeModelsFolder(value);
        //        mesh.modelPath = relativePath;
        //        mesh.modelName = Path.GetFileName(mesh.modelPath);
        //        mesh.ProcessObj(mesh.modelPath);

        //        if (mesh.model.meshes.Count > 0 && mesh.model.meshes[0].uniqueVertices.Count > 0 && !mesh.model.meshes[0].uniqueVertices[0].gotNormal)
        //        {
        //            mesh.ComputeVertexNormalsSpherical();
        //        }
        //        else if (mesh.model.meshes.Count > 0 && mesh.model.meshes[0].uniqueVertices.Count > 0 && mesh.model.meshes[0].uniqueVertices[0].gotNormal)
        //            mesh.ComputeVertexNormals();

        //        mesh.ComputeTangents();
        //        mesh.recalculate = true;
        //    }
        //}

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
                    throw new NotImplementedException();
                }
                components.RemoveAt(i);
            }
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