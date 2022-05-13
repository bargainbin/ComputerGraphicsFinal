/* MVRNavigationWandJoystick
 * MiddleVR
 * (c) MiddleVR
 */

using MiddleVR;
using MiddleVR.Unity;
using UnityEngine;

public class MVRNavigationElastic : MonoBehaviour
{
    public uint WandActionButton = 1;
    public float TranslationSpeed = 1.0f;
    public float RotationSpeed = 45.0f;
    public float DistanceThreshold = 0.025f;
    public float AngleThreshold = 5.0f;
    public bool UseRotationYaw = true;
    public bool Fly = false;
    public GameObject ElasticRepresentationPrefab;

    private MVRWand _wand = null;
    private MVRManagerScript _mvrManager = null;
    private GameObject _rootNode = null;
    private GameObject _referenceNode = null;
    private GameObject _pivotNode = null;
    private GameObject _elasticRepresentationObject;
    private MVRElasticRepresentation _elasticRepresentation;
    private Vector3 _initPos;
    private float _initYaw;
    private InteractionState _state = InteractionState.Inactive;

    private enum InteractionState
    {
        Inactive,
        Running
    }

    #region MonoBehaviour
    private void Start()
    {
        _referenceNode = GameObject.Find("HandNode");
        _pivotNode = GameObject.Find("HeadNode");
        _wand = GameObject.FindObjectOfType<MVRWand>();
        _mvrManager = GameObject.FindObjectOfType<MVRManagerScript>();
        _rootNode = _mvrManager.GetRoot();

        if (_referenceNode == null || _pivotNode == null)
        {
            MVR.Log(VRLogLevel.Info, "[X] VRNavigationWandJoystick: One or several nodes are missing.");
        }
    }

    // Update is called once per frame
    private void Update()
    {
        float dt = (float)MVR.Kernel.GetDeltaTime();

        if (ElasticRepresentationPrefab == null)
        {
            MVRTools.Log("[X] VRInteractionNavigationElastic error: bad elastic prefab reference");
            return;
        }

        switch (_state)
        {
            case InteractionState.Inactive:
                {
                    // Start interaction
                    if (_wand.IsButtonPressed(1))
                    {
                        StartInteraction();
                    }
                    break;
                }

            case InteractionState.Running:
                {
                    // Stopping interaction
                    if (!_wand.IsButtonPressed(1))
                    {
                        GameObject.Destroy(_elasticRepresentationObject);
                        _state = InteractionState.Inactive;
                    }
                    else
                    {
                        // Continue running the interaction
                        UpdateTransform();
                        UpdateElasticRepresentation();
                    }
                    break;
                }
        }
    }
    #endregion

    private void StartInteraction()
    {
        _elasticRepresentationObject = (GameObject)GameObject.Instantiate(ElasticRepresentationPrefab);
        _elasticRepresentationObject.transform.parent = _rootNode.transform;
        _elasticRepresentation = _elasticRepresentationObject.GetComponent<MVRElasticRepresentation>();

        // Initial position in Root Node space
        _initPos = _rootNode.transform.InverseTransformPoint(_referenceNode.transform.position);
        _initYaw = _referenceNode.transform.localRotation.y;

        UpdateElasticRepresentation();

        _state = InteractionState.Running;
    }

    private void UpdateTransform()
    {
        Vector3 newPos = _rootNode.transform.InverseTransformPoint(_referenceNode.transform.position);
        Vector3 direction = newPos - _initPos;
        float startEndDistance = direction.magnitude;

        if (startEndDistance >= DistanceThreshold)
        {
            float deltaDistance = 1.0f + (startEndDistance - DistanceThreshold);
            float speed = TranslationSpeed * deltaDistance * deltaDistance;

            if (!Fly) direction.y = 0.0f;

            _rootNode.transform.Translate(direction * speed * (float)MVR.Kernel.GetDeltaTime(), Space.Self);
        }

        if (UseRotationYaw)
        {
            float deltaYaw = (_referenceNode.transform.localRotation.y - _initYaw) % 360;
            float speed = -RotationSpeed * (float)MVR.Kernel.GetDeltaTime() * deltaYaw;

            Vector3 pivotDelta = _pivotNode.transform.position - _rootNode.transform.position;

            // Rotate around pivot node
            _rootNode.transform.Translate(pivotDelta, Space.Self);
            _rootNode.transform.Rotate(0, speed, 0);
            _rootNode.transform.Translate(-pivotDelta, Space.Self);
        }
    }

    private void UpdateElasticRepresentation()
    {
        if (_elasticRepresentation == null)
        {
            MVR.Log(VRLogLevel.Info, "[X] VRInteractionNavigationElastic error: bad elastic representation reference");
            return;
        }

        Vector3 worldInitPos = _rootNode.transform.TransformPoint(_initPos);
        Vector3 worldPos = _referenceNode.transform.position;

        _elasticRepresentation.SetElasticPoints(worldInitPos, worldPos);
    }
}
