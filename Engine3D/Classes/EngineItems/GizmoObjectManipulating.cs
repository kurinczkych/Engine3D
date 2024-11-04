using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#pragma warning disable CS8600
#pragma warning disable CS8602

namespace Engine3D
{
    public partial class Engine    
    {
        private void GizmoObjectManipulating()
        {
            bool[] which = new bool[3] { false, false, false };

            if (selectedObject == null)
                return;

            if (gizmoManager.gizmoType == GizmoType.Move) // Moving
            {
                if (gizmoManager.PerInstanceMove && gizmoManager.instIndex != -1 && selectedObject is Object insto &&
                    insto.GetComponent<BaseMesh>() is InstancedMesh instMesh)
                {
                    if (objectMovingAxis == Axis.X && objectMovingPlane != null)
                    {
                        Vector3 dir = mainCamera.GetCameraRay(MouseState.Position);
                        Vector3? _pos = objectMovingPlane.RayPlaneIntersection(mainCamera.GetPosition(), dir);

                        if (_pos != null)
                        {
                            Vector3 pos = (Vector3)_pos;
                            if (gizmoManager.AbsoluteMoving)
                            {
                                pos.Y = objectMovingOrig.Y;
                            }
                            else
                            {
                                Vector3 searchDir = Vector3.Transform(new Vector3(1, 0, 0), insto.transformation.Rotation);
                                if (searchDir.X == 0)
                                    searchDir.X = 0.01f;
                                float slopeY = searchDir.Y / searchDir.X;
                                float yIntercept = 0 - slopeY * 0;

                                float slopeZ = searchDir.Z / searchDir.X;
                                float zIntercept = 0 - slopeZ * 0;

                                pos.Y = slopeY * pos.X + yIntercept;
                                pos.Z = slopeZ * pos.X + zIntercept;
                            }

                            ((InstancedMesh)instMesh).instancedData[gizmoManager.instIndex].Position =
                                ((InstancedMesh)instMesh).instancedData[gizmoManager.instIndex].Position + (pos - objectMovingOrig);
                            objectMovingOrig = pos;
                        }
                    }
                    else if (objectMovingAxis == Axis.Y && objectMovingPlane != null)
                    {
                        Vector3 dir = mainCamera.GetCameraRay(MouseState.Position);
                        Vector3? _pos = objectMovingPlane.RayPlaneIntersection(mainCamera.GetPosition(), dir);

                        if (_pos != null)
                        {
                            Vector3 pos = (Vector3)_pos;
                            if (gizmoManager.AbsoluteMoving)
                            {
                                pos.X = objectMovingOrig.X;
                            }
                            else
                            {
                                Vector3 searchDir = Vector3.Transform(new Vector3(0, 1, 0), insto.transformation.Rotation);
                                if (searchDir.Y == 0)
                                    searchDir.Y = 0.01f;
                                float slopeX = searchDir.X / searchDir.Y;
                                float xIntercept = 0 - slopeX * 0;

                                float slopeZ = searchDir.Z / searchDir.Y;
                                float zIntercept = 0 - slopeZ * 0;

                                pos.X = slopeX * pos.Y + xIntercept;
                                pos.Z = slopeZ * pos.Y + zIntercept;
                            }
                            ((InstancedMesh)instMesh).instancedData[gizmoManager.instIndex].Position =
                                ((InstancedMesh)instMesh).instancedData[gizmoManager.instIndex].Position + (pos - objectMovingOrig);
                            objectMovingOrig = pos;
                        }
                    }
                    else if (objectMovingAxis == Axis.Z && objectMovingPlane != null)
                    {
                        Vector3 dir = mainCamera.GetCameraRay(MouseState.Position);
                        Vector3? _pos = objectMovingPlane.RayPlaneIntersection(mainCamera.GetPosition(), dir);

                        if (_pos != null)
                        {
                            Vector3 pos = (Vector3)_pos;

                            if (gizmoManager.AbsoluteMoving)
                            {
                                pos.Y = objectMovingOrig.Y;
                            }
                            else
                            {
                                Vector3 searchDir = Vector3.Transform(new Vector3(0, 0, 1), insto.transformation.Rotation);
                                if (searchDir.Z == 0)
                                    searchDir.Z = 0.01f;
                                float slopeY = searchDir.Y / searchDir.Z;
                                float yIntercept = 0 - slopeY * 0;

                                float slopeX = searchDir.X / searchDir.Z;
                                float xIntercept = 0 - slopeX * 0;

                                pos.Y = slopeY * pos.Z + yIntercept;
                                pos.X = slopeX * pos.Z + xIntercept;
                            }

                            ((InstancedMesh)instMesh).instancedData[gizmoManager.instIndex].Position =
                                ((InstancedMesh)instMesh).instancedData[gizmoManager.instIndex].Position + (pos - objectMovingOrig);
                            objectMovingOrig = pos;
                        }
                    }
                }
                else if(selectedObject is Object o)
                {
                    if (objectMovingAxis == Axis.X && objectMovingPlane != null)
                    {
                        Vector3 dir = mainCamera.GetCameraRay(MouseState.Position);
                        Vector3? _pos = objectMovingPlane.RayPlaneIntersection(mainCamera.GetPosition(), dir);

                        if (_pos != null)
                        {
                            Vector3 pos = (Vector3)_pos;
                            if (gizmoManager.AbsoluteMoving)
                            {
                                pos.Y = objectMovingOrig.Y;
                            }
                            else
                            {
                                Vector3 searchDir = Vector3.Transform(new Vector3(1, 0, 0), o.transformation.Rotation);
                                if (searchDir.X == 0)
                                    searchDir.X = 0.01f;
                                float slopeY = searchDir.Y / searchDir.X;
                                float yIntercept = 0 - slopeY * 0;

                                float slopeZ = searchDir.Z / searchDir.X;
                                float zIntercept = 0 - slopeZ * 0;

                                pos.Y = slopeY * pos.X + yIntercept;
                                pos.Z = slopeZ * pos.X + zIntercept;
                            }

                            ((ISelectable)selectedObject).transformation.Position = 
                                    ((ISelectable)selectedObject).transformation.Position + (pos - objectMovingOrig);
                            objectMovingOrig = pos;
                        }
                    }
                    else if (objectMovingAxis == Axis.Y && objectMovingPlane != null)
                    {
                        Vector3 dir = mainCamera.GetCameraRay(MouseState.Position);
                        Vector3? _pos = objectMovingPlane.RayPlaneIntersection(mainCamera.GetPosition(), dir);

                        if (_pos != null)
                        {
                            Vector3 pos = (Vector3)_pos;
                            if (gizmoManager.AbsoluteMoving)
                            {
                                pos.X = objectMovingOrig.X;
                            }
                            else
                            {
                                Vector3 searchDir = Vector3.Transform(new Vector3(0, 1, 0), o.transformation.Rotation);
                                if (searchDir.Y == 0)
                                    searchDir.Y = 0.01f;
                                float slopeX = searchDir.X / searchDir.Y;
                                float xIntercept = 0 - slopeX * 0;

                                float slopeZ = searchDir.Z / searchDir.Y;
                                float zIntercept = 0 - slopeZ * 0;

                                pos.X = slopeX * pos.Y + xIntercept;
                                pos.Z = slopeZ * pos.Y + zIntercept;
                            }
                            ((ISelectable)selectedObject).transformation.Position = 
                                    ((ISelectable)selectedObject).transformation.Position + (pos - objectMovingOrig);
                            objectMovingOrig = pos;
                        }
                    }
                    else if (objectMovingAxis == Axis.Z && objectMovingPlane != null)
                    {
                        Vector3 dir = mainCamera.GetCameraRay(MouseState.Position);
                        Vector3? _pos = objectMovingPlane.RayPlaneIntersection(mainCamera.GetPosition(), dir);

                        if (_pos != null)
                        {
                            Vector3 pos = (Vector3)_pos;

                            if (gizmoManager.AbsoluteMoving)
                            {
                                pos.Y = objectMovingOrig.Y;
                            }
                            else
                            {
                                Vector3 searchDir = Vector3.Transform(new Vector3(0, 0, 1), o.transformation.Rotation);
                                if (searchDir.Z == 0)
                                    searchDir.Z = 0.01f;
                                float slopeY = searchDir.Y / searchDir.Z;
                                float yIntercept = 0 - slopeY * 0;

                                float slopeX = searchDir.X / searchDir.Z;
                                float xIntercept = 0 - slopeX * 0;

                                pos.Y = slopeY * pos.Z + yIntercept;
                                pos.X = slopeX * pos.Z + xIntercept;
                            }

                            ((ISelectable)selectedObject).transformation.Position = 
                                    ((ISelectable)selectedObject).transformation.Position + (pos - objectMovingOrig);
                            objectMovingOrig = pos;
                        }
                    }
                }
                else
                {
                    if (objectMovingAxis == Axis.X)
                    {
                        ((ISelectable)selectedObject).transformation.Position = 
                                ((ISelectable)selectedObject).transformation.Position - new Vector3(deltaX / 10, 0, 0);
                    }
                    else if (objectMovingAxis == Axis.Y)
                    {
                        ((ISelectable)selectedObject).transformation.Position = 
                                ((ISelectable)selectedObject).transformation.Position - new Vector3(0, deltaY / 10, 0);
                    }
                    else if (objectMovingAxis == Axis.Z)
                    {
                        ((ISelectable)selectedObject).transformation.Position = 
                                ((ISelectable)selectedObject).transformation.Position + new Vector3(0, 0, deltaX / 10);
                    }
                }

                which[0] = true;
            }
            else if (gizmoManager.gizmoType == GizmoType.Rotate) // Rotating
            {
                if (selectedObject is Object o)
                {
                    if (gizmoManager.PerInstanceMove && gizmoManager.instIndex != -1 && o.GetComponent<BaseMesh>() is InstancedMesh instMesh)
                    {
                        Vector3 rot = Helper.EulerFromQuaternion(instMesh.instancedData[gizmoManager.instIndex].Rotation);
                        if (objectMovingAxis == Axis.X)
                        {
                            instMesh.instancedData[gizmoManager.instIndex].Rotation =
                                Helper.QuaternionFromEuler(rot - new Vector3(deltaX / 10, 0, 0));
                        }
                        else if (objectMovingAxis == Axis.Y)
                        {
                            instMesh.instancedData[gizmoManager.instIndex].Rotation =
                                Helper.QuaternionFromEuler(rot - new Vector3(0, deltaY / 10, 0));
                        }
                        else if (objectMovingAxis == Axis.Z)
                        {
                            instMesh.instancedData[gizmoManager.instIndex].Rotation =
                                Helper.QuaternionFromEuler(rot + new Vector3(0, 0, deltaX / 10));
                        }
                    }
                    else
                    {
                        Vector3 rot = Helper.EulerFromQuaternion(o.transformation.Rotation);
                        if (objectMovingAxis == Axis.X)
                        {
                            o.transformation.Rotation = Helper.QuaternionFromEuler(rot - new Vector3(deltaX / 10, 0, 0));
                        }
                        else if (objectMovingAxis == Axis.Y)
                        {
                            o.transformation.Rotation = Helper.QuaternionFromEuler(rot - new Vector3(0, deltaY / 10, 0));
                        }
                        else if (objectMovingAxis == Axis.Z)
                        {
                            o.transformation.Rotation = Helper.QuaternionFromEuler(rot + new Vector3(0, 0, deltaX / 10));
                        }
                    }

                    which[1] = true;
                }
            }
            else if (gizmoManager.gizmoType == GizmoType.Scale) // Scaling
            {
                if (selectedObject is Object o)
                {
                    if (gizmoManager.PerInstanceMove && gizmoManager.instIndex != -1 && o.GetComponent<BaseMesh>() is InstancedMesh instMesh)
                    {
                        if (objectMovingAxis == Axis.X)
                        {
                            instMesh.instancedData[gizmoManager.instIndex].Scale =
                                instMesh.instancedData[gizmoManager.instIndex].Scale - new Vector3(deltaX / 10, 0, 0);
                        }
                        else if (objectMovingAxis == Axis.Y)
                        {
                            instMesh.instancedData[gizmoManager.instIndex].Scale = 
                                instMesh.instancedData[gizmoManager.instIndex].Scale - new Vector3(0, deltaY / 10, 0);
                        }
                        else if (objectMovingAxis == Axis.Z)
                        {
                            instMesh.instancedData[gizmoManager.instIndex].Scale = 
                                instMesh.instancedData[gizmoManager.instIndex].Scale + new Vector3(0, 0, deltaX / 10);
                        }
                    }
                    else
                    {
                        // TODO 
                        if (objectMovingAxis == Axis.X)
                        {
                            o.transformation.Scale = o.transformation.Scale - new Vector3(deltaX / 10, 0, 0);
                        }
                        else if (objectMovingAxis == Axis.Y)
                        {
                            o.transformation.Scale = o.transformation.Scale - new Vector3(0, deltaY / 10, 0);
                        }
                        else if (objectMovingAxis == Axis.Z)
                        {
                            o.transformation.Scale = o.transformation.Scale + new Vector3(0, 0, deltaX / 10);
                        }
                    }

                    which[2] = true;
                }
            }

            if (selectedObject is Object toCalcO)
            {
                if (toCalcO.GetComponent<BaseMesh>() != null)
                {
                    ((BaseMesh)toCalcO.GetComponent<BaseMesh>()).recalculate = true;
                    ((BaseMesh)toCalcO.GetComponent<BaseMesh>()).RecalculateModelMatrix(which);
                }

                if(toCalcO.GetComponent<Physics>() is Physics physics)
                    physics.UpdatePhysxPositionAndRotation(toCalcO.transformation);
                if (toCalcO.GetComponent<Light>() is Light light)
                    light.RecalculateGizmos();
            }

        }
    }
}
