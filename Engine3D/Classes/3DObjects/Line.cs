using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine3D
{
    public class Line
    {
        public Vector3 Start;
        public Vector3 End;
        public Color4 StartColor;
        public Color4 EndColor;

        public Line(Vector3 start, Vector3 end)
        {
            Start = start;
            End = end;
            StartColor = new Color4(1, 1, 1, 1f);
            EndColor = new Color4(1, 1, 1, 1f);
        }

        public Line(Vector3 start, Vector3 end, Color4 color)
        {
            Start = start;
            End = end;
            StartColor = color;
            EndColor = color;
        }

        public Line(Vector3 start, Vector3 end, Color4 startColor, Color4 endColor)
        {
            Start = start;
            End = end;
            StartColor = startColor;
            EndColor = endColor;
        }

        public static Vector3 ClosestPointOnLineSegment(Vector3 A, Vector3 B, Vector3 Point)
        {
            Vector3 AB = B - A;
            float t = Vector3.Dot(Point - A, AB) / Vector3.Dot(AB, AB);

            return A + Math.Min(Math.Max(t, 0.0f), 1.0f) * AB;
        }
    }
}
