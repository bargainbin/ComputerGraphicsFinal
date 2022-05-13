/* MVRActor
 * MiddleVR
 * (c) MiddleVR
 */

using UnityEngine;

[AddComponentMenu("MiddleVR/Interactions/Actor")]
public class MVRActor : MonoBehaviour
{
    public bool Grabable = true;
    public bool AddCollider = true;
    public MVRWand.MVRWandTouchEvent MVRWandTouch = new MVRWand.MVRWandTouchEvent();
    public MVRWand.MVRWandButtonEvent MVRWandButton = new MVRWand.MVRWandButtonEvent();

    #region MonoBehaviour
    private void Start()
    {
        if (AddCollider && gameObject.GetComponent<Collider>() == null)
        {
            gameObject.AddComponent<BoxCollider>();
        }
    }
    #endregion
}
