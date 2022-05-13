/* MVRRaySelection
 * MiddleVR
 * (c) MiddleVR
 */

using MiddleVR.Unity;
using UnityEngine;

[AddComponentMenu("")]
public class MVRRaySelection : MonoBehaviour
{
    public Color HoverColor = Color.green;

    private MVRSelection _lastSelection = new MVRSelection();
    private MVRSelectionManager _selectionMgr = null;
    private MVRWand _wand = null;

    #region MonoBehaviour
    private void Start()
    {
        _selectionMgr = this.GetComponent<MVRSelectionManager>();
        if (_selectionMgr == null)
        {
            MVRTools.Log(0, "[X] VRRaySelection: impossible to retrieve VRSelectionManager.");
            enabled = false;
            return;
        }

        _wand = this.GetComponent<MVRWand>();
        if (_wand == null)
        {
            MVRTools.Log(0, "[X] VRRaySelection: impossible to retrieve VRWand.");
            enabled = false;
            return;
        }
    }

    private void Update()
    {
        RaySelection();
        RefreshRayMesh();
        SendWandEvents();
    }
    #endregion

    private void SendWandEvents()
    {
        MVRSelection selection = _selectionMgr.GetSelection();

        // Enter/exit events
        if (!MVRSelection.Compare(_lastSelection, selection))
        {
            // Selection changed

            // Exit last
            if (_lastSelection != null)
            {
                _lastSelection.SelectedObject.GetComponent<MVRActor>().MVRWandTouch.Invoke(false);
                _lastSelection.SelectedObject.SendMessage("OnMVRWandExit", _lastSelection, SendMessageOptions.DontRequireReceiver);
            }

            // Enter new
            if (selection != null)
            {
                selection.SelectedObject.GetComponent<MVRActor>().MVRWandTouch.Invoke(true);
                selection.SelectedObject.SendMessage("OnMVRWandEnter", selection, SendMessageOptions.DontRequireReceiver);
            }
        }
        else
        {
            // Hover current
            if (selection != null)
            {
                selection.SelectedObject.SendMessage("OnMVRWandHover", selection, SendMessageOptions.DontRequireReceiver);
            }
        }
    }

    private void RaySelection()
    {
        // Ray picking
        Vector3 rayOrigin = transform.position;
        Vector3 rayDirection = transform.TransformDirection(Vector3.forward);

        MVRSelection newSelection = null;

        foreach (RaycastHit raycastHit in Physics.RaycastAll(rayOrigin, rayDirection, _wand.GetDefaultRayLength()))
        {
            if (newSelection != null && raycastHit.distance >= newSelection.SelectionDistance)
            {
                continue;
            }

            GameObject objectHit = raycastHit.collider.gameObject;

            if (objectHit.name != "VRWand")
            {
                // Ignore GameObject without the VRActor component
                if (objectHit.GetComponent<MVRActor>() == null)
                {
                    continue;
                }

                // Create selection if it does not exist
                if (newSelection == null)
                {
                    newSelection = new MVRSelection();
                }

                newSelection.SourceWand = _wand;
                newSelection.SelectedObject = objectHit;
                newSelection.TextureCoordinate = raycastHit.textureCoord;
                newSelection.SelectionDistance = raycastHit.distance;
                newSelection.SelectionContact = raycastHit.point;
                newSelection.SelectionNormal = raycastHit.normal;
            }
        }

        _lastSelection = _selectionMgr.GetSelection();
        _selectionMgr.SetSelection(newSelection);
    }

    private void RefreshRayMesh()
    {
        MVRSelection selection = _selectionMgr.GetSelection();

        if (selection != null)
        {
            _wand.SetRayColor(HoverColor);
            _wand.SetRayLength(selection.SelectionDistance);
        }
        else
        {
            _wand.SetRayColor(_wand.DefaultRayColor);
            _wand.SetRayLength(_wand.DefaultRayLength);
        }
    }
}
