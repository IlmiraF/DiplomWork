using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace RidingSystem
{
    public class MinMaxRangeAttribute : Attribute
    {
        public MinMaxRangeAttribute(float min, float max)
        {
            Min = min;
            Max = max;
        }

        public float Min { get; private set; }
        public float Max { get; private set; }

    }

    [System.AttributeUsage(System.AttributeTargets.Field, Inherited = true, AllowMultiple = true)]
    public sealed class LineAttribute : Attribute
    {
        public readonly float height;

        public LineAttribute()
        {
            this.height = 8;
        }

        public LineAttribute(float height) { this.height = height; }
    }

#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(RangedFloat), true)]
    public class RangedFloatDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            label = EditorGUI.BeginProperty(position, label, property);
            position = EditorGUI.PrefixLabel(position, label);

            SerializedProperty minProp = property.FindPropertyRelative("minValue");
            SerializedProperty maxProp = property.FindPropertyRelative("maxValue");

            int indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            var ranges = (MinMaxRangeAttribute[])fieldInfo.GetCustomAttributes(typeof(MinMaxRangeAttribute), true);

            if (ranges.Length > 0)
            {
                float rangeMin = ranges[0].Min;
                float rangeMax = ranges[0].Max;
                const float rangeBoundsLabelWidth = 50;

                var minRect = new Rect(position);
                minRect.width = rangeBoundsLabelWidth;

                minProp.floatValue = EditorGUI.FloatField(minRect, GUIContent.none, minProp.floatValue);

                position.xMin += rangeBoundsLabelWidth + 3;

                var rangeBoundsLabel2Rect = new Rect(position);
                rangeBoundsLabel2Rect.xMin = rangeBoundsLabel2Rect.xMax - rangeBoundsLabelWidth;
                maxProp.floatValue = EditorGUI.FloatField(rangeBoundsLabel2Rect, GUIContent.none, maxProp.floatValue);

                float minValue = minProp.floatValue;
                float maxValue = maxProp.floatValue;


                position.xMax -= rangeBoundsLabelWidth + 3;

                EditorGUI.BeginChangeCheck();


                EditorGUI.MinMaxSlider(position, ref minValue, ref maxValue, rangeMin, rangeMax);
                if (EditorGUI.EndChangeCheck())
                {
                    minProp.floatValue = (float)Mathf.Round(minValue * 100f) / 100f; ;
                    maxProp.floatValue = (float)Mathf.Round(maxValue * 100f) / 100f; ;
                }
            }
            else
            {
                var half = position.width / 2;
                var minRect = new Rect(position);
                minRect.width = half - 4;
                var MaxRect = new Rect(position);
                MaxRect.x += half;
                MaxRect.width = half;

                EditorGUIUtility.labelWidth = 28;
                minProp.floatValue = EditorGUI.FloatField(minRect, new GUIContent("Min"), minProp.floatValue);
                EditorGUIUtility.labelWidth = 30;
                maxProp.floatValue = EditorGUI.FloatField(MaxRect, new GUIContent("Max"), maxProp.floatValue);
                EditorGUIUtility.labelWidth = 0;
            }

            EditorGUI.indentLevel = indent;
            EditorGUI.EndProperty();
        }
    }
#endif
}