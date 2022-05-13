/* MVRManagerScript
 * MiddleVR
 * (c) MiddleVR
 */

using System.Collections.Generic;
using MiddleVR;
using MiddleVR.Unity;
//using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;

#if !UNITY_2018_4 && !UNITY_2019 && !UNITY_2020_1 && !UNITY_2020_2
#error This version of Unity is not supported.
#endif

[AddComponentMenu("")]
[HelpURL("http://www.middlevr.com/2/doc/current/#vrmanager_options")]
public class MVRManagerScript : MonoBehaviour
{
    // Exposed parameters:
    [Tooltip("Which configuration will be loaded on play?")]
    public string EditorConfigFile = "C:\\Program Files\\MiddleVR2\\data\\Config\\Default\\Default.vrx";

    [Tooltip("Attach MVRManager to the first active camera in the scene")]
    public bool AttachToCamera = true;

    [Tooltip("Enable the preview window")]
    public bool PreviewWindow = false;

    public ClusterProperties clusterProperties;

    [System.Serializable]
    public class AdvancedProperties
    {
        public bool ShowWand = true;

        [SerializeField]
        private bool _showFPS = false;
        
        public bool ShowFPS
        {
            get
            {
                return _showFPS;
            }
            set
            {
                _showFPS = value;
                var _mvrManager = GameObject.FindObjectOfType<MVRManagerScript>();
                if (_mvrManager != null) _mvrManager.EnableFPSDisplay(value);
            }
        }

        [Tooltip("Choose which camera in the scene should be duplicated for each MiddleVR camera")]
        public GameObject TemplateCamera = null;

        [Tooltip("Disable existing cameras in the scene for performances reasons")]
        public bool DisableExistingCameras = true;

        [Tooltip("Quit the application when the user presses 'Esc'.")]
        public bool QuitOnEsc = true;

        [SerializeField]
        private bool _showScreenProximityWarnings = false;
        public bool ShowScreenProximityWarnings
        {
            get
            {
                return _showScreenProximityWarnings;
            }
            set
            {
                _showScreenProximityWarnings = value;
                var _mvrManager = GameObject.FindObjectOfType<MVRManagerScript>();
                if (_mvrManager != null) _mvrManager.EnableProximityWarning(value);
            }
        }
    }

    public AdvancedProperties advancedProperties;

    // Private members
    private GameObject _root = null;

    private bool _logsToUnityConsole = true;
    private bool _forceQuality = false;
    private int _forceQualityIndex = 3;

    private List<Text> _fpsTexts = new List<Text>();

    private GameObject _wand = null;

    private bool _isInit = false;
    private bool _isGeometrySet = false;

    private uint _firstFrameAfterReset = 0;

    private struct TimeStruct
    {
        public float TimeScale;
        public float FixedDeltaTime;
    }

    private TimeStruct _time = new TimeStruct();

    // Public methods

    public GameObject GetRoot()
    {
        return _root;
    }

    public void Log(string text)
    {
        MVRTools.Log(text);
    }

    private enum ClusterChannels : int
    {
        Quit = -1,
        RandomSeed = -2,
        Time = -3
    }

    // Private methods

    private void InitializeVR()
    {
        if (MVR.Kernel != null)
        {
            MVRTools.Log(VRLogLevel.Debug, "[ ] VRKernel already alive, reset Unity Manager.");
            MVRTools.VRReset();
            _isInit = true;
            _firstFrameAfterReset = MVR.Kernel.GetFrame();
        }
        else
        {
            _isInit = MVRTools.VRInitialize(EditorConfigFile, PreviewWindow, clusterProperties);
        }

        if (XRSettings.enabled)
        {
            // XR needs cameras to run and MiddleVR will not be creating its cameras
            advancedProperties.DisableExistingCameras = false;
        }

        DumpOptions();

        if (MVR.ClusterMgr.IsClusterConfig())
        {
            if (clusterProperties.SimpleCluster)
            {
                SetupSimpleCluster();
            }

            if (clusterProperties.SimpleClusterParticles)
            {
                foreach (var particleSystem in FindObjectsOfType<ParticleSystem>())
                {
                    if (particleSystem.gameObject.GetComponent<MVRSynchronizeParticleSystem>() == null)
                    {
                        particleSystem.gameObject.AddComponent<MVRSynchronizeParticleSystem>();
                    }
                }
            }

            Cluster.AddMessageHandler(
                null,
                (int exitCode) =>
                {
                    LocalQuit(exitCode);
                },
                channel: ClusterChannels.Quit);

            Cluster.AddMessageHandler(
                null,
                (UnityEngine.Random.State state) =>
                {
                    UnityEngine.Random.state = state;

                    Cluster.RemoveMessageHandler(
                        null,
                        channel: ClusterChannels.RandomSeed);
                },
                channel: ClusterChannels.RandomSeed);

            Cluster.BroadcastMessage(
                null,
                UnityEngine.Random.state,
                channel: ClusterChannels.RandomSeed);

            _time.TimeScale = Time.timeScale;
            _time.FixedDeltaTime = Time.fixedDeltaTime;

            Cluster.AddMessageHandler(
                null,
                (TimeStruct time) =>
                {
                    Time.fixedDeltaTime = time.FixedDeltaTime;
                    Time.timeScale = time.TimeScale;
                },
                channel: ClusterChannels.Time);
        }

        if (advancedProperties.TemplateCamera == null)
        {
            GrabTemplateCamera();
        }

        _root = this.gameObject;

        MVRTools.CreateNodes(_root,advancedProperties.TemplateCamera);

        if (AttachToCamera && Camera.allCamerasCount > 0)
        {
            var cam = Camera.allCameras[0];
            this.transform.parent = cam.transform;
            this.transform.localPosition = new Vector3(0, 0, 0);
            this.transform.localRotation = new Quaternion(0, 0, 0, 1);
            this.transform.localScale = new Vector3(1, 1, 1);
        }

        if (advancedProperties.DisableExistingCameras)
        {
            foreach (var camera in FindObjectsOfType<Camera>())
            {
                if (camera.targetTexture == null)
                {
                    camera.enabled = false;
                }
            }
        }

        MVRTools.CreateViewportsAndCameras(mvrManager: gameObject);

        if (MVR.DisplayMgr.GetNode("HandNode") == null)
        {
            GameObject.Find("MVRWand").SetActive(false);
        }

        ManageAudioListeners();

        MVRTools.Log(4, "[<] End of VR initialization script");
    }

    protected void Awake()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.playModeStateChanged += HandleOnPlayModeChanged;
#endif

        this.gameObject.AddComponent<MVRClusterObject>();

        bool disableMiddleVR = false;

        if (Application.isEditor)
        {
            MVRTools.Log(3,$"[ ] In Unity Editor, XR settings = {XRSettings.enabled}");

            if (XRSettings.enabled)
            {
                disableMiddleVR = true;
            }
        }
        else // Player
        {
            if (XRSettings.enabled)
            {
                string[] args = System.Environment.GetCommandLineArgs();
                bool foundCfgParam = false;

                for (int i = 0; i < args.Length; i++)
                {
                    if (args[i] == "--config")
                    {
                        foundCfgParam = true;
                    }
                }

                if (!foundCfgParam)
                {
                    Debug.Log("[ ] In Unity Player, XR is activated and command line argument --config not found. Disabling MiddleVR.");
                    disableMiddleVR = true;
                }
            }
        }

        if (disableMiddleVR)
        {
            Debug.Log("[ ] Disabling MiddleVR");
            gameObject.SetActive(false);
            // We still need to initialize the MiddleVR so that the other scripts in the scene that depend on it don't fail
            MVR.VRInitialize();
            MVRTools.Log(2, "[ ] MiddleVR is disabled because Unity XR is enabled.");
            return;
        }
        else
        {
            if (XRSettings.enabled)
            {
                Debug.Log("[ ] Disabling Unity XR");
                // Disable Unity XR
                XRSettings.enabled = false;
            }
        }

        MVRNodesMapper.CreateInstance();

        MVRTools.RedirectingLogsToUnityConsole = _logsToUnityConsole;

        InitializeVR();
    }

#if UNITY_EDITOR
    void HandleOnPlayModeChanged(UnityEditor.PlayModeStateChange iState)
    {
        // This method is run whenever the playmode state is changed.
        if (Application.isEditor
            && clusterProperties.editorClusterDebuggingProperties.EnableEditorClusterDebugging
            && iState == UnityEditor.PlayModeStateChange.ExitingPlayMode)
        {
            // Tell every cluster node to quit
            QuitApplication();
        }
    }
#endif

    protected void Start()
    {
        MVRTools.Log(4, "[>] VR Manager Start.");

        _wand = GameObject.Find("MVRWand");

        ShowWandGeometry(advancedProperties.ShowWand);

        EnableProximityWarning(advancedProperties.ShowScreenProximityWarnings);

        EnableFPSDisplay(advancedProperties.ShowFPS);

        if (_forceQuality)
        {
            QualitySettings.SetQualityLevel(_forceQualityIndex);
        }

        MVRTools.Log(4, "[<] End of VR Manager Start.");
    }

    private void EnableProximityWarning(bool iShow)
    {
        if (_wand != null)
        {
            MVRScreenProximityWarning proximityWarning = _wand.GetComponent<MVRScreenProximityWarning>();

            if (proximityWarning != null)
            {
                proximityWarning.enabled = advancedProperties.ShowScreenProximityWarnings;
            }
        }
    }

    private void EnableFPSDisplay(bool iEnable)
    {
        foreach(var cam in Camera.allCameras)
        {
            Transform camCanvas = cam.transform.Find(cam.name + "_Canvas");
            if ( camCanvas == null)
            {
                GameObject go = new GameObject(cam.name + "_Canvas");
                go.transform.parent = cam.transform;
                //go.transform.position = new Vector3(0, 0, 0);
                //go.transform.rotation = new Quaternion(0, 0, 0, 1);

                var canvas = go.gameObject.AddComponent<Canvas>();
                // Draw 2D interface
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = cam;
                // Draw on top of everything
                //canvas.sortingOrder = 1000;

                var canvasScaler = go.gameObject.AddComponent<CanvasScaler>();
                // Use coordinates in pixels
                //canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;

                var textGO = new GameObject("FPS Text");
                textGO.transform.parent = go.transform;

                var text = textGO.AddComponent<Text>();
                var rct = text.rectTransform;

                text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                text.fontSize = 15;
                text.rectTransform.pivot = new Vector2(0, 0);
                rct.anchorMin = new Vector2(0.0f, 0.0f);
                rct.anchorMax = new Vector2(1.0f, 1.0f);
                text.alignment = TextAnchor.LowerLeft;
                text.rectTransform.anchoredPosition3D = new Vector3(0, 0, 0);
                text.transform.localRotation = new Quaternion(0, 0, 0, 1);
                text.transform.localScale = new Vector3(1, 1, 1);

                _fpsTexts.Add(text);

            }

            //GameObject camCanvasGO = cam.transform.Find(cam.name + "_Canvas").gameObject;
            GameObject camCanvasGO = GameObject.Find(cam.name + "_Canvas");
            camCanvasGO.GetComponent<Canvas>().enabled = advancedProperties.ShowFPS;
        }
    }

    public void ShowWandGeometry(bool iShow)
    {
        if (_wand != null)
        {
            _wand.GetComponent<MVRWand>().Show(iShow);
        }
    }

    protected void GrabTemplateCamera()
    {
        // List all *active/enabled* cameras
        Camera[] allCameras = Camera.allCameras;

        if (allCameras.Length > 0)
        {
            advancedProperties.TemplateCamera = allCameras[0].gameObject;

            MVRTools.Log(3, "[ ] Using camera '" + advancedProperties.TemplateCamera.name + "' as template camera.");
        }
        else
        {
            MVRTools.Log(3, "[~] Couldn't find an active camera to use as a template camera.");
        }
    }

    protected void ManageAudioListeners()
    {
        // Disable all audio listeners
        foreach (var listener in FindObjectsOfType<AudioListener>())
        {
            listener.enabled = false;
        }

        // Enable AudioListener on HeadNode
        var headNode = GameObject.Find("HeadNode");
        if (headNode != null)
        {
            var headListener = headNode.GetComponent<AudioListener>();
            if (headListener == null)
                headListener = headNode.AddComponent<AudioListener>();
            headListener.enabled = true;
        }
    }

    // Update is called once per frame
    protected void Update()
    {
        MVRNodesMapper nodesMapper = MVRNodesMapper.Instance;

        if (_isInit)
        {
            MVRTools.Log(4, "[>] Unity Update - Start");

            if (MVR.Kernel.GetFrame() >= _firstFrameAfterReset + 1 && !_isGeometrySet && !Application.isEditor)
            {
                MVR.DisplayMgr.SetUnityWindowGeometry();
                _isGeometrySet = true;
            }

            // Set the random seed in kernel for dispatching only during start-up.
            if (MVR.Kernel.GetFrame() == 0)
            {
                // Disable obsolescence warning for Random.seed as we only use it to initalize
                // the MiddleVR random seed.
                // Using Random.seed was deprecated because it is a single integer and does not
                // contain the full state of the random number generator and thus could not be
                // used for restoring or synchronizing the state.
#pragma warning disable 618
                // The cast is safe because the seed is always positive.
                MVR.Kernel._SetRandomSeed((uint)UnityEngine.Random.seed);
#pragma warning restore 618
            }

            if (MVR.ClusterMgr.IsClusterConfig())
            {
                if (Mathf.Abs(Time.timeScale - _time.TimeScale) > 0.00001f
                || Mathf.Abs(Time.fixedDeltaTime - _time.FixedDeltaTime) > 0.00001f)
                {
                    _time.TimeScale = Time.timeScale;
                    _time.FixedDeltaTime = Time.fixedDeltaTime;

                    Cluster.BroadcastMessage(
                        null,
                        _time,
                        channel: ClusterChannels.Time
                        );
                }
            }

            MVR.Kernel.Update();

            if (advancedProperties.ShowFPS)
            {
                foreach (var text in _fpsTexts)
                {
                    text.text = MVR.Kernel.GetFPS().ToString("f2");
                }
            }

            nodesMapper.UpdateNodesMiddleVRToUnity();

            MVRTools.UpdateCameraProperties(nodesMapper);

            Cluster.DispatchMessages();

            vrKeyboard keyb = MVR.DeviceMgr.GetKeyboard();

            if (keyb != null)
            {
                if (keyb.IsKeyPressed(MVR.VRK_LSHIFT) || keyb.IsKeyPressed(MVR.VRK_RSHIFT))
                {

                    if (keyb.IsKeyToggled(MVR.VRK_D))
                    {
                        advancedProperties.ShowFPS = !advancedProperties.ShowFPS;
                    }

                    if (keyb.IsKeyToggled(MVR.VRK_W) || keyb.IsKeyToggled(MVR.VRK_Z))
                    {
                        advancedProperties.ShowWand = !advancedProperties.ShowWand;
                        ShowWandGeometry(advancedProperties.ShowWand);
                    }
                }
            }

            MVRTools.Log(4, "[<] Unity Update - End");
        }
        else
        {
            //Debug.LogWarning("[ ] If you have an error mentioning 'DLLNotFoundException: MiddleVR_CSharp', please restart Unity. If this does not fix the problem, please make sure MiddleVR is in the PATH environment variable.");
        }
    }

    private void AddClusterScript(GameObject iObject)
    {
        MVRTools.Log(2, $"[ ] Adding cluster sharing scripts to {iObject.name}");
        if (iObject.GetComponent<MVRClusterObject>() == null)
        {
            iObject.AddComponent<MVRClusterObject>();
        }
    }

    private void SetupSimpleCluster()
    {
        foreach (var rigidBody in FindObjectsOfType<Rigidbody>())
        {
            if (!rigidBody.isKinematic)
            {
                AddClusterScript(rigidBody.gameObject);
            }
        }

        foreach (var characterController in FindObjectsOfType<CharacterController>())
        {
            AddClusterScript(characterController.gameObject);
        }

        foreach(var anim in FindObjectsOfType<Animator>())
        {
            anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        }
    }

    private void DumpOptions()
    {
        MVRTools.Log(3, "[ ] Dumping MVRManager's options:");
        MVRTools.Log(3, "[ ] - Config File : " + EditorConfigFile);
        MVRTools.Log(3, "[ ] - Template Camera : " + advancedProperties.TemplateCamera);
        MVRTools.Log(3, "[ ] - Show Wand : " + advancedProperties.ShowWand);
        MVRTools.Log(3, "[ ] - Show FPS  : " + advancedProperties.ShowFPS);
        MVRTools.Log(3, "[ ] - Disable Existing Cameras : " + advancedProperties.DisableExistingCameras);
        MVRTools.Log(3, "[ ] - Quit On Esc : " + advancedProperties.QuitOnEsc);
        MVRTools.Log(3, "[ ] - Simple Cluster : " + clusterProperties.SimpleCluster);
        MVRTools.Log(3, "[ ] - Simple Cluster Particles : " + clusterProperties.SimpleClusterParticles);
    }

    public void QuitApplication(int exitCode = 0)
    {
        MVRTools.Log(3, "[ ] Execute QuitCommand.");

        if (MVR.ClusterMgr.IsClusterConfig())
        {
            Cluster.BroadcastMessage(null, exitCode, channel: ClusterChannels.Quit);
        }
        else
        {
            LocalQuit(exitCode);
        }
    }

    private void LocalQuit(int exitCode = 0)
    {
        MVRTools.Log(3,"[ ] Unity says we're quitting.");
        MVR.Kernel.SetQuitting();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit(exitCode);
#endif
    }

    protected void OnApplicationQuit()
    {
        MVRNodesMapper.DestroyInstance();

        MVRTools.VRDestroy(Application.isEditor);
    }

    protected void OnDestroy()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.playModeStateChanged -= HandleOnPlayModeChanged;
#endif
    }
}
