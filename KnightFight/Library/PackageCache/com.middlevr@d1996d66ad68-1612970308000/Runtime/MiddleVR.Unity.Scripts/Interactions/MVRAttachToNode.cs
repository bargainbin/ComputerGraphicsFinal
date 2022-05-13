/* MVRAttachToNode
 * MiddleVR
 * (c) MiddleVR
 */

using MiddleVR.Unity;
using UnityEngine;

[AddComponentMenu("MiddleVR/Interactions/Attach to Node")]
public class MVRAttachToNode : MonoBehaviour
{
    public string MVRParentNode = "HandNode";

    public bool DisableGameObjectIfParentNotFound = false;
    public bool KeepLocalPosition = true;
    public bool KeepLocalRotation = true;
    public bool KeepLocalScale = true;

    private bool _attached = false;
    private bool _searched = false;

    #region MonoBehaviour
    private void Update()
    {
        if (!_attached)
        {
            GameObject node = GameObject.Find(MVRParentNode);

            if (MVRParentNode.Length == 0)
            {
                MVRTools.Log(0, "[X] AttachToNode: Please specify a valid MVRParentNode name.");
            }

            if (node != null)
            {
                Vector3 oldPos = transform.localPosition;
                Quaternion oldRot = transform.localRotation;
                Vector3 oldScale = transform.localScale;

                // Setting new parent
                transform.parent = node.transform;

                if (!KeepLocalPosition)
                {
                    transform.localPosition = new Vector3(0, 0, 0);
                }
                else
                {
                    transform.localPosition = oldPos;
                }

                if (!KeepLocalRotation)
                {
                    transform.localRotation = new Quaternion(0, 0, 0, 1);
                }
                else
                {
                    transform.localRotation = oldRot;
                }

                if (!KeepLocalScale)
                {
                    transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
                }
                else
                {
                    transform.localScale = oldScale;
                }


                MVRTools.Log(3, "[+] AttachToNode: " + this.name + " attached to : " + node.name);
                _attached = true;

                // Stop this component now.
                enabled = false;
            }
            else
            {
                if (_searched == false)
                {
                    MVRTools.Log(1, "[~] AttachToNode: Failed to find Game object '" + MVRParentNode + "'");
                    _searched = true;

                    // Stop this component now.
                    enabled = false;

                    if (DisableGameObjectIfParentNotFound)
                    {
                        MVRTools.Log(2, "[ ] Deactivating Game Object '" + gameObject.name + "'.");
                        gameObject.SetActive(false);
                    }
                }
            }
        }
    }
    #endregion
}
