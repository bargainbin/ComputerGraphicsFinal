/* MVRShortcutReload
 * MiddleVR
 * (c) MiddleVR
 */

using MiddleVR;
using UnityEngine;
using UnityEngine.SceneManagement;

[AddComponentMenu("MiddleVR/Samples/Shortcut Reload")]
public class MVRShortcutReload : MonoBehaviour
{
    private void Update()
    {
        vrKeyboard keyboard = MVR.DeviceMgr.GetKeyboard();

        if (keyboard != null &&
            keyboard.IsKeyToggled(MVR.VRK_R) &&
            (keyboard.IsKeyPressed(MVR.VRK_LSHIFT) || keyboard.IsKeyPressed(MVR.VRK_RSHIFT)))
        {
            if ((keyboard.IsKeyPressed(MVR.VRK_LCONTROL) || keyboard.IsKeyPressed(MVR.VRK_RCONTROL)))
            {
                // Reload Simulation (level 0).
                SceneManager.LoadScene(0);
            }
            else
            {
                // Reload last loaded level.
                Scene scene = SceneManager.GetActiveScene();
                SceneManager.LoadScene(scene.name);
            }
        }
    }
}
