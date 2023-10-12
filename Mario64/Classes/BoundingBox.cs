using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine3D
{
    public class BoundingBox
    {
        public Vector3 Min { get; set; }
        public Vector3 Max { get; set; }

        public Vector3 Center
        {
            get
            {
                return new Vector3(
                    (Min.X + Max.X) * 0.5f,
                    (Min.Y + Max.Y) * 0.5f,
                    (Min.Z + Max.Z) * 0.5f
                );
            }
        }

        public Vector3 Extents
        {
            get
            {
                return new Vector3(
                    (Max.X - Min.X) * 0.5f,
                    (Max.Y - Min.Y) * 0.5f,
                    (Max.Z - Min.Z) * 0.5f
                );
            }
        }

        public BoundingBox(Vector3 min, Vector3 max)
        {
            Min = min;
            Max = max;
        }

        // Add other useful methods as necessary.
        // For example, a method to check if a point is inside the bounding box:
        public bool Contains(Vector3 point)
        {
            return point.X >= Min.X && point.X <= Max.X &&
                   point.Y >= Min.Y && point.Y <= Max.Y &&
                   point.Z >= Min.Z && point.Z <= Max.Z;
        }

        // ... you might also want methods to expand the bounding box, compute its center, etc.
    }
}
