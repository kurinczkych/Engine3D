using System;
using System.Collections.Generic;
using System.Linq;
using OpenTK.Mathematics;
using System.Text;
using System.Threading.Tasks;
using System.Data.Common;
using MagicPhysX;

namespace Engine3D
{
    //public class Quaternion
    //{
    //    public float x;
    //    public float y;
    //    public float z;
    //    public float w;

    //    public Quaternion(float x, float y, float z, float w)
    //    {
    //        this.x = x;
    //        this.y = y;
    //        this.z = z;
    //        this.w = w;
    //    }

    //    public Quaternion()
    //    {
    //        this.x = 0f;
    //        this.y = 0f;
    //        this.z = 0f;
    //        this.w = 1f;
    //    }

    //    public PxQuat ToPxQuat()
    //    {
    //        PxQuat quat = new PxQuat();
    //        quat.x = x;
    //        quat.y = y;
    //        quat.z = z;
    //        quat.w = w;
    //        return quat;
    //    }

    //    public static Quaternion operator *(Quaternion left, Quaternion right)
    //    {
    //        float w = left.w * right.w - left.x * right.x - left.y * right.y - left.z * right.z;
    //        float x = left.w * right.x + left.x * right.w + left.y * right.z - left.z * right.y;
    //        float y = left.w * right.y - left.x * right.z + left.y * right.w + left.z * right.x;
    //        float z = left.w * right.z + left.x * right.y - left.y * right.x + left.z * right.w;
    //        return new Quaternion(x,y,z,w);
    //    }

    //    public static bool operator ==(Quaternion left, Quaternion right)
    //    {
    //        if (left.x == right.x && left.y == right.y && left.z == right.z && left.w == right.w)
    //            return true;
    //        return false;
    //    }

    //    public static bool operator !=(Quaternion left, Quaternion right)
    //    {
    //        if (left.x != right.x || left.y != right.y || left.z != right.z || left.w != right.w)
    //            return true;
    //        return false;
    //    }

    //    public static Quaternion Zero
    //    {
    //        get { return new Quaternion(); }
    //    }
    //}

    public static class QuatHelper
    {
        public static Quaternion PxToOpenTk(PxQuat pxQuat)
        {
            Quaternion quat = new Quaternion(pxQuat.x, pxQuat.y, pxQuat.z, pxQuat.w);

            return quat;
        }

        public static PxQuat OpenTkToPx(Quaternion quat)
        {
            PxQuat pxquat = new PxQuat();
            pxquat.x = quat.X; 
            pxquat.y = quat.Y; 
            pxquat.z = quat.Z; 
            pxquat.w = quat.W;

            return pxquat;
        }
    }
}
