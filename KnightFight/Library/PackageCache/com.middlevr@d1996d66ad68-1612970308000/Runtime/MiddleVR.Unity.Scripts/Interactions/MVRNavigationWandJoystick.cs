/* MVRNavigationWandJoystick
 * MiddleVR
 * (c) MiddleVR
 */

using MiddleVR;
using UnityEngine;

public class MVRNavigationWandJoystick : MonoBehaviour
{
    public float TranslationSpeed = 1.0f;
    public float RotationSpeed = 45.0f;
    public bool Fly = false;

    private MVRWand _wand = null;
    private MVRManagerScript _mvrManager = null;
    private GameObject _rootNode = null;
    private GameObject _directionReferenceNode = null;
    private GameObject _pivotNode = null;

    #region MonoBehaviour
    private void Start()
    {
        _directionReferenceNode = GameObject.Find("HandNode");
        _pivotNode = GameObject.Find("HeadNode");
        _wand = GameObject.FindObjectOfType<MVRWand>();
        _mvrManager = GameObject.FindObjectOfType<MVRManagerScript>();
        _rootNode = _mvrManager.GetRoot();

        if (_directionReferenceNode == null || _pivotNode == null)
        {
            MVR.Log(VRLogLevel.Info, "[X] VRNavigationWandJoystick: One or several nodes are missing.");
        }
    }

    private void Update()
    {
        float dt = (float)MVR.Kernel.GetDeltaTime();
        float x = _wand.GetHorizontalAxisValue();
        float y = _wand.GetVerticalAxisValue();

        // Translation
        if (Mathf.Abs(y) > 0.01f)
        {
            float speed = y * TranslationSpeed * dt;
            Vector3 front = _directionReferenceNode.transform.forward;

            if (!Fly)
            {
                front.y = 0.0f;
            }

            _rootNode.transform.Translate(front * speed, Space.World);
        }

        // Rotation
        if (Mathf.Abs(x) > 0.01f)
        {
            Vector3 pivotDelta = _pivotNode.transform.position - _rootNode.transform.position;
            float speed = x * RotationSpeed * dt;

            // Rotate around pivot node
            _rootNode.transform.Translate(pivotDelta, Space.Self);
            _rootNode.transform.Rotate(0, speed, 0);
            _rootNode.transform.Translate(-pivotDelta, Space.Self);
        }
    }
    #endregion
}
