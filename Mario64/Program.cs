namespace Mario64
{
    internal class Program
    {
        static void Main(string[] args)
        {
            using(Engine engine = new Engine(500,500))
            {
                engine.Run();
            }
        }
    }
}