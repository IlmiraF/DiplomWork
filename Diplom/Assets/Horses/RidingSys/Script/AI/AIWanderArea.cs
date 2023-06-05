using UnityEngine;



namespace RidingSystem
{
    [AddComponentMenu("RidingSystem/AI/AI Wander Area")]
    public class AIWanderArea : MWayPoint
    {
        public enum AreaType { Circle, Box };

        public AreaType m_AreaType = AreaType.Circle;

        [Min(0)] public float radius = 5;

        public Vector3 BoxArea = new Vector3(10, 1, 10);


        [Range(0, 1)]
        public float WanderWeight = 1f;

        public Vector3 Destination { get; internal set; }

        private Transform currentNextTarget;

        [SerializeField] private AIWanderArea MainArea;
        [SerializeField] private AIWanderArea[] ChildWanderAreas;

        bool IsChild => MainArea != this;

        protected override void OnEnable()
        {
            base.OnEnable();

            FindWanderAreas();

            if (!IsChild) GetNextDestination();
            currentNextTarget = MainArea.transform;
        }

        private void FindWanderAreas()
        {
            MainArea = transform.parent != null ? (transform.parent.GetComponentInParent<AIWanderArea>()) : this;
            if (MainArea == null) MainArea = this;

            ChildWanderAreas = null;

            if (!IsChild)
            {
                ChildWanderAreas = GetComponentsInChildren<AIWanderArea>();
                if (ChildWanderAreas != null) foreach (var wa in ChildWanderAreas)
                    {
                        wa.DebugColor = DebugColor;
                        wa.stoppingDistance = stoppingDistance;
                    }
            }
        }

        public Vector3 GetNextDestination()
        {
            if (!IsChild && ChildWanderAreas != null && ChildWanderAreas.Length > 1)
            {
                return ChildWanderAreas[Random.Range(0, ChildWanderAreas.Length)].GetNextDestinationArea();
            }
            else
            {
                return GetNextDestinationArea();
            }
        }

        private Vector3 GetNextDestinationArea()
        {
            switch (m_AreaType)
            {
                case AreaType.Circle:
                    Vector2 vector2 = (Random.insideUnitCircle * radius);
                    Destination = transform.TransformPoint(new Vector3(vector2.x, 0, vector2.y));
                    break;
                case AreaType.Box:
                    Destination = transform.TransformPoint(RandomPointInBox(BoxArea));
                    break;
                default:
                    Destination = transform.position;
                    break;
            }

            MainArea.Destination = Destination;

            MTools.DrawWireSphere(Destination, Color.red, 0.1f, 3);

            return MainArea.Destination;
        }

        public override Vector3 GetPosition()
          => GetNextDestination();

        public override float StopDistance() => MainArea.stoppingDistance;

        public override float SlowDistance() => MainArea.slowingDistance;

        public override Transform NextTarget() => MainArea.currentNextTarget;

        public override void TargetArrived(GameObject target)
        {
            MainArea.OnTargetArrived.Invoke(target);

            if (NextTargets != null && NextTargets.Count > 0)
            {
                var probability = UnityEngine.Random.Range(0f, 1f);

                if (probability <= WanderWeight)
                {
                    GetNextDestination();
                    currentNextTarget = transform;
                }
                else
                {
                    currentNextTarget = NextTargets[UnityEngine.Random.Range(0, NextTargets.Count)];
                }
            }
            else
            {
                GetNextDestination();
            }
        }

        private Vector3 RandomPointInBox(Vector3 size)
        {
            return new Vector3(
                (Random.value - 0.5f) * size.x,
                (Random.value - 0.5f) * size.y,
                (Random.value - 0.5f) * size.z);
        }

        [HideInInspector, SerializeField] private bool ShowRadius;

        private void Reset()
        {
            DebugColor.a = 0.2f;
        }

        private void OnValidate()
        {
            FindWanderAreas();

            if (BoxArea.x < 0) BoxArea.x = 0;
            if (BoxArea.y < 0) BoxArea.y = 0;
            if (BoxArea.z < 0) BoxArea.z = 0;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            var DebugColorWire = DebugColor;
            DebugColorWire.a = 1;


            UnityEditor.Handles.color = DebugColorWire;
            UnityEditor.Handles.DrawWireDisc(transform.position, transform.up, stoppingDistance);
            UnityEditor.Handles.DrawWireDisc(transform.position, transform.up, slowingDistance);

            switch (m_AreaType)
            {
                case AreaType.Circle:
                    UnityEditor.Handles.color = DebugColorWire;
                    UnityEditor.Handles.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
                    UnityEditor.Handles.DrawWireDisc(Vector3.zero, Vector3.up, radius);
                    UnityEditor.Handles.color = DebugColor;
                    UnityEditor.Handles.DrawSolidDisc(Vector3.zero, Vector3.up, radius);
                    break;
                case AreaType.Box:

                    var sizeX = transform.lossyScale.x * BoxArea.x;
                    var sizeY = transform.lossyScale.y * BoxArea.y;
                    var sizeZ = transform.lossyScale.z * BoxArea.z;

                    Matrix4x4 rotationMatrix = Matrix4x4.TRS(transform.position, transform.rotation, new Vector3(sizeX, sizeY, sizeZ));
                    Gizmos.matrix = rotationMatrix;
                    Gizmos.color = DebugColor;
                    Gizmos.DrawCube(Vector3.zero, Vector3.one);
                    Gizmos.color = DebugColorWire;
                    Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
                    break;
            }
        }


        private void OnDrawGizmosSelected()
        {
            var DebugColorWire = DebugColor;
            DebugColorWire.a = 1;
            Gizmos.color = DebugColorWire;

            Gizmos.DrawRay(transform.position, transform.up * Height);
            Gizmos.DrawWireSphere(transform.position + transform.up * Height, Height * 0.1f);
            Gizmos.DrawWireSphere(transform.position, Height * 0.1f);

            if (nextWayPoints != null)
            {
                foreach (var item in nextWayPoints)
                {
                    if (item) Gizmos.DrawLine(transform.position, item.position);
                }
            }
        }
#endif
    }

    //INSPECTOR--------------


    #region Inspector


#if UNITY_EDITOR
    [UnityEditor.CustomEditor(typeof(AIWanderArea))]
    [UnityEditor.CanEditMultipleObjects]
    public class AIWanderAreaEditor : UnityEditor.Editor
    {
        UnityEditor.SerializedProperty
            pointType, stoppingDistance, slowingDistance, m_AreaType, m_height,
            radius, BoxArea, WaitTime, WanderWeight, nextWayPoints, DebugColor, OnTargetArrived;
        AIWanderArea M;

        private bool isChild;

        private void OnEnable()
        {
            M = (AIWanderArea)target;
            pointType = serializedObject.FindProperty("pointType");
            stoppingDistance = serializedObject.FindProperty("stoppingDistance");
            slowingDistance = serializedObject.FindProperty("slowingDistance");
            m_AreaType = serializedObject.FindProperty("m_AreaType");
            radius = serializedObject.FindProperty("radius");
            BoxArea = serializedObject.FindProperty("BoxArea");
            WaitTime = serializedObject.FindProperty("m_WaitTime");
            WanderWeight = serializedObject.FindProperty("WanderWeight");
            nextWayPoints = serializedObject.FindProperty("nextWayPoints");
            DebugColor = serializedObject.FindProperty("DebugColor");
            OnTargetArrived = serializedObject.FindProperty("OnTargetArrived");
            m_height = serializedObject.FindProperty("m_height");

            isChild = M.transform.parent != null && (M.transform.parent.GetComponentInParent<AIWanderArea>() != null);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            MalbersEditor.DrawDescription("Type of Waypoint that uses an Area to get the Destination point");
            UnityEditor.EditorGUILayout.BeginVertical(MTools.StyleGray);
            {
                if (!isChild)
                {
                    UnityEditor.EditorGUILayout.BeginVertical(UnityEditor.EditorStyles.helpBox);
                    {
                        UnityEditor.EditorGUILayout.BeginHorizontal();
                        UnityEditor.EditorGUILayout.PropertyField(pointType);
                        UnityEditor.EditorGUILayout.PropertyField(DebugColor, GUIContent.none, GUILayout.Width(40));
                        UnityEditor.EditorGUILayout.EndHorizontal();
                        UnityEditor.EditorGUILayout.PropertyField(m_height);
                        UnityEditor.EditorGUILayout.PropertyField(stoppingDistance);
                        UnityEditor.EditorGUILayout.PropertyField(slowingDistance);
                        UnityEditor.EditorGUILayout.PropertyField(WaitTime);
                    }
                    UnityEditor.EditorGUILayout.EndVertical();
                }
                UnityEditor.EditorGUILayout.BeginVertical(UnityEditor.EditorStyles.helpBox);
                {
                    UnityEditor.EditorGUILayout.PropertyField(m_AreaType);
                    var aretype = (AIWanderArea.AreaType)m_AreaType.intValue;

                    switch (aretype)
                    {
                        case AIWanderArea.AreaType.Circle:
                            UnityEditor.EditorGUILayout.PropertyField(radius);

                            break;
                        case AIWanderArea.AreaType.Box:
                            UnityEditor.EditorGUILayout.PropertyField(BoxArea);
                            break;
                        default:
                            break;
                    }

                }
                UnityEditor.EditorGUILayout.EndVertical();

                if (isChild)
                {
                    UnityEditor.EditorGUILayout.HelpBox("Type, Stop Distance, Wait Time, and Next Destination properties are handled by the parent Wander Area",
                        UnityEditor.MessageType.Info);
                }
                else
                {
                    UnityEditor.EditorGUILayout.BeginVertical(UnityEditor.EditorStyles.helpBox);
                    {
                        UnityEditor.EditorGUILayout.LabelField("Next Destination", UnityEditor.EditorStyles.boldLabel);
                        UnityEditor.EditorGUILayout.PropertyField(WanderWeight);
                        UnityEditor.EditorGUI.indentLevel++;
                        UnityEditor.EditorGUILayout.PropertyField(nextWayPoints, true);
                        UnityEditor.EditorGUI.indentLevel--;
                    }
                    UnityEditor.EditorGUILayout.EndVertical();
                    UnityEditor.EditorGUILayout.PropertyField(OnTargetArrived);
                }
            }
            UnityEditor.EditorGUILayout.EndVertical();
            serializedObject.ApplyModifiedProperties();
        }
    }
#endif

    #endregion
}