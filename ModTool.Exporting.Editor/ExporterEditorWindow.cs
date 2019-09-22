using UnityEditor;
using UnityEngine;

namespace ModTool.Exporting.Editor
{
    internal class ExporterEditorWindow : EditorWindow
    {
        private UnityEditor.Editor exportSettingsEditor;

        [MenuItem("Tools/ModTool/Export Mod")]
        public static void ShowWindow()
        {
            ExporterEditorWindow window = GetWindow<ExporterEditorWindow>();
            window.maxSize = new Vector2(385f, 265);
            window.minSize = new Vector2(300f, 265);
            window.titleContent = new GUIContent("Mod Exporter");
        }

        void OnEnable()
        {
            ExportSettings exportSettings = ExportSettings.instance;

            exportSettingsEditor = UnityEditor.Editor.CreateEditor(exportSettings);
        }

        void OnDisable()
        {
            DestroyImmediate(exportSettingsEditor);
        }

        void OnGUI()
        {
            GUI.enabled = !EditorApplication.isCompiling && !ModExporter.isExporting && !Application.isPlaying;

            exportSettingsEditor.OnInspectorGUI();

            GUILayout.FlexibleSpace();

            bool buttonPressed = GUILayout.Button("Export Mod", GUILayout.Height(30));

            if (buttonPressed)
                ModExporter.ExportMod();
        }
    }
}
