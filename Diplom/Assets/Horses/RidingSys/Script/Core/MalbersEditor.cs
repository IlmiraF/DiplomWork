using UnityEngine;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.IO;

#if UNITY_EDITOR
using UnityEditor.Presets;
using UnityEditor;
#endif

namespace RidingSystem
{
#if UNITY_EDITOR
    public static class MalbersEditor
    {
        public static GUIStyle BoldFoldout
        {
            get
            {
                var boldFoldout = new GUIStyle(EditorStyles.foldout);
                boldFoldout.fontStyle = FontStyle.Bold;
                return boldFoldout;
            }
        }

        #region Styles      
        public static GUIStyle StyleGray => MTools.StyleGray;
        public static GUIStyle StyleBlue => MTools.StyleBlue;
        public static GUIStyle StyleGreen => MTools.StyleGreen;

        #endregion

        public static Preset GetPreset(string name)
        {
            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:" + typeof(Preset).Name);
            Preset[] a = new Preset[guids.Length];

            for (int i = 0; i < guids.Length; i++) 
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[i]);
                a[i] = UnityEditor.AssetDatabase.LoadAssetAtPath<Preset>(path);
            }
            var allInstances = a.ToList();

            return allInstances.Find(x => x.name == name);

        }


        public static bool CopyObjectSerialization(Object source, Object target)
        {
            Preset preset = new Preset(source);
            return preset.ApplyTo(target);
        }

        public static void CreatePresetAsset(Object source, string name)
        {
            Preset preset = new Preset(source);
            AssetDatabase.CreateAsset(preset, "Assets/" + name + ".preset");
        }

        public static string GetPropertyType(SerializedProperty property)
        {
            var type = property.type;
            var match = Regex.Match(type, @"PPtr<\$(.*?)>");
            if (match.Success)
                type = match.Groups[1].Value;
            return type;
        }

        public static System.Type[] GetTypesByName(string className)
        {
            List<System.Type> returnVal = new List<System.Type>();

            foreach (Assembly a in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                System.Type[] assemblyTypes = a.GetTypes();
                for (int j = 0; j < assemblyTypes.Length; j++)
                {
                    if (assemblyTypes[j].Name == className)
                    {
                        returnVal.Add(assemblyTypes[j]);
                    }
                }
            }

            return returnVal.ToArray();
        }

        public static System.Type GetTypeByName(string className)
        {
            return System.AppDomain.CurrentDomain.GetAssemblies().SelectMany(x => x.GetTypes()).FirstOrDefault(t => t.Name == className);
        }

        public static bool IsAssetAFolder(Object obj)
        {
            string path = "";

            if (obj == null)
            {
                return false;
            }

            path = AssetDatabase.GetAssetPath(obj.GetInstanceID());

            if (path.Length > 0)
            {
                if (Directory.Exists(path))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }

            return false;
        }

        public static void AddParametersOnAnimator(UnityEditor.Animations.AnimatorController AnimController, UnityEditor.Animations.AnimatorController Mounted)
        {
            AnimatorControllerParameter[] parameters = AnimController.parameters;
            AnimatorControllerParameter[] Mountedparameters = Mounted.parameters;

            foreach (var param in Mountedparameters)
            {
                if (!SearchParameter(parameters, param.name))
                {
                    AnimController.AddParameter(param);
                }
            }
        }

        public static bool SearchParameter(AnimatorControllerParameter[] parameters, string name)
        {
            foreach (AnimatorControllerParameter item in parameters)
            {
                if (item.name == name) return true;
            }
            return false;
        }

        public static void DrawHeader(string title)
        {
            var backgroundRect = GUILayoutUtility.GetRect(1f, 17f);

            var labelRect = backgroundRect;
            labelRect.xMin += 16f;
            labelRect.xMax -= 20f;

            var foldoutRect = backgroundRect;
            foldoutRect.y += 1f;
            foldoutRect.width = 13f;
            foldoutRect.height = 13f;

            backgroundRect.xMin = 0f;
            backgroundRect.width += 4f;

            float backgroundTint = EditorGUIUtility.isProSkin ? 0.1f : 1f;
            EditorGUI.DrawRect(backgroundRect, new Color(backgroundTint, backgroundTint, backgroundTint, 0.2f));

            EditorGUI.LabelField(labelRect, title, EditorStyles.boldLabel);
            EditorGUILayout.Space();
        }

        public static void DrawSplitter()
        {
            EditorGUILayout.Space();
            var rect = GUILayoutUtility.GetRect(1f, 1f);

            rect.xMin = 20f;
            rect.width += 4f;

            if (Event.current.type != EventType.Repaint)
                return;

            EditorGUI.DrawRect(rect, !EditorGUIUtility.isProSkin
                ? new Color(0.6f, 0.6f, 0.6f, 1.333f)
                : new Color(0.12f, 0.12f, 0.12f, 1.333f));
        }

        public static bool DrawHeaderFoldout(string title, bool state)
        {
            var backgroundRect = GUILayoutUtility.GetRect(1f, 17f);

            var labelRect = backgroundRect;
            labelRect.xMin += 16f;
            labelRect.xMax -= 20f;

            var foldoutRect = backgroundRect;
            foldoutRect.y += 1f;
            foldoutRect.width = 13f;
            foldoutRect.height = 13f;

            backgroundRect.xMin = 0f;
            backgroundRect.width += 4f;

            float backgroundTint = EditorGUIUtility.isProSkin ? 0.1f : 1f;
            EditorGUI.DrawRect(backgroundRect, new Color(backgroundTint, backgroundTint, backgroundTint, 0.2f));

            EditorGUI.LabelField(labelRect, title, EditorStyles.boldLabel);

            state = GUI.Toggle(foldoutRect, state, GUIContent.none, EditorStyles.foldout);

            var e = Event.current;
            if (e.type == EventType.MouseDown && backgroundRect.Contains(e.mousePosition) && e.button == 0)
            {
                state = !state;
                e.Use();
            }

            return state;
        }
        public static void DrawCross(Transform m_transform)
        {
            var gizmoSize = 0.25f;
            Gizmos.DrawLine(m_transform.position, m_transform.position + m_transform.TransformVector(m_transform.root.forward * gizmoSize / m_transform.localScale.z));
            Gizmos.DrawLine(m_transform.position, m_transform.position + m_transform.TransformVector(m_transform.root.forward * -gizmoSize / m_transform.localScale.z));
            Gizmos.DrawLine(m_transform.position, m_transform.position + m_transform.TransformVector(m_transform.root.up * gizmoSize / m_transform.localScale.y));
            Gizmos.DrawLine(m_transform.position, m_transform.position + m_transform.TransformVector(m_transform.root.up * -gizmoSize / m_transform.localScale.y));
            Gizmos.DrawLine(m_transform.position, m_transform.position + m_transform.TransformVector(m_transform.root.right * gizmoSize / m_transform.localScale.x));
            Gizmos.DrawLine(m_transform.position, m_transform.position + m_transform.TransformVector(m_transform.root.right * -gizmoSize / m_transform.localScale.x));
        }

        public static void DrawLineHelpBox()
        {
            DrawUILine(Color.black, 1, 10);
        }


        public static void DrawUILine(Color color, int thickness = 2, int padding = 10)
        {
            Rect r = EditorGUILayout.GetControlRect(GUILayout.Height(padding + thickness));
            r.height = thickness;
            r.y += padding / 2;
            r.x -= 2;
            r.width += 6;
            EditorGUI.DrawRect(r, color);
        }

        public static void DrawScript(MonoScript script)
        {
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.ObjectField("Script", script, typeof(MonoScript), false);
            EditorGUI.EndDisabledGroup();
        }

        public static void DrawEventConnection(Transform t, UnityEngine.Events.UnityEvent e, bool selected)
        {
            DrawEventConnection(t, e, selected, Color.white);
        }
        public static void DrawEventConnection(Transform t, UnityEngine.Events.UnityEvent e, bool selected, Color color)
        {
            for (int i = 0; i < e.GetPersistentEventCount(); i++)
            {
                var item = e.GetPersistentTarget(i);

                GameObject go = null;

                if (item is GameObject)
                {
                    go = (item as GameObject);
                }
                else if (item is Component)
                {
                    go = (item as Component).gameObject;
                }

                if (go != null && go != t.gameObject && !go.IsPrefab())
                {
                    DrawInteraction(t.position, go.transform.position, selected, color);
                }
            }
        }

        public static void DrawInteraction(Vector3 start, Vector3 end, bool selected, Color color)
        {
            if (end == start) return;

            Handles.color = selected ? color : new Color(color.r, color.g, color.b, 0.4f);

            if (selected)
            {
                Handles.DrawLine(start, end);
            }
            else
            {
                Handles.DrawDottedLine(start, end, 5);
            }
        }

        public static void DrawDescription(string v)
        {
            var styleDesc = new GUIStyle(StyleBlue)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                stretchWidth = true
            };

            styleDesc.normal.textColor = EditorStyles.label.normal.textColor;

            UnityEditor.EditorGUILayout.LabelField(v, styleDesc);
        }

        public static GUIContent DebugCont = new GUIContent((Texture)(AssetDatabase.LoadAssetAtPath("Assets/Malbers Animations/Common/Scripts/Editor/Icons/Debug_Icon.png", typeof(Texture))), "Debug");


        public static void DrawDebugIcon(SerializedProperty property)
        {
            var currentGUIColor = GUI.color;
            GUI.color = property.boolValue ? Color.red : currentGUIColor;
            property.boolValue = GUILayout.Toggle(property.boolValue, DebugCont, EditorStyles.miniButton, GUILayout.Width(25));
            GUI.color = currentGUIColor;
        }

        public static void DrawDebugIcon(Rect rect, SerializedProperty property)
        {
            var currentGUIColor = GUI.color;
            GUI.color = property.boolValue ? Color.red : currentGUIColor;
            property.boolValue = GUI.Toggle(rect, property.boolValue, DebugCont, EditorStyles.miniButton);
            GUI.color = currentGUIColor;
        }

        public static void DrawButtonHighlight(SerializedProperty property, GUIContent name, Color Highlight)
        {
            var currentGUIColor = GUI.color;
            GUI.color = property.boolValue ? Highlight : currentGUIColor;
            property.boolValue = GUILayout.Toggle(property.boolValue, name, EditorStyles.miniButton);
            GUI.color = currentGUIColor;
        }


        public static void BoolButton(SerializedProperty prop, GUIContent content)
        {
            prop.boolValue = GUILayout.Toggle(prop.boolValue, content, EditorStyles.miniButton);
        }

        public static void Arrays(SerializedProperty prop, GUIContent content = null)
        {
            EditorGUI.indentLevel++;
            if (content != null)
                EditorGUILayout.PropertyField(prop, content, true);
            else
                EditorGUILayout.PropertyField(prop, true);
            EditorGUI.indentLevel--;
        }

        public static bool Foldout(SerializedProperty prop, string name)
        {
            EditorGUI.indentLevel++;
            prop.boolValue = GUILayout.Toggle(prop.boolValue, name, EditorStyles.foldoutHeader);
            EditorGUI.indentLevel--;
            return prop.boolValue;
        }

        public static bool Foldout(bool prop, string name)
        {
            EditorGUI.indentLevel++;
            prop = GUILayout.Toggle(prop, name, EditorStyles.foldoutHeader);
            EditorGUI.indentLevel--;
            return prop;
        }
    }
#endif
}