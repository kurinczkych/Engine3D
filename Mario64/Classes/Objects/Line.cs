using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mario64
{
    public class Line
    {
        public Vector3 Start;
        public Vector3 End;
        public Color4 Color;

        public Line(Vector3 start, Vector3 end)
        {
            Start = start;
            End = end;
        }

        public static Vector3 ClosestPointOnLineSegment(Vector3 A, Vector3 B, Vector3 Point)
        {
            Vector3 AB = B - A;
            float t = Vector3.Dot(Point - A, AB) / Vector3.Dot(AB, AB);

            return A + Math.Min(Math.Max(t, 0.0f), 1.0f) * AB;
        }
    }
}
