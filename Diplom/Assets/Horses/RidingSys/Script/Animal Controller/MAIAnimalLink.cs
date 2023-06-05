using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using RidingSystem.Controller.Reactions;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif
namespace RidingSystem.Controller.AI
{
    [AddComponentMenu("RidingSystem/AI/AI Animal Link")]
    public class MAIAnimalLink : MonoBehaviour
    {
        public static List<MAIAnimalLink> OffMeshLinks;
        public bool BiDirectional = true;

        public Transform Start;
        public Transform End;

        public MReaction StartReaction;
        public MReaction EndReaction;
        public Color DebugColor = Color.yellow;


        public float StoppingDistance = 1f;
        public float SlowingDistance = 1f;
        public float SlowingLimit = 0.3f;

        public bool AlignToLink = true;
        public float AlignTime = 0.2f;

        public bool UseInputAxis;
        public Vector2 Axis = Vector2.up;

        public bool debug = true;

        protected virtual void OnEnable()
        {
            if (OffMeshLinks == null) OffMeshLinks = new List<MAIAnimalLink>();
            OffMeshLinks.Add(this);
        }

        protected virtual void OnDisable()
        {
            OffMeshLinks.Remove(this);
        }


        private void Reset()
        {
            var offMeshLink = GetComponent<OffMeshLink>();

            if (offMeshLink)
            {
                Start = offMeshLink.startTransform;
                End = offMeshLink.endTransform;
                BiDirectional = offMeshLink.biDirectional;
            }
            else
            {
                Start = transform;
            }
        }


        internal void Execute(IAIControl ai, MAnimal animal)
        {
            var NearLink = ai.Transform.NearestTransform(Start, End);
            var FarLink = NearLink == Start ? End : Start;


            var axis = Axis;
            if (BiDirectional && NearLink == End) axis *= -1;


            ai.AIDirection = (FarLink.position - NearLink.position).normalized;

            animal.StartCoroutine(OffMeshMove(ai, animal, NearLink, FarLink, axis));
        }

        public IEnumerator Coroutine_Execute(IAIControl ai, MAnimal animal)
        {
            var NearLink = ai.Transform.NearestTransform(Start, End);
            var FarLink = NearLink == Start ? End : Start;
            var axis = Axis;
            if (BiDirectional && NearLink == End) axis *= -1;
            ai.AIDirection = (FarLink.position - NearLink.position).normalized;

            yield return OffMeshMove(ai, animal, NearLink, FarLink, axis);
        }

        private IEnumerator OffMeshMove(IAIControl ai, MAnimal animal, Transform NearLink, Transform EndLink, Vector2 NewAxis)
        {
            if (AlignToLink)
            {
                yield return MTools.AlignTransform_Rotation(animal.transform, NearLink.rotation, AlignTime);
            }

            StartReaction?.React(animal);

            ai.InOffMeshLink = true;
            var AIDirection = (EndLink.position - animal.transform.position).normalized;

            RemainingDistance = float.MaxValue;

            while (RemainingDistance >= StoppingDistance && ai.InOffMeshLink)
            {
                MTools.DrawWireSphere(EndLink.position, DebugColor, StoppingDistance);
                MTools.DrawWireSphere(EndLink.position, Color.cyan, SlowingDistance);

                if (!UseInputAxis)
                {
                    ai.AIDirection = (AIDirection);
                    animal.Move(AIDirection * SlowMultiplier);
                }
                else
                {
                    animal.SetInputAxis(NewAxis * SlowMultiplier);
                    animal.UsingMoveWithDirection = false;
                }

                RemainingDistance = Vector3.Distance(animal.transform.position, EndLink.position);
                yield return null;
            }

            if (ai.InOffMeshLink)
                EndReaction?.React(animal);

            ai.CompleteOffMeshLink();
        }


        public float SlowMultiplier
        {
            get
            {
                var result = 1f;
                if (SlowingDistance > StoppingDistance && RemainingDistance < SlowingDistance)
                    result = Mathf.Max(RemainingDistance / SlowingDistance, SlowingLimit);
                return result;
            }
        }

        public float RemainingDistance { get; private set; }


#if UNITY_EDITOR

        private void OnDrawGizmos()
        {
            Gizmos.color = DebugColor;
            Handles.color = DebugColor;

            var AxisSize = transform.lossyScale.y;

            if (Start)
            {
                Gizmos.DrawSphere(Start.position, 0.2f * AxisSize);
                Handles.ArrowHandleCap(0, Start.position, Start.rotation, AxisSize, EventType.Repaint);
            }
            if (End)
            {
                Gizmos.DrawSphere(End.position, 0.2f * AxisSize);
                Handles.ArrowHandleCap(0, End.position, End.rotation, AxisSize, EventType.Repaint);

            }
            if (Start && End)
                Handles.DrawDottedLine(Start.position, End.position, 5);
        }

        private void OnDrawGizmosSelected()
        {
            if (Start)
            {
                Gizmos.color = DebugColor;
                Gizmos.DrawWireSphere(Start.position, 0.2f * transform.lossyScale.y);
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(Start.position, StoppingDistance);
                if (StoppingDistance < SlowingDistance)
                {
                    Gizmos.color = Color.cyan;
                    Gizmos.DrawWireSphere(Start.position, SlowingDistance);
                }
            }
            if (End)
            {
                Gizmos.color = DebugColor;
                Gizmos.DrawWireSphere(End.position, 0.2f * transform.lossyScale.y);
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(End.position, StoppingDistance);
                if (StoppingDistance < SlowingDistance)
                {
                    Gizmos.color = Color.cyan;
                    Gizmos.DrawWireSphere(End.position, SlowingDistance);
                }
            }
        }
#endif


#if UNITY_EDITOR
        [CustomEditor(typeof(MAIAnimalLink))]
        public class MAILinkEditor : Editor
        {
            SerializedProperty StartReaction, EndReaction, Start, End, DebugColor, UseInputAxis, Axis, AlignToLink, AlignTime, debug,
                StoppingDistance, SlowingLimit, SlowingDistance, BiDirectional;

            MAIAnimalLink M;

            private void OnEnable()
            {
                M = (MAIAnimalLink)target;
                StartReaction = serializedObject.FindProperty("StartReaction");
                debug = serializedObject.FindProperty("debug");
                EndReaction = serializedObject.FindProperty("EndReaction");
                Start = serializedObject.FindProperty("Start");
                End = serializedObject.FindProperty("End");
                StoppingDistance = serializedObject.FindProperty("StoppingDistance");
                SlowingLimit = serializedObject.FindProperty("SlowingLimit");
                SlowingDistance = serializedObject.FindProperty("SlowingDistance");
                DebugColor = serializedObject.FindProperty("DebugColor");
                UseInputAxis = serializedObject.FindProperty("UseInputAxis");
                Axis = serializedObject.FindProperty("Axis");
                BiDirectional = serializedObject.FindProperty("BiDirectional");
                AlignToLink = serializedObject.FindProperty("AlignToLink");
                AlignTime = serializedObject.FindProperty("AlignTime");
            }
            public override void OnInspectorGUI()
            {
                serializedObject.Update();

                MalbersEditor.DrawDescription("Uses Animal reactions to move the Agent when its at a OffMeshLinks");

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(StoppingDistance);
                EditorGUILayout.PropertyField(DebugColor, GUIContent.none, GUILayout.Width(50));
                MalbersEditor.DrawDebugIcon(debug);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.PropertyField(SlowingDistance);
                EditorGUILayout.PropertyField(SlowingLimit);
                EditorGUILayout.PropertyField(BiDirectional);
                EditorGUILayout.PropertyField(UseInputAxis);

                if (UseInputAxis.boolValue)
                {
                    EditorGUILayout.PropertyField(Axis);
                }
                EditorGUILayout.PropertyField(Start);
                EditorGUILayout.PropertyField(End);


                EditorGUILayout.PropertyField(AlignToLink);

                if (AlignToLink.boolValue)
                    EditorGUILayout.PropertyField(AlignTime);


                MalbersEditor.DrawSplitter();
                MTools.DrawScriptableObject(StartReaction, true, false);
                MalbersEditor.DrawSplitter();
                MTools.DrawScriptableObject(EndReaction, true, false);
                serializedObject.ApplyModifiedProperties();
            }

            void OnSceneGUI()
            {
                using (var cc = new EditorGUI.ChangeCheckScope())
                {
                    if (M.Start && M.Start != M.transform)
                    {
                        var start = M.Start.position;
                        start = Handles.PositionHandle(start, M.transform.rotation);

                        if (cc.changed)
                        {
                            Undo.RecordObject(M.Start, "Move Start AI Link");
                            M.Start.position = start;
                        }
                    }
                }

                using (var cc = new EditorGUI.ChangeCheckScope())
                {
                    if (M.End && M.End != M.transform)
                    {
                        var end = M.End.position;
                        end = Handles.PositionHandle(end, M.transform.rotation);

                        if (cc.changed)
                        {
                            Undo.RecordObject(M.End, "Move End AI Link");
                            M.End.position = end;
                        }
                    }
                }
            }
        }
#endif
    }
}