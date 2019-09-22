using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using ModTool.Shared;
using ModTool.Shared.Editor;

namespace ModTool.Exporting.Editor
{
    /// <summary>
    /// Main class for exporting a project as a mod.
    /// </summary>
    public class ModExporter : EditorScriptableSingleton<ModExporter>
    {
        /// <summary>
        /// Occurs when the export process is starting.
        /// </summary>
        public static event Action ExportStarting;

        /// <summary>
        /// Occurs after the export process is completed.
        /// </summary>
        public static event Action ExportComplete;

        /// <summary>
        /// Is this ModExporter currently exporting a Mod?
        /// </summary>
        public static bool isExporting
        {
            get
            {
                return instance.currentStep >= 0;
            }
        }

        private ExportStep[] exportSteps = new ExportStep[]
        {
            new StartExport(),
            new Verify(),
            new GetContent(),
            new CreateBackup(),
            new ImportScripts(),
            new UpdateAssets(),
            new Export(),
            //new RestoreProject(),
        };

        [SerializeField]
        private int currentStep = -1;        
                
        private ExportData data;

        private static bool didReloadScripts;
        
        void OnEnable()
        {
            EditorApplication.update += Update;
        }

        void OnDisable()
        {
            ExportStarting = null;
            ExportComplete = null;
            EditorApplication.update -= Update;
        }

        void Update()
        {
            if (isExporting && EditorApplication.isPlayingOrWillChangePlaymode)
                EditorApplication.isPlaying = false;
            
            if (didReloadScripts)
            {
                didReloadScripts = false;
                OnAssemblyReload();
            }
        }
        
        [UnityEditor.Callbacks.DidReloadScripts]
        static void OnDidReloadScripts()
        {
            didReloadScripts = true;
        }

        /// <summary>
        /// Start exporting a Mod.
        /// </summary>
        public static void ExportMod()
        {
            if(isExporting)
            {
                LogUtility.LogError("Already exporting");
                return;
            }

            instance.StartExport();
        }

        private void StartExport()
        {
            data = new ExportData();

            LogUtility.LogInfo("Exporting Mod: " + ExportSettings.name);

            ExportStarting?.Invoke();

            currentStep = 0;

            Continue();
        }
        
        private void Continue()
        {
            while (currentStep < exportSteps.Length)
            {
                ExportStep step = exportSteps[currentStep];
                float progress = (float)currentStep / exportSteps.Length;

                EditorUtility.DisplayProgressBar("Exporting", step.message + "...", progress);
                                
                if (!ExecuteStep(step))
                    break;

                currentStep++;

                if (currentStep == exportSteps.Length)
                    ExportComplete?.Invoke();

                if (step.waitForAssemblyReload)
                    return;
            }

            StopExport();
        }

        private bool ExecuteStep(ExportStep step)
        {
            LogUtility.LogDebug(step.message);

            try
            {
                step.Execute(data);
            }
            catch (Exception e)
            {
                Debug.LogError(step.message + " failed: " + e.Message);                
                return false;
            }

            return true;
        }               

        private void OnAssemblyReload()
        {
            if (!isExporting)
                return;
                        
            Continue();
        }
        
        private void StopExport()
        {
            if (!isExporting)
                return;
            
            currentStep = -1;

            Restore();

            EditorUtility.ClearProgressBar();
        }

        private void Restore()
        {
            if(!ExecuteStep(new RestoreProject()))            
                Debug.LogWarning("Some assets might be corrupted. A backup has been made in " + Path.Combine(Directory.GetCurrentDirectory(), Asset.backupDirectory));            
        }        
    }    
}
