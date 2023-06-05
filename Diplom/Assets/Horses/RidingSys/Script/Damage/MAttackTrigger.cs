using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Events;
using System;
using System.Collections;
using RidingSystem.Utilities;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace RidingSystem.Controller
{
    public class MAttackTrigger : MDamager
    {
        [RequiredField]
        public Collider Trigger;

        protected TriggerProxy Proxy { get; private set; }

        public StatModifier SelfStatEnter;

        public StatModifier SelfStatExit;

        public StatModifier EnemyStatExit;

        public UnityEvent OnAttackBegin = new UnityEvent();
        public UnityEvent OnAttackEnd = new UnityEvent();

        public Color DebugColor = new Color(1, 0.25f, 0, 0.15f);

        private IMDamage damagee;

        [HideInInspector] public int Editor_Tabs1;

        private void Awake()
        {
            this.Delay_Action(1, () => { if (animator) defaultAnimatorSpeed = animator.speed; });
            FindTrigger();
        }


        private void FindTrigger()
        {
            if (Owner == null)
                Owner = transform.root.gameObject;

            if (Trigger)
            {
                Proxy = TriggerProxy.CheckTriggerProxy(Trigger, Layer, TriggerInteraction, Owner.transform);
            }
            else
            {
                Debug.LogWarning($"Attack trigger {name} need a Collider", this);
            }
        }

        void OnEnable()
        {
            if (Trigger)
            {
                Trigger.enabled = Trigger.isTrigger = Proxy.Active = true;
            }

            Proxy.EnterTriggerInteraction += AttackTriggerEnter;
            Proxy.ExitTriggerInteraction += AttackTriggerExit;

            damagee = null;

            OnAttackBegin.Invoke();
        }

        void OnDisable()
        {
            if (Trigger)
            {
                Trigger.enabled = Proxy.Active = false;
            }

            Proxy.EnterTriggerInteraction -= AttackTriggerEnter;
            Proxy.ExitTriggerInteraction -= AttackTriggerExit;

            TryDamage(damagee);

            OnAttackEnd.Invoke();

            if (animator) animator.speed = defaultAnimatorSpeed;
            damagee = null;
        }

        private void AttackTriggerEnter(GameObject newGo, Collider other)
        {
            if (dontHitOwner && Owner != null && other.transform.IsChildOf(Owner.transform)) return;

            damagee = other.GetComponentInParent<IMDamage>(); 
            var center = Trigger.bounds.center;
            Direction = (other.bounds.center - center).normalized;

            TryPhysics(other.attachedRigidbody, other, center, Direction, Force);
            TryStopAnimator();
            TryDamage(damagee);  
        }

        private void AttackTriggerExit(GameObject newGo, Collider other)
        {
            if (dontHitOwner && Owner != null && other.transform.IsChildOf(Owner.transform)) return;

            TryDamage(other.GetComponentInParent<IMDamage>());
        }

        public override void DoDamage(bool value) => enabled = value;




#if UNITY_EDITOR
        protected override void Reset()
        {
            base.Reset();
            Trigger = this.FindComponent<Collider>();
            if (!Trigger) Trigger = gameObject.AddComponent<BoxCollider>();
            Trigger.isTrigger = true;
            enabled = false;
        }


        void OnDrawGizmos()
        {
            if (Trigger != null)
                MTools.DrawTriggers(transform, Trigger, DebugColor, false);
        }

        void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying)
                MTools.DrawTriggers(transform, Trigger, DebugColor, true);
        }
#endif
    }

#if UNITY_EDITOR


    [CustomEditor(typeof(MAttackTrigger)), CanEditMultipleObjects]
    public class MAttackTriggerEd : MDamagerEd
    {
        SerializedProperty Trigger, EnemyStatExit, DebugColor, OnAttackBegin, OnAttackEnd, Editor_Tabs1;
        protected string[] Tabs1 = new string[] { "General", "Extras"};


        private void OnEnable()
        {
            FindBaseProperties();

            Trigger = serializedObject.FindProperty("Trigger");

            EnemyStatExit = serializedObject.FindProperty("EnemyStatExit");

            DebugColor = serializedObject.FindProperty("DebugColor");

            OnAttackBegin = serializedObject.FindProperty("OnAttackBegin");
            OnAttackEnd = serializedObject.FindProperty("OnAttackEnd");
            Editor_Tabs1 = serializedObject.FindProperty("Editor_Tabs1");
        }


        protected override void DrawCustomEvents()
        {
            EditorGUILayout.PropertyField(OnAttackBegin);
            EditorGUILayout.PropertyField(OnAttackEnd);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawDescription("Attack Trigger Logic. Creates damage to the stats of any collider entering the trigger");

            Editor_Tabs1.intValue = GUILayout.Toolbar(Editor_Tabs1.intValue, Tabs1);

            int Selection = Editor_Tabs1.intValue;

            if (Selection == 0) DrawGeneral();
            else if (Selection == 1) DrawExtras();

            serializedObject.ApplyModifiedProperties();
        }

        protected override void DrawGeneral(bool drawbox = true)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.PropertyField(Trigger);
                    EditorGUILayout.PropertyField(DebugColor, GUIContent.none, GUILayout.Width(55));
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();
            base.DrawGeneral(true);
        }

        private void DrawExtras()
        {
            DrawPhysics();
            DrawMisc();
        }
    }
#endif
}

