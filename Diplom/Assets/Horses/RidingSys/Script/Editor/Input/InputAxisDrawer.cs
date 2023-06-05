using UnityEngine;
using UnityEditor;

namespace RidingSystem
{
    [CustomPropertyDrawer(typeof(InputAxis))]
    public class InputAxisDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            property.FindPropertyRelative("name").stringValue = label.text;

            var activeRect = new Rect(position.x, position.y, 15, position.height);
            var LabelRect = new Rect(position.x + 15, position.y, EditorGUIUtility.labelWidth - 15, position.height);
            var valueRect = new Rect(EditorGUIUtility.labelWidth + 15, position.y, position.width - EditorGUIUtility.labelWidth - 23, position.height);
            var RawRect = new Rect(position.width - 6, position.y, 20, position.height);


            EditorGUI.PropertyField(activeRect, property.FindPropertyRelative("active"), GUIContent.none);
            EditorGUI.LabelField(LabelRect, label);

            EditorGUI.PropertyField(valueRect, property.FindPropertyRelative("input"), GUIContent.none);

            var RawProp = property.FindPropertyRelative("raw");

            RawProp.boolValue = GUI.Toggle(RawRect, RawProp.boolValue, new GUIContent("R", "Use Raw Values for the Axis"), EditorStyles.miniButton);

            EditorGUI.indentLevel = indent;

            EditorGUI.EndProperty();
        }
    }
}