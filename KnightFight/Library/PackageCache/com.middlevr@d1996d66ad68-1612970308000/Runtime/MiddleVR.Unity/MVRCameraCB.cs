/* MVRCameraCB
 * MiddleVR
 * (c) MiddleVR
 */

using System;
using System.Runtime.InteropServices;
using MiddleVR;
using UnityEngine;

[AddComponentMenu("")]
public class MVRCameraCB : MonoBehaviour
{
    [DllImport("MiddleVR_UnityRendering", CallingConvention = CallingConvention.StdCall)]
    private static extern IntPtr GetCopyCameraCallback();

    private vrCamera _camera;

    protected void OnGUI()
    {
        var camComponent = GetComponent<Camera>();

        if (camComponent.targetTexture != null &&
            Event.current.type == EventType.Repaint)
        {
            if (_camera == null)
            {
                _camera = MVR.DisplayMgr.GetCamera(name);
            }

            int id = -1;

            if (_camera != null)
            {
                id = (int)_camera.GetId();
            }

            GL.IssuePluginEvent(GetCopyCameraCallback(), id);
        }
    }
}
