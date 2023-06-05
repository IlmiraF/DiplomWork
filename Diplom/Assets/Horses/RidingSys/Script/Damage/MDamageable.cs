using RidingSystem.Controller;
using RidingSystem.Controller.Reactions;
using RidingSystem.Events;
using RidingSystem.Scriptables;
using UnityEngine;
using UnityEngine.Events;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace RidingSystem
{
    [DisallowMultipleComponent]
    public class MDamageable : MonoBehaviour, IMDamage
    {
        public MReaction reaction;

        public MDamageable Root;

        public Vector3 HitDirection { get; set; }
        public GameObject Damager { get; set; }
        public GameObject Damagee => gameObject;

        private Component character;

        private void Start()
        {
            character = GetComponent(reaction.ReactionType());
        }


        public virtual void ReceiveDamage(Vector3 Direction, GameObject Damager, bool react)
        {
            if (!enabled) return;

            SetDamageable(Direction, Damager);
            Root?.SetDamageable(Direction, Damager);
            
            if (react && reaction) reaction.React(character);
        }

        internal void SetDamageable(Vector3 Direction, GameObject Damager)
        {
            HitDirection = Direction;
            this.Damager = Damager;
        }


#if UNITY_EDITOR
        private void Reset()
        {
            reaction = MTools.GetInstance<ModeReaction>("Damaged");
            Root = transform.root.GetComponent<MDamageable>();
            if (Root == this) Root = null;
        }
#endif
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(MDamageable))]
    public class MDamageableEditor : Editor
    {
        SerializedProperty reaction, Root;
        MDamageable M;


        private void OnEnable()
        {
            M = (MDamageable)target;

            reaction = serializedObject.FindProperty("reaction");
            Root = serializedObject.FindProperty("Root");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.BeginVertical(MalbersEditor.StyleGray);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            if (M.transform.parent != null)
                EditorGUILayout.PropertyField(Root);
            EditorGUILayout.PropertyField(reaction);
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndVertical();
            serializedObject.ApplyModifiedProperties();
        }
    }
#endif
}