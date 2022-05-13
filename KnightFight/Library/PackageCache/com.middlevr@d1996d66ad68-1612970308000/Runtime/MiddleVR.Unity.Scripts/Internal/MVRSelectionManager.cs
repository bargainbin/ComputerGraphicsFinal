/* MVRSelectionManager
 * MiddleVR
 * (c) MiddleVR
 */

using UnityEngine;

[AddComponentMenu("")]
public class MVRSelectionManager : MonoBehaviour
{
    MVRSelection _selection = null;

    public void SetSelection(MVRSelection iSelection)
    {
        _selection = iSelection;
    }

    public MVRSelection GetSelection()
    {
        return _selection;
    }
}

public class MVRSelection
{
    public GameObject SelectedObject = null;
    public MVRWand SourceWand = null;
    public Vector2 TextureCoordinate = Vector2.zero;
    // Result of raycast hit distance: Distance from origin of wand to point of intersection
    public float SelectionDistance = 0.0f;

    // Needed later? (all RaycastHit structure?)
    public Vector3 SelectionContact = Vector3.zero;
    public Vector3 SelectionNormal = Vector3.zero;

    public static bool Compare(MVRSelection iFirst, MVRSelection iSecond)
    {
        if (iFirst != null && iSecond != null)
        {
            return iFirst.SelectedObject == iSecond.SelectedObject;
        }
        else if (iFirst == null && iSecond == null)
        {
            return true;
        }
        else
        {
            return false;
        }
    }
}
