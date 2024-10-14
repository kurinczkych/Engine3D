using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Windowing.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assimp;
using PuppeteerSharp.Media;

namespace Engine3D
{
    public partial class Engine
    {
        bool movedInsideEditor = false;

        private void EditorMoving(FrameEventArgs args)
        {
            bool moved = false;
            if (IsMouseInGameWindow(MouseState))
            {
                if (Math.Abs(MouseState.ScrollDelta.Y) > 0)
                {
                    character.Position += mainCamera.front * MouseState.ScrollDelta.Y * 2;
                    mainCamera.SetPosition(character.Position);

                    moved = true;
                }

            }

            if(movedInsideEditor || IsMouseInGameWindow(MouseState))
            {
                if(MouseState.IsButtonPressed(MouseButton.Middle) || MouseState.IsButtonPressed(MouseButton.Right))
                {
                    movedInsideEditor = true;
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
                    #region Moving with right click

                    bool characterMoved = false;
                    float flySpeed_ = character.flySpeed;
                    if (KeyboardState.IsKeyDown(Keys.LeftShift))
                    {
                        flySpeed_ *= 2;
                    }

                    if (KeyboardState.IsKeyDown(Keys.W))
                    {
                        character.Velocity += (mainCamera.front * flySpeed_) * (float)args.Time;
                        characterMoved = true;
                    }
                    if (KeyboardState.IsKeyDown(Keys.S))
                    {
                        character.Velocity -= (mainCamera.front * flySpeed_) * (float)args.Time;
                        characterMoved = true;
                    }
                    if (KeyboardState.IsKeyDown(Keys.A))
                    {
                        character.Velocity -= (mainCamera.right * flySpeed_) * (float)args.Time;
                        characterMoved = true;
                    }
                    if (KeyboardState.IsKeyDown(Keys.D))
                    {
                        character.Velocity += (mainCamera.right * flySpeed_) * (float)args.Time;
                        characterMoved = true;
                    }

                    if (characterMoved)
                        character.UpdatePosition(KeyboardState, MouseState, args);
                    #endregion
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

                if (MouseState.IsButtonReleased(MouseButton.Middle) || MouseState.IsButtonReleased(MouseButton.Right))
                {
                    movedInsideEditor = false;
                }
            }

            if (moved)
            {
                ParallelOptions parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = BaseMesh.threadSize };
                Parallel.ForEach(objects, parallelOptions, obj =>
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
