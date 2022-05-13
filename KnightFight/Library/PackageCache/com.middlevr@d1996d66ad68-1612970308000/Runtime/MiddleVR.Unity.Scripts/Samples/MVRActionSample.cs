/* MyMVRAction
 * MiddleVR
 * (c) MiddleVR
 */

using UnityEngine;

public class MVRActionSample : MonoBehaviour
{
    public void MyWandButtonReaction(int iButton, bool iPressed)
    {
        if (iButton == 0 && iPressed == true)
        {
            Debug.Log("Button 0 pressed!");
        }
    }

    public void MyWandTouchReaction(bool iTouched)
    {
        if (iTouched)
        {
            Debug.Log("Started being touched by wand.");
        }
        else
        {
            Debug.Log("Stopped being touched by wand.");
        }
    }

    protected void OnMVRWandEnter(MVRSelection iSelection)
    {
        Debug.Log(name + ": OnMVRWandEnter.");
    }

    protected void OnMVRWandHover(MVRSelection iSelection)
    {
        //Debug.Log(name + ": OnMVRWandHover.");
    }

    protected void OnMVRWandExit(MVRSelection iSelection)
    {
        Debug.Log(name + ": OnMVRWandExit.");
    }

    protected void OnMVRWandButtonPressed(MVRSelection iSelection)
    {
        Debug.Log(name + ": OnMVRWandButtonPressed.");
    }

    protected void OnMVRWandButtonReleased(MVRSelection iSelection)
    {
        Debug.Log(name + ": OnMVRWandButtonReleased.");
    }
}
