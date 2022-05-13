/* MVRManagerPostFrameScript
 * MiddleVR
 * (c) MiddleVR
 */

using System.Collections;
using MiddleVR;
using MiddleVR.Unity;
using UnityEngine;

[AddComponentMenu("")]
public class MVRManagerPostFrameScript : MonoBehaviour
{
    private bool _loggedNoKeyboard = false;
    private bool _continuePostFrameUpdate = false;

    #region MonoBehaviour
    private void Start()
    {
        MVRTools.Log(VRLogLevel.Debug1, "[ ] Unity: StartCoroutine PostFrameUpdate");

        _continuePostFrameUpdate = true;
        StartCoroutine(PostFrameUpdate());
    }

    private void Update()
    {
        MVRTools.Log(VRLogLevel.Debug1, "[>] Unity: VR EndFrame Update!");

        MVR.Kernel.EndFrameUpdate();

        Cluster.DispatchMessages();

        MVRTools.Log(VRLogLevel.Debug1, "[<] Unity: End of VR EndFrame Update!");
    }

    private void OnApplicationQuit()
    {
        _continuePostFrameUpdate = false;
    }
    #endregion

    #region VRManagerPostFrame
    // This coroutine when started, will wait for every end of frame and then
    // call the PostFrameUpdate of the vrKernel.
    private IEnumerator PostFrameUpdate()
    {
        while (_continuePostFrameUpdate)
        {
            yield return new WaitForEndOfFrame();

            var mgr = GetComponent<MVRManagerScript>();

            MVRTools.Log(VRLogLevel.Debug1, "[>] Unity: Start of VR PostFrameUpdate.");

            if (MVR.DeviceMgr != null)
            {
                var keyboard = MVR.DeviceMgr.GetKeyboard();
                if (keyboard != null)
                {
                    if (mgr != null && mgr.advancedProperties.QuitOnEsc && (keyboard.IsKeyPressed((uint)MVR.VRK_ESCAPE)))
                    {
                        mgr.QuitApplication();
                    }
                }
                else
                {
                    if (!_loggedNoKeyboard)
                    {
                        MVRTools.Log(VRLogLevel.Error, "[X] No VR keyboard.");
                        _loggedNoKeyboard = true;
                    }
                }
            }

            if (MVR.Kernel != null)
            {
                MVR.Kernel.PostFrameUpdate();
            }

            MVRTools.Log(VRLogLevel.Debug1, "[<] Unity: End of VR PostFrameUpdate.");

            if (MVR.Kernel != null && MVR.Kernel.GetFrame() == 2 && !Application.isEditor)
            {
                MVRTools.Log(VRLogLevel.Info, "[ ] If the application is stuck here and you're using Quad-buffer active stereoscopy, make sure that in the Player Settings of Unity, the option 'Run in Background' is checked.");
            }
        }
    }
    #endregion
}
