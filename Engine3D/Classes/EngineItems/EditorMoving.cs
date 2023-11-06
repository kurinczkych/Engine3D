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
        private void EditorMoving(float deltaX, float deltaY, FrameEventArgs args)
        {
            if (IsMouseInGameWindow(MouseState))
            {
                bool moved = false;
                if (Math.Abs(MouseState.ScrollDelta.Y) > 0)
                {
                    character.Position += character.camera.front * MouseState.ScrollDelta.Y * 2;
                    character.camera.SetPosition(character.Position);

                    moved = true;
                }
                if (MouseState.IsButtonDown(MouseButton.Right) && !MouseState.IsButtonDown(MouseButton.Middle))
                {
                    if (deltaX != 0 || deltaY != 0)
                    {
                        lastPos = new Vector2(MouseState.X, MouseState.Y);

                        character.camera.SetYaw(character.camera.GetYaw() + deltaX * sensitivity * (float)args.Time);
                        character.camera.SetPitch(character.camera.GetPitch() - deltaY * sensitivity * (float)args.Time);
                        moved = true;
                    }
                }
                else if (MouseState.IsButtonDown(MouseButton.Middle))
                {
                    if (deltaX != 0 || deltaY != 0)
                    {
                        lastPos = new Vector2(MouseState.X, MouseState.Y);

                        character.Position += (character.camera.up * deltaY) - (character.camera.right * deltaX);
                        character.camera.SetPosition(character.Position);
                        moved = true;
                    }
                }

                if (moved)
                {
                    ParallelOptions parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = BaseMesh.threadSize };
                    Parallel.ForEach(objects, parallelOptions, obj =>
                    {
                        obj.GetMesh().recalculate = true;
                    });
                }
            }
        }
    }
}
