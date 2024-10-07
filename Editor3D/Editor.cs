using Engine3D;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Editor3D
{
    public class Editor
    {
        public ImGuiController imGuiController;
        public Vector2i windowSize = new Vector2i(1280, 768);

        public void Main(string[] args)
        {
            Engine engine = new Engine(windowSize.X, windowSize.Y); 
            engine.Run();

            imGuiController = new ImGuiController(windowSize.X, windowSize.Y, ref editorData);
        }

        public static void Update()
        {
            if (!editorData.isGameFullscreen)
                imGuiController.EditorWindow(ref editorData, KeyboardState, MouseState, this);
            else
                imGuiController.FullscreenWindow(ref editorData);

            if (editorData.windowResized)
                ResizedEditorWindow();
            if (Cursor != editorData.mouseType)
                Cursor = editorData.mouseType;
            imGuiController.Render();
        }
    }
}
