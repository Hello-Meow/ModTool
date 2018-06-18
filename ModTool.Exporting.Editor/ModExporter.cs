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
        /// Is this ModExporter currently exporting a Mod?
        /// </summary>
        public bool isExporting { get { return currentStep >= 0; } }

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
        
        private ExportSettings settings;     
        
        private ExportData data;

        private static bool didReloadScripts;
        
        void OnEnable()
        {
            EditorApplication.update += Update;
        }

        void OnDisable()
        {
            ExportStarting = null;
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
        /// <param name="settings">The settings to use for exporting the Mod.</param>
        public void ExportMod(ExportSettings settings)
        {
            if(isExporting)
            {
                LogUtility.LogError("Already exporting");
                return;
            }

            this.settings = settings;
            data = new ExportData();

            LogUtility.LogInfo("Exporting Mod: " + settings.name);

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
                step.Execute(settings, data);
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
