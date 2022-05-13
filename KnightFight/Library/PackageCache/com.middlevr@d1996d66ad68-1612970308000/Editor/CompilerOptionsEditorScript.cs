/* MVRTools
 * MiddleVR
 * (c) MiddleVR
 */

// https://support.unity3d.com/hc/en-us/articles/210452343-How-to-stop-automatic-assembly-compilation-from-script
using UnityEditor;
using UnityEngine;

namespace MiddleVR.Unity.Editor
{
    [InitializeOnLoad]
    public class CompilerOptionsEditorScript
    {
        static bool s_waitingForStop = false;

        static CompilerOptionsEditorScript()
        {
            EditorApplication.update += OnEditorUpdate;
        }

        static void OnEditorUpdate()
        {
            if (!s_waitingForStop
                && EditorApplication.isCompiling
                && EditorApplication.isPlaying)
            {
                EditorApplication.LockReloadAssemblies();
                EditorApplication.playModeStateChanged
                     += PlaymodeChanged;
                s_waitingForStop = true;
            }
        }


        static void PlaymodeChanged(UnityEditor.PlayModeStateChange iState)
        {
            if (EditorApplication.isPlaying)
                return;

            EditorApplication.UnlockReloadAssemblies();
            EditorApplication.playModeStateChanged
                 -= PlaymodeChanged;
            s_waitingForStop = false;
        }
    }
}
