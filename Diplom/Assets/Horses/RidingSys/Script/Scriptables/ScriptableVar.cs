using UnityEngine;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif


namespace RidingSystem.Scriptables
{
    public abstract class ScriptableVar : ScriptableObject
    {
#if UNITY_EDITOR
        [TextArea(3, 20)]
        public string Description = "";
        [HideInInspector] public bool debug = false;
#endif
    }





#if UNITY_EDITOR
    public class VariableEditor : Editor
    {
        public static GUIStyle StyleBlue => MTools.Style(new Color(0, 0.5f, 1f, 0.3f));

        protected SerializedProperty value, Description, debug;

        private void OnEnable()
        {
            value = serializedObject.FindProperty("value");
            Description = serializedObject.FindProperty("Description");
            debug = serializedObject.FindProperty("debug");
        }

        public virtual void PaintInspectorGUI(string title)
        {
            serializedObject.Update();
            MalbersEditor.DrawDescription(title);


            using (new GUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new GUILayout.HorizontalScope())
                {
                    EditorGUILayout.PropertyField(value, new GUIContent("Value", "The current value"));
                    MalbersEditor.DrawDebugIcon(debug);
                }
                EditorGUILayout.PropertyField(Description);
            }
            serializedObject.ApplyModifiedProperties();
        }
    }

    [CanEditMultipleObjects, CustomEditor(typeof(FloatVar))]
    public class FloatVarEditor : VariableEditor
    {
        public override void OnInspectorGUI() => PaintInspectorGUI("Float Variable");
    }

    [CanEditMultipleObjects, CustomEditor(typeof(BoolVar))]
    public class BoolVarEditor : VariableEditor
    {
        public override void OnInspectorGUI() => PaintInspectorGUI("Bool Variable");
    }

    [CanEditMultipleObjects, CustomEditor(typeof(Vector3Var))]
    public class Vector3VarEditor : VariableEditor
    {
        public override void OnInspectorGUI() => PaintInspectorGUI("Vector3 Variable");
    }

    [CanEditMultipleObjects, CustomEditor(typeof(Vector2Var))]
    public class Vector2VarEditor : VariableEditor
    {
        public override void OnInspectorGUI() => PaintInspectorGUI("Vector2 Variable");
    }

    [CanEditMultipleObjects, CustomEditor(typeof(GameObjectVar))]
    public class GameObjectVarEditor : VariableEditor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            MalbersEditor.DrawDescription("GameObject/Prefab Variable");
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            var go = value.objectReferenceValue as GameObject;

            EditorGUILayout.BeginHorizontal();

            if (go == null || go.IsPrefab())
            {
                EditorGUILayout.PropertyField(value, new GUIContent("Prefab", "The current value"));
            }
            else
            {
                if (Application.isPlaying)
                {
                    EditorGUILayout.ObjectField("Value ", go, typeof(GameObject), false);
                }
            }

            MalbersEditor.DrawDebugIcon(debug);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.PropertyField(Description);
            EditorGUILayout.EndVertical();

            serializedObject.ApplyModifiedProperties();
        }
    }

    [CanEditMultipleObjects, CustomEditor(typeof(TransformVar))]
    public class TransformVarEditor : VariableEditor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.BeginHorizontal();
            MalbersEditor.DrawDescription("Runtime Transform Reference");
            MalbersEditor.DrawDebugIcon(debug);
            EditorGUILayout.EndHorizontal();
            var go = value.objectReferenceValue as Transform;

            if (Application.isPlaying)
            {
                EditorGUILayout.ObjectField("Value ", go, typeof(GameObject), false);
            }
            else
            {
                EditorGUILayout.LabelField("<Runtime Only>", EditorStyles.boldLabel);
            }


            EditorGUILayout.PropertyField(Description);


            serializedObject.ApplyModifiedProperties();
        }
    }


    [CanEditMultipleObjects, CustomEditor(typeof(IntVar))]
    public class IntVarEditor : VariableEditor
    {
        public override void OnInspectorGUI() { PaintInspectorGUI("Int Variable"); }
    }

    [CanEditMultipleObjects, CustomEditor(typeof(ColorVar))]
    public class ColorVarEditor : VariableEditor
    {
        public override void OnInspectorGUI() { PaintInspectorGUI("Color Variable"); }
    }

    [CanEditMultipleObjects, CustomEditor(typeof(LayerVar))]
    public class LayerVarEditor : VariableEditor
    {
        public override void OnInspectorGUI() { PaintInspectorGUI("LayerMask Variable"); }
    }
#endif
}