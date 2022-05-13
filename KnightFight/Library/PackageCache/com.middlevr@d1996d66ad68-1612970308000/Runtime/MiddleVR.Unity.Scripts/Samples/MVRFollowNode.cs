/* MVRFollowNode
 * MiddleVR
 * (c) MiddleVR
 */

using UnityEngine;

[AddComponentMenu("MiddleVR/Samples/Follow Node")]
public class MVRFollowNode : MonoBehaviour
{
    public string VRNodeName = "HeadNode";

    private GameObject _node = null;

    #region MonoBehaviour
    private void Update()
    {
        if (_node == null)
        {
            _node = GameObject.Find(VRNodeName);
        }

        if (_node != null)
        {
            transform.position = _node.transform.position;
            transform.rotation = _node.transform.rotation;
        }
    }
    #endregion
}
