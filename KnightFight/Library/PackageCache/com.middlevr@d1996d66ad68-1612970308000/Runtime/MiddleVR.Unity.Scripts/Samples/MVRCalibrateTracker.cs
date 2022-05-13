/* MVRCalibrateTracker
 * MiddleVR
 * (c) MiddleVR
 */

using MiddleVR;
using UnityEngine;

[AddComponentMenu("MiddleVR/Samples/Calibrate Tracker")]
public class MVRCalibrateTracker : MonoBehaviour
{
    public string Tracker = "VRPNTracker0.Tracker0";

    #region MonoBehaviour
    private void Update()
    {
        vrTracker tracker = null;
        vrKeyboard keyboard = null;

        var deviceMgr = MVR.DeviceMgr;

        if (deviceMgr != null)
        {
            tracker = deviceMgr.GetTracker(Tracker);
            keyboard = deviceMgr.GetKeyboard();
        }

        if (keyboard != null && keyboard.IsKeyToggled(MVR.VRK_SPACE))
        {
            if (tracker != null)
            {
                tracker.SetNeutralOrientation(tracker.GetOrientation());
            }
        }
    }
    #endregion
}
