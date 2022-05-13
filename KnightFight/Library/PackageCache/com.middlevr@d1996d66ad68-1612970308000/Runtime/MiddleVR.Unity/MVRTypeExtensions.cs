/* MVRTypeExtensions
 * MiddleVR
 * (c) MiddleVR
 */

using UnityEngine;

namespace MiddleVR.Unity
{
    public static class MVRTypeExtensions
    {
        private static readonly Matrix4x4 s_toMvrMatrix = new Matrix4x4(
            new Vector4(1.0f, 0.0f, 0.0f, 0.0f),
            new Vector4(0.0f, 0.0f, 1.0f, 0.0f),
            new Vector4(0.0f, 1.0f, 0.0f, 0.0f),
            new Vector4(0.0f, 0.0f, 0.0f, 1.0f));

        private static readonly Matrix4x4 s_toMvrMatrixInv = new Matrix4x4(
            new Vector4(1.0f, 0.0f, 0.0f, 0.0f),
            new Vector4(0.0f, 0.0f, 1.0f, 0.0f),
            new Vector4(0.0f, 1.0f, 0.0f, 0.0f),
            new Vector4(0.0f, 0.0f, 0.0f, 1.0f));

        public static Vector3 ToUnity(this vrVec3 iVec)
        {
            return new Vector3(iVec.x, iVec.z, iVec.y);
        }

        public static vrVec3 ToMiddleVR(this Vector3 iVec)
        {
            return new vrVec3(iVec.x, iVec.z, iVec.y);
        }

        public static Quaternion ToUnity(this vrQuat iQuat)
        {
            // About the negation of the w component, see comments in FromUnity.
            return new Quaternion(iQuat.x, iQuat.z, iQuat.y, -iQuat.w);
        }

        public static vrQuat ToMiddleVR(this Quaternion iQuat)
        {
            // Unity is left-handed while MVR is right-handed.
            // Cross-products of vectors with the left hand are the inverse of
            // right-handed ones. Since a product of quaternions contains
            // a cross-product of vectors, this means that rotations are
            // opposed between right-handed and left-handed cases: when turning
            // to the right along x with the left hand, it turns to the left
            // along x with the right hand.
            //
            // To compensate the left to right-hand conversion, we thus 'return'
            // a quaternion from Unity. The operation is possible by two means:
            // 1 - negating all vector components of a quaternion (i.e. x, y, z)
            // *or*
            // 2 - negating the w component of a quaternion.

            return new vrQuat(iQuat.x, iQuat.z, iQuat.y, -iQuat.w);
        }

        public static Matrix4x4 ToUnity(this vrMatrix iM)
        {
            return s_toMvrMatrixInv * iM.RawToUnity() * s_toMvrMatrix;
        }

        public static vrMatrix ToMiddleVR(this Matrix4x4 iM)
        {
            return (s_toMvrMatrix * iM * s_toMvrMatrixInv).RawToMiddleVR();
        }

        public static vrMatrix RawToMiddleVR(this Matrix4x4 iM)
        {
            return new vrMatrix(
                /* mat.m00 = */ iM.m00,
                /* mat.m01 = */ iM.m10,
                /* mat.m02 = */ iM.m20,
                /* mat.m03 = */ iM.m30,
                /* mat.m10 = */ iM.m01,
                /* mat.m11 = */ iM.m11,
                /* mat.m12 = */ iM.m21,
                /* mat.m13 = */ iM.m31,
                /* mat.m20 = */ iM.m02,
                /* mat.m21 = */ iM.m12,
                /* mat.m22 = */ iM.m22,
                /* mat.m23 = */ iM.m32,
                /* mat.m30 = */ iM.m03,
                /* mat.m31 = */ iM.m13,
                /* mat.m32 = */ iM.m23,
                /* mat.m33 = */ iM.m33);
        }

        public static Matrix4x4 RawToUnity(this vrMatrix iM)
        {
            return new Matrix4x4(
                new Vector4(iM.m00, iM.m01, iM.m02, iM.m03),  // column 1
                new Vector4(iM.m10, iM.m11, iM.m12, iM.m13),  // column 2
                new Vector4(iM.m20, iM.m21, iM.m22, iM.m23),  // column 3
                new Vector4(iM.m30, iM.m31, iM.m32, iM.m33)); // column 4 (contains translation)
        }
    }
}
