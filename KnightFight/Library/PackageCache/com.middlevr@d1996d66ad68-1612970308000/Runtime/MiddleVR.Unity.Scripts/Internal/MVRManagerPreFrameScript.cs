/* MVRManagerPreFrameScript
 * MiddleVR
 * (c) MiddleVR
 */

using MiddleVR;
using MiddleVR.Unity;
using UnityEngine;

[AddComponentMenu("")]
public class MVRManagerPreFrameScript : MonoBehaviour
{
    #region MonoBehaviour
    void Update()
    {
        MVRTools.Log(4, "[>] Unity: VRManagerPreFrame !");

        if (MVR.Kernel != null) MVR.Kernel.PreFrameUpdate();

        MVRTools.Log(4, "[<] Unity: End of VR EndFrame Update!");
    }
    #endregion
}
