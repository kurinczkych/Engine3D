using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Mario64
{

    public class Capsule
    {
        public Vector3 Position;
        public float Radius;
        public float Height;

        public Capsule(float radius, float height, Vector3 position)
        {
            Radius = radius;
            Height = height;
            Position = position;
        }
        public List<Line> GetWireframe(int segments)
        {
            List<Line> lines = new List<Line>();

            float thirdHeight = Height / 3.0f;

            // Add two circles at the top third and bottom third
            lines.AddRange(GetCircleOfLines(thirdHeight, segments));
            lines.AddRange(GetCircleOfLines(2 * thirdHeight, segments));

            // Add the diagonal 'X' and vertical lines
            for (int i = 0; i < 4; i++)
            {
                float theta = (float)(i * Math.PI / 2); // 0, 90, 180, 270 degrees
                float x = (float)(Radius * Math.Cos(theta));
                float z = (float)(Radius * Math.Sin(theta));

                // Vertical line
                lines.Add(new Line(Position + new Vector3(x, thirdHeight, z), Position + new Vector3(x, 2 * thirdHeight, z)));
            }

            lines.AddRange(GetHalfCircleOfLines(thirdHeight, true, false, 10));
            lines.AddRange(GetHalfCircleOfLines(thirdHeight, false, false, 10));

            lines.AddRange(GetHalfCircleOfLines(thirdHeight * 2, true, true, 10));
            lines.AddRange(GetHalfCircleOfLines(thirdHeight * 2, false, true, 10));

            return lines;
        }

        public List<Line> GetHalfCircleOfLines(float yOffset, bool onXAxis, bool upward, int segments)
        {
            List<Line> halfCircleLines = new List<Line>();

            for (int i = 0; i < segments-1; i++)
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

                halfCircleLines.Add(new Line( Position + new Vector3(x, y, z), Position + new Vector3(x2, y2, z2)));
            }

            return halfCircleLines;
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

        public bool IsCapsuleInCapsule(Capsule capsule, out Vector3 penetration_normal, out float penetration_depth)
        {
            penetration_normal = new Vector3();
            penetration_depth = 0.0f;

            Vector3 aTip = Position + new Vector3(0, Height, 0);
            Vector3 aBase = Position;
            Vector3 bTip = capsule.Position + new Vector3(0, capsule.Height, 0);
            Vector3 bBase = capsule.Position;

            // capsule A:
            Vector3 a_Normal = Vector3.Normalize(aTip - aBase);
            Vector3 a_LineEndOffset = a_Normal * Radius;
            Vector3 a_A = aBase + a_LineEndOffset;
            Vector3 a_B = aTip - a_LineEndOffset;

            // capsule B:
            Vector3 b_Normal = Vector3.Normalize(bTip - bBase);
            Vector3 b_LineEndOffset = b_Normal * capsule.Radius;
            Vector3 b_A = bBase + b_LineEndOffset;
            Vector3 b_B = bTip - b_LineEndOffset;

            // vectors between line endpoints:
            Vector3 v0 = b_A - a_A;
            Vector3 v1 = b_B - a_A;
            Vector3 v2 = b_A - a_B;
            Vector3 v3 = b_B - a_B;

            // squared distances:
            float d0 = Vector3.Dot(v0, v0);
            float d1 = Vector3.Dot(v1, v1);
            float d2 = Vector3.Dot(v2, v2);
            float d3 = Vector3.Dot(v3, v3);

            // select best potential endpoint on capsule A:
            Vector3 bestA;
            if (d2 < d0 || d2 < d1 || d3 < d0 || d3 < d1)
            {
                bestA = a_B;
            }
            else
            {
                bestA = a_A;
            }

            // select point on capsule B line segment nearest to best potential endpoint on A capsule:
            Vector3 bestB = Line.ClosestPointOnLineSegment(b_A, b_B, bestA);

            // now do the same for capsule A segment:
            bestA = Line.ClosestPointOnLineSegment(a_A, a_B, bestB);

            penetration_normal = bestA - bestB;
            float len = penetration_normal.Length;
            penetration_normal /= len;  // normalize
            penetration_depth = Radius + capsule.Radius - len;
            bool intersects = penetration_depth > 0;

            return intersects;
        }
    }
}
