using UnityEngine;
using UnityEngine.Events;
using RidingSystem.Events;
using RidingSystem.Scriptables;
using RidingSystem.Controller;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace RidingSystem.HAP
{
    [AddComponentMenu("RidingSystem/Riding/Mount")]
    public class Mount : MonoBehaviour, IAnimatorListener
    {
        #region References
        [RequiredField] public MAnimal Animal;


        #region Mount Point References
        public Transform MountPoint;  
        public Transform MountBase;  
        public Transform FootLeftIK;
        public Transform FootRightIK;
        public Transform KneeLeftIK;
        public Transform KneeRightIK;


        public Transform LeftRein; 
        public Transform RightRein;  
        #endregion

        #endregion

        #region General
        public BoolReference active = new BoolReference(true);

        public BoolReference Set_AIMount = new BoolReference(false);

        public BoolReference Set_AIDismount = new BoolReference(true);

        public BoolReference Set_InputMount = new BoolReference(true);

        public BoolReference set_InputDismount = new BoolReference(false);

        public BoolReference Set_MTriggersMount = new BoolReference(false);

        public BoolReference Set_MTriggersDismount = new BoolReference(true);

        public IntReference ID;

        public string mountIdle = "Idle";

        public bool MountOnly;
        public bool DismountOnly;
        public bool ForceDismount;
        public List<StateID> MountOnlyStates = new List<StateID>();
        public List<StateID> DismountOnlyStates = new List<StateID>();
        public List<StateID> ForceDismountStates = new List<StateID>();

        public AnimatorUpdateMode DefaultAnimUpdateMode { get; set; }
        #endregion

        public List<SpeedTimeMultiplier> SpeedMultipliers;

        #region Straight Mount
        public BoolReference straightSpine;  
        public BoolReference UseSpeedModifiers;
        public Vector3 pointOffset = new Vector3(0, 0, 3);
        public Vector3 MonturaSpineOffset => StraightSpineOffsetTransform.TransformPoint(pointOffset);

        public float smoothSM = 0.5f;
        #endregion

        #region Events
        public UnityEvent OnMounted = new UnityEvent();
        public UnityEvent OnDismounted = new UnityEvent();
        public BoolEvent OnCanBeMounted = new BoolEvent();
        #endregion

        #region Properties
        public bool StraightSpine { get => straightSpine; set => straightSpine.Value = value; }

        public Transform StraightSpineOffsetTransform;
        private bool defaultStraightSpine;

        public Animator Anim => Animal.Anim;
        public IInputSource MountInput => Animal.InputSource;
        public IAIControl AI { get; internal set; }

        public List<MountTriggers> MountTriggers { get; private set; }


        protected bool mounted;
        public bool Mounted
        {
            get => mounted;
            set
            {
                if (value != mounted)
                {
                    mounted = value;

                    if (mounted)
                        OnMounted.Invoke(); 
                    else
                        OnDismounted.Invoke();
                }
            }
        }


        public virtual bool CanDismount => Mounted;

        public virtual string MountIdle { get => mountIdle; set => mountIdle = value; }

        public virtual bool CanBeMounted { get => active; set => active.Value = value; }

        public bool HasIKFeet => FootLeftIK != null && FootRightIK != null && KneeLeftIK != null && KneeRightIK != null;

        public bool CanBeMountedByState { get; set; }
        public bool CanBeDismountedByState { get; set; }

        public MRider Rider { get; set; }

        public MRider NearbyRider { get; set; }
        #endregion

        #region IK Reins
        public Vector3 DefaultLeftReinPos { get; internal set; }

        public Vector3 DefaultRightReinPos { get; internal set; }
        #endregion

        public bool debug;

        public void Awake()
        {
            if (Animal == null)
                Animal = this.FindComponent<MAnimal>();

            AI = Animal.GetComponentInChildren<IAIControl>(true);

            MountTriggers = GetComponentsInChildren<MountTriggers>(true).ToList();

            CanBeDismountedByState = CanBeMountedByState = true;
            defaultStraightSpine = StraightSpine;
            if (Anim) DefaultAnimUpdateMode = Anim.updateMode;

            if (!StraightSpineOffsetTransform)
            {
                StraightSpineOffsetTransform = transform;
            }


            if (LeftRein && RightRein)
            {
                DefaultLeftReinPos = LeftRein.localPosition;
                DefaultRightReinPos = RightRein.localPosition;
            }
        }



        void OnEnable()
        {
            Animal.OnStateActivate.AddListener(AnimalStateChange);
            Animal.OnSpeedChange.AddListener(SetAnimatorSpeed);
        }

        void OnDisable()
        {
            Animal.OnStateActivate.RemoveListener(AnimalStateChange);
            Animal.OnSpeedChange.RemoveListener(SetAnimatorSpeed);

            if (NearbyRider) NearbyRider.MountTriggerExit();
        }

        public virtual void EnableInput(bool value)
        {
            MountInput?.Enable(value);
            Animal.StopMoving();
        }


        public void ResetRightRein()
        {
            if (RightRein) RightRein.localPosition = DefaultRightReinPos;
        }

        public void ResetLeftRein()
        {
            if (LeftRein) LeftRein.localPosition = DefaultLeftReinPos;
        }

        public virtual void StartMounting(MRider rider)
        {
            Mounted = true;
            Rider = rider; 

            Set_MountTriggers(Set_MTriggersMount.Value);
        }


        public virtual void End_Mounting()
        {
            EnableInput(Set_InputMount.Value); 
            AI?.SetActive(Set_AIMount.Value);

            SetAnimatorSpeed(Animal.currentSpeedModifier);
        }

        public virtual void Start_Dismounting()
        {
            Mounted = false;
            EnableInput(set_InputDismount.Value);

            Animal.Mode_Interrupt();

            ResetLeftRein();
            ResetRightRein();
        }

        public virtual void EndDismounting()
        {
            Set_MountTriggers(Set_MTriggersDismount.Value); 
            AI?.SetActive(Set_AIDismount.Value);
            Rider = null;
        }


        public virtual void PauseStraightSpine(bool value) => StraightSpine = !value && defaultStraightSpine;

        public virtual void Set_MountTriggers(bool value)
        {
            foreach (var mt in MountTriggers)
            {
                mt.gameObject.SetActive(value);
                mt.WasAutomounted = true;
            }

            if (!value) ExitMountTrigger();
        }

        public virtual void ExitMountTrigger()
        {
            OnCanBeMounted.Invoke(false);
            NearbyRider = null;

            foreach (var mt in MountTriggers)
                mt.WasAutomounted = false;
        }

        protected virtual void AnimalStateChange(int StateID)
        {
            var ActiveState = Animal.ActiveStateID;

            if (MountOnly)
            {
                CanBeMountedByState = MountOnlyStates.Contains(ActiveState); 
            }

            if (DismountOnly)
            {
                CanBeDismountedByState = DismountOnlyStates.Contains(ActiveState);
            }

            if (Rider)
            {
                Rider.UpdateCanMountDismount();

                if (ForceDismount)
                {
                    if (ForceDismountStates.Contains(ActiveState))
                        Rider.ForceDismount();
                }
            }
        }

        protected virtual void SetAnimatorSpeed(MSpeed SpeedModifier)
        {
            if (!Rider || !Rider.IsRiding) return; 

            if (UseSpeedModifiers)
            {
                var speed = SpeedMultipliers.Find(s => s.name == SpeedModifier.name);

                float TargetAnimSpeed = speed != null ? speed.AnimSpeed * SpeedModifier.animator * Animal.AnimatorSpeed : 1f;

                Rider.TargetSpeedMultiplier = TargetAnimSpeed;
            }
        }

        public virtual void StraightMount(bool value) => StraightSpine = value;

        public virtual bool OnAnimatorBehaviourMessage(string message, object value) => this.InvokeWithParams(message, value);

        [HideInInspector] public int Editor_Tabs1;
        [HideInInspector] public int Editor_Tabs2;



#if UNITY_EDITOR
        private void Reset()
        {
            Animal = GetComponent<MAnimal>();
            StraightSpineOffsetTransform = transform;

            MEvent RiderMountUIE = MTools.GetInstance<MEvent>("Rider Mount UI");

            if (RiderMountUIE != null)
            {
                UnityEditor.Events.UnityEventTools.AddObjectPersistentListener<Transform>(OnCanBeMounted, RiderMountUIE.Invoke, transform);
                UnityEditor.Events.UnityEventTools.AddPersistentListener(OnCanBeMounted, RiderMountUIE.Invoke);
            }
        }

        void OnDrawGizmos()
        {
            if (!debug) return;

            Gizmos.color = Color.red;
            if (StraightSpineOffsetTransform)
            {
                Gizmos.DrawSphere(MonturaSpineOffset, 0.125f);
            }
            else
            {
                StraightSpineOffsetTransform = transform;
            }
        }
#endif
    }

    [System.Serializable]
    public class SpeedTimeMultiplier
    {
        public string name = "SpeedName";

        public float AnimSpeed = 1f;
    }

    #region INSPECTOR
#if UNITY_EDITOR

    [CanEditMultipleObjects, CustomEditor(typeof(Mount))]
    public class MountEd : Editor
    {
        bool helpUseSpeeds;
        bool helpEvents;
        Mount M;

        SerializedProperty
            UseSpeedModifiers, MountOnly, DismountOnly, active, mountIdle, instantMount, instantDismount, straightSpine, ID, StraightSpineOffsetTransform,
           pointOffset, Animal, smoothSM, mountPoint, rightIK, rightKnee, leftIK, leftKnee, SpeedMultipliers,
            OnMounted, Editor_Tabs1, Editor_Tabs2, OnDismounted, OnCanBeMounted, MountOnlyStates, DismountOnlyStates, MountBase,

            ForceDismountStates, ForceDismount, debug,

            LeftRein, RightRein,

            Set_AIMount, Set_InputMount, Set_MTriggersMount,
            Set_AIDismount, Set_InputDismount, Set_MTriggersDismount
            ;


        private void OnEnable()
        {
            M = (Mount)target;

            UseSpeedModifiers = serializedObject.FindProperty("UseSpeedModifiers");
            Animal = serializedObject.FindProperty("Animal");
            debug = serializedObject.FindProperty("debug");
            ID = serializedObject.FindProperty("ID");


            LeftRein = serializedObject.FindProperty("LeftRein");
            RightRein = serializedObject.FindProperty("RightRein");


            MountOnly = serializedObject.FindProperty("MountOnly");
            DismountOnly = serializedObject.FindProperty("DismountOnly");
            active = serializedObject.FindProperty("active");
            mountIdle = serializedObject.FindProperty("mountIdle");
            straightSpine = serializedObject.FindProperty("straightSpine");


            smoothSM = serializedObject.FindProperty("smoothSM");

            mountPoint = serializedObject.FindProperty("MountPoint");
            MountBase = serializedObject.FindProperty("MountBase");
            rightIK = serializedObject.FindProperty("FootRightIK");
            rightKnee = serializedObject.FindProperty("KneeRightIK");
            leftIK = serializedObject.FindProperty("FootLeftIK");
            leftKnee = serializedObject.FindProperty("KneeLeftIK");

            Editor_Tabs1 = serializedObject.FindProperty("Editor_Tabs1");
            Editor_Tabs2 = serializedObject.FindProperty("Editor_Tabs2");

            SpeedMultipliers = serializedObject.FindProperty("SpeedMultipliers");
            OnMounted = serializedObject.FindProperty("OnMounted");
            pointOffset = serializedObject.FindProperty("pointOffset");
            StraightSpineOffsetTransform = serializedObject.FindProperty("StraightSpineOffsetTransform");

            OnDismounted = serializedObject.FindProperty("OnDismounted");
            OnCanBeMounted = serializedObject.FindProperty("OnCanBeMounted");
            MountOnlyStates = serializedObject.FindProperty("MountOnlyStates");
            DismountOnlyStates = serializedObject.FindProperty("DismountOnlyStates");

            ForceDismountStates = serializedObject.FindProperty("ForceDismountStates");
            ForceDismount = serializedObject.FindProperty("ForceDismount");

            Set_MTriggersMount = serializedObject.FindProperty("Set_MTriggersMount");
            Set_AIMount = serializedObject.FindProperty("Set_AIMount");
            Set_InputMount = serializedObject.FindProperty("Set_InputMount");

            Set_MTriggersDismount = serializedObject.FindProperty("Set_MTriggersDismount");
            Set_AIDismount = serializedObject.FindProperty("Set_AIDismount");
            Set_InputDismount = serializedObject.FindProperty("set_InputDismount");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            MalbersEditor.DrawDescription("Makes this animal mountable. Requires Mount Triggers and Moint Points");

            EditorGUILayout.BeginVertical(MalbersEditor.StyleGray);
            {
                EditorGUI.BeginChangeCheck();
                {
                    Editor_Tabs1.intValue = GUILayout.Toolbar(Editor_Tabs1.intValue, new string[] { "General", "Links", "Custom Mount" });
                    if (Editor_Tabs1.intValue != 3) Editor_Tabs2.intValue = 3;

                    Editor_Tabs2.intValue = GUILayout.Toolbar(Editor_Tabs2.intValue, new string[] { "M/D States", "Events", "Debug" });
                    if (Editor_Tabs2.intValue != 3) Editor_Tabs1.intValue = 3;


                    int Selection = Editor_Tabs1.intValue;

                    if (Selection == 0) ShowGeneral();
                    else if (Selection == 1) ShowLinks();
                    else if (Selection == 2) ShowCustom();

                    Selection = Editor_Tabs2.intValue;

                    if (Selection == 0) ShowStates();
                    else if (Selection == 1) ShowEvents();
                    else if (Selection == 2) ShowDebug();
                }

                EditorGUILayout.EndVertical();


                if (M.MountPoint == null)
                {
                    EditorGUILayout.HelpBox("'Mount Point'  is empty, please set a reference", MessageType.Warning);
                }
            }
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(target, "Mount Inspector");
            }
            serializedObject.ApplyModifiedProperties();
        }

        private void ShowDebug()
        {
            if (Application.isPlaying)
            {
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.ObjectField("Current Rider", M.Rider, typeof(MRider), false);
                EditorGUILayout.ObjectField("Nearby Rider", M.NearbyRider, typeof(MRider), false);
                EditorGUILayout.Space();
                EditorGUILayout.ToggleLeft("Mounted/Can Dismount", M.Mounted);
                EditorGUILayout.ToggleLeft("Can Be Mounted by State", M.CanBeDismountedByState);
                EditorGUILayout.ToggleLeft("Can Be Mounted", M.CanBeMounted);
                EditorGUILayout.ToggleLeft("Straight Spine", M.StraightSpine);
                Repaint();
                EditorGUI.EndDisabledGroup();
            }
        }


        private void ShowEvents()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.LabelField("Events", EditorStyles.boldLabel);
                    helpEvents = GUILayout.Toggle(helpEvents, "?", EditorStyles.miniButton, GUILayout.Width(18));
                }
                EditorGUILayout.EndHorizontal();
                if (helpEvents) EditorGUILayout.HelpBox("On Mounted: Invoked when the rider start to mount the animal\nOn Dismounted: Invoked when the rider start to dismount the animal\nInvoked when the Mountable has an available Rider Nearby", MessageType.None);

                EditorGUILayout.PropertyField(OnMounted);
                EditorGUILayout.PropertyField(OnDismounted);
                EditorGUILayout.PropertyField(OnCanBeMounted);
            }
            EditorGUILayout.EndVertical();
        }

        private void ShowStates()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                EditorGUILayout.LabelField("Mount/Dismount States", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(MountOnly, new GUIContent("Mount Only", "The Rider can only Mount when the Animal is on any of these states"));

                if (MountOnly.boolValue) MalbersEditor.Arrays(MountOnlyStates);

                EditorGUILayout.PropertyField(DismountOnly, new GUIContent("Dismount Only", "The Rider can only Dismount when the Animal is on any of these states"));

                if (DismountOnly.boolValue) MalbersEditor.Arrays(DismountOnlyStates);


                EditorGUILayout.PropertyField(ForceDismount, new GUIContent("Force Dismount", "The Rider is forced to dismount when the Animal is on any of these states"));

                if (ForceDismount.boolValue) MalbersEditor.Arrays(ForceDismountStates);
            }
            EditorGUILayout.EndVertical();
        }

        private void ShowCustom()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                EditorGUILayout.PropertyField(straightSpine, new GUIContent("Straight Spine", "Straighten the Mount Point to fix the Rider Animation"));

                if (M.StraightSpine)
                {
                    EditorGUILayout.PropertyField(StraightSpineOffsetTransform, new GUIContent("Transf Ref", "Transform to use for the Point Offset Calculation"));
                    EditorGUILayout.PropertyField(pointOffset, new GUIContent("Point Offset", "Point in front of the Mount to Straight the Spine of the Rider"));
                    EditorGUILayout.PropertyField(smoothSM, new GUIContent("Smoothness", "Smooth changes between the rotation and the straight Mount"));
                }
            }
            EditorGUILayout.EndVertical();


            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.PropertyField(UseSpeedModifiers, new GUIContent("Animator Speeds", "Use this for other animals but the horse"));
                    helpUseSpeeds = GUILayout.Toggle(helpUseSpeeds, "?", EditorStyles.miniButton, GUILayout.Width(18));
                }
                EditorGUILayout.EndHorizontal();

                if (M.UseSpeedModifiers)
                {
                    if (helpUseSpeeds) EditorGUILayout.HelpBox("Changes the Speed on the Rider's Animator to Sync with the Animal Animator.\nThe Original Riding Animations are meant for the Horse. Only change the Speeds for other creatures", MessageType.None);
                    MalbersEditor.Arrays(SpeedMultipliers, new GUIContent("Animator Speed Multipliers", "Velocity changes for diferent Animation Speeds... used on other animals"));
                }
            }
            EditorGUILayout.EndVertical();

        }

        private void ShowLinks()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                EditorGUILayout.HelpBox("'Mount Point' is obligatory, the rest are optional", MessageType.None);

                EditorGUILayout.PropertyField(MountBase, new GUIContent("Mount Base", "Reference for the Mount Base, Parent of the Mount Point, used for Straight movement for the mount"));
                EditorGUILayout.PropertyField(mountPoint, new GUIContent("Mount Point", "Reference for the Mount Point"));
                EditorGUILayout.Space();
                EditorGUILayout.PropertyField(rightIK, new GUIContent("Right Foot", "Reference for the Right Foot correct position on the mount"));
                EditorGUILayout.PropertyField(rightKnee, new GUIContent("Right Knee", "Reference for the Right Knee correct position on the mount"));
                EditorGUILayout.Space();
                EditorGUILayout.PropertyField(leftIK, new GUIContent("Left Foot", "Reference for the Left Foot correct position on the mount"));
                EditorGUILayout.PropertyField(leftKnee, new GUIContent("Left Knee", "Reference for the Left Knee correct position on the mount"));
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                EditorGUILayout.LabelField("Reins [Optional]", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(LeftRein, new GUIContent("Left Rein Point", "Reference for the Left Rein, to parent it to the Rider Left Hand while mounting"));
                EditorGUILayout.PropertyField(RightRein, new GUIContent("Right Rein Point", "Reference for the Right Rein, to parent it to the Rider Right Hand while mounting"));
            }
            EditorGUILayout.EndVertical();
        }

        private void ShowGeneral()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(active, new GUIContent("Active", "If the animal can be mounted. Deactivate if the mount is death or destroyed or is not ready to be mountable"));
                MalbersEditor.DrawDebugIcon(debug);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.PropertyField(Animal, new GUIContent("Animal", "Animal Reference for the Mounting System"));
                EditorGUILayout.PropertyField(ID, new GUIContent("ID", "Default should be 0.... change this and the Stance parameter on the Rider will change to that value... allowing other types of mounts like Wagon"));
                EditorGUILayout.PropertyField(mountIdle, new GUIContent("Mount Idle", "Animation to Play directly when instant mount is enabled"));
            }
            EditorGUILayout.EndVertical();


            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                EditorGUILayout.LabelField("Set values on Mounted", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(Set_InputMount, new GUIContent("Mount Input"));
                EditorGUILayout.PropertyField(Set_AIMount, new GUIContent("Mount AI"));
                EditorGUILayout.PropertyField(Set_MTriggersMount, new GUIContent("Mount Triggers"));
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                EditorGUILayout.LabelField("Set values on Dismounted", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(Set_InputDismount, new GUIContent("Mount Input"));
                EditorGUILayout.PropertyField(Set_AIDismount, new GUIContent("Mount AI"));
                EditorGUILayout.PropertyField(Set_MTriggersDismount, new GUIContent("Mount Triggers"));
            }
            EditorGUILayout.EndVertical();
        }
    }



    [CustomPropertyDrawer(typeof(SpeedTimeMultiplier))]
    public class SpeedTimeMultiplierDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {

            label = EditorGUI.BeginProperty(position, label, property);
            var indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            EditorGUI.BeginChangeCheck();

            var name = property.FindPropertyRelative("name");
            var AnimSpeed = property.FindPropertyRelative("AnimSpeed");
            var height = EditorGUIUtility.singleLineHeight;
            var line = position;
            line.height = height;

            var MainRect = new Rect(line.x, line.y, line.width / 2, height);
            var lerpRect = new Rect(line.x + line.width / 2, line.y, line.width / 2, height);

            EditorGUIUtility.labelWidth = 45f;
            EditorGUI.PropertyField(MainRect, name, new GUIContent("Name", "Name of the Speed to modify for the Rider"));
            EditorGUIUtility.labelWidth = 75f;
            EditorGUI.PropertyField(lerpRect, AnimSpeed, new GUIContent(" Speed Mult", "Anim Speed Multiplier"));
            if (name.stringValue == string.Empty) name.stringValue = "SpeedName";
            EditorGUIUtility.labelWidth = 0;

            if (EditorGUI.EndChangeCheck())
                property.serializedObject.ApplyModifiedProperties();

            EditorGUI.indentLevel = indent;
            EditorGUI.EndProperty();
        }
    }
#endif

    #endregion
}