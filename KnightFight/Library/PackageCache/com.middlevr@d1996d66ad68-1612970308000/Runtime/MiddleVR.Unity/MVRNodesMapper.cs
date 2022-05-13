/* MVRNodesMapper
 * MiddleVR
 * (c) MiddleVR
 */

using System.Collections.Generic;
using UnityEngine;

namespace MiddleVR.Unity
{
    public class MVRNodesMapper
    {
        public static MVRNodesMapper Instance { get; private set; } = null;
        public static bool HasInstance => Instance != null;

        public static MVRNodesMapper CreateInstance()
        {
            if (Instance == null)
            {
                Instance = new MVRNodesMapper();
            }

            return Instance;
        }

        public static void DestroyInstance()
        {
            if (Instance != null)
            {
                Instance.ClearMappings();
                Instance = null;
            }
        }

        private class MappingData
        {
            public GameObject UnityNode { get; set; } = null;
            public vrNode3D MiddleVRNode { get; set; } = null;

            public override string ToString() =>
                $"The MiddleVR vrNode3D '{UnityNode?.name ?? "null"}' gives its positioning to the Unity GameObject '{MiddleVRNode?.GetName() ?? "null"}'.";
        }

        private readonly Dictionary<uint, MappingData> _mappingPerMVRNodeId = new Dictionary<uint, MappingData>();
        private readonly Dictionary<GameObject, MappingData> _mappingPerGO = new Dictionary<GameObject, MappingData>();

        /// @brief Remove every mapping.
        ///
        /// Every vrNode3D that was created automatically will be destroyed.
        public void ClearMappings()
        {
            // Clear cache
            _mappingPerMVRNodeId.Clear();
            _mappingPerGO.Clear();
        }

        /// \brief Tell whether mapping for a Unity node exists.
        public bool HasMappingFor(GameObject iUnityNode)
        {
            if (iUnityNode != null)
            {
                return _mappingPerGO.ContainsKey(iUnityNode);
            }
            else
            {
                return false;
            }
        }

        /// @brief Tell whether mapping for a MiddleVR node exists.
        public bool HasMappingFor(vrNode3D iMvrNode)
        {
            if (iMvrNode != null)
            {
                return _mappingPerMVRNodeId.ContainsKey(iMvrNode.GetId());
            }
            else
            {
                return false;
            }
        }

        /// @brief Tell whether mapping between a Unity node and a MiddleVR node exists.
        public bool AreMappedTogether(GameObject iUnityNode, vrNode3D iMvrNode)
        {
            var mvrNode = GetNode(iUnityNode);
            return mvrNode != null && mvrNode.GetId() == iMvrNode.GetId();
        }

        /// @brief Retrieve the vrNode3D mapped to a GameObject.
        public vrNode3D GetNode(GameObject iUnityNode)
        {
            if (iUnityNode != null)
            {
                MappingData mapping = null;
                if (_mappingPerGO.TryGetValue(iUnityNode, out mapping))
                {
                    return mapping.MiddleVRNode;
                }
            }

            return null;
        }

        /// @brief Retrieve the GameObject mapped to a vrNode3D.
        public GameObject GetNode(vrNode3D iMvrNode)
        {
            if (iMvrNode != null)
            {
                MappingData mapping = null;
                if (_mappingPerMVRNodeId.TryGetValue(iMvrNode.GetId(), out mapping))
                {
                    return mapping.UnityNode;
                }
            }

            return null;
        }

        public void UpdateNodesMiddleVRToUnity()
        {
            MVRTools.Log(VRLogLevel.Debug1, "[>] Starting to update Unity nodes from MiddleVR nodes.");

            UpdateChildrenNodesMiddleVRToUnity(MVR.DisplayMgr.GetRootNode());

            MVRTools.Log(VRLogLevel.Debug1, "[<] Finished MiddleVR to Unity update.");
        }

        public void CreateMapping(GameObject iUnityNode, vrNode3D iMvrNode)
        {
            var mapping = new MappingData
            {
                UnityNode = iUnityNode,
                MiddleVRNode = iMvrNode
            };

            // Store the mapping
            _mappingPerMVRNodeId[iMvrNode.GetId()] = mapping;
            _mappingPerGO[iUnityNode] = mapping;

            MVRTools.Log(VRLogLevel.Debug, $"[ ] Created mapping for MVR node '{iMvrNode.GetName()}'.");
        }

        private void UpdateChildrenNodesMiddleVRToUnity(vrNode3D iMvrNode)
        {
            if (iMvrNode == null)
                return;

            foreach (var mvrChild in iMvrNode.Children)
            {
                if (_mappingPerMVRNodeId.TryGetValue(mvrChild.GetId(), out var mapping))
                {
                    var unityNode = mapping.UnityNode;
                    var transform = unityNode.transform;

                    var m = mvrChild.GetMatrixLocal().ToUnity();
                    transform.localPosition = m.GetColumn(3);
                    transform.localRotation = m.rotation;
                    transform.localScale = m.lossyScale;
                }

                UpdateChildrenNodesMiddleVRToUnity(mvrChild);
            }
        }
    }
} // namespace MiddleVR.Unity
