/* MVRTools
 * MiddleVR
 * (c) MiddleVR
 */

using System;
using JetBrains.Annotations;
using UnityEngine;

namespace MiddleVR.Unity
{
    public partial class MVRTools
    {
        #region Nodes setup
        /// <summary>
        /// Create GameObjects with appropriate behaviours for every vrNode3D in the current configuration
        /// </summary>
        /// <param name="rootGameObject">Root GameObject</param>
        /// <param name="templateCameraObject">Template GameObject/Prefab for cameras</param>
        public static void CreateNodes(
            [NotNull] GameObject rootGameObject,
            GameObject templateCameraObject)
        {
            MVRTools.Log(VRLogLevel.Debug, "[>] Creating " + MVR.DisplayMgr.GetNodesNb() + " nodes.");

            // Clean nodes cache before creating new ones
            MVRNodesMapper.Instance.ClearMappings();

            foreach (var node in MVR.DisplayMgr.GetRootNode().Children)
            {
                CreateNode(node, rootGameObject, templateCameraObject);
            }

            // Force update of all nodes, even those without a tracker
            MVRNodesMapper.Instance.UpdateNodesMiddleVRToUnity();

            MVRTools.Log(VRLogLevel.Debug, "[<] End of node creation.");
        }

        /// <summary>
        /// Recursively create a node and its children under a parent GameObject
        /// </summary>
        /// <param name="node">Instance of vrNode3D</param>
        /// <param name="parentGameObject">Parent GameObject</param>
        /// <param name="templateCameraObject">Template GameObject/Prefab for cameras</param>
        private static void CreateNode(
            [NotNull] vrNode3D node,
            [NotNull] GameObject parentGameObject,
            GameObject templateCameraObject)
        {
            var nodeName = node.GetName();

            //
            // GameObject creation
            //

            GameObject nodeGameObject = null;

            // 2- Create a camera
            if (nodeGameObject == null && node is vrCamera && !(node is vrCameraStereo) && templateCameraObject != null)
            {
                nodeGameObject = CreateCameraInstanceFromTemplate(nodeName, templateCameraObject);
            }

            // 3- Create an empty GameObject
            if (nodeGameObject == null)
            {
                nodeGameObject = new GameObject(nodeName);
            }

            //
            // Setup node according to its type
            //

            MVRTools.Log(VRLogLevel.Debug, $"[+] Creating '{nodeName}' [{node.GetType()}]");

            if (node is vrCamera camera && !(node is vrCameraStereo))
            {
                SetupCamera(camera, nodeGameObject);
            }
            
            //
            // Register node in mapping
            //

            nodeGameObject.transform.parent = parentGameObject.transform;
            MVRNodesMapper.Instance.CreateMapping(nodeGameObject, node);

            //
            // Recurse through children
            //

            foreach (var child in node.Children)
            {
                CreateNode(child, nodeGameObject, templateCameraObject);
            }
        }

        private static GameObject CreateCameraInstanceFromTemplate([NotNull] string name, [NotNull] GameObject templateCamera)
        {
            MVRTools.Log(VRLogLevel.Debug, $"[ ] Unity: Creating camera '{name}' using template camera '{templateCamera.name}'.");

            var cameraGameObject = UnityEngine.Object.Instantiate(templateCamera) as GameObject;
            cameraGameObject.SetActive(true);
            cameraGameObject.name = name;

            return cameraGameObject;
        }

        private static void SetupCamera([NotNull] vrCamera camera, [NotNull] GameObject cameraGameObject)
        {
            // Ensure the GameObject has a Camera component
            var cameraComponent = cameraGameObject.GetComponent<Camera>();
            if (cameraComponent == null)
                cameraComponent = cameraGameObject.AddComponent<Camera>();

            MVRTools.Log(VRLogLevel.Debug, $"[ ] Camera '{cameraGameObject.name}' rendering path: {cameraComponent.actualRenderingPath}.");

            if (MVR.DisplayMgr.GetAntiAliasing() >= 2 && cameraComponent.actualRenderingPath == RenderingPath.DeferredLighting)
            {
                MVRTools.Log(VRLogLevel.Warning, "[~] Anti-Aliasing is enabled but currently does not work in deferred rendering mode.");
            }

            camera.SetFrustumNear(cameraComponent.nearClipPlane);
            camera.SetFrustumFar(cameraComponent.farClipPlane);

            cameraComponent.fieldOfView = camera.GetVerticalFov();


            // By default all cameras are deactivated until the viewports are created
            if (MVR.DisplayMgr.GetViewportsNb() > 0)
            {
                cameraComponent.enabled = false;
            }
            else
            {
                MVRTools.Log(VRLogLevel.Info, "[ ] No viewport defined, so enabling all MiddleVR cameras. You will probably only see the last camera defined.");
            }
        }

        #endregion
    }
} // namespace MiddleVR.Unity
