using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine3D
{
    public partial class Engine
    {
        private void CreateAxisPlane(PixelInfo pixel, Object selectedO)
        {
            if (pixel.objectId != 0)
            {
                if (pixel.objectId == 1)
                {
                    objectMovingAxis = Axis.X;
                    if (editorData.gizmoManager.AbsoluteMoving)
                    {
                        if (editorData.gizmoManager.PerInstanceMove && editorData.instIndex == -1 && selectedO.meshType == typeof(InstancedMesh))
                        {
                            Vector3 instPos = ((InstancedMesh)selectedO.GetMesh()).instancedData[editorData.instIndex].Position;
                            objectMovingPlane = new Plane(new Vector3(0, 0, 1), selectedO.Position.Z + instPos.Z);
                        }
                        else
                            objectMovingPlane = new Plane(new Vector3(0, 0, 1), selectedO.Position.Z);
                    }
                    else
                    {
                        if (editorData.gizmoManager.PerInstanceMove && editorData.instIndex == -1 && selectedO.meshType == typeof(InstancedMesh))
                        {
                            Vector3 instPos = ((InstancedMesh)selectedO.GetMesh()).instancedData[editorData.instIndex].Position;
                            Quaternion instRot = ((InstancedMesh)selectedO.GetMesh()).instancedData[editorData.instIndex].Rotation;

                            objectMovingPlane = new Plane(Vector3.Transform(new Vector3(0, 0, 1), selectedO.Rotation * instRot),
                                              selectedO.Position + instPos);
                        }
                        else
                        {
                            objectMovingPlane = new Plane(Vector3.Transform(new Vector3(0, 0, 1), selectedO.Rotation),
                                              selectedO.Position);
                        }
                    }

                    Vector3 dir = character.camera.GetCameraRay(MouseState.Position);
                    Vector3? _pos = objectMovingPlane.RayPlaneIntersection(character.camera.GetPosition(), dir);

                    if (_pos != null)
                    {
                        Vector3 pos = (Vector3)_pos;
                        if (editorData.gizmoManager.AbsoluteMoving)
                            pos.Y = 0;
                        else
                        {
                            Vector3 searchDir = Vector3.Transform(new Vector3(1, 0, 0), selectedO.Rotation);
                            if (searchDir.X == 0)
                                searchDir.X = 0.01f;
                            float slopeY = searchDir.Y / searchDir.X;
                            float yIntercept = 0 - slopeY * 0;

                            float slopeZ = searchDir.Z / searchDir.X;
                            float zIntercept = 0 - slopeZ * 0;

                            pos.Y = slopeY * pos.X + yIntercept;
                            pos.Z = slopeZ * pos.X + zIntercept;
                        }
                        objectMovingOrig = pos;
                    }
                }
                else if (pixel.objectId == 2)
                {
                    objectMovingAxis = Axis.Y;
                    if (editorData.gizmoManager.AbsoluteMoving)
                    {
                        objectMovingPlane = new Plane(new Vector3(0, 0, 1), selectedO.Position.Z);
                    }
                    else
                    {
                        objectMovingPlane = new Plane(Vector3.Transform(new Vector3(0, 0, 1), selectedO.Rotation),
                                          selectedO.Position);
                    }

                    Vector3 dir = character.camera.GetCameraRay(MouseState.Position);
                    Vector3? _pos = objectMovingPlane.RayPlaneIntersection(character.camera.GetPosition(), dir);

                    if (_pos != null)
                    {
                        Vector3 pos = (Vector3)_pos;
                        if (editorData.gizmoManager.AbsoluteMoving)
                            pos.X = 0;
                        else
                        {
                            Vector3 searchDir = Vector3.Transform(new Vector3(0, 1, 0), selectedO.Rotation);
                            if (searchDir.Y == 0)
                                searchDir.Y = 0.01f;
                            float slopeX = searchDir.X / searchDir.Y;
                            float xIntercept = 0 - slopeX * 0;

                            float slopeZ = searchDir.Z / searchDir.Y;
                            float zIntercept = 0 - slopeZ * 0;

                            pos.X = slopeX * pos.Y + xIntercept;
                            pos.Z = slopeZ * pos.Y + zIntercept;
                        }

                        objectMovingOrig = pos;
                    }
                }
                else if (pixel.objectId == 3)
                {
                    objectMovingAxis = Axis.Z;
                    if (editorData.gizmoManager.AbsoluteMoving)
                    {
                        objectMovingPlane = new Plane(new Vector3(1, 0, 0), selectedO.Position.X);
                    }
                    else
                    {
                        objectMovingPlane = new Plane(Vector3.Transform(new Vector3(1, 0, 0), selectedO.Rotation),
                                          selectedO.Position);
                    }

                    Vector3 dir = character.camera.GetCameraRay(MouseState.Position);
                    Vector3? _pos = objectMovingPlane.RayPlaneIntersection(character.camera.GetPosition(), dir);

                    if (_pos != null)
                    {
                        Vector3 pos = (Vector3)_pos;
                        if (editorData.gizmoManager.AbsoluteMoving)
                            pos.Y = 0;
                        else
                        {
                            Vector3 searchDir = Vector3.Transform(new Vector3(0, 0, 1), selectedO.Rotation);
                            if (searchDir.Z == 0)
                                searchDir.Z = 0.01f;
                            float slopeY = searchDir.Y / searchDir.Z;
                            float yIntercept = 0 - slopeY * 0;

                            float slopeX = searchDir.X / searchDir.Z;
                            float xIntercept = 0 - slopeX * 0;

                            pos.Y = slopeY * pos.Z + yIntercept;
                            pos.X = slopeX * pos.Z + xIntercept;
                        }
                        objectMovingOrig = pos;
                    }
                }
            }
        }
    }
}
