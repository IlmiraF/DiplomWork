using RidingSystem.Events;
using RidingSystem.Scriptables;
using UnityEngine;
using RidingSystem.Utilities;
using System.Collections;
using RidingSystem.Controller.Reactions;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace RidingSystem.Controller
{
    public abstract class MDamager : MonoBehaviour, IMDamager
    {
        #region Public Variables
        [SerializeField]
        protected int index = 1;

        [SerializeField]
        protected BoolReference m_Active = new BoolReference(true);

        [SerializeField, ContextMenuItem("Get Layer from Root", "GetLayerFromRoot")]
        protected LayerReference m_hitLayer = new LayerReference(-1);

        [SerializeField]
        protected QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Ignore;

        [SerializeField]
        [ContextMenuItem("Find Owner", "Find_Owner")]
        protected GameObject owner;

        public BoolReference dontHitOwner = new BoolReference(true);

        [CreateScriptableAsset] public MReaction CustomReaction;

        public virtual Transform IgnoreTransform { get; set; }

        public BoolReference interact = new BoolReference(true);

        public BoolReference react = new BoolReference(true);

        [SerializeField]
        protected FloatReference m_Force = new FloatReference(50f);

        public ForceMode forceMode = ForceMode.Force;
        protected Vector3 Direction = Vector3.forward;

        public TransformEvent OnHit = new TransformEvent();
        public Vector3Event OnHitPosition = new Vector3Event();
        public IntEvent OnHitInteractable = new IntEvent();


        [ContextMenuItem("Find Animator", "Find_Animator")]
        public Animator animator;
        public FloatReference AnimatorSpeed = new FloatReference(0.1f);
        public FloatReference AnimatorStopTime = new FloatReference(0.2f);

        #endregion

        #region Properties
        public virtual GameObject Owner { get => owner; set => owner = value; }

        public float Force { get => m_Force; set => m_Force = value; }


        public LayerMask Layer { get => m_hitLayer.Value; set => m_hitLayer.Value = value; }
        public QueryTriggerInteraction TriggerInteraction { get => triggerInteraction; set => triggerInteraction = value; }

        public bool debug;

        public virtual int Index => index;

        public virtual bool Active
        {
            get => m_Active.Value;
            set => m_Active.Value = enabled = value;
        }


        public Vector3 HitPosition { get; private set; }

        public Quaternion HitRotation { get; private set; }
        #endregion

        public virtual bool IsInvalid(Collider damagee)
        {
            if (damagee.isTrigger && TriggerInteraction == QueryTriggerInteraction.Ignore) return true;
            if (!MTools.Layer_in_LayerMask(damagee.gameObject.layer, Layer)) { return true; }
            if (dontHitOwner && Owner != null && damagee.transform.IsChildOf(Owner.transform)) { return true; }
            return false;
        }

        protected virtual bool TryDamage(IMDamage damagee)
        {
            if (damagee != null)
            {
                damagee.ReceiveDamage(Direction, Owner, react.Value);

                return true;
            }
            return false;
        }

        protected virtual bool TryDamage(GameObject other) => TryDamage(other.FindInterface<IMDamage>());

        public virtual void DoDamage(bool value) { }


        protected void TryStopAnimator()
        {
            if (animator && C_StopAnim == null)
            {
                C_StopAnim = C_StopAnimator();
                StartCoroutine(C_StopAnim);
            }
        }

        protected IEnumerator C_StopAnim;
        protected float defaultAnimatorSpeed = 1;

        protected IEnumerator C_StopAnimator()
        {
            animator.speed = AnimatorSpeed;
            yield return new WaitForSeconds(AnimatorStopTime.Value);
            animator.speed = defaultAnimatorSpeed;

            C_StopAnim = null;
        }

        public virtual void Restart() { }

        protected virtual bool TryPhysics(Rigidbody rb, Collider col, Vector3 Origin, Vector3 Direction, float force)
        {
            if (rb && force > 0)
            {
                if (col)
                {
                    var HitPoint = col.ClosestPoint(Origin);
                    rb.AddForceAtPosition(Direction * force, HitPoint, forceMode);

                    MTools.DrawWireSphere(HitPoint, Color.red, 0.1f, 2f);
                    MTools.Draw_Arrow(HitPoint, Direction * force, Color.red, 2f);

                }
                else
                    rb.AddForce(Direction * force, forceMode);

                return true;
            }
            return false;
        }

        public void SetOwner(GameObject owner) => Owner = owner;
        public void SetOwner(Transform owner) => Owner = owner.gameObject;

        protected void Find_Owner()
        {
            if (Owner == null) Owner = transform.root.gameObject;
            MTools.SetDirty(this);
        }

        protected void Find_Animator()
        {
            if (animator == null) animator = gameObject.FindComponent<Animator>();
            MTools.SetDirty(this);
        }


#if UNITY_EDITOR
        protected virtual void Reset()
        {
            m_hitLayer.Variable = MTools.GetInstance<LayerVar>("Hit Layer");
            m_hitLayer.UseConstant = false;

            owner = transform.root.gameObject;
        }
#endif
    }


    ///--------------------------------INSPECTOR-------------------
    ///
#if UNITY_EDITOR
    [CustomEditor(typeof(MDamager)), CanEditMultipleObjects]
    public class MDamagerEd : Editor
    {
        public static GUIStyle StyleBlue => MTools.Style(new Color(0, 0.5f, 1f, 0.3f));

        protected MonoScript script;
        protected MDamager MD;
        protected SerializedProperty Force, forceMode, index, onhit, OnHitPosition, OnHitInteractable, dontHitOwner, owner, m_Active, debug,
            hitLayer, triggerInteraction, react, CustomReaction, interact, m_HitEffect, interactorID, DestroyHitEffect,
            StopAnimator, AnimatorSpeed, AnimatorStopTime, animator;


        private void OnEnable() => FindBaseProperties();

        protected virtual void FindBaseProperties()
        {
            script = MonoScript.FromMonoBehaviour((MonoBehaviour)target);
            MD = (MDamager)target;
            index = serializedObject.FindProperty("index");
            OnHitPosition = serializedObject.FindProperty("OnHitPosition");
            m_Active = serializedObject.FindProperty("m_Active");
            hitLayer = serializedObject.FindProperty("m_hitLayer");
            triggerInteraction = serializedObject.FindProperty("triggerInteraction");
            dontHitOwner = serializedObject.FindProperty("dontHitOwner");
            owner = serializedObject.FindProperty("owner");


            react = serializedObject.FindProperty("react");
            CustomReaction = serializedObject.FindProperty("CustomReaction");

            interact = serializedObject.FindProperty("interact");

            Force = serializedObject.FindProperty("m_Force");
            forceMode = serializedObject.FindProperty("forceMode");

            debug = serializedObject.FindProperty("debug");


            StopAnimator = serializedObject.FindProperty("StopAnimator");
            animator = serializedObject.FindProperty("animator");
            AnimatorSpeed = serializedObject.FindProperty("AnimatorSpeed");
            AnimatorStopTime = serializedObject.FindProperty("AnimatorStopTime");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawDescription("Damager Core Logic");
            DrawGeneral();
            DrawPhysics();
            DrawMisc();
            serializedObject.ApplyModifiedProperties();
        }

        protected virtual void DrawCustomEvents() { }


        protected virtual void DrawMisc(bool drawbox = true)
        {
            if (drawbox) EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            AnimatorStopTime.isExpanded = MalbersEditor.Foldout(AnimatorStopTime.isExpanded, "Stop Animator");

            if (AnimatorStopTime.isExpanded)
            {
                EditorGUILayout.PropertyField(AnimatorStopTime);

                if (MD.AnimatorStopTime.Value > 0)
                {
                    EditorGUILayout.PropertyField(AnimatorSpeed);
                    EditorGUILayout.PropertyField(animator);
                }
            }


            if (drawbox) EditorGUILayout.EndVertical();
        }

        protected virtual void DrawGeneral(bool drawbox = true)
        {
            if (drawbox) EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(m_Active);
            MalbersEditor.DrawDebugIcon(debug);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.PropertyField(index);
            EditorGUILayout.PropertyField(hitLayer);
            EditorGUILayout.PropertyField(triggerInteraction);

            EditorGUILayout.PropertyField(dontHitOwner, new GUIContent("Don't hit Owner"));
            if (MD.dontHitOwner.Value)
            {
                EditorGUILayout.PropertyField(owner);
            }

            if (drawbox) EditorGUILayout.EndVertical();
        }

        protected virtual void DrawPhysics(bool drawbox = true)
        {
            if (drawbox) EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            Force.isExpanded = MalbersEditor.Foldout(Force.isExpanded, "Physics");

            if (Force.isExpanded)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(Force);
                EditorGUILayout.PropertyField(forceMode, GUIContent.none, GUILayout.MaxWidth(90), GUILayout.MinWidth(20));
                EditorGUILayout.EndHorizontal();
            }
            if (drawbox) EditorGUILayout.EndVertical();
        }


        protected void DrawDescription(string desc) => MalbersEditor.DrawDescription(desc);
    }
#endif
}