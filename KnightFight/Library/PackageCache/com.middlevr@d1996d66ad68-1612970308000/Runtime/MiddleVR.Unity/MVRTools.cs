/* MVRTools
 * MiddleVR
 * (c) MiddleVR
 */

using System;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.XR;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MiddleVR.Unity
{
    [System.Serializable]
    public class EditorClusterDebuggingProperties
    {
        [Tooltip("Will start all cluster nodes and use this Unity Editor as a cluster node")]
        public bool EnableEditorClusterDebugging = false;

        [Tooltip("You need to build the project so the other cluster nodes have the same project version. If left empty will automatically get the path of the last build")]
        public string ExePath;

        [Tooltip("Which cluster ID should be used by this Unity Editor")]
        public string EditorClusterID = "";

        [Tooltip("You can decide to start other cluster nodes manually, with MiddleVR Config, a .bat file, or with other Unity Editors")]
        public bool StartOtherClusterNodes = true;

        [Tooltip("Same argument as in MiddleVR Config")]
        public int LogLevel = 2;
        [Tooltip("Same argument as in MiddleVR Config")]
        public string LogFolder;
        [Tooltip("Same argument as in MiddleVR Config")]
        public bool LogDatePrefix;
        [Tooltip("Same argument as in MiddleVR Config")]
        public string CustomArguments;
    }

    [System.Serializable]
    public class ClusterProperties
    {
        [Tooltip("Tries to identify objects that will not be correctly synchronised, such as physics objects")]
        public bool SimpleCluster = true;

        [Tooltip("Tries to synchronize particles parameters")]
        public bool SimpleClusterParticles = true;
        public EditorClusterDebuggingProperties editorClusterDebuggingProperties;
    }

    public partial class MVRTools
    {
        private static string s_commandLineConfigFile = null;
        private static VRLogLevel s_logLevel = VRLogLevel.Info;

        #region Properties

        /// Activate/deactivate sending of messages to the underlying MiddleVR log system.
        public static bool UsingMiddleVRLogSystem { get; set; } = true;

        /// Activate/deactivate sending of messages to the Unity console.
        public static bool RedirectingLogsToUnityConsole { get; set; } = true;

        #endregion Properties
        public static bool VRDestroy(bool iInEditorMode)
        {
            Log(3, "[>] Destroying MiddleVR.");

            MVR.VRDestroy();

            return true;
        }

        public static bool VRLMCheck()
        {
            bool lic = false;

            vrClusterManager clusterMgr = MVR.ClusterMgr;

            if (clusterMgr.GetRequestedClusterID().Length != 0 && clusterMgr.GetRequestedClusterID() != "ClusterServer")
            {
                Log(3, "[ ] In Unity, Cluster client, not checking for license.");
                lic = true;
            }
            else
            {
                MVR.LogMgr.SetLogPopupEnabled(false);

                if (MVR.LicenseMgr.Checkout("MVR2-Pro-Unity"))
                {
                    lic = true;
                }

                MVR.LogMgr.SetLogPopupEnabled(true);
            }

            if (!lic)
            {
                lic = MVR.LicenseMgr.CheckoutTrial();
            }

            if (!lic)
            {
                Log(2, "[ ] No license or trial license found, starting trial.");
                MVR.LicenseMgr.StartTrial();
                lic = MVR.LicenseMgr.CheckoutTrial();
            }

            MVR.LicenseMgr.DumpLicense();

            return lic;
        }

        public static bool VRInitialize(string iEditorConfigFile,
                                        bool iEnablePreviewWindow,
                                        ClusterProperties iProps)
        {
            Application.runInBackground = true;

            bool isInit = false;

            Log(3, "[>] MiddleVR.Unity - Starting VR init.");

            MVR.VRInitialize();

            if (MVR.Kernel == null)
            {
                Log(0, "[X] VR Manager failed to initialize.");
                return false;
            }

            // Set export version to current MiddleVR.Unity.dll version.
            Version version = Assembly.GetExecutingAssembly().GetName().Version;

            uint versionMajor = (uint)version.Major;
            uint versionMinor = (uint)version.Minor;
            uint versionRevision = (uint)version.Revision;

            MVR.Kernel.SetExportVersion(versionMajor, versionMinor, versionRevision);

            MVR.Log(2, "[ ] 'MiddleVR.Unity' version '" + version + "'.");

            var log_path = System.IO.Path.Combine(Environment.GetEnvironmentVariable("AppData"),
                                        "..",
                                        "LocalLow",
                                        Application.companyName,
                                        Application.productName);
            MVR.Log(2, "[ ] Unity logs (if not overriden by -logfile): " + log_path);

            if(Application.isEditor)
            {
                MVR.Kernel.SetRuntimeMode(MiddleVR.VRRuntimeMode.Editor);
            }
            
            vrKernel.Set3DEngine("Unity3D");

            // This will set the clusterId, which is required for LMCheck
            MVRTools.ProcessCommandLineArguments();

            if (Application.isEditor)
            {
                if(iEnablePreviewWindow)
                {
                    MVR.DisplayMgr.SetForceWindowMode(VRWindowMode.Compositor);
                }
                else
                {
                    MVR.DisplayMgr.SetForceWindowMode(VRWindowMode.UnityWindow);
                }

                if (iProps.editorClusterDebuggingProperties.EnableEditorClusterDebugging)
                {
                    MVR.DisplayMgr.SetForceWindowMode(VRWindowMode.Compositor);
                    MVR.ClusterMgr.SetDebugClusterInEditorMode(true);
                    MVR.ClusterMgr.SetRequestedClusterID(iProps.editorClusterDebuggingProperties.EditorClusterID);
                    MVR.LogMgr.SetLogLevel((VRLogLevel)iProps.editorClusterDebuggingProperties.LogLevel);
                }
            }

            VRLMCheck();

            Log(3, "[<] MiddleVR.Unity - End of init.");

            string cfgFile = null;

            if (Application.isEditor)
            {
                cfgFile = iEditorConfigFile;
            }
            else
            {
                // Player mode
                if (s_commandLineConfigFile != null)
                {
                    cfgFile = s_commandLineConfigFile;
                }
            }

            if (cfgFile != null)
            {
                MVR.Log(2, "[ ] Unity Loading Config: '" + cfgFile + "'.");

                if (MVR.Kernel.LoadConfig(cfgFile))
                {
                    MVR.Log(2, "[+] MiddleVR: Config loaded successfully: '" + cfgFile + "'.");
                    isInit = true;
                }
                else
                {
                    Log(0, "[X] Config failed to load: '" + cfgFile + "'.");
                    isInit = true;
                }
            }
            else
            {
                Log(1, "[~] No configuration file was loaded.");

                isInit = true;
            }

            if (isInit == true)
            {
                Log(3, "[+] VR Manager Initialization OK!");
                s_logLevel = MVR.LogMgr.GetLogLevel();

                MVR.DisplayMgr.RenameUnityWindow();
            }
            else
            {
                Log(0, "[X] VR Manager failed to load config file '" + iEditorConfigFile + "'.");
            }

            MVR.Log(2,"[ ] Unity: Current quality level: " + QualitySettings.GetQualityLevel());
            MVR.Log(2,"[ ] Unity: In this project's current quality level, VSync is: " + QualitySettings.vSyncCount);

            if (QualitySettings.vSyncCount != 0)
            {
                Log(1, "[X] Unity: You *MUST* deactivate VSync in your Unity application, MiddleVR will handle it." +
                    " (Edit > Project Settings > Quality > VSync Count > Don't Sync)");

                if (!Application.isEditor)
                {
                    vrLogger.MessageWindow(
                        "You *MUST* deactivate VSync in your Unity application, MiddleVR will handle it." +
                        " (Edit > Project Settings > Quality > VSync Count > Don't Sync)");
                }
            }

            Log(3, "[<] MiddleVR.Unity - End of VRInitialize.");

            if (Application.isEditor
                    && iProps.editorClusterDebuggingProperties.EnableEditorClusterDebugging)
            {
                if (MVR.DisplayMgr.GetWindowMode() != VRWindowMode.Compositor)
                {
                    Log(0, "[X] Editor Cluster Debugging is only compatible with a configuration where ForceViewportOutputMode is 'Compositor'");
                    MVR.Kernel.SetQuitting();

#if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
#endif
                }
                else
                {
#if UNITY_EDITOR
                    if (MVR.ClusterMgr.IsClusterConfig())
                    {
                        if (EditorUtility.DisplayDialog("Did you build your application?",
                            "Did you rebuild your application ? If you did not build the latest version of your application, the cluster nodes will not be executing the same version of the application.",
                            "Yes, continue",
                            "No, stop running"))
                        {
#endif


                            if (iProps.editorClusterDebuggingProperties.StartOtherClusterNodes)
                            {
                                StartClusterNodes(cfgFile, iProps);
                            }

#if UNITY_EDITOR
                        }
                        else
                        {
                            UnityEditor.EditorApplication.isPlaying = false;
                        }
                    }
#endif
                }
            }

            return isInit;
        }

        private static void StartClusterNodes(string iCfgFile, ClusterProperties iProps)
        {
            // Create command line
            string cmdLineParameters = MVR.ClusterMgr.CreateCommandLineParameters(
                    iCfgFile,
                    iProps.editorClusterDebuggingProperties.CustomArguments,
                    iProps.editorClusterDebuggingProperties.LogLevel,
                    iProps.editorClusterDebuggingProperties.LogFolder,
                    iProps.editorClusterDebuggingProperties.LogDatePrefix);

            // We don't want the cluster server to start the nodes, we are doing it here
            cmdLineParameters += " --nostartclients";

            if( MVR.ClusterMgr.FindAndSetMyClusterNode() != null )
            {
                MVR.ClusterMgr.GetMyClusterNode().SetStartManually(true);

                string exe = iProps.editorClusterDebuggingProperties.ExePath;
#if UNITY_EDITOR
                if (exe.Length == 0)
                {
                    string path = UnityEditor.EditorUserBuildSettings.GetBuildLocation(UnityEditor.BuildTarget.StandaloneWindows64);
                    exe = "\"" + Path.GetFullPath(path) + "\"";
                }
#endif
                if(exe.Length == 0)
                {
                    Log(0, "[X] Editor Cluster Debugging: You must build your application");
                    MVR.Kernel.SetQuitting();
                }

                string cmdLine = exe + " " + cmdLineParameters;
                Log(2, "[ ] Unity editor executing : '" + cmdLine + "'.");

                MVR.ClusterMgr.RemoteStartAll(cmdLine, true);
                MVR.ClusterMgr.StartMyClusterNode();

                MVR.ClusterMgr.GetMyClusterNode().SetStartManually(false);
            }
        }

        public static void VRReset()
        {
            Log(2, "[>] Doing VRReset.");

            MVR.DisplayMgr.ResetUnityManager();

            Log(2, "[<] Did VRReset.");
        }

        private static void ProcessCommandLineArguments()
        {
            // Extract arguments and apply them in the MiddleVR kernel.
            MVR.Kernel.ExtractCommandLineArguments(true);

            // Find if a configuration file was passed.
            bool nextArgIsConfigFile = false;
            for (uint i = 0, iEnd = MVR.Kernel.GetCommandLineArgumentsNb(); i < iEnd; ++i)
            {
                var cmdLineArg = MVR.Kernel.GetCommandLineArgument(i);

                if (cmdLineArg == "--config")
                {
                    nextArgIsConfigFile = true;
                }
                else if (nextArgIsConfigFile)
                {
                    s_commandLineConfigFile = cmdLineArg;
                    nextArgIsConfigFile = false;
                }
            }
        }

        public static void Log(string iMsg)
        {
            Log(VRLogLevel.Info, iMsg);
        }

        public static void Log(int iLogLevel, string iMsg)
        {
            Log((VRLogLevel)iLogLevel, iMsg);
        }

        public static void Log(VRLogLevel iLogLevel, string iMsg)
        {
            if (iMsg != null)
            {
                if (RedirectingLogsToUnityConsole &&
                    iLogLevel <= s_logLevel && Application.isEditor)
                {
                    switch (iLogLevel)
                    {
                        case VRLogLevel.Critical:
                        // Treat this as an error.
                        case VRLogLevel.Error:
                            Debug.LogError(iMsg);
                            break;
                        case VRLogLevel.Warning:
                            Debug.LogWarning(iMsg);
                            break;
                        default:
                            Debug.Log(iMsg);
                            break;
                    }
                }

                if (UsingMiddleVRLogSystem)
                {
                    MVR.Log(iLogLevel, iMsg);
                }
            }
        }

        public static void UpdateCameraProperties(MVRNodesMapper iNodesMapper)
        {
            var logLevel = MVR.LogMgr.GetLogLevel();

            foreach (var camera in MVR.DisplayMgr.Cameras)
            {
                if (camera is vrCameraStereo)
                    continue;

                var cameraGameObject = iNodesMapper.GetNode(camera);
                var cameraComponent = cameraGameObject.GetComponent<Camera>();

                // Check of the log level to avoid unneeded calls to mvrCam.GetName().
                if (logLevel >= VRLogLevel.Debug1)
                {
                    MVRTools.Log(VRLogLevel.Debug1, "[ ] Computing projection matrix for camera '" +
                        camera.GetName() + "', enabled? '" +
                        cameraComponent.enabled + "'.");
                }

                cameraComponent.nearClipPlane = camera.GetFrustumNear();
                cameraComponent.farClipPlane = camera.GetFrustumFar();
                cameraComponent.projectionMatrix = camera.GetProjectionMatrix().RawToUnity();
            }
        }
    }
} // namespace MiddleVR.Unity
