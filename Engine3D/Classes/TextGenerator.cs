using Newtonsoft.Json;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

            public triangle t1;
            public triangle t2;
        }

        private SortedDictionary<char, Symbol> symbols { get; set; }
        private Root? font;

        public TextGenerator()
        {
            font = JsonConvert.DeserializeObject<Root>(GetFile("font.json"));
            if (font == null)
                throw new Exception("Can't find font.json!");

            symbols = new SortedDictionary<char, Symbol>();
            foreach (Symbol s in font.symbols)
            {
                var tris = Generate(s.c, s);
                s.t1 = tris[0];
                s.t2 = tris[1];

                symbols.Add(s.c, s);
            }
        }

        private List<triangle> Generate(char c, Symbol s)
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

            triangle t1 = new triangle();
            t1.p[0].X = start.X;
            t1.p[0].Y = start.Y;
            t1.t[0].u = topleft.X;
            t1.t[0].v = topleft.Y;
            t1.c[0] = color;
            t1.p[2].X = start.X;
            t1.p[2].Y = start.Y + height;
            t1.t[2].u = bottomLeft.X;
            t1.t[2].v = bottomLeft.Y;
            t1.c[2] = color;
            t1.p[1].X = start.X + width;
            t1.p[1].Y = start.Y;
            t1.t[1].u = topRight.X;
            t1.t[1].v = topRight.Y;
            t1.c[1] = color;

            triangle t2 = new triangle();
            t2.p[0].X = start.X + width;
            t2.p[0].Y = start.Y;
            t2.t[0].u = topRight.X;
            t2.t[0].v = topRight.Y;
            t2.c[0] = color;
            t2.p[2].X = start.X;
            t2.p[2].Y = start.Y + height;
            t2.t[2].u = bottomLeft.X;
            t2.t[2].v = bottomLeft.Y;
            t2.c[2] = color;
            t2.p[1].X = start.X + width;
            t2.p[1].Y = start.Y + height;
            t2.t[1].u = bottomRight.X;
            t2.t[1].v = bottomRight.Y;
            t2.c[1] = color;

            return new List<triangle> { t1, t2 };
        }

        public List<triangle> GetTriangles(string t)
        {
            List<triangle> tris = new List<triangle>();

            Vector2 currentPos = Vector2.Zero;
            foreach(char c in t)
            {
                Symbol s = symbols[c];
                triangle t1 = s.t1.GetCopy();
                triangle t2 = s.t2.GetCopy();
                t1.TransformPosition(new Vector3(currentPos.X, currentPos.Y, 0));
                t2.TransformPosition(new Vector3(currentPos.X, currentPos.Y, 0));

                tris.Add(t1);
                tris.Add(t2);

                currentPos.X += s.width;
            }

            return tris;
        }

        private string GetFile(string fileName)
        {
            // Load the image (using System.Drawing or another library)
            Stream? stream = FileManager.GetFileStream(fileName, "Fonts");
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
