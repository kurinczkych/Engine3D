using OpenTK.Windowing.Common.Input;
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
        private void EditorManaging()
        {
            if (!editorData.isGameFullscreen)
                imGuiController.EditorWindow(gameWindowProperty, ref editorData, KeyboardState, MouseState, this);
            else
                imGuiController.FullscreenWindow(gameWindowProperty, ref editorData);

            if (editorData.windowResized)
                ResizedEditorWindow();
            if (Cursor != editorData.mouseType)
                Cursor = editorData.mouseType;
            imGuiController.Render();
        }
    }
}
