using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Engine3D
{
    public static class GizmoRaycast
    {
        public static bool[] GetSelectedAxis(List<Object> gizmos, MouseState mouseState, ref Camera camera)
        {
            // Array to store which gizmos are selected
            bool[] selectedGizmos = new bool[gizmos.Count];

            //// Step 1: Convert mouse position to normalized device coordinates
            //Vector2 mousePosition = new Vector2(mouseState.X, mouseState.Y);
            //Vector2 normalizedMousePosition = new Vector2(
            //    (mousePosition.X / camera.screenSize.X) * 2f - 1f,
            //    1f - (mousePosition.Y / camera.screenSize.Y) * 2f
            //);

            //// Step 2: Create a ray from the mouse position in world space
            //Matrix4 inverseViewProjection = Matrix4.Invert(camera.viewMatrix * camera.projectionMatrix);
            //Vector4 nearPoint = new Vector4(normalizedMousePosition.X, normalizedMousePosition.Y, -1, 1);
            //Vector4 farPoint = new Vector4(normalizedMousePosition.X, normalizedMousePosition.Y, 1, 1);

            //Vector4 nearWorld = nearPoint * inverseViewProjection;
            //Vector4 farWorld = farPoint * inverseViewProjection;
            //nearWorld /= nearWorld.W;
            //farWorld /= farWorld.W;

            //Vector3 rayOrigin = new Vector3(nearWorld.X, nearWorld.Y, nearWorld.Z);
            //Vector3 rayDirection = new Vector3(farWorld.X - nearWorld.X, farWorld.Y - nearWorld.Y, farWorld.Z - nearWorld.Z).Normalized();

            Vector3 mousePos = new Vector3(mouseState.Position.X, mouseState.Position.Y, 10.0f);


            // Step 3: Check for intersections with gizmo AABBs
            for (int i = 0; i < gizmos.Count; i++)
            {
                Object gizmo = gizmos[i];
                float dist = float.MaxValue;
                if(gizmo.Bounds.RayIntersects(rayOrigin, rayDirection, out dist))
                {
                    selectedGizmos[i] = true;
                }
            }

            return selectedGizmos;
        }
    }
}
