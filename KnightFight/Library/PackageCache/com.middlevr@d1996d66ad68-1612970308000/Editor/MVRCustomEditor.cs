/* MVRCustomEditor
 * MiddleVR
 * (c) MiddleVR
 */

using MiddleVR.Unity;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

[CustomEditor(typeof(MVRManagerScript))]
public class MVRCustomEditor : Editor
{
    //This will just be a shortcut to the target, ex: the object you clicked on.
    private MVRManagerScript _mvrManager;
    private static bool s_settingsApplied = false;
    private ListRequest _listRequest;

    private void Awake()
    {
        _mvrManager = (MVRManagerScript)target;

        if (!s_settingsApplied)
        {
            ApplyVRSettings();
            s_settingsApplied = true;
        }

        CheckPackages();
    }

    public void ApplyVRSettings()
    {
#pragma warning disable 618
        if(PlayerSettings.defaultIsFullScreen == true)
        {
            PlayerSettings.defaultIsFullScreen = false;
            MVRTools.Log("Player setting 'DefaultIsFullScreen' set to 'False'");
        }

        if(PlayerSettings.displayResolutionDialog != ResolutionDialogSetting.Disabled)
        {
            PlayerSettings.displayResolutionDialog = ResolutionDialogSetting.Disabled;
            MVRTools.Log("Player setting 'DisplayResolutionDialog' set to 'Disabled'");
        }

        if(PlayerSettings.runInBackground != true)
        {
            PlayerSettings.runInBackground = true;
            MVRTools.Log("Player setting 'RunInBackground' set to 'True'");
        }

        if(PlayerSettings.captureSingleScreen != false)
        {
            PlayerSettings.captureSingleScreen = false;
            MVRTools.Log("Player setting 'CaptureSingleScreen' set to 'False'");
        }
        
#if UNITY_D3D11_FULLSCREEN_MODE
        if(PlayerSettings.d3d11FullscreenMode != D3D11FullscreenMode.ExclusiveMode)
        {
            PlayerSettings.d3d11FullscreenMode = D3D11FullscreenMode.ExclusiveMode;
            MVRTools.Log("Player setting 'd3d11FullscreenMode' set to 'ExclusiveMode'");
        }
#endif
#pragma warning restore 618

        string[] names = QualitySettings.names;
        int qualityLevel = QualitySettings.GetQualityLevel();

        // Disable VSync on all quality levels
        for (int i = 0; i < names.Length; ++i)
        {
            QualitySettings.SetQualityLevel(i);
            QualitySettings.vSyncCount = 0;
        }

        QualitySettings.SetQualityLevel(qualityLevel);

        // Force to build in x64
        if (!EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64))
        {
            MVRTools.Log("Switch to StandAloneWindows64 failed.");
        }
    }

    public override void OnInspectorGUI()
    {
        GUILayout.BeginVertical();

        if (GUILayout.Button("Re-apply VR player settings"))
        {
            ApplyVRSettings();
        }

        if (GUILayout.Button("Pick configuration file"))
        {
            string path = EditorUtility.OpenFilePanel("Please choose MiddleVR configuration file", "C:/Program Files/MiddleVR2/data/Config/Default", "vrx");
            _mvrManager.EditorConfigFile = path;
            EditorUtility.SetDirty(_mvrManager);
        }

        DrawDefaultInspector();
        GUILayout.EndVertical();
    }

    private void EnableHDRP()
    {
        var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone);

        if (!defines.Contains("MVR_HDRP"))
        {
            if (GameObject.Find("MVR_HDRP") == null)
            {
                EditorUtility.DisplayDialog("HDRP detected", "You must add the MiddleVR_HDRP package and the MVR_HDRP prefab.", "Ok");
            }
        }
    }

    private void EnableURP()
    {
        var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone);

        if (!defines.Contains("MVR_URP"))
        {
            if (GameObject.Find("MVR_URP") == null)
            {
                EditorUtility.DisplayDialog("URP detected", "You must add the MiddleVR_URP package and the MVR_URP prefab.", "Ok");
            }
        }
    }

    private void CheckPackages()
    {
        _listRequest = Client.List();
        EditorApplication.update += ListProgress;
    }

    private void ListProgress()
    {
        if (_listRequest.IsCompleted)
        {
            if (_listRequest.Status == StatusCode.Success)
            {
                foreach (var package in _listRequest.Result)
                {
                    //Debug.Log("Package name: " + package.name);
                    if (package.name == "com.unity.render-pipelines.universal")
                    {
                        EnableURP();
                    }
                    else if (package.name == "com.unity.render-pipelines.high-definition")
                    {
                        EnableHDRP();
                    }
                }
            }
            else if (_listRequest.Status >= StatusCode.Failure)
            {
                Debug.Log(_listRequest.Error.message);
            }

            EditorApplication.update -= ListProgress;
        }
    }
}
