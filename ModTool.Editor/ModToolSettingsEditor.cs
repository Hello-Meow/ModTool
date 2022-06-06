using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using ModTool.Shared;

namespace ModTool.Editor
{
    [CustomEditor(typeof(ModToolSettings))]
    public class ModToolSettingsEditor : UnityEditor.Editor
    {
        private SerializedProperty _supportedPlatforms;
        private SerializedProperty _supportedContent;
        private SerializedProperty _logLevel;

        void OnEnable()
        {
            _supportedPlatforms = serializedObject.FindProperty("_supportedPlatforms");
            _supportedContent = serializedObject.FindProperty("_supportedContent");
            _logLevel = serializedObject.FindProperty("_logLevel");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            ModPlatform supportedPlatforms = (ModPlatform)EditorGUILayout.EnumMaskField("Supported Platforms", (ModPlatform)_supportedPlatforms.intValue);
            ModContent supportedContent = (ModContent)EditorGUILayout.EnumMaskField("Supported Content", (ModContent)_supportedContent.intValue);
            LogLevel logLevel = (LogLevel)EditorGUILayout.EnumPopup("Log Level", (LogLevel)_logLevel.intValue);

            _supportedPlatforms.intValue = supportedPlatforms.FixEnum();
            _supportedContent.intValue = supportedContent.FixEnum();
            _logLevel.intValue = (int)logLevel;

            EditorGUILayout.EndVertical();
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label(ModToolSettings.sharedAssets.Count + " Shared Assets");
                GUILayout.FlexibleSpace();

                if (GUILayout.Button("edit"))
                    AssetSelector.Open();
            }

            GUILayout.Space(2);

            EditorGUILayout.EndVertical();

            GUILayout.BeginVertical(EditorStyles.helpBox);

            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label(ModToolSettings.sharedPackages.Count + " Shared Packages");
                GUILayout.FlexibleSpace();

                if (GUILayout.Button("edit"))
                    PackageSelector.Open();
            }

            GUILayout.EndVertical();

            serializedObject.ApplyModifiedProperties();
        }        
    }
}
