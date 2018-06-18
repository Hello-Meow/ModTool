using UnityEditor;
using ModTool.Shared;

namespace ModTool.Editor
{
    [CustomEditor(typeof(CodeSettings))]
    public class CodeSettingsEditor : UnityEditor.Editor
    {
        private SerializedProperty inheritanceRestrictions;
        private SerializedProperty memberRestrictions;
        private SerializedProperty typeRestrictions;
        private SerializedProperty namespaceRestrictions;
        private SerializedProperty apiAssemblies;

        void OnEnable()
        {
            inheritanceRestrictions = serializedObject.FindProperty("_inheritanceRestrictions");
            memberRestrictions = serializedObject.FindProperty("_memberRestrictions");
            typeRestrictions = serializedObject.FindProperty("_typeRestrictions");
            namespaceRestrictions = serializedObject.FindProperty("_namespaceRestrictions");
            apiAssemblies = serializedObject.FindProperty("_apiAssemblies");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            //Note: Bug in inspector does not indent list PropertyField
            //EditorGUI.indentLevel++;
            
            EditorGUILayout.PropertyField(inheritanceRestrictions, true);
            EditorGUILayout.PropertyField(memberRestrictions, true);
            EditorGUILayout.PropertyField(typeRestrictions, true);
            EditorGUILayout.PropertyField(namespaceRestrictions, true);
            EditorGUILayout.PropertyField(apiAssemblies, true);

            //EditorGUI.indentLevel--;

            EditorGUILayout.EndVertical();

            serializedObject.ApplyModifiedProperties();
        }
    }
}
