using OpenTK.Windowing.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Engine3D
{
    public partial class Engine
    {
        private void CursorAndGameStateSetting()
        {
            if (editorData.gameRunning == GameState.Stopped &&
               editorData.justSetGameState)
            {
                editorData.manualCursor = false;

                editorData.justSetGameState = false;
            }

            if (editorData.gameRunning == GameState.Running && CursorState != CursorState.Grabbed && !editorData.manualCursor)
            {
                CursorState = CursorState.Grabbed;
            }
            else if (editorData.gameRunning == GameState.Stopped && CursorState != CursorState.Normal)
            {
                CursorState = CursorState.Normal;
            }

            if (KeyboardState.IsKeyReleased(Keys.F5))
            {
                editorData.gameRunning = editorData.gameRunning == GameState.Stopped ? GameState.Running : GameState.Stopped;
            }

            if (KeyboardState.IsKeyReleased(Keys.F2))
            {
                if (CursorState == CursorState.Normal)
                {
                    CursorState = CursorState.Grabbed;
                    editorData.manualCursor = false;
                }
                else if (CursorState == CursorState.Grabbed)
                {
                    CursorState = CursorState.Normal;
                    editorData.manualCursor = true;
                }
            }
        }
    }
}
