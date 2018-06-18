using UnityEditor;
using UnityEngine;
using ModTool.Shared;
using ModTool.Shared.Editor;

namespace ModTool.Editor
{
    internal class SettingsEditorWindow : EditorWindow
    {
        private ModToolSettings modToolSettings;
        private CodeSettings codeSettings;

        private UnityEditor.Editor modToolSettingsEditor;
        private UnityEditor.Editor codeSettingsEditor;

        Vector2 scrollPos = Vector2.zero;

        [MenuItem("Tools/ModTool/Settings")]
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
