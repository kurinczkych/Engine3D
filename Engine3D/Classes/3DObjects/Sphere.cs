using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine3D
{
    public class Sphere
    {
        public Vector3 Position;
        public float Radius;

        public Sphere(Vector3 position, float radius)
        {
            Position = position;
            Radius = radius;
        }

        public List<Line> GetWireframe(int segments)
        {
            List<Line> lines = new List<Line>();

            // Add two circles at the top third and bottom third
            lines.AddRange(GetCircleOfLines(Radius/2, segments));

            lines.AddRange(GetHalfCircleOfLines(Radius / 2, true, false, segments));
            lines.AddRange(GetHalfCircleOfLines(Radius / 2, false, false, segments));

            lines.AddRange(GetHalfCircleOfLines(Radius / 2, true, true, segments));
            lines.AddRange(GetHalfCircleOfLines(Radius / 2, false, true, segments));

            return lines;
        }

        private List<Line> GetCircleOfLines(float y, int segments)
        {
            List<Line> circleLines = new List<Line>();

            for (int i = 0; i < segments; i++)
            {
                float theta = (float)(i * 2.0f * Math.PI / segments);
                float nextTheta = (float)((i + 1) * 2.0f * Math.PI / segments);

                float x1 = (float)(Radius * Math.Cos(theta));
                float z1 = (float)(Radius * Math.Sin(theta));

                float x2 = (float)(Radius * Math.Cos(nextTheta));
                float z2 = (float)(Radius * Math.Sin(nextTheta));

                circleLines.Add(new Line(Position + new Vector3(x1, y, z1), Position + new Vector3(x2, y, z2)));
            }

            return circleLines;
        }

        public List<Line> GetHalfCircleOfLines(float yOffset, bool onXAxis, bool upward, int segments)
        {
            List<Line> halfCircleLines = new List<Line>();

            for (int i = 0; i < segments - 1; i++)
            {
                float theta = i * (float)Math.PI / (segments - 1);
                float nextTheta = (i + 1) * (float)Math.PI / (segments - 1);

                float x = (float)Math.Cos(theta) * Radius;
                float y = yOffset + (float)Math.Sin(theta) * Radius * (upward ? 1 : -1);
                float z = 0;

                float x2 = (float)Math.Cos(nextTheta) * Radius;
                float y2 = yOffset + (float)Math.Sin(nextTheta) * Radius * (upward ? 1 : -1);
                float z2 = 0;

                if (!onXAxis)
                {
                    float temp = x;
                    x = z;
                    z = temp;

                    temp = x2;
                    x2 = z2;
                    z2 = temp;
                }

                halfCircleLines.Add(new Line(Position + new Vector3(x, y, z), Position + new Vector3(x2, y2, z2)));
            }

            return halfCircleLines;
        }

    }
}
