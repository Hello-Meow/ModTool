using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.Compilation;
using ModTool.Shared;

namespace ModTool.Editor.Exporting
{
    /// <summary>
    /// Main class for exporting a project as a mod.
    /// </summary>
    public class ModExporter : Singleton<ModExporter>
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
                return instance._isExporting;
            }
        }

        private ExportStep[] exportSteps = new ExportStep[]
        {
            new StartExport(),
            new Verify(),
            new CreateAssemblies(),
            new GetContent(),            
            new CreateBackup(),
            new UpdateAssemblies(),
            new UpdateAssets(),
            new Export(),
        };

        [SerializeField]
        private bool _isExporting;

        [SerializeField]
        private int currentStep;
                        
        [SerializeField]
        private ExportData data;

        private bool didReloadScripts;

        void OnEnable()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            CompilationPipeline.assemblyCompilationFinished += OnAssemblyCompilationFinished;

            EditorApplication.update += Update;
        }

        void OnDisable()
        {
            ExportStarting = null;
            ExportComplete = null;

            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            CompilationPipeline.assemblyCompilationFinished -= OnAssemblyCompilationFinished;

            EditorApplication.update += Update;
        }

        private void OnPlayModeStateChanged(PlayModeStateChange playModeState)
        {
            if (!isExporting)
                return;

            if (playModeState == PlayModeStateChange.ExitingEditMode)
                EditorApplication.isPlaying = false;
        }

        private void OnAssemblyCompilationFinished(string assemblyName, CompilerMessage[] messages)
        {
            if (!isExporting)
                return;

            foreach (var message in messages)
            {
                if (message.type == CompilerMessageType.Error)
                {
                    Debug.LogError(message.message + " " + message.file);
                    LogUtility.LogWarning("Export aborted due to compiler error");
                    StopExport();
                    return;
                }
            }
        }

        [UnityEditor.Callbacks.DidReloadScripts]
        static void OnDidReloadScripts()
        {
            instance.didReloadScripts = true;
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

        private void Update()
        {
            if (didReloadScripts)
            {
                didReloadScripts = false;
                Continue();
            }
        }

        private void StartExport()
        {
            data = new ExportData();

            LogUtility.LogInfo("Exporting Mod: " + ExportSettings.name);

            ExportStarting?.Invoke();

            _isExporting = true;
            currentStep = 0;

            Continue();
        }
        
        private void Continue()
        {
            while (isExporting)
            {
                ExportStep step = exportSteps[currentStep];
                float progress = (float)currentStep / exportSteps.Length;

                EditorUtility.DisplayProgressBar("Exporting", step.message + "...", progress);

                if (ExecuteStep(step))
                    currentStep++; 
                else
                    StopExport();

                if (currentStep == exportSteps.Length)
                    StopExport();

                if (step.waitForAssemblyReload)
                    return;
            }
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
                Debug.LogError(step.message + " failed: " + e.ToString());
                return false;
            }            

            return true;
        }

        private void StopExport()
        {
            if (!isExporting)
                return;

            _isExporting = false;

            Restore();

            ExportComplete?.Invoke();

            EditorUtility.ClearProgressBar();
        }

        private void Restore()
        {
            if(!ExecuteStep(new RestoreProject()))            
                Debug.LogWarning("Some assets might be corrupted. A backup has been made in " + Path.Combine(Directory.GetCurrentDirectory(), Asset.backupDirectory));            
        }
    }    
}
