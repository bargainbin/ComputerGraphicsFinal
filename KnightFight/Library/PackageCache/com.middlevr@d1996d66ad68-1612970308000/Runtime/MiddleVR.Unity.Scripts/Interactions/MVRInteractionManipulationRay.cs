/* MVRInteractionManipulationRay
 * MiddleVR
 * (c) MiddleVR
 *
 * Note: Made to be attached to the Wand
 */

using MiddleVR;
using UnityEngine;

public class MVRInteractionManipulationRay : MonoBehaviour
{
    private vrNode3D _handNode = null;
    private MVRWand _wand = null;
    private GameObject _currentSelectedObject = null;
    private GameObject _currentManipulatedObject = null;
    private Transform _currentManipulatedObjectParent = null;
    private bool _manipulatedObjectInitialIsKinematic;
    private MVRRaySelection _selectionScript = null;
    private InteractionState _state = InteractionState.Inactive;

    private enum InteractionState
    {
        Inactive,
        Running
    }

    #region MonoBehaviour
    private void Start()
    {
        _handNode = MVR.DisplayMgr.GetNode("HandNode");
        _wand = this.GetComponent<MVRWand>();
        _selectionScript = this.GetComponent<MVRRaySelection>();

        if (_handNode != null && _wand != null)
        {
            //m_it.SetGrabWandButton(WandGrabButton);
            //m_it.SetManipulatorNode(m_HandNode);
        }
        else
        {
            MVR.Log(VRLogLevel.Info, "[X] VRInteractionManipulationRay: One or several nodes are missing.");
        }
    }

    private void Update()
    {
        // Retrieve selection result
        MVRSelection selection = _wand.GetSelection();

        if (selection == null || !selection.SelectedObject.GetComponent<MVRActor>().Grabable)
        {
            return;
        }

        _currentSelectedObject = selection.SelectedObject;

        switch (_state)
        {
            case InteractionState.Inactive:
                {
                    if (_wand.IsButtonPressed(0))
                    {
                        Grab(_currentSelectedObject);
                        _state = InteractionState.Running;
                    }
                    break;
                }

            case InteractionState.Running:
                {
                    if (!_wand.IsButtonPressed(0))
                    {
                        Ungrab();
                        _state = InteractionState.Inactive;
                    }
                    break;
                }
        }
    }
    #endregion

    private void Grab(GameObject iGrabbedObject)
    {
        // Initialize manipulated node
        _currentManipulatedObject = iGrabbedObject;

        // Pause rigidbody acceleration 
        Rigidbody manipulatedRigidbody = iGrabbedObject.GetComponent<Rigidbody>();
        if (manipulatedRigidbody != null)
        {
            _manipulatedObjectInitialIsKinematic = manipulatedRigidbody.isKinematic;
            manipulatedRigidbody.isKinematic = true;
        }

        _currentManipulatedObjectParent = _currentManipulatedObject.transform.parent;

        _currentManipulatedObject.transform.parent = this.transform;

        // Deactivate selection during the manipulation
        _selectionScript.enabled = false;
    }

    private void Ungrab()
    {
        if (_currentManipulatedObject == null)
        {
            return;
        }

        _currentManipulatedObject.transform.parent = _currentManipulatedObjectParent;

        // Unpause rigidbody acceleration 
        Rigidbody manipulatedRigidbody = _currentManipulatedObject.GetComponent<Rigidbody>();
        if (manipulatedRigidbody != null)
        {
            manipulatedRigidbody.isKinematic = _manipulatedObjectInitialIsKinematic;
        }

        // Reactivate selection after the manipulation
        _selectionScript.enabled = true;

        _currentManipulatedObject = null;
    }
}
