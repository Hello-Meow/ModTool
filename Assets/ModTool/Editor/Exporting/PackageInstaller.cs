using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using ModTool.Shared;

namespace ModTool.Editor.Exporting
{
    /// <summary>
    /// Installs packages that are configured in the exporter package.
    /// </summary>
    public class PackageInstaller
    {
        private static ListRequest listRequest;
        private static AddRequest addRequest;

        private static List<string> install;

        /// <summary>
        /// Install any shared packages that are currently not installed.
        /// </summary>
        [InitializeOnLoadMethod]
        public static void InstallSharedPackages()
        {
            EditorApplication.delayCall += Initialize;
        }

        private static void Initialize()
        {
            if (ModToolSettings.sharedPackages.Count == 0)
                return;

            EditorApplication.update += CheckInstalled;
        }

        private static void CheckInstalled()
        {
            if (listRequest == null)
                listRequest = Client.List();

            if (listRequest.Status == StatusCode.InProgress)
                return;

            if (listRequest.Status == StatusCode.Failure)            
                Debug.Log(listRequest.Error);

            if (listRequest.Status == StatusCode.Success)
            {
                install = new List<string>(ModToolSettings.sharedPackages);

                foreach(var package in listRequest.Result)
                    install.Remove(package.name);
                
                if (install.Count > 0)
                {
                    int total = ModToolSettings.sharedPackages.Count;
                    int installed = total - install.Count;

                    LogUtility.LogInfo("Installing shared packages: " + (installed + 1) + "/" + total);
                    EditorApplication.update += Install;
                }
            }

            EditorApplication.update -= CheckInstalled;            
        }

        private static void Install()
        {
            if (addRequest == null)
                addRequest = Client.Add(install[0]);

            if (addRequest.Status == StatusCode.InProgress)
                return;
            
            if (addRequest.Status == StatusCode.Failure)
                Debug.Log(addRequest.Error);

            if (addRequest.Status == StatusCode.Success)
                Debug.Log("Installed " + addRequest.Result.displayName);

            addRequest = null;

            EditorApplication.update -= Install;
        }
    }
}
