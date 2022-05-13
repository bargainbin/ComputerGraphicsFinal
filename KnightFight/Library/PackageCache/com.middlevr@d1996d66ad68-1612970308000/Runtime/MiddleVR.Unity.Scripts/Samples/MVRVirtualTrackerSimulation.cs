/* MVRVirtualTrackerSimulation
 * MiddleVR
 * (c) MiddleVR
 */

using MiddleVR;
using MiddleVR.Unity;
using UnityEngine;

[AddComponentMenu("MiddleVR/Samples/Virtual Tracker Simulation")]
public class MVRVirtualTrackerSimulation : MonoBehaviour
{
    public string VirtualTrackerName = "MyTracker";

    private bool _isInit = false;

    // The trackers
    private vrTracker _tracker = null;
    private vrAxis _wiimote = null;

    #region MonoBehaviour
    private void Start()
    {
        // Retrieve trackers by name
        _tracker = MVR.DeviceMgr.GetTracker(VirtualTrackerName);

        _wiimote = MVR.DeviceMgr.GetAxis("VRPNAxis0.Axis");

        if (_tracker == null)
        {
            MVRTools.Log("[X] VirtualTrackerMapping: Error : Can't find tracker '" + VirtualTrackerName + "'.");
        }

        if (_wiimote == null)
        {
            MVRTools.Log("[X] Wiimote not found.");
        }

        if (_tracker != null && _wiimote != null)
        {
            _isInit = true;
        }
    }

    protected void Update()
    {
        if (_isInit)
        {
            _tracker.SetX(0.0f);
            _tracker.SetY(0.0f);
            _tracker.SetZ(0.0f);

            float yaw = 0.0f;
            float pitch = MVR.RadToDeg(Mathf.Asin(Mathf.Clamp(_wiimote.GetValue(2), -1, 1)));
            float roll = MVR.RadToDeg(Mathf.Asin(Mathf.Clamp(_wiimote.GetValue(1), -1, 1)));

            _tracker.SetYaw(yaw);
            _tracker.SetPitch(pitch);
            _tracker.SetRoll(roll);
        }
    }
    #endregion
}
