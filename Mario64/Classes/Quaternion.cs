using System;
using System.Collections.Generic;
using System.Linq;
using OpenTK.Mathematics;
using System.Text;
using System.Threading.Tasks;
using System.Data.Common;
using MagicPhysX;

namespace Mario64
{
    public class Quaternion
    {
        public float x;
        public float y;
        public float z;
        public float w;

        public Quaternion(float x, float y, float z, float w)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.w = w;
        }

        public PxQuat ToPxQuat()
        {
            PxQuat quat = new PxQuat();
            quat.x = x;
            quat.y = y;
            quat.z = z;
            quat.w = w;
            return quat;
        }

        public static Quaternion operator *(Quaternion left, Quaternion right)
        {
            float w = left.w * right.w - left.x * right.x - left.y * right.y - left.z * right.z;
            float x = left.w * right.x + left.x * right.w + left.y * right.z - left.z * right.y;
            float y = left.w * right.y - left.x * right.z + left.y * right.w + left.z * right.x;
            float z = left.w * right.z + left.x * right.y - left.y * right.x + left.z * right.w;
            return new Quaternion(x,y,z,w);
        }
    }

    public static class QuatHelper
    {
        public static Quaternion ToQuaternion(Vector3 vec)
        {
            float yawOver2 = MathHelper.DegreesToRadians(vec.Y) / 2.0f;
            float pitchOver2 = MathHelper.DegreesToRadians(vec.X) / 2.0f;
            float rollOver2 = MathHelper.DegreesToRadians(vec.Z) / 2.0f;

            Quaternion qYaw = new Quaternion((float)Math.Cos(yawOver2), 0, 0, (float)Math.Sin(yawOver2));
            Quaternion qPitch = new Quaternion((float)Math.Cos(pitchOver2), (float)Math.Sin(pitchOver2), 0, 0);
            Quaternion qRoll = new Quaternion((float)Math.Cos(rollOver2), 0, (float)Math.Sin(rollOver2), 0);

            Quaternion qYawPitch = qYaw * qPitch;
            Quaternion qFinal = qYawPitch * qRoll;
            return qFinal;
        }
        public static Vector3 ToRotation(Quaternion quat)
        {
            // Compute yaw (y-axis rotation)
            float yaw = (float)Math.Atan2(2.0 * (quat.w * quat.y + quat.z * quat.x), 1.0 - 2.0 * (quat.y * quat.y + quat.z * quat.z));

            // Compute pitch (x-axis rotation)
            float sinp = 2.0f * (quat.w * quat.z - quat.x * quat.y);
            float pitch = 0;
            if (Math.Abs(sinp) >= 1)
                pitch = (float)Math.CopySign(Math.PI / 2, sinp);  // Use 90 degrees if out of range
            else
                pitch = (float)Math.Asin(sinp);

            // Compute roll (z-axis rotation)
            float roll = (float)Math.Atan2(2.0 * (quat.w * quat.x + quat.y * quat.z), 1.0 - 2.0 * (quat.x * quat.x + quat.y * quat.y));

            Vector3 rot = new Vector3(MathHelper.RadiansToDegrees(pitch),
                                      MathHelper.RadiansToDegrees(yaw),
                                      MathHelper.RadiansToDegrees(roll));
            return rot;
        }

        public static Quaternion ToQuaternion(PxQuat pxQuat)
        {
            Quaternion quat = new Quaternion(pxQuat.x, pxQuat.y, pxQuat.z, pxQuat.w);

            return quat;
        }
    }
}
