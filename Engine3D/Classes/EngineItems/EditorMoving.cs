using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Windowing.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine3D
{
    public partial class Engine
    {
        private void EditorMoving(FrameEventArgs args)
        {
            if (IsMouseInGameWindow(MouseState))
            {
                bool moved = false;
                if (Math.Abs(MouseState.ScrollDelta.Y) > 0)
                {
                    character.Position += mainCamera.front * MouseState.ScrollDelta.Y * 2;
                    mainCamera.SetPosition(character.Position);

                    moved = true;
                }
                if (MouseState.IsButtonDown(MouseButton.Right) && !MouseState.IsButtonDown(MouseButton.Middle))
                {
                    if (deltaX != 0 || deltaY != 0)
                    {
                        lastPos = new Vector2(MouseState.X, MouseState.Y);

                        mainCamera.SetYaw(mainCamera.GetYaw() + deltaX * sensitivity * (float)args.Time);
                        mainCamera.SetPitch(mainCamera.GetPitch() - deltaY * sensitivity * (float)args.Time);
                        moved = true;
                    }
                }
                else if (MouseState.IsButtonDown(MouseButton.Middle))
                {
                    if (deltaX != 0 || deltaY != 0)
                    {
                        lastPos = new Vector2(MouseState.X, MouseState.Y);

                        character.Position += (mainCamera.up * deltaY) - (mainCamera.right * deltaX);
                        mainCamera.SetPosition(character.Position);
                        moved = true;
                    }
                }

                if (moved)
                {
                    ParallelOptions parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = BaseMesh.threadSize };
                    Parallel.ForEach(scene.objects, parallelOptions, obj =>
                    {
                        BaseMesh? mesh = (BaseMesh?)obj.GetComponent<BaseMesh>();
                        if (mesh != null)
                        {
                            mesh.recalculate = true;
                        }
                    });
                }
            }
        }
    }
}
