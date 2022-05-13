#define MIDDLEVR_DONT_EMBED_PACKAGES
#if UNITY_2019_1_OR_NEWER && !MIDDLEVR_DONT_EMBED_PACKAGES
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;

namespace MiddleVR.Unity.Editor
{
    public static class AutoEmbedPackages
    {
        // Embed packages of name <PackageNamespace> or <PackageNamespace>.*
        const string PackageNamespace = "com.middlevr";

        [InitializeOnLoadMethod]
        private static void RunAutoEmbed()
        {
            s_currentRequest = Client.List(true);
            EditorApplication.update += OnEditorUpdate;
        }

        private static Request s_currentRequest = null;
        private static readonly List<string> packagesToEmbed = new List<string>();

        private static void OnEditorUpdate()
        {
            if (s_currentRequest == null
                || (s_currentRequest != null && !s_currentRequest.IsCompleted))
            {
                return;
            }

            var request = s_currentRequest;
            s_currentRequest = null;

            if (request.Error != null)
            {
                Debug.Log("Error: " + request.Error.message);
                return;
            }

            if (request is ListRequest listRequest)
            {
                foreach (var package in listRequest.Result)
                {
                    if (package.status == PackageStatus.Available &&
                        package.source != PackageSource.Embedded &&
                        (package.name == PackageNamespace || package.name.StartsWith($"{PackageNamespace}.")) &&
                        !packagesToEmbed.Contains(package.name))
                    {
                        packagesToEmbed.Add(package.name);
                    }
                }
            }

            if (packagesToEmbed.Count != 0)
            {
                var packageName = packagesToEmbed[0];
                packagesToEmbed.RemoveAt(0);

                Debug.Log($"Auto-embedding unity package: {packageName}");
                s_currentRequest = Client.Embed(packageName);
            }
            else
            {
                // No more packages to embed, stop calling this
                EditorApplication.update += OnEditorUpdate;
            }

        }
    }
}
#endif
