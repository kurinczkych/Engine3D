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
                if (editorData.gizmoManager.PerInstanceMove && editorData.instIndex != -1 && editorData.selectedItem is Object insto &&
                    insto.meshType == typeof(InstancedMesh))
                {
                    if (objectMovingAxis == Axis.X)
                    {
                        ((InstancedMesh)insto.GetMesh()).instancedData[editorData.instIndex].Position =
                            ((InstancedMesh)insto.GetMesh()).instancedData[editorData.instIndex].Position - new Vector3(deltaX / 10, 0, 0);
                    }
                    else if (objectMovingAxis == Axis.Y)
                    {
                        ((InstancedMesh)insto.GetMesh()).instancedData[editorData.instIndex].Position =
                            ((InstancedMesh)insto.GetMesh()).instancedData[editorData.instIndex].Position - new Vector3(0, deltaY / 10, 0);
                    }
                    else if (objectMovingAxis == Axis.Z)
                    {
                        ((InstancedMesh)insto.GetMesh()).instancedData[editorData.instIndex].Position =
                            ((InstancedMesh)insto.GetMesh()).instancedData[editorData.instIndex].Position + new Vector3(0, 0, deltaX / 10);
                    }
                }
                else if(editorData.selectedItem is Object o)
                {
                    if (objectMovingAxis == Axis.X && objectMovingPlane != null)
                    {
                        Vector3 dir = character.camera.GetCameraRay(MouseState.Position);
                        Vector3? _pos = objectMovingPlane.RayPlaneIntersection(character.camera.GetPosition(), dir);

                        if (_pos != null)
                        {
                            Vector3 pos = (Vector3)_pos;
                            if (editorData.gizmoManager.AbsoluteMoving)
                            {
                                pos.Y = objectMovingOrig.Y;
                            }
                            else
                            {
                                Vector3 searchDir = Vector3.Transform(new Vector3(1, 0, 0), o.Rotation);
                                if (searchDir.X == 0)
                                    searchDir.X = 0.01f;
                                float slopeY = searchDir.Y / searchDir.X;
                                float yIntercept = 0 - slopeY * 0;

                                float slopeZ = searchDir.Z / searchDir.X;
                                float zIntercept = 0 - slopeZ * 0;

                                pos.Y = slopeY * pos.X + yIntercept;
                                pos.Z = slopeZ * pos.X + zIntercept;
                            }

                            ((ISelectable)editorData.selectedItem).Position = ((ISelectable)editorData.selectedItem).Position + (pos - objectMovingOrig);
                            objectMovingOrig = pos;
                        }
                    }
                    else if (objectMovingAxis == Axis.Y && objectMovingPlane != null)
                    {
                        Vector3 dir = character.camera.GetCameraRay(MouseState.Position);
                        Vector3? _pos = objectMovingPlane.RayPlaneIntersection(character.camera.GetPosition(), dir);

                        if (_pos != null)
                        {
                            Vector3 pos = (Vector3)_pos;
                            if (editorData.gizmoManager.AbsoluteMoving)
                            {
                                pos.X = objectMovingOrig.X;
                            }
                            else
                            {
                                Vector3 searchDir = Vector3.Transform(new Vector3(0, 1, 0), o.Rotation);
                                if (searchDir.Y == 0)
                                    searchDir.Y = 0.01f;
                                float slopeX = searchDir.X / searchDir.Y;
                                float xIntercept = 0 - slopeX * 0;

                                float slopeZ = searchDir.Z / searchDir.Y;
                                float zIntercept = 0 - slopeZ * 0;

                                pos.X = slopeX * pos.Y + xIntercept;
                                pos.Z = slopeZ * pos.Y + zIntercept;
                            }
                            ((ISelectable)editorData.selectedItem).Position = ((ISelectable)editorData.selectedItem).Position + (pos - objectMovingOrig);
                            objectMovingOrig = pos;
                        }
                    }
                    else if (objectMovingAxis == Axis.Z && objectMovingPlane != null)
                    {
                        Vector3 dir = character.camera.GetCameraRay(MouseState.Position);
                        Vector3? _pos = objectMovingPlane.RayPlaneIntersection(character.camera.GetPosition(), dir);

                        if (_pos != null)
                        {
                            Vector3 pos = (Vector3)_pos;

                            if (editorData.gizmoManager.AbsoluteMoving)
                            {
                                pos.Y = objectMovingOrig.Y;
                            }
                            else
                            {
                                Vector3 searchDir = Vector3.Transform(new Vector3(0, 0, 1), o.Rotation);
                                if (searchDir.Z == 0)
                                    searchDir.Z = 0.01f;
                                float slopeY = searchDir.Y / searchDir.Z;
                                float yIntercept = 0 - slopeY * 0;

                                float slopeX = searchDir.X / searchDir.Z;
                                float xIntercept = 0 - slopeX * 0;

                                pos.Y = slopeY * pos.Z + yIntercept;
                                pos.X = slopeX * pos.Z + xIntercept;
                            }

                            ((ISelectable)editorData.selectedItem).Position = ((ISelectable)editorData.selectedItem).Position + (pos - objectMovingOrig);
                            objectMovingOrig = pos;
                        }
                    }
                }
                else
                {
                    if (objectMovingAxis == Axis.X)
                    {
                        ((ISelectable)editorData.selectedItem).Position = ((ISelectable)editorData.selectedItem).Position - new Vector3(deltaX / 10, 0, 0);
                    }
                    else if (objectMovingAxis == Axis.Y)
                    {
                        ((ISelectable)editorData.selectedItem).Position = ((ISelectable)editorData.selectedItem).Position - new Vector3(0, deltaY / 10, 0);
                    }
                    else if (objectMovingAxis == Axis.Z)
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
                        if (objectMovingAxis == Axis.X)
                        {
                            ((InstancedMesh)o.GetMesh()).instancedData[editorData.instIndex].Rotation =
                                Helper.QuaternionFromEuler(rot - new Vector3(deltaX / 10, 0, 0));
                        }
                        else if (objectMovingAxis == Axis.Y)
                        {
                            ((InstancedMesh)o.GetMesh()).instancedData[editorData.instIndex].Rotation =
                                Helper.QuaternionFromEuler(rot - new Vector3(0, deltaY / 10, 0));
                        }
                        else if (objectMovingAxis == Axis.Z)
                        {
                            ((InstancedMesh)o.GetMesh()).instancedData[editorData.instIndex].Rotation =
                                Helper.QuaternionFromEuler(rot + new Vector3(0, 0, deltaX / 10));
                        }
                    }
                    else if (o.meshType == typeof(Mesh) || o.meshType == typeof(InstancedMesh))
                    {
                        Vector3 rot = Helper.EulerFromQuaternion(o.Rotation);
                        if (objectMovingAxis == Axis.X)
                        {
                            o.Rotation = Helper.QuaternionFromEuler(rot - new Vector3(deltaX / 10, 0, 0));
                        }
                        else if (objectMovingAxis == Axis.Y)
                        {
                            o.Rotation = Helper.QuaternionFromEuler(rot - new Vector3(0, deltaY / 10, 0));
                        }
                        else if (objectMovingAxis == Axis.Z)
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
                        if (objectMovingAxis == Axis.X)
                        {
                            ((InstancedMesh)o.GetMesh()).instancedData[editorData.instIndex].Scale = 
                                ((InstancedMesh)o.GetMesh()).instancedData[editorData.instIndex].Scale - new Vector3(deltaX / 10, 0, 0);
                        }
                        else if (objectMovingAxis == Axis.Y)
                        {
                            ((InstancedMesh)o.GetMesh()).instancedData[editorData.instIndex].Scale = 
                                ((InstancedMesh)o.GetMesh()).instancedData[editorData.instIndex].Scale - new Vector3(0, deltaY / 10, 0);
                        }
                        else if (objectMovingAxis == Axis.Z)
                        {
                            ((InstancedMesh)o.GetMesh()).instancedData[editorData.instIndex].Scale = 
                                ((InstancedMesh)o.GetMesh()).instancedData[editorData.instIndex].Scale + new Vector3(0, 0, deltaX / 10);
                        }
                    }
                    else if (o.meshType == typeof(Mesh) || o.meshType == typeof(InstancedMesh))
                    {
                        // TODO 
                        if (objectMovingAxis == Axis.X)
                        {
                            o.Scale = o.Scale - new Vector3(deltaX / 10, 0, 0);
                        }
                        else if (objectMovingAxis == Axis.Y)
                        {
                            o.Scale = o.Scale - new Vector3(0, deltaY / 10, 0);
                        }
                        else if (objectMovingAxis == Axis.Z)
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
