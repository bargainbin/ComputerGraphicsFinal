/* MVRVirtualTrackerMapping
 * MiddleVR
 * (c) MiddleVR
 */

using MiddleVR;
using MiddleVR.Unity;
using UnityEngine;

[AddComponentMenu("MiddleVR/Samples/Virtual Tracker Mapping")]
public class MVRVirtualTrackerMapping : MonoBehaviour
{
    public string SourceTrackerName = "VRPNTracker0.Tracker0";
    public string DestinationVirtualTrackerName = "MyTracker";

    public bool UsePositionX = true;
    public bool UsePositionY = true;
    public bool UsePositionZ = true;

    public bool UsePositionScale = false;
    public float PositionScaleValue = 1.0f;

    public bool UseYaw = true;
    public bool UsePitch = true;
    public bool UseRoll = true;

    private bool _isInit = false;

    // The trackers
    private vrTracker _sourceTracker = null;
    private vrTracker _destinationVirtualTracker = null;

    #region MonoBehaviour
    private void Start()
    {
        // Retrieve trackers by name
        _sourceTracker = MVR.DeviceMgr.GetTracker(SourceTrackerName);
        _destinationVirtualTracker = MVR.DeviceMgr.GetTracker(DestinationVirtualTrackerName);

        if (_sourceTracker == null)
        {
            MVRTools.Log("[X] VirtualTrackerMapping: Error : Can't find tracker '"
                + SourceTrackerName + "'.");
        }
        if (_destinationVirtualTracker == null)
        {
            MVRTools.Log("[X] VirtualTrackerMapping: Error : Can't find tracker '" +
                DestinationVirtualTrackerName + "'.");
        }

        if (_sourceTracker != null && _destinationVirtualTracker != null)
        {
            _isInit = true;
        }
    }

    private void Update()
    {
        if (_isInit)
        {
            float scale = 1.0f;

            if (UsePositionScale)
            {
                scale = PositionScaleValue;
            }

            // Position
            //
            // Show how coordinates values can be changed when feeding a virtual tracker.
            //
            if (UsePositionX)
            {
                _destinationVirtualTracker.SetX(scale * _sourceTracker.GetX());
            }
            if (UsePositionY)
            {
                _destinationVirtualTracker.SetZ(scale * _sourceTracker.GetZ());
            }
            if (UsePositionZ)
            {
                _destinationVirtualTracker.SetY(scale * _sourceTracker.GetY());
            }

            // Orientation
            //
            // Note that it is suggested to use quaternions if you do not need
            // to decompose a rotation.
            //
            if (UseYaw)
            {
                _destinationVirtualTracker.SetYaw(_sourceTracker.GetYaw());
            }
            if (UsePitch)
            {
                _destinationVirtualTracker.SetPitch(_sourceTracker.GetPitch());
            }
            if (UseRoll)
            {
                _destinationVirtualTracker.SetRoll(_sourceTracker.GetRoll());
            }
        }
    }
    #endregion
}
