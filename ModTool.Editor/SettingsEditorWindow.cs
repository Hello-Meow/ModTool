using UnityEditor;
using UnityEngine;
using ModTool.Shared;

namespace ModTool.Editor
{
    /// <summary>
    /// ModTool's settings window.
    /// </summary>
    public class SettingsEditorWindow : EditorWindow
    {
        private ModToolSettings modToolSettings;
        private CodeSettings codeSettings;

        private UnityEditor.Editor modToolSettingsEditor;
        private UnityEditor.Editor codeSettingsEditor;

        Vector2 scrollPos = Vector2.zero;

        /// <summary>
        /// Open ModTool's settings window.
        /// </summary>
        [MenuItem("Tools/ModTool/Settings", priority = 0)]
        public static void ShowWindow()
        {
            SettingsEditorWindow window = GetWindow<SettingsEditorWindow>();

            window.maxSize = new Vector2(385f, 255);
            window.minSize = new Vector2(300f, 162);
            window.titleContent = new GUIContent("ModTool Settings");
        }

        void OnEnable()
        {
            modToolSettings = ModToolSettings.instance;
            codeSettings = CodeSettings.instance;

            modToolSettingsEditor = UnityEditor.Editor.CreateEditor(modToolSettings);
            codeSettingsEditor = UnityEditor.Editor.CreateEditor(codeSettings);
        }

        void OnDisable()
        {
            DestroyImmediate(modToolSettingsEditor);
            DestroyImmediate(codeSettingsEditor);
        }

        void OnGUI()
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            modToolSettingsEditor.OnInspectorGUI();
            codeSettingsEditor.OnInspectorGUI();

            EditorGUILayout.EndScrollView();
        }
    }
}
