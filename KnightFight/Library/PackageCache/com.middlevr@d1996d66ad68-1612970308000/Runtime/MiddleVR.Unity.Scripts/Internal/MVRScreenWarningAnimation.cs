/* MVRScreenWarningAnimation
 * MiddleVR
 * (c) MiddleVR
 */

using UnityEngine;
using MiddleVR;

[AddComponentMenu("")]
public class MVRScreenWarningAnimation : MonoBehaviour
{
    private GameObject _nodeToWatch;
    private float _nearDistance = 0.01f;

    #region MonoBehaviour
    private void Update()
    {
        if (_nodeToWatch == null)
        {
            return;
        }

        var rendererMaterial = GetComponent<Renderer>().material;

        // Set near distance
        rendererMaterial.SetFloat("_NearDistance", _nearDistance);

        // Halo position
        Vector3 nodePosition = _nodeToWatch.transform.position;
        rendererMaterial.SetVector("_HeadPosition", new Vector4(nodePosition.x, nodePosition.y, nodePosition.z, 1.0f));

        float time = (float)MVR.Kernel.GetTime();

        // Make texture slide
        rendererMaterial.SetTextureOffset("_MainTex", new Vector2(0.0f, 0.08f * time % 1.0f));

        // Make texture blink
        float bright = Mathf.Clamp(1.5f - (time % 1.0f), 0.0f, 1.0f);
        rendererMaterial.SetFloat("_Brightness", bright);
    }
    #endregion

    public void SetNodeToWatch(GameObject iNode)
    {
        _nodeToWatch = iNode;
    }

    public void SetNearDistance(float iNearDistance)
    {
        _nearDistance = iNearDistance;
    }
}
