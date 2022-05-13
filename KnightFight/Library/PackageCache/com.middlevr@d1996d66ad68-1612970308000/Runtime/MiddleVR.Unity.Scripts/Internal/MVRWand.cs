/* MVRWand
 * MiddleVR
 * (c) MiddleVR
 */

using MiddleVR;
using MiddleVR.Unity;
using UnityEngine;
using UnityEngine.Events;

[AddComponentMenu("")]
public class MVRWand : MonoBehaviour
{
    public float DefaultRayLength = 10;
    public Color DefaultRayColor = Color.white;

    [System.Serializable]
    public class MVRWandButtonEvent : UnityEvent<int, bool> { }

    [System.Serializable]
    public class MVRWandTouchEvent : UnityEvent<bool> { }

    private vrWand _wandDevice = null;
    private GameObject _wandCube = null;
    private GameObject _wandRay = null;
    private Renderer _wandRayRenderer = null;
    private float _rayLength = 0;
    private MVRSelectionManager _selectionMgr = null;

    #region MonoBehaviour
    private void Start()
    {
        _selectionMgr = this.GetComponent<MVRSelectionManager>();
        if (_selectionMgr == null)
        {
            MVRTools.Log(0, "[X] VRWand: impossible to retrieve VRSelectionManager.");
            enabled = false;
            return;
        }

        _wandDevice = MVR.DeviceMgr.GetWand("Wand0");
        if (_wandDevice == null)
        {
            MVRTools.Log(0, "[X] VRWand: impossible to retrieve vrWand device.");
        }

        FindWandGeometry();

        SetRayLength(DefaultRayLength);
    }

    private void Update()
    {
        MVRSelection selection = _selectionMgr.GetSelection();

        // Send action if selection not null
        if (selection != null && selection.SelectedObject != null)
        {
            for (int i = 0; i < MVR.DeviceMgr.GetWandButtons().GetButtonsNb(); i++)
            {
                if (MVR.DeviceMgr.IsWandButtonToggled((uint)i))
                {
                    selection.SelectedObject.GetComponent<MVRActor>().MVRWandButton.Invoke(i, true);
                    selection.SelectedObject.SendMessage("OnMVRWandButtonPressed", selection, SendMessageOptions.DontRequireReceiver);
                }
                if (MVR.DeviceMgr.IsWandButtonToggled((uint)i, false))
                {
                    selection.SelectedObject.GetComponent<MVRActor>().MVRWandButton.Invoke(i, false);
                    selection.SelectedObject.SendMessage("OnMVRWandButtonReleased", selection, SendMessageOptions.DontRequireReceiver);
                }
            }
        }
    }
    #endregion

    private void FindWandGeometry()
    {
        _wandCube = transform.Find("MVRWandCube").gameObject;
        _wandRay = transform.Find("MVRWandRay").gameObject;
        _wandRayRenderer = transform.Find("MVRWandRay/MVRRayMesh").GetComponent<Renderer>();
    }

    public void Show(bool iValue)
    {
        if (_wandRay == null || _wandRayRenderer == null || _wandCube == null)
        {
            FindWandGeometry();
        }

        if (_wandRayRenderer != null && _wandCube != null)
        {
            _wandRayRenderer.enabled = iValue;
            _wandCube.GetComponent<Renderer>().enabled = iValue;
        }
    }

    public void ShowRay(bool iValue)
    {
        _wandRayRenderer.enabled = iValue;
    }

    public bool IsRayVisible()
    {
        return _wandRayRenderer.enabled;
    }

    public MVRSelection GetSelection()
    {
        // Find Selection Mgr
        MVRSelectionManager selectionManager = this.GetComponent<MVRSelectionManager>();

        // Return selection
        return selectionManager.GetSelection();
    }

    public float GetDefaultRayLength()
    {
        return DefaultRayLength;
    }

    private float GetRayLength()
    {
        return _rayLength;
    }

    public void SetRayLength(float iLength)
    {
        _rayLength = iLength;
        _wandRay.transform.localScale = new Vector3(1.0f, 1.0f, _rayLength);
    }

    public void SetRayColor(Color iColor)
    {
        _wandRayRenderer.material.SetColor("_EmissionColor", iColor);
    }

    public float GetVerticalAxisValue()
    {
        return _wandDevice.GetVerticalAxisValue();
    }

    public float GetHorizontalAxisValue()
    {
        return _wandDevice.GetHorizontalAxisValue();
    }

    public bool IsButtonPressed(uint iButton)
    {
        return _wandDevice.IsButtonPressed(iButton);
    }

    public bool IsButtonToggled(uint iButton)
    {
        return _wandDevice.IsButtonToggled(iButton, true);
    }

    public bool IsButtonToggled(uint iButton, bool iPressed)
    {
        return _wandDevice.IsButtonToggled(iButton, iPressed);
    }
}
