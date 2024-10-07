using Engine3D;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Editor3D
{
    public class Editor
    {
        public static void Main(string[] args)
        {
            Vector2i windowSize = new Vector2i(1280, 768);

            Engine engine = new Engine(windowSize.X, windowSize.Y);
            ImGuiController imGuiController = new ImGuiController(windowSize.X, windowSize.Y, ref engine);
            engine.AddOnLoadMethod(imGuiController.OnLoad);
            engine.Run();
        }

        
    }
}
