using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine3D
{
    public partial class Engine    
    {
        private void GizmoObjectManipulating()
        {
            bool[] which = new bool[3] { false, false, false };
            if (editorData.gizmoManager.gizmoType == GizmoType.Move) // Moving
            {
                if (editorData.gizmoManager.PerInstanceMove && editorData.instIndex != -1 && editorData.selectedItem is Object o &&
                    o.meshType == typeof(InstancedMesh))
                {
                    if (objectMovingAxis == Vector3.UnitX)
                    {
                        ((InstancedMesh)o.GetMesh()).instancedData[editorData.instIndex].Position =
                            ((InstancedMesh)o.GetMesh()).instancedData[editorData.instIndex].Position - new Vector3(deltaX / 10, 0, 0);
                    }
                    else if (objectMovingAxis == Vector3.UnitY)
                    {
                        ((InstancedMesh)o.GetMesh()).instancedData[editorData.instIndex].Position =
                            ((InstancedMesh)o.GetMesh()).instancedData[editorData.instIndex].Position - new Vector3(0, deltaY / 10, 0);
                    }
                    else if (objectMovingAxis == Vector3.UnitZ)
                    {
                        ((InstancedMesh)o.GetMesh()).instancedData[editorData.instIndex].Position =
                            ((InstancedMesh)o.GetMesh()).instancedData[editorData.instIndex].Position + new Vector3(0, 0, deltaX / 10);
                    }
                }
                else
                {
                    if (objectMovingAxis == Vector3.UnitX)
                    {
                        ((ISelectable)editorData.selectedItem).Position = ((ISelectable)editorData.selectedItem).Position - new Vector3(deltaX / 10, 0, 0);
                    }
                    else if (objectMovingAxis == Vector3.UnitY)
                    {
                        ((ISelectable)editorData.selectedItem).Position = ((ISelectable)editorData.selectedItem).Position - new Vector3(0, deltaY / 10, 0);
                    }
                    else if (objectMovingAxis == Vector3.UnitZ)
                    {
                        ((ISelectable)editorData.selectedItem).Position = ((ISelectable)editorData.selectedItem).Position + new Vector3(0, 0, deltaX / 10);
                    }
                }

                which[0] = true;
            }
            else if (editorData.gizmoManager.gizmoType == GizmoType.Rotate) // Rotating
            {
                if (editorData.selectedItem is Object o && (o.meshType == typeof(Mesh) || o.meshType == typeof(InstancedMesh)))
                {
                    if (editorData.gizmoManager.PerInstanceMove && editorData.instIndex != -1 && o.meshType == typeof(InstancedMesh))
                    {
                        Vector3 rot = Helper.EulerFromQuaternion(((InstancedMesh)o.GetMesh()).instancedData[editorData.instIndex].Rotation);
                        if (objectMovingAxis == Vector3.UnitX)
                        {
                            ((InstancedMesh)o.GetMesh()).instancedData[editorData.instIndex].Rotation =
                                Helper.QuaternionFromEuler(rot - new Vector3(deltaX / 10, 0, 0));
                        }
                        else if (objectMovingAxis == Vector3.UnitY)
                        {
                            ((InstancedMesh)o.GetMesh()).instancedData[editorData.instIndex].Rotation =
                                Helper.QuaternionFromEuler(rot - new Vector3(0, deltaY / 10, 0));
                        }
                        else if (objectMovingAxis == Vector3.UnitZ)
                        {
                            ((InstancedMesh)o.GetMesh()).instancedData[editorData.instIndex].Rotation =
                                Helper.QuaternionFromEuler(rot + new Vector3(0, 0, deltaX / 10));
                        }
                    }
                    else if (o.meshType == typeof(Mesh) || o.meshType == typeof(InstancedMesh))
                    {
                        Vector3 rot = Helper.EulerFromQuaternion(o.Rotation);
                        if (objectMovingAxis == Vector3.UnitX)
                        {
                            o.Rotation = Helper.QuaternionFromEuler(rot - new Vector3(deltaX / 10, 0, 0));
                        }
                        else if (objectMovingAxis == Vector3.UnitY)
                        {
                            o.Rotation = Helper.QuaternionFromEuler(rot - new Vector3(0, deltaY / 10, 0));
                        }
                        else if (objectMovingAxis == Vector3.UnitZ)
                        {
                            o.Rotation = Helper.QuaternionFromEuler(rot + new Vector3(0, 0, deltaX / 10));
                        }
                    }

                    which[1] = true;
                }
            }
            else if (editorData.gizmoManager.gizmoType == GizmoType.Scale) // Scaling
            {
                if (editorData.selectedItem is Object o && (o.meshType == typeof(Mesh) || o.meshType == typeof(InstancedMesh)))
                {
                    if (editorData.gizmoManager.PerInstanceMove && editorData.instIndex != -1 && o.meshType == typeof(InstancedMesh))
                    {
                        if (objectMovingAxis == Vector3.UnitX)
                        {
                            ((InstancedMesh)o.GetMesh()).instancedData[editorData.instIndex].Scale = 
                                ((InstancedMesh)o.GetMesh()).instancedData[editorData.instIndex].Scale - new Vector3(deltaX / 10, 0, 0);
                        }
                        else if (objectMovingAxis == Vector3.UnitY)
                        {
                            ((InstancedMesh)o.GetMesh()).instancedData[editorData.instIndex].Scale = 
                                ((InstancedMesh)o.GetMesh()).instancedData[editorData.instIndex].Scale - new Vector3(0, deltaY / 10, 0);
                        }
                        else if (objectMovingAxis == Vector3.UnitZ)
                        {
                            ((InstancedMesh)o.GetMesh()).instancedData[editorData.instIndex].Scale = 
                                ((InstancedMesh)o.GetMesh()).instancedData[editorData.instIndex].Scale + new Vector3(0, 0, deltaX / 10);
                        }
                    }
                    else if (o.meshType == typeof(Mesh) || o.meshType == typeof(InstancedMesh))
                    {
                        if (objectMovingAxis == Vector3.UnitX)
                        {
                            o.Scale = o.Scale - new Vector3(deltaX / 10, 0, 0);
                        }
                        else if (objectMovingAxis == Vector3.UnitY)
                        {
                            o.Scale = o.Scale - new Vector3(0, deltaY / 10, 0);
                        }
                        else if (objectMovingAxis == Vector3.UnitZ)
                        {
                            o.Scale = o.Scale + new Vector3(0, 0, deltaX / 10);
                        }
                    }

                    which[2] = true;
                }
            }

            if (editorData.selectedItem is Object toCalcO)
            {
                toCalcO.GetMesh().recalculate = true;
                toCalcO.GetMesh().RecalculateModelMatrix(which);
                toCalcO.UpdatePhysxPositionAndRotation();
            }

        }
    }
}
