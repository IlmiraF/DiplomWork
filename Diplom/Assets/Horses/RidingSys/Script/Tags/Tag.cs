using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif


namespace RidingSystem
{
    [CreateAssetMenu(menuName = "RidingSystem/Tag", fileName = "New Tag", order = 3000)]
    public class Tag : IDs
    {
        private void OnEnable() => ID = name.GetHashCode();
    }
#if UNITY_EDITOR

    [CustomEditor(typeof(Tag))]
    public class TagEd : Editor
    {
        SerializedProperty ID, DisplayName;

        private void OnEnable()
        {
            ID = serializedObject.FindProperty("ID");
            DisplayName = serializedObject.FindProperty("DisplayName");
            ID.intValue = target.name.GetHashCode();
            serializedObject.ApplyModifiedProperties();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.HelpBox("Tags ID are calculated using GetHashCode()", MessageType.None);
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.PropertyField(ID);
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.PropertyField(DisplayName);
            serializedObject.ApplyModifiedProperties();

        }
    }
#endif
}