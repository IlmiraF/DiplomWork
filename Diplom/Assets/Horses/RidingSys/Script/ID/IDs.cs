using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine.Serialization;
#if UNITY_EDITOR
using UnityEditor;
#endif


namespace RidingSystem
{
    public abstract class IDs : ScriptableObject
    {

        public string DisplayName;
        public int ID;

        public static implicit operator int(IDs reference) => reference != null ? reference.ID : 0;

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(DisplayName)) DisplayName = name;
        }
    }


#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(IDs), true)]
    public class IDDrawer : PropertyDrawer
    {
        private GUIStyle popupStyle;

        List<IDs> Instances;
        List<string> popupOptions;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (popupStyle == null)
            {
                popupStyle = new GUIStyle(GUI.skin.GetStyle("PaneOptions"));
                popupStyle.imagePosition = ImagePosition.ImageOnly;
            }

            label = EditorGUI.BeginProperty(position, label, property);

            if (property.objectReferenceValue)
            {
                label.tooltip += $"\n ID Value: [{(property.objectReferenceValue as IDs).ID}]";
                if (label.text.Contains("Element")) label.text = property.objectReferenceValue.GetType().Name;
            }
            position = EditorGUI.PrefixLabel(position, label);

            EditorGUI.BeginChangeCheck();

            float height = EditorGUIUtility.singleLineHeight;


            Rect buttonRect = new Rect(position);
            buttonRect.yMin += popupStyle.margin.top;
            buttonRect.width = popupStyle.fixedWidth + popupStyle.margin.right;
            buttonRect.x -= 20;
            buttonRect.height = height;

            int indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;


            if (Instances == null || Instances.Count == 0)
            {
                var NameOfType = GetPropertyType(property);
                string[] guids = AssetDatabase.FindAssets("t:" + NameOfType);

                Instances = new List<IDs>();
                popupOptions = new List<string>();
                popupOptions.Add("None");

                for (int i = 0; i < guids.Length; i++)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                    var inst = AssetDatabase.LoadAssetAtPath<IDs>(path);
                    Instances.Add(inst);
                }

                Instances = Instances.OrderBy(x => x.ID).ToList();

                for (int i = 0; i < Instances.Count; i++)
                {
                    var inst = Instances[i];
                    var displayname = inst.name;
                    var idString = "[" + Instances[i].ID.ToString() + "] ";

                    if (Instances[i] is Tag) idString = ""; 

                    if (!string.IsNullOrEmpty(inst.DisplayName))
                    {
                        displayname = inst.DisplayName;
                        int pos = displayname.LastIndexOf("/") + 1;
                        displayname = displayname.Insert(pos, idString);
                    }
                    else
                    {
                        displayname = idString + displayname;
                    }

                    popupOptions.Add(displayname);
                }
            }
            var PropertyValue = property.objectReferenceValue;

            int result = 0;

            if (PropertyValue != null && Instances.Count > 0)
            {
                result = Instances.FindIndex(i => i.name == PropertyValue.name) + 1;
            }



            result = EditorGUI.Popup(buttonRect, result, popupOptions.ToArray(), popupStyle);

            if (result == 0)
            {
                property.objectReferenceValue = null;
            }
            else
            {
                var NewSelection = Instances[result - 1];
                property.objectReferenceValue = NewSelection;

            }

            position.height = EditorGUIUtility.singleLineHeight;


            EditorGUI.PropertyField(position, property, GUIContent.none, false);

            if (EditorGUI.EndChangeCheck())
                property.serializedObject.ApplyModifiedProperties();

            EditorGUI.indentLevel = indent;
            EditorGUI.EndProperty();
        }

        public static string GetPropertyType(SerializedProperty property)
        {
            var type = property.type;
            var match = Regex.Match(type, @"PPtr<\$(.*?)>");
            if (match.Success)
                type = match.Groups[1].Value;
            return type;
        }
    }
#endif
}