using UnityEditor;
using UnityEngine;

namespace ModTool.Exporting.Editor
{
    internal class ExporterEditorWindow : EditorWindow
    {
        public ModExporter exporter;

        private ExportSettings exportSettings;

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
            exporter = ModExporter.instance;

            exportSettings = ExportSettings.instance;

            exportSettingsEditor = UnityEditor.Editor.CreateEditor(exportSettings);
        }

        void OnDisable()
        {
            DestroyImmediate(exportSettingsEditor);
        }

        void OnGUI()
        {
            GUI.enabled = !EditorApplication.isCompiling && !exporter.isExporting && !Application.isPlaying;

            exportSettingsEditor.OnInspectorGUI();

            GUILayout.FlexibleSpace();

            bool buttonPressed = GUILayout.Button("Export Mod", GUILayout.Height(30));

            if (buttonPressed)
                exporter.ExportMod(exportSettings);
        }
    }
}
