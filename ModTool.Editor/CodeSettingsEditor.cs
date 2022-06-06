using UnityEngine;
using UnityEditor;
using ModTool.Shared;

using UnityEditorInternal;

namespace ModTool.Editor
{
    [CustomEditor(typeof(CodeSettings))]
    public class CodeSettingsEditor : UnityEditor.Editor
    {
        private SerializedProperty inheritanceRestrictions;
        private SerializedProperty memberRestrictions;
        private SerializedProperty typeRestrictions;
        private SerializedProperty namespaceRestrictions;

        void OnEnable()
        {
            inheritanceRestrictions = serializedObject.FindProperty("_inheritanceRestrictions");
            memberRestrictions = serializedObject.FindProperty("_memberRestrictions");
            typeRestrictions = serializedObject.FindProperty("_typeRestrictions");
            namespaceRestrictions = serializedObject.FindProperty("_namespaceRestrictions");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.ExpandHeight(true));

            EditorGUILayout.PropertyField(inheritanceRestrictions, true);
            EditorGUILayout.PropertyField(memberRestrictions, true);
            EditorGUILayout.PropertyField(typeRestrictions, true);
            EditorGUILayout.PropertyField(namespaceRestrictions, true);

            EditorGUILayout.EndVertical();

            serializedObject.ApplyModifiedProperties();
        }
    }
}
