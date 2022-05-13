/* MVRCanvas
 * MiddleVR
 * (c) MiddleVR
 */

// Add this script to the Unity Canvas object to interact with it using a Wand.
// For this to work you need to make sure that the Unity EventSystem script
// is in the scene.

using System.Collections.Generic;
using MiddleVR;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(BoxCollider))]
[RequireComponent(typeof(Canvas))]
[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(MVRActor))]
[RequireComponent(typeof(MVRWandRaycaster))]
public class MVRCanvas : MonoBehaviour
{
    private MVRWandRaycaster _wandRayCaster;

    private List<RaycastResult> _previouslyHoveredObjects = new List<RaycastResult>();
    private List<RaycastResult> _previouslyPressedObjects = new List<RaycastResult>();

    #region MonoBehaviour
    private void Start()
    {
        _wandRayCaster = GetComponent<MVRWandRaycaster>();

        var rectTransform = GetComponent<RectTransform>();
        var boxCollider = GetComponent<BoxCollider>();
        boxCollider.size = new Vector3(rectTransform.rect.width, rectTransform.rect.height, .01f);

        var vrActor = GetComponent<MVRActor>();
        vrActor.Grabable = false;
    }

    private void Update()
    {
        List<RaycastResult> hoveredObjects = new List<RaycastResult>();
        _wandRayCaster.Raycast(null, hoveredObjects);

        for (int i = 0, iEnd = hoveredObjects.Count; i < iEnd; ++i)
        {
            int previouslyHoveredObjectNdx = _previouslyHoveredObjects.FindIndex(o => o.gameObject == hoveredObjects[i].gameObject);
            if (previouslyHoveredObjectNdx < 0)
            {
                var pointer = new PointerEventData(null);
                pointer.pointerCurrentRaycast = hoveredObjects[i];
                ExecuteEvents.Execute(hoveredObjects[i].gameObject, pointer, ExecuteEvents.pointerEnterHandler);
            }
            else
            {
                _previouslyHoveredObjects.RemoveAt(previouslyHoveredObjectNdx);
            }

            if (MVR.DeviceMgr.IsWandButtonPressed(0))
            {
                int previouslyPressedObjectNdx = _previouslyPressedObjects.FindIndex(o => o.gameObject == hoveredObjects[i].gameObject);
                if (previouslyPressedObjectNdx < 0)
                {
                    var pointer = new PointerEventData(null);
                    pointer.button = PointerEventData.InputButton.Left;
                    pointer.pointerPress = hoveredObjects[i].gameObject;
                    pointer.pointerCurrentRaycast = hoveredObjects[i];
                    pointer.pointerPressRaycast = hoveredObjects[i];
                    pointer.position = new Vector2(Screen.width / 2.0f, Screen.height / 2.0f);

                    ExecuteEvents.Execute(hoveredObjects[i].gameObject, pointer, ExecuteEvents.pointerDownHandler);

                    _previouslyPressedObjects.Add(hoveredObjects[i]);
                }
            }
            else
            {
                var pointer = new PointerEventData(null);
                pointer.button = PointerEventData.InputButton.Left;
                pointer.pointerPress = hoveredObjects[i].gameObject;
                pointer.pointerCurrentRaycast = hoveredObjects[i];
                pointer.pointerPressRaycast = hoveredObjects[i];
                pointer.position = new Vector2(Screen.width / 2.0f, Screen.height / 2.0f);

                ExecuteEvents.Execute(hoveredObjects[i].gameObject, pointer, ExecuteEvents.pointerUpHandler);

                int previouslyClickedObjectNdx = _previouslyPressedObjects.FindIndex(o => o.gameObject == hoveredObjects[i].gameObject);
                if (previouslyClickedObjectNdx >= 0)
                {
                    ExecuteEvents.Execute(hoveredObjects[i].gameObject, pointer, ExecuteEvents.pointerClickHandler);
                    _previouslyPressedObjects.RemoveAt(previouslyClickedObjectNdx);
                }
            }
        }

        for (int i = 0, iEnd = _previouslyHoveredObjects.Count; i < iEnd; ++i)
        {
            var pointer = new PointerEventData(null);
            ExecuteEvents.Execute(_previouslyHoveredObjects[i].gameObject, pointer, ExecuteEvents.pointerExitHandler);
        }

        _previouslyHoveredObjects = hoveredObjects;
    }
    #endregion
}
