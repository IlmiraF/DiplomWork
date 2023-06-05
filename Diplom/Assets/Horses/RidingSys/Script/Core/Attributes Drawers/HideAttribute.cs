using UnityEngine;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif


namespace RidingSystem
{

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property |
        AttributeTargets.Class | AttributeTargets.Struct, Inherited = true)]
    public sealed class HideAttribute : PropertyAttribute
    {
        public string ConditionalSourceField = "";
        public string ConditionalSourceField2 = "";
        public string[] ConditionalSourceFields = new string[] { };
        public bool[] ConditionalSourceFieldInverseBools = new bool[] { };
        public bool HideInInspector = false;
        public bool Inverse = false;
        public bool UseOrLogic = false;

        public bool InverseCondition1 = false;


        public HideAttribute(string conditionalSourceField)
        {
            this.ConditionalSourceField = conditionalSourceField;
            this.HideInInspector = false;
            this.Inverse = false;
        }

        public HideAttribute(string conditionalSourceField, bool hideInInspector)
        {
            this.ConditionalSourceField = conditionalSourceField;
            this.HideInInspector = hideInInspector;
            this.Inverse = false;
        }

        public HideAttribute(string conditionalSourceField, bool hideInInspector, bool inverse)
        {
            this.ConditionalSourceField = conditionalSourceField;
            this.HideInInspector = hideInInspector;
            this.Inverse = inverse;
        }
    }

#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(HideAttribute))]
    public class HidePropertyDrawer : PropertyDrawer
    {



        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            HideAttribute condHAtt = (HideAttribute)attribute;

            bool enabled = GetConditionalHideAttributeResult(condHAtt, property);

            bool wasEnabled = GUI.enabled;

            GUI.enabled = enabled;
            if (!condHAtt.HideInInspector || enabled)
            {
                EditorGUI.PropertyField(position, property, label, true);
            }

            GUI.enabled = wasEnabled;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            HideAttribute condHAtt = (HideAttribute)attribute;
            bool enabled = GetConditionalHideAttributeResult(condHAtt, property);

            if (!condHAtt.HideInInspector || enabled)
            {
                return EditorGUI.GetPropertyHeight(property, label);
            }
            else
            {
                return -EditorGUIUtility.standardVerticalSpacing;
            }
        }



        private bool GetConditionalHideAttributeResult(HideAttribute condHAtt, SerializedProperty property)
        {
            bool enabled = (condHAtt.UseOrLogic) ? false : true;

            SerializedProperty sourcePropertyValue = null;

            if (!property.isArray)
            {
                string propertyPath = property.propertyPath;
                string conditionPath = propertyPath.Replace(property.name, condHAtt.ConditionalSourceField);
                sourcePropertyValue = property.serializedObject.FindProperty(conditionPath);

                if (sourcePropertyValue == null)
                {
                    sourcePropertyValue = property.serializedObject.FindProperty(condHAtt.ConditionalSourceField);
                }
            }
            else
            {
                sourcePropertyValue = property.serializedObject.FindProperty(condHAtt.ConditionalSourceField);
            }


            if (sourcePropertyValue != null)
            {
                enabled = CheckPropertyType(sourcePropertyValue);
            }
            
            if (condHAtt.Inverse) enabled = !enabled;

            return enabled;
        }

        private bool CheckPropertyType(SerializedProperty sourcePropertyValue)
        {
            switch (sourcePropertyValue.propertyType)
            {
                case SerializedPropertyType.Boolean:
                    return sourcePropertyValue.boolValue;
                case SerializedPropertyType.ObjectReference:
                    return sourcePropertyValue.objectReferenceValue != null;
                default:
                    Debug.LogError("Data type of the property used for conditional hiding [" + sourcePropertyValue.propertyType + "] is currently not supported");
                    return true;
            }
        }
    }
#endif
}