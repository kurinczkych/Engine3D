using Newtonsoft.Json;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Intrinsics;
using System.Text;
using System.Threading.Tasks;

namespace Engine3D
{
    public class TextGenerator
    {
        public class Root
        {
            public Config config { get; set; }
            public List<Symbol> symbols { get; set; }
        }
        public class Config
        {
            public int _base { get; set; }
            public int bold { get; set; }
            public int charHeight { get; set; }
            public int charSpacing { get; set; }
            public string face { get; set; }
            public int italic { get; set; }
            public int lineSpacing { get; set; }
            public int size { get; set; }
            public int smooth { get; set; }
            public string textureFile { get; set; }
            public int textureHeight { get; set; }
            public int textureWidth { get; set; }
        }
        public class Symbol
        {
            public int height { get; set; }
            public int id { get; set; }
            public char c { get { return (char)id; } }
            public int width { get; set; }
            public int x { get; set; }
            public int xadvance { get; set; }
            public int xoffset { get; set; }
            public int y { get; set; }
            public int yoffset { get; set; }

            public Vertex v1;
            public Vertex v2;
            public Vertex v3;

            public Vertex v4;
            public Vertex v5;
            public Vertex v6;
        }

        private SortedDictionary<char, Symbol> symbols { get; set; }
        private Root? font;

        public TextGenerator()
        {
            string jsonFile = GetFile("font.json");
            if (jsonFile == "")
            {
                Engine.consoleManager.AddLog("Can't find font.json!", LogType.Warning);
                return;
            }

            font = JsonConvert.DeserializeObject<Root>(jsonFile);
            if (font == null)
            {
                Engine.consoleManager.AddLog("Can't deserialize font.json!", LogType.Warning);
                return;
            }

            symbols = new SortedDictionary<char, Symbol>();
            foreach (Symbol s in font.symbols)
            {
                var vertices = Generate(s.c, s);
                s.v1 = vertices[0];
                s.v2 = vertices[1];
                s.v3 = vertices[2];
                s.v4 = vertices[3];
                s.v5 = vertices[4];
                s.v6 = vertices[5];

                symbols.Add(s.c, s);
            }
        }

        private List<Vertex> Generate(char c, Symbol s)
        {
            Vector2 start = Vector2.Zero;

            float width = s.width;
            float height = s.height;

            Color4 color = Color4.White;

            if (font == null)
                throw new Exception("Font dictionary is null!");

            Vector2 topleft = new Vector2(s.x / (float)font.config.textureWidth, s.y / (float)font.config.textureHeight);
            Vector2 topRight = new Vector2((s.x + s.width) / (float)font.config.textureWidth, s.y / (float)font.config.textureHeight);
            Vector2 bottomLeft = new Vector2(s.x / (float)font.config.textureWidth, (s.y + s.height) / (float)font.config.textureHeight);
            Vector2 bottomRight = new Vector2((s.x + s.width) / (float)font.config.textureWidth, (s.y + s.height) / (float)font.config.textureHeight);

            Vertex v1 = new Vertex();
            v1.p.X = start.X;
            v1.p.Y = start.Y;
            v1.t.u = topleft.X;
            v1.t.v = topleft.Y;
            v1.c = color;
            Vertex v3 = new Vertex();
            v3.p.X = start.X;
            v3.p.Y = start.Y + height;
            v3.t.u = bottomLeft.X;
            v3.t.v = bottomLeft.Y;
            v3.c = color;
            Vertex v2 = new Vertex();
            v2.p.X = start.X + width;
            v2.p.Y = start.Y;
            v2.t.u = topRight.X;
            v2.t.v = topRight.Y;
            v2.c = color;

            Vertex v4 = new Vertex();
            v4.p.X = start.X + width;
            v4.p.Y = start.Y;
            v4.t.u = topRight.X;
            v4.t.v = topRight.Y;
            v4.c = color;
            Vertex v6 = new Vertex();
            v6.p.X = start.X;
            v6.p.Y = start.Y + height;
            v6.t.u = bottomLeft.X;
            v6.t.v = bottomLeft.Y;
            v6.c = color;
            Vertex v5 = new Vertex();
            v5.p.X = start.X + width;
            v5.p.Y = start.Y + height;
            v5.t.u = bottomRight.X;
            v5.t.v = bottomRight.Y;
            v5.c = color;

            return new List<Vertex>() { v1, v2, v3, v4, v5, v6 };
        }


        public MeshData GetTriangles(string t)
        {
            MeshData meshData = new MeshData();

            List<Vertex> list = new List<Vertex>();

            Vector2 currentPos = Vector2.Zero;
            foreach(char c in t)
            {
                Symbol s = symbols[c];

                Vertex v1 = s.v1; Vertex v2 = s.v2; Vertex v3 = s.v3;
                Vertex v4 = s.v4; Vertex v5 = s.v5; Vertex v6 = s.v6;
                List<Vertex> sublist = new List<Vertex>() { v1, v2, v3, v4, v5, v6 };

                for (int i = 0; i < sublist.Count; i++)
                {
                    var a = sublist[i];
                    a.p += new Vector3(currentPos.X, currentPos.Y, 0);
                    sublist[i] = a;
                }

                currentPos.X += s.width;
            }

            Dictionary<int, uint> hash = new Dictionary<int, uint>();
            for (int i = 0; i < list.Count; i++)
            {
                int vh = list[i].GetHashCode();
                if (!hash.ContainsKey(vh))
                {
                    meshData.vertices.Add(list[i]);
                    meshData.visibleVerticesData.AddRange(list[i].GetData());
                    meshData.indices.Add((uint)meshData.vertices.Count - 1);
                    hash.Add(vh, (uint)meshData.vertices.Count - 1);
                    meshData.bounds.Enclose(list[i]);
                }
                else
                {
                    meshData.indices.Add(hash[vh]);
                }
            }

            return meshData;
        }

        private string GetFile(string fileName)
        {

            string filepath = FileManager.GetFilePath(fileName, FileType.Fonts.ToString());
            if (filepath == "")
            {
                Engine.consoleManager.AddLog("File '" + fileName + "' was not found!", LogType.Warning);
                return "";
            }

            Stream? stream = FileManager.GetFileStream(filepath);
            if (stream != null)
            {
                using (stream)
                {
                    StreamReader sr = new StreamReader(stream);
                    return sr.ReadToEnd();
                }
            }
            else
            {
                throw new Exception("No file was found");
            }
        }
    }
}
