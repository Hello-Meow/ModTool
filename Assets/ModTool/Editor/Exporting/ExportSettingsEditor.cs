using UnityEngine;
using UnityEditor;
using ModTool.Shared;

namespace ModTool.Editor.Exporting
{
    [CustomEditor(typeof(ExportSettings))]
    public class ExportSettingsEditor : UnityEditor.Editor
    {
        private SerializedProperty _name;
        private SerializedProperty _author;
        private SerializedProperty _description;
        private SerializedProperty _version;
        private SerializedProperty _platforms;
        private SerializedProperty _content;
        private SerializedProperty _outputDirectory;

        private FilteredEnumMaskField platforms;
        private FilteredEnumMaskField content;

        void OnEnable()
        {
            _name = serializedObject.FindProperty("_name");
            _author = serializedObject.FindProperty("_author");
            _description = serializedObject.FindProperty("_description");
            _version = serializedObject.FindProperty("_version");
            _platforms = serializedObject.FindProperty("_platforms");
            _content = serializedObject.FindProperty("_content");
            _outputDirectory = serializedObject.FindProperty("_outputDirectory");

            platforms = new FilteredEnumMaskField(typeof(ModPlatform), (int)ModToolSettings.supportedPlatforms);
            content = new FilteredEnumMaskField(typeof(ModContent), (int)ModToolSettings.supportedContent);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            GUILayout.Space(5);

            EditorGUILayout.PropertyField(_name, new GUIContent("Mod Name*:"));
            EditorGUILayout.PropertyField(_author, new GUIContent("Author:"));
            EditorGUILayout.PropertyField(_version, new GUIContent("Version:"));

            EditorGUILayout.PropertyField(_description, new GUIContent("Description:"), GUILayout.Height(60));

            GUILayout.Space(5);

            _platforms.intValue = platforms.DoMaskField("Platforms*:", _platforms.intValue);
            _content.intValue = content.DoMaskField("Content*:", _content.intValue);
            ModToolSettings.logLevel = (LogLevel)EditorGUILayout.EnumPopup("Log Level:", ModToolSettings.logLevel);

            bool enabled = GUI.enabled;

            GUILayout.BeginHorizontal();

            GUI.enabled = false;

            EditorGUILayout.TextField("Output Directory*:", GetShortString(_outputDirectory.stringValue));

            GUI.enabled = enabled;

            if (GUILayout.Button("...", GUILayout.Width(30)))
            {
                string selectedDirectory = EditorUtility.SaveFolderPanel("Choose output directory", _outputDirectory.stringValue, "");
                if (!string.IsNullOrEmpty(selectedDirectory))
                    _outputDirectory.stringValue = selectedDirectory;

                Repaint();
            }

            GUILayout.EndHorizontal();

            GUILayout.FlexibleSpace();

            EditorGUILayout.EndVertical();

            serializedObject.ApplyModifiedProperties();
        }

        private string GetShortString(string str)
        {
            int maxWidth = (int)EditorGUIUtility.currentViewWidth - 252;
            int cutoffIndex = Mathf.Max(0, str.Length - 7 - (maxWidth / 7));
            string shortString = str.Substring(cutoffIndex);
            if (cutoffIndex > 0)
                shortString = "..." + shortString;
            return shortString;
        }
    }
}