using System.Collections.Generic;
using UnityEngine;
using RidingSystem.Scriptables;
using UnityEngine.Events;
using UnityEngine.Serialization;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace RidingSystem.Controller
{
    [AddComponentMenu("RidingSystem/Animal Controller/Zone")]
    public class Zone : MonoBehaviour
    {
        public bool debug;

        public bool automatic;

        [FormerlySerializedAs("HeadOnly")]
        public bool BoneOnly;



        [Range(0, 360)]
        public float Angle = 360;


        [Range(0, 1)]
        public float Weight = 1;

        public bool DoubleSide = false;
        public bool Flip = false;


        [FormerlySerializedAs("HeadName")]
        public string BoneName = "Head";


        public ZoneType zoneType = ZoneType.Mode;
        public StateAction stateAction = StateAction.Activate;
        public StanceAction stanceAction = StanceAction.Enter;

        public LayerReference Layer = new LayerReference(1048576);
        public IntReference stateStatus = new IntReference(0);

        [SerializeField] private List<Tag> tags;

        public ModeID modeID;
        public StateID stateID;

        public StanceID stanceID;
        public MAction ActionID;
        [SerializeField] private IntReference modeIndex = new IntReference(-99);

        public int ModeAbilityIndex => modeID.ID == 4 ? ActionID.ID : modeIndex.Value;

        public int ZoneID;

        public AbilityStatus m_abilityStatus = AbilityStatus.PlayOneTime;
        public float AbilityTime = 3f;


        public FloatReference Force = new FloatReference(10);
        [FormerlySerializedAs("EnterDrag")]
        public FloatReference EnterAceleration = new FloatReference(2);
        public FloatReference ExitDrag = new FloatReference(4);

        [FormerlySerializedAs("Bounce")]
        public FloatReference LimitForce = new FloatReference(8);

        public BoolReference ForceGrounded = new BoolReference();

        public BoolReference ForceAirControl = new BoolReference(true);


        public List<MAnimal> CurrentAnimals { get; internal set; }


        public MAnimal CurrentAnimal => CurrentAnimals.Count > 0 ? CurrentAnimals[0] : null;

        internal List<Collider> m_Colliders = new List<Collider>();


        [Min(0)] public float ModeFloat = 0;

        public bool RemoveAnimalOnActive = false;


        public AnimalEvent OnEnter = new AnimalEvent();
        public AnimalEvent OnExit = new AnimalEvent();
        public AnimalEvent OnZoneActivation = new AnimalEvent();


        public AnimalEvent OnZoneFailed = new AnimalEvent();

        [RequiredField] public Collider ZoneCollider;

        public static List<Zone> Zones;

        private int GetID
        {
            get
            {
                switch (zoneType)
                {
                    case ZoneType.Mode:
                        return modeID;
                    case ZoneType.State:
                        return stateID;
                    case ZoneType.Stance:
                        return stanceID;
                    case ZoneType.Force:
                        return 100;
                    default:
                        return 0;
                }
            }
        }

        public bool IsMode => zoneType == ZoneType.Mode;

        public bool IsState => zoneType == ZoneType.State;

        public bool IsStance => zoneType == ZoneType.Stance;

        public List<Tag> Tags { get => tags; set => tags = value; }


        void OnEnable()
        {
            if (Zones == null)
                Zones = new List<Zone>();


            if (ZoneCollider == null)
                ZoneCollider = GetComponent<Collider>();


            if (ZoneCollider)
            {
                ZoneCollider.isTrigger = true;
                ZoneCollider.enabled = true;
            }

            Zones.Add(this);

            if (ZoneID == 0) ZoneID = GetID;

            CurrentAnimals = new List<MAnimal>();
        }

        void OnDisable()
        {
            Zones.Remove(this);

            foreach (var animal in CurrentAnimals)
            {
                ResetStoredAnimal(animal);
                OnExit.Invoke(animal);
            }

            CurrentAnimals = new List<MAnimal>();


            if (ZoneCollider) ZoneCollider.enabled = false;
        }


        void OnTriggerEnter(Collider other)
        {
            if (IgnoreCollider(other)) return;

            if (Tags != null && Tags.Count > 0)
            {
                bool hasTag = false;
                foreach (var t in tags)
                {
                    if (t != null && other.transform.HasMalbersTagInParent(t))
                    {
                        hasTag = true;
                        break;
                    }
                }

                if (!hasTag)
                {
                    if (debug)
                        Debug.LogWarning($"The Zone:<B>[{name}]</B> cannot be activated by <B>[{other.transform.root.name}]</B>. The Zone is using Tags and <B>[{other.transform.root.name}]</B> does not have any.");

                    return;
                }
            }

            MAnimal animal = other.GetComponentInParent<MAnimal>();
            if (!animal || animal.Sleep || !animal.enabled) return;

            if (!m_Colliders.Contains(other))
            {
                m_Colliders.Add(other);
            }
            else return;

            if (CurrentAnimals.Contains(animal)) return;
            else
            {
                animal.Zone = this;

                CurrentAnimals.Add(animal);
                OnEnter.Invoke(animal);

                if (automatic)
                {
                    ActivateZone(animal);
                }
                else
                {
                    PrepareZone(animal);
                }
            }
        }
        void OnTriggerExit(Collider other)
        {
            if (IgnoreCollider(other)) return;

            MAnimal animal = other.GetComponentInParent<MAnimal>();

            if (animal == null) return;

            if (m_Colliders != null && m_Colliders.Contains(other))
            {
                m_Colliders.Remove(other);
            }

            if (CurrentAnimals.Contains(animal))
            {
                if (!m_Colliders.Exists(x => x != null && x.transform.root == animal.transform))
                {
                    OnExit.Invoke(animal);
                    ResetStoredAnimal(animal);
                    CurrentAnimals.Remove(animal);

                    animal.Zone = null;
                }
            }
        }

        private bool IgnoreCollider(Collider other) =>
            !isActiveAndEnabled ||
            other.isTrigger ||
            other.transform.root == transform.root ||
            !MTools.CollidersLayer(other, Layer.Value) ||
            BoneOnly && !other.name.ToLower().Contains(BoneName.ToLower());


        public virtual void ActivateZone(MAnimal animal)
        {
            var prob = Random.Range(0, 1);
            if (Weight != 1 && Weight < prob)
            {
                return;
            }

            var flip = (Flip ? 1 : -1);

            var EntrySideAngle = Vector3.Angle(transform.forward * flip, animal.Forward) * 2;
            var OtherSideAngle = EntrySideAngle;

            if (DoubleSide) OtherSideAngle = Vector3.Angle(-transform.forward * flip, animal.Forward) * 2;

            var side = Vector3.Dot((animal.transform.position - transform.position).normalized, transform.forward) * -1;
            if (Angle == 360 || (EntrySideAngle < Angle && side < 0) || (OtherSideAngle < Angle && side > 0))
            {
                var isZoneActive = false;

                animal.Zone = this;

                switch (zoneType)
                {
                    case ZoneType.Mode:
                        isZoneActive = ActivateModeZone(animal);
                        break;
                    case ZoneType.State:
                        isZoneActive = ActivateStateZone(animal);
                        break;
                    case ZoneType.Force:
                        isZoneActive = SetForceZone(animal, true);
                        break;
                }
                if (isZoneActive)
                {
                    if (debug) Debug.Log($"<b>{name}</b> [Zone Activate] -> <b>[{animal.name}]</b>");
                    OnZoneActive(animal);
                }
            }
        }

        public virtual void ActivateZone()
        {
            foreach (var animal in CurrentAnimals) ActivateZone(animal);
        }

        protected virtual void PrepareZone(MAnimal animal)
        {
            switch (zoneType)
            {
                case ZoneType.Mode:
                    var PreMode = animal.Mode_Get(ZoneID);

                    if (PreMode == null || !PreMode.HasAbilityIndex(ModeAbilityIndex))
                    {
                        OnZoneFailed.Invoke(animal);

                        Debug.LogWarning($"<B>[{name}]</B> cannot be activated by <B>[{animal.name}]</B>." +
                            $" It does not have The <B>[Mode {modeID.name}]</B> with <B>[Ability {ModeAbilityIndex}]</B>");
                        return;
                    }
                    PreMode.SetAbilityIndex(ModeAbilityIndex);
                    break;
                case ZoneType.State:
                    var PreState = animal.State_Get(ZoneID);
                    if (!PreState) OnZoneFailed.Invoke(animal);
                    break;
                case ZoneType.Stance:
                    break;
                case ZoneType.Force:
                    break;
            }
        }

        private bool ActivateStateZone(MAnimal animal)
        {
            var Succesful = false;
            switch (stateAction)
            {
                case StateAction.Activate:
                    if (animal.ActiveStateID != ZoneID)
                    {
                        animal.State_Activate(ZoneID);
                        if (stateStatus != -1) animal.State_SetStatus(stateStatus);
                        Succesful = true;
                    }
                    break;
                case StateAction.AllowExit:
                    if (animal.ActiveStateID == ZoneID)
                    {
                        animal.ActiveState.AllowExit();
                        Succesful = true;
                    }
                    break;
                case StateAction.ForceActivate:
                    animal.State_Force(ZoneID);
                    if (stateStatus != -1) animal.State_SetStatus(stateStatus);
                    Succesful = true;
                    break;
                case StateAction.Enable:
                    animal.State_Enable(ZoneID);
                    Succesful = true;
                    break;
                case StateAction.Disable:
                    animal.State_Disable(ZoneID);
                    Succesful = true;
                    break;
                case StateAction.SetExitStatus:
                    if (animal.ActiveStateID == stateID)
                    {
                        animal.State_SetExitStatus(stateStatus);
                        Succesful = true;
                    }
                    break;
                default:
                    break;
            }
            return Succesful;
        }

        private bool ActivateModeZone(MAnimal animal)
        {
            if (!animal.IsPlayingMode)
            {
                animal.Mode_SetPower(ModeFloat);
                return animal.Mode_TryActivate(ZoneID, ModeAbilityIndex, m_abilityStatus);
            }
            return false;
        }

        private bool SetForceZone(MAnimal animal, bool ON)
        {
            if (ON)
            {
                var StartExtForce = animal.CurrentExternalForce + animal.GravityStoredVelocity;


                if (StartExtForce.magnitude > LimitForce)
                {
                    StartExtForce = StartExtForce.normalized * LimitForce;
                }


                animal.CurrentExternalForce = StartExtForce;
                animal.ExternalForce = transform.up * Force;
                animal.ExternalForceAcel = EnterAceleration;

                if (animal.ActiveState.ID == StateEnum.Fall)
                {
                    var fall = animal.ActiveState as Fall;
                    fall.FallCurrentDistance = 0;
                }

                animal.GravityTime = 0;
                animal.Grounded = ForceGrounded.Value;
                animal.ExternalForceAirControl = ForceAirControl.Value;
            }
            else
            {
                if (animal.ActiveState.ID == StateEnum.Fall) animal.UseGravity = true;

                if (ExitDrag > 0)
                {
                    animal.ExternalForceAcel = ExitDrag;
                    animal.ExternalForce = Vector3.zero;
                }
            }
            return ON;
        }

        internal void OnZoneActive(MAnimal animal)
        {
            OnZoneActivation.Invoke(animal);

            if (RemoveAnimalOnActive)
            {
                ResetStoredAnimal(animal);
                CurrentAnimals.Remove(animal);
            }
        }

        public void TargetArrived(GameObject go)
        {
            var animal = go.FindComponent<MAnimal>();
            ActivateZone(animal);
        }

        public virtual void ResetStoredAnimal(MAnimal animal)
        {
            if (animal)
            {
                animal.Zone = null;

                switch (zoneType)
                {
                    case ZoneType.Mode:

                        var mode = animal.Mode_Get(ZoneID);

                        if (mode != null)
                        {
                            if (mode.AbilityIndex == ModeAbilityIndex) mode.ResetAbilityIndex();
                        }

                        break;
                    case ZoneType.State:

                        break;
                    case ZoneType.Force:
                        SetForceZone(animal, false);
                        break;
                    default:
                        break;
                }
            }
        }

        [HideInInspector] public int Editor_Tabs1 = 0;

#if UNITY_EDITOR
        [ContextMenu("Connect to Align")]
        void TryAlign()
        {
            var method = this.GetUnityAction<MAnimal>("Aligner", "Align");
            if (method != null) UnityEditor.Events.UnityEventTools.AddPersistentListener(OnZoneActivation, method);
            MTools.SetDirty(this);
        }



        private void OnDrawGizmos()
        {
            if (Application.isPlaying && CurrentAnimals != null)
            {
                foreach (var animal in CurrentAnimals)
                {
                    var flip = (Flip ? 1 : -1);

                    var EntrySideAngle = Vector3.Angle(transform.forward * flip, animal.Forward) * 2;
                    var OtherSideAngle = EntrySideAngle;

                    if (DoubleSide) OtherSideAngle = Vector3.Angle(-transform.forward * flip, animal.Forward) * 2;

                    var DColor = Color.red;

                    var side = Vector3.Dot((animal.transform.position - transform.position).normalized, transform.forward) * -1;


                    if (Angle == 360 || (EntrySideAngle < Angle && side < 0) || (OtherSideAngle < Angle && side > 0))
                    {
                        DColor = Color.green;
                    }


                    MTools.Draw_Arrow(animal.transform.position + Vector3.up * 0.05f, animal.Forward, DColor);
                }
            }
        }
#endif
    }

    public enum StateAction
    {
        Activate,
        AllowExit,
        ForceActivate,
        Enable,
        Disable,
        SetExitStatus,

    }
    public enum StanceAction
    {
        Enter,
        Exit,
        Toggle,
        Stay,
        SetDefault,
    }
    public enum ZoneType
    {
        Mode,
        State,
        Stance,
        Force,
    }


#if UNITY_EDITOR
    [CustomEditor(typeof(Zone))]
    public class ZoneEditor : Editor
    {
        private Zone m;


        protected string[] Tabs1 = new string[] { "General", "Events" };


        SerializedProperty
            HeadOnly, stateAction, HeadName, zoneType, stateID, modeID, modeIndex, ActionID, auto, debug, m_abilityStatus, AbilityTime, Editor_Tabs1,
            OnZoneActivation, OnExit, OnEnter, ForceGrounded, OnZoneFailed, Angle, DoubleSide, Weight, Flip, ForceAirControl, ZoneCollider,
            stanceAction, layer, stanceID, RemoveAnimalOnActive, m_tag, ModeFloat, Force, EnterAceleration, ExitAceleration, stateStatus, Bounce;

        private void OnEnable()
        {
            m = ((Zone)target);

            HeadOnly = serializedObject.FindProperty("BoneOnly");
            HeadName = serializedObject.FindProperty("BoneName");

            RemoveAnimalOnActive = serializedObject.FindProperty("RemoveAnimalOnActive");
            layer = serializedObject.FindProperty("Layer");
            Flip = serializedObject.FindProperty("Flip");

            stateStatus = serializedObject.FindProperty("stateStatus");
            OnZoneFailed = serializedObject.FindProperty("OnZoneFailed");


            Force = serializedObject.FindProperty("Force");
            EnterAceleration = serializedObject.FindProperty("EnterAceleration");
            ExitAceleration = serializedObject.FindProperty("ExitDrag");
            Bounce = serializedObject.FindProperty("LimitForce");
            ForceGrounded = serializedObject.FindProperty("ForceGrounded");
            ForceAirControl = serializedObject.FindProperty("ForceAirControl");

            m_abilityStatus = serializedObject.FindProperty("m_abilityStatus");
            AbilityTime = serializedObject.FindProperty("AbilityTime");

            Angle = serializedObject.FindProperty("Angle");
            Weight = serializedObject.FindProperty("Weight");
            DoubleSide = serializedObject.FindProperty("DoubleSide");


            m_tag = serializedObject.FindProperty("tags");
            ModeFloat = serializedObject.FindProperty("ModeFloat");
            zoneType = serializedObject.FindProperty("zoneType");
            stateID = serializedObject.FindProperty("stateID");
            stateAction = serializedObject.FindProperty("stateAction");
            stanceAction = serializedObject.FindProperty("stanceAction");
            modeID = serializedObject.FindProperty("modeID");
            stanceID = serializedObject.FindProperty("stanceID");
            modeIndex = serializedObject.FindProperty("modeIndex");
            ActionID = serializedObject.FindProperty("ActionID");
            auto = serializedObject.FindProperty("automatic");
            debug = serializedObject.FindProperty("debug");
            ZoneCollider = serializedObject.FindProperty("ZoneCollider");
            Editor_Tabs1 = serializedObject.FindProperty("Editor_Tabs1");


            OnEnter = serializedObject.FindProperty("OnEnter");
            OnExit = serializedObject.FindProperty("OnExit");
            OnZoneActivation = serializedObject.FindProperty("OnZoneActivation");


            if (ZoneCollider.objectReferenceValue == null)
            {
                ZoneCollider.objectReferenceValue = m.GetComponent<Collider>();
                serializedObject.ApplyModifiedProperties();
            }
        }


        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            MalbersEditor.DrawDescription("Area to modify States, Stances or Modes on an Animal");



            EditorGUILayout.BeginVertical(MalbersEditor.StyleGray);
            {
                EditorGUILayout.BeginHorizontal();

                Editor_Tabs1.intValue = GUILayout.Toolbar(Editor_Tabs1.intValue, Tabs1);
                MalbersEditor.DrawDebugIcon(debug);
                EditorGUILayout.EndHorizontal();


                EditorGUI.BeginChangeCheck();

                if (Editor_Tabs1.intValue == 0)
                {
                    DrawGeneral();
                }
                else
                {
                    DrawEvents();
                }

                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(target, "Zone Changed");
                    EditorUtility.SetDirty(target);
                }


                if (Application.isPlaying && debug.boolValue)
                {
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                    EditorGUILayout.LabelField("Debug", EditorStyles.boldLabel);

                    if (m.ZoneCollider) EditorGUILayout.ObjectField("Zone Collider", m.ZoneCollider, typeof(Collider), false);

                    EditorGUILayout.LabelField("Current Animals (" + m.CurrentAnimals.Count + ")", EditorStyles.boldLabel);
                    foreach (var item in m.CurrentAnimals)
                    {
                        EditorGUILayout.ObjectField(item.name, item, typeof(MAnimal), false);
                    }

                    EditorGUILayout.LabelField("Current Colliders (" + m.m_Colliders.Count + ")", EditorStyles.boldLabel);
                    foreach (var item in m.m_Colliders)
                    {
                        EditorGUILayout.ObjectField(item.name, item, typeof(Collider), false);
                    }

                    EditorGUILayout.EndVertical();
                    Repaint();
                    EditorGUI.EndDisabledGroup();
                }

            }
            EditorGUILayout.EndVertical();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawGeneral()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);


            EditorGUILayout.PropertyField(auto, new GUIContent("Automatic", "As soon as the animal enters the zone it will execute the logic. If False then Call the Method Zone.Activate()"));



            EditorGUILayout.PropertyField(ZoneCollider, new GUIContent("Trigger", "Collider for the Zone. If is not set, it will find the first collider attached to this gameobject"));
            EditorGUILayout.PropertyField(layer, new GUIContent("Animal Layer", "Layer to detect the Animal"));

            EditorGUILayout.PropertyField(zoneType, new GUIContent("Zone Type", "Choose between a Mode or a State for the Zone"));

            ZoneType zone = (ZoneType)zoneType.intValue;


            switch (zone)
            {
                case ZoneType.Mode:


                    EditorGUILayout.PropertyField(modeID,
                        new GUIContent("Mode ID: [" + (m.modeID ? m.modeID.ID.ToString() : "") + "]", "Which Mode to Set when entering the Zone"));

                    serializedObject.ApplyModifiedProperties();


                    if (m.modeID != null && m.modeID == 4)
                    {
                        EditorGUILayout.PropertyField(ActionID,
                            new GUIContent("Action Index: [" + (m.ActionID ? m.ActionID.ID.ToString() : "") + "]", "Which Action to Set when entering the Zone"));

                        if (ActionID.objectReferenceValue == null)
                        {
                            EditorGUILayout.HelpBox("Please Select an Action ID", MessageType.Error);
                        }
                    }
                    else
                    {
                        EditorGUILayout.PropertyField(modeIndex, new GUIContent("Ability Index", "Which Ability to Set when entering the Zone"));
                    }

                    EditorGUILayout.PropertyField(m_abilityStatus, new GUIContent("Status", "Mode Ability Status"));

                    if (m_abilityStatus.intValue == (int)AbilityStatus.ActiveByTime)
                    {
                        EditorGUILayout.PropertyField(AbilityTime);
                    }
                    EditorGUILayout.PropertyField(ModeFloat, new GUIContent("Mode Power"));

                    break;
                case ZoneType.State:
                    EditorGUILayout.PropertyField(stateID, new GUIContent("State ID", "Which State will Activate when entering the Zone"));
                    EditorGUILayout.PropertyField(stateAction, new GUIContent("Option", "Execute a State logic when the animal enters the zone"));

                    int stateaction = stateAction.intValue;
                    if (MTools.CompareOR(stateaction, (int)StateAction.Activate, (int)StateAction.ForceActivate, (int)StateAction.SetExitStatus))
                    {
                        EditorGUILayout.PropertyField(stateStatus, new GUIContent("State Status"));
                    }

                    if (stateID.objectReferenceValue == null)
                    {
                        EditorGUILayout.HelpBox("Please Select an State ID", MessageType.Error);
                    }
                    break;
                case ZoneType.Stance:
                    EditorGUILayout.PropertyField(stanceID, new GUIContent("Stance ID", "Which Stance will Activate when entering the Zone"));
                    EditorGUILayout.PropertyField(stanceAction, new GUIContent("Status", "Execute a Stance logic when the animal enters the zone"));
                    if (stanceID.objectReferenceValue == null)
                    {
                        EditorGUILayout.HelpBox("Please Select an Stance ID", MessageType.Error);
                    }
                    break;
                case ZoneType.Force:
                    EditorGUILayout.PropertyField(Force);
                    EditorGUILayout.PropertyField(EnterAceleration);
                    EditorGUILayout.PropertyField(ExitAceleration);
                    EditorGUILayout.PropertyField(Bounce);
                    EditorGUILayout.PropertyField(ForceAirControl, new GUIContent("Air Control"));
                    EditorGUILayout.PropertyField(ForceGrounded, new GUIContent("Grounded? "));
                    break;
                default:
                    break;
            }



            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                EditorGUILayout.PropertyField(Weight);
                EditorGUILayout.PropertyField(Angle);

                if (Angle.floatValue != 360)
                {
                    EditorGUILayout.PropertyField(DoubleSide);
                    EditorGUILayout.PropertyField(Flip);
                }

                EditorGUILayout.PropertyField(RemoveAnimalOnActive,
                    new GUIContent("Reset on Active", "Remove the stored Animal on the Zone when the Zones gets Active, Reseting it to its default state"));

                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_tag,
                    new GUIContent("Tags", "Set this parameter if you want the zone to Interact only with gameObject with that tag"));
                EditorGUI.indentLevel--;

                EditorGUILayout.PropertyField(HeadOnly,
                    new GUIContent("Bone Only", "Activate when a bone enter the Zone.\nThat Bone needs a collider!!"));

                if (HeadOnly.boolValue)
                    EditorGUILayout.PropertyField(HeadName,
                        new GUIContent("Bone Name", "Name for the Bone you need to check if it has enter the zone"));
            }
            EditorGUILayout.EndVertical();

        }

        private void DrawEvents()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                EditorGUILayout.PropertyField(OnEnter, new GUIContent("On Animal Enter Zone"));
                EditorGUILayout.PropertyField(OnExit, new GUIContent("On Animal Exit Zone"));
                EditorGUILayout.PropertyField(OnZoneActivation, new GUIContent("On Zone Active"));
                EditorGUILayout.PropertyField(OnZoneFailed, new GUIContent("On Zone Failed"));
            }
            EditorGUILayout.EndVertical();
        }

        private void OnSceneGUI()
        {
            var angle = Angle.floatValue;
            if (angle != 360)
            {
                angle /= 2;

                var Direction = m.transform.forward * (Flip.boolValue ? -1 : 1);

                Handles.color = new Color(0, 1, 0, 0.1f);
                Handles.DrawSolidArc(m.transform.position, m.transform.up, Quaternion.Euler(0, -angle, 0) * Direction, angle * 2, m.transform.localScale.y);
                Handles.color = Color.green;
                Handles.DrawWireArc(m.transform.position, m.transform.up, Quaternion.Euler(0, -angle, 0) * Direction, angle * 2, m.transform.localScale.y);

                if (DoubleSide.boolValue)
                {
                    Handles.color = new Color(0, 1, 0, 0.1f);
                    Handles.DrawSolidArc(m.transform.position, m.transform.up, Quaternion.Euler(0, -angle, 0) * -Direction, angle * 2, m.transform.localScale.y);
                    Handles.color = Color.green;
                    Handles.DrawWireArc(m.transform.position, m.transform.up, Quaternion.Euler(0, -angle, 0) * -Direction, angle * 2, m.transform.localScale.y);
                }
            }
        }
    }
#endif
}
