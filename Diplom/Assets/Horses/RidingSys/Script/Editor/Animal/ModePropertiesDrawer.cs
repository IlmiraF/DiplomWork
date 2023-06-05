using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace RidingSystem.Controller
{
    [CustomPropertyDrawer(typeof(ModeProperties))]
    public class ModePropertiesDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var indent = EditorGUI.indentLevel;

            var Light = new Color(0.6f, 0.6f, 0.6f, 0.333f);
            var Dark = new Color(0.12f, 0.12f, 0.12f, 0.333f);

            EditorGUI.indentLevel = 0;


            #region Serialized Properties
            var affect = property.FindPropertyRelative("affect");
            var affectStates = property.FindPropertyRelative("affectStates");
            var affectSt = property.FindPropertyRelative("affect_Stance");
            var Stances = property.FindPropertyRelative("Stances");
            var TransitionFrom = property.FindPropertyRelative("TransitionFrom");

            #endregion

            var height = EditorGUIUtility.singleLineHeight;
            var line = new Rect(position);
            line.height = height;

            line.x += 4;
            line.width -= 8;

            line.y += 2;
            EditorGUI.PropertyField(line, affect, new GUIContent("Affect States (" + affectStates.arraySize + ")"));
            line.y += height + 2;

            if (affect.intValue != 0)
            {
                EditorGUI.indentLevel++;
                EditorGUI.PropertyField(line, affectStates, new GUIContent("States"), true);
                EditorGUI.indentLevel--;

                line.y += EditorGUI.GetPropertyHeight(affectStates);
            }

            line.y += 2;
            EditorGUI.PropertyField(line, affectSt, new GUIContent("Affect Stances (" + Stances.arraySize + ")"));
            line.y += height + 2;

            if (affectSt.intValue != 0)
            {
                EditorGUI.indentLevel++;
                EditorGUI.PropertyField(line, Stances, true);
                EditorGUI.indentLevel--;

                line.y += EditorGUI.GetPropertyHeight(Stances); ;
            }

            float TransitionfromHeight = EditorGUI.GetPropertyHeight(TransitionFrom);


            EditorGUI.indentLevel++;
            EditorGUI.PropertyField(line, TransitionFrom, new GUIContent("Can Transition from Ability (" + TransitionFrom.arraySize + ")"), true);
            EditorGUI.indentLevel--;

            line.y += TransitionfromHeight;

            EditorGUI.indentLevel = indent;

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var TransitionFrom = property.FindPropertyRelative("TransitionFrom");

            var affect = property.FindPropertyRelative("affect");
            var affectSt = property.FindPropertyRelative("affect_Stance");

            var Stances = property.FindPropertyRelative("Stances");
            var affectStates = property.FindPropertyRelative("affectStates");

            var height = EditorGUIUtility.singleLineHeight;

            float TotalHeight = (height + 2) * 2 + 4;

            if (affect.intValue != 0) TotalHeight += EditorGUI.GetPropertyHeight(affectStates);
            if (affectSt.intValue != 0) TotalHeight += EditorGUI.GetPropertyHeight(Stances);


            TotalHeight += EditorGUI.GetPropertyHeight(TransitionFrom);

            return TotalHeight;
        }
    }
}