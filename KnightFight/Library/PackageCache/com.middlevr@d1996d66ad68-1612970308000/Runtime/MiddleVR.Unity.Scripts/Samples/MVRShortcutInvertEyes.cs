/* VRShortcutInvertEyes
 * MiddleVR
 * (c) MiddleVR
 */

using MiddleVR;
using UnityEngine;

[AddComponentMenu("MiddleVR/Samples/Shortcut Invert-Eyes")]
public class MVRShortcutInvertEyes : MonoBehaviour
{
    #region MonoBehaviour
    private void Update()
    {
        vrKeyboard keyboard = MVR.DeviceMgr.GetKeyboard();

        // Invert eyes.
        if (keyboard != null &&
            keyboard.IsKeyToggled(MVR.VRK_I) &&
            (keyboard.IsKeyPressed(MVR.VRK_LSHIFT) || keyboard.IsKeyPressed(MVR.VRK_RSHIFT)))
        {
            // For each vrCameraStereo, invert inter eye distance.
            foreach (var cam in MVR.DisplayMgr.Cameras)
            {
                if (cam is vrCameraStereo stereoCam)
                {
                    stereoCam.SetInterEyeDistance(-stereoCam.GetInterEyeDistance());
                }
            }
        }
    }
    #endregion
}
