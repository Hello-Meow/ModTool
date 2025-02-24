using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using ModTool.Shared.Verification;

namespace ModTool.Editor
{
    [CustomPropertyDrawer(typeof(Restriction), true)]
    internal class RestrictionDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            ClipLabel(position, label);

            position.height = 18;

            property.isExpanded = EditorGUI.Foldout(position, property.isExpanded, label, true);

            if (property.isExpanded)
                DrawChildren(position, property);
        }

        private void ClipLabel(Rect position, GUIContent label)
        {
            Vector2 labelSize = GUI.skin.label.CalcSize(label);

            float maxWidth = position.width - 50;

            if (maxWidth < 1)
                return;
            
            if (labelSize.x < maxWidth)
                return;

            string labelText = label.text;
            
            float r = maxWidth / labelSize.x;
            int length = Mathf.FloorToInt(labelText.Length * r);
            label.text = labelText.Substring(0, length) + "...";
        }

        private void DrawChildren(Rect position, SerializedProperty property)
        {
            EditorGUI.indentLevel++;

            var endProperty = property.GetEndProperty();

            while (property.NextVisible(true) && !SerializedProperty.EqualContents(property, endProperty))
            {
                position.y += position.height;
                position.height = EditorGUI.GetPropertyHeight(property);

                EditorGUI.PropertyField(position, property);                
            }

            EditorGUI.indentLevel--;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = 18;

            if (!property.isExpanded)
                return height;

            var endProperty = property.GetEndProperty();

            while (property.NextVisible(true) && !SerializedProperty.EqualContents(property, endProperty))
                height += EditorGUI.GetPropertyHeight(property);

            return height;
        }
    }
}
