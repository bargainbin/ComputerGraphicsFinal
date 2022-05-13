/* PostProcessBuild
 * MiddleVR
 * (c) MiddleVR
 */

using System;
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace MiddleVR.Unity.Editor
{
    public static class PostProcessBuild
    {
        [PostProcessBuild(1)]
        public static void OnPostProcessBuild(BuildTarget target, string pathToBuiltProject)
        {
            string playerPath = Path.GetFullPath(pathToBuiltProject);
            string playerPlugins = GetPluginsOutputFolder(target, playerPath);

            CopyExeAsset("MiddleVR_UnityPlayer.exe", target, playerPath);
            CopyExeAsset("MiddleVR_Compositor.exe", target, Path.Combine(playerPlugins, "MiddleVR_Compositor.exe"));

            // Move d3d11.dll into proxy subfolder
            string pluginsProxyFolder = Path.Combine(playerPlugins, "proxy");
            Directory.CreateDirectory(pluginsProxyFolder);
            File.Delete(Path.Combine(pluginsProxyFolder, "d3d11.dll"));
            File.Move(Path.Combine(playerPlugins, "d3d11.dll"), Path.Combine(pluginsProxyFolder, "d3d11.dll"));
        }

        // In Unity 2018.4.0f1 up to 2019.3.8, plugins were placed in _Data/Plugins instead of _Data/Plugins/$ARCH
        // https://issuetracker.unity3d.com/issues/dlls-are-misplaced-in-standalone-player
        private static string GetPluginsOutputFolder(BuildTarget target, string playerPath)
        {
            string playerPlugins = Path.Combine(
                Path.GetDirectoryName(playerPath),
                Path.GetFileNameWithoutExtension(playerPath) + "_Data",
                "Plugins");

            if (File.Exists(Path.Combine(playerPlugins, "MiddleVR2.dll")))
            {
                return playerPlugins;
            }
            else
            {
                return Path.Combine(playerPlugins, GetPlayerArchFolderName(target));
            }
        }

        private static void CopyExeAsset(string assetFileName, BuildTarget target, string destination)
        {
            string mvrUnityPlayerPath = FindExeAsset(assetFileName, GetProjectArchFolderName(target));
            if (mvrUnityPlayerPath == null)
            {
                throw new FileNotFoundException($"Could not find {assetFileName} for BuildTarget '{target}'!");
            }

            try
            {
                File.Copy(
                    mvrUnityPlayerPath,
                    destination,
                    overwrite: true);
            }
            catch (IOException iox)
            {
                throw new IOException($"Could not copy '{assetFileName}' file to '{destination}': {iox.Message}!", iox);
            }

            Debug.Log($"Copied '{assetFileName}' file to '{destination}'.");
        }

        private static string FindExeAsset(string assetFileName, string subfolderName)
        {
            foreach (string guid in AssetDatabase.FindAssets(Path.GetFileNameWithoutExtension(assetFileName)))
            {
                string foundAssetPath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(Application.dataPath), AssetDatabase.GUIDToAssetPath(guid)));
                string fountAssetFileName = Path.GetFileName(foundAssetPath);
                string parentDirectoryName = Path.GetFileName(Path.GetDirectoryName(foundAssetPath));

                if (fountAssetFileName.Equals(assetFileName, StringComparison.OrdinalIgnoreCase) &&
                    parentDirectoryName.Equals(subfolderName, StringComparison.OrdinalIgnoreCase))
                {
                    return foundAssetPath;
                }
            }

            return null;
        }


        private static string GetProjectArchFolderName(BuildTarget target)
        {
            switch (target)
            {
                case BuildTarget.StandaloneWindows: return "x86";
                case BuildTarget.StandaloneWindows64: return "x64";
                default: throw new InvalidOperationException($"MiddleVR Player PostProcessing: Unknown BuildTarget '{target}'!");
            };
        }

        private static string GetPlayerArchFolderName(BuildTarget target)
        {
            switch (target)
            {
                case BuildTarget.StandaloneWindows: return "x86";
                case BuildTarget.StandaloneWindows64: return "x86_64";
                default: throw new InvalidOperationException($"MiddleVR Player PostProcessing: Unknown BuildTarget '{target}'!");
            };
        }
    }
}
