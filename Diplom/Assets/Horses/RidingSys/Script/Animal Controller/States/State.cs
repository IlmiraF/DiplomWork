using RidingSystem.Scriptables;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using RidingSystem.Utilities;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace RidingSystem.Controller
{
    public abstract class State : ScriptableObject
    {
        public abstract string StateName { get; }

        [HideInInspector] public bool Active = true;

        internal MAnimal animal;

        protected float Height => animal.Height;



        #region Animal Shortcuts
        internal Vector3 MovementRaw => animal.MovementAxisRaw;

        internal Vector3 MovementSmooth => animal.MovementAxisSmoothed;
        protected Vector3 Gravity => animal.Gravity.normalized;
        protected Transform transform;
        protected LayerMask GroundLayer => animal.GroundLayer;

        protected Vector3 UpVector => animal.UpVector;
        protected Vector3 Forward => animal.Forward;
        protected Vector3 Up => animal.Up;

        protected Vector3 Right => animal.Right;
        protected Vector3 DeltaPos => animal.DeltaPos;
        protected float ScaleFactor => animal.ScaleFactor;
        #endregion




        [Space]
        public string Input;
        public StringReference ExitInput;

        public int Priority;

        public AnimalModifier General;
        public List<MesssageItem> GeneralMessage;
        public List<TagModifier> TagModifiers = new List<TagModifier>();
        public bool UseSendMessage = false;
        public bool IncludeChildren = true;

        internal Vector3 MovementAxisMult;

        public bool AllowExitFromAnim = false;

        public bool IncludeSleepState = true;

        public List<StateID> SleepFromState = new List<StateID>();

        public List<ModeID> SleepFromMode = new List<ModeID>();

        public List<StateID> QueueFrom = new List<StateID>();


        public List<StanceID> SleepFromStance = new List<StanceID>();

        public List<StanceID> stances = new List<StanceID>();

        public bool HasStances => stances != null && stances.Count > 0;

        public IntReference TryLoop = new IntReference(1);

        public StringReference EnterTag = new StringReference();
        public StringReference ExitTag = new StringReference();
        public bool ExitFrame = true;
        public bool ExitOnMain = true;
        public FloatReference EnterCooldown = new FloatReference(0.1f);
        public FloatReference ExitCooldown = new FloatReference(0.1f);

        public bool CanStrafe;
        [Range(0, 1)]
        public float MovementStrafe = 1f;

        internal bool ValidStance(StanceID currentStance)
        {
            if (!HasStances) return true;
            return stances.Contains(currentStance);
        }

        [Range(0, 1)]
        public float IdleStrafe = 1f;

        public bool debug = true;

        [HideInInspector] public int Editor_Tabs1;


        #region Properties
        protected QueryTriggerInteraction IgnoreTrigger => QueryTriggerInteraction.Ignore;

        public int UniqueID { get; private set; }

        protected Animator Anim => animal.Anim;

        internal OnEnterExitState EnterExitEvent;

        public bool CanBeActivated
        {
            get
            {
                if ((CurrentActiveState == null)
                || animal.JustActivateState
                || (!Active || IsSleep)
                || (CurrentActiveState.Priority > Priority && CurrentActiveState.IgnoreLowerStates)
                || (CurrentActiveState.IsPersistent)
                || IsActiveState
                || OnEnterCoolDown
                ) return false;

                return true;
            }
        }

        public bool OnEnterCoolDown => EnterCooldown > 0 && !MTools.ElapsedTime(CurrentExitTime, EnterCooldown + 0.01f);

        public int MainTagHash { get; private set; }


        protected int ExitTagHash { get; private set; }

        protected int EnterTagHash { get; private set; }

        public bool InExitAnimation => ExitTagHash != 0 && ExitTagHash == CurrentAnimTag;

        public bool InEnterAnimation => EnterTagHash != 0 && EnterTagHash == CurrentAnimTag;

        internal float CurrentExitTime { get; set; }

        internal float CurrentEnterTime { get; set; }

        protected int CurrentAnimTag => animal.AnimStateTag;

        protected State CurrentActiveState => animal.ActiveState;

        public bool CanExit { get; internal set; }
        public bool AllowingExit => !IgnoreLowerStates && !IsPersistent;

        public bool IsActiveState => animal.ActiveState == this;


        public virtual bool InputValue { get; set; }

        public virtual bool ExitInputValue { get; set; }

        public virtual bool IsSleepFromState { get; internal set; }

        public virtual bool IsSleepFromMode { get; internal set; }

        public virtual bool IsSleepFromStance { get; internal set; }

        public virtual bool IsSleep => IsSleepFromMode || IsSleepFromState || IsSleepFromStance;

        public virtual bool OnQueue { get; internal set; }

        public bool OnActiveQueue { get; internal set; }

        public bool InCoreAnimation => CurrentAnimTag == MainTagHash;

        public float CurrentSpeedPos
        {
            get => animal.CurrentSpeedModifier.position;
            set => animal.currentSpeedModifier.position = value;
        }

        public MSpeed CurrentSpeed => animal.CurrentSpeedModifier;


        public bool IsPersistent { get; set; }
        public bool IgnoreLowerStates { get; set; }

        public bool IsPending { get; set; }

        public bool PendingExit { get; set; }


        public List<MSpeedSet> SpeedSets { get; internal set; }
        #endregion

        public StateID ID;

        private IAnimatorListener[] listeners;


        #region Methods
        protected bool StateAnimationTags(int MainTag)
        {
            if (MainTagHash == MainTag) return true;

            var Foundit = TagModifiers.Find(tag => tag.TagHash == MainTag);

            return Foundit != null;
        }

        public void AwakeState(MAnimal mAnimal)
        {
            animal = mAnimal;
            transform = animal.transform;

            AwakeState();
        }

        public virtual void AwakeState()
        {
            if (ID == null) Debug.LogError($"State {name} is missing its ID", this);

            MainTagHash = Animator.StringToHash(ID.name);
            ExitTagHash = Animator.StringToHash(ExitTag.Value);
            EnterTagHash = Animator.StringToHash(EnterTag.Value);

            foreach (var mod in TagModifiers)
                mod.TagHash = Animator.StringToHash(mod.AnimationTag);

            SpeedSets = new List<MSpeedSet>();

            foreach (var set in animal.speedSets)
                if (set.states.Contains(ID)) SpeedSets.Add(set);

            if (SpeedSets.Count > 0) SpeedSets.Sort();


            EnterExitEvent = animal.OnEnterExitStates.Find(st => st.ID == ID);

            InputValue = false;
            ExitInputValue = false;
            ResetState();
            ResetStateValues();

            CurrentExitTime = -EnterCooldown;

            if (TryLoop < 1) TryLoop = 1;

            UniqueID = UnityEngine.Random.Range(0, 99999);

            if (!UseSendMessage)
            {
                if (IncludeChildren)
                    listeners = animal.GetComponentsInChildren<IAnimatorListener>();
                else
                    listeners = animal.GetComponents<IAnimatorListener>();
            }
        }

        public virtual Vector3 Speed_Direction() => animal.Forward * Mathf.Abs(animal.VerticalSmooth);


        public bool CheckQueuedState()
        {
            if (OnQueue)
            {
                OnActiveQueue = true;

                animal.ActiveState.AllowExit();
                animal.QueueState = this;
                return true;
            }
            return false;
        }

        internal void ConnectInput(IInputSource InputSource, bool connect)
        {
            if (!string.IsNullOrEmpty(Input))
            {
                var input = InputSource.GetInput(Input);

                if (input != null)
                {
                    if (connect)
                        input.InputChanged.AddListener(ActivatebyInput);
                    else
                        input.InputChanged.RemoveListener(ActivatebyInput);
                }
            }

            if (!string.IsNullOrEmpty(ExitInput.Value))
            {
                var exitInput = InputSource.GetInput(ExitInput.Value);

                if (exitInput != null)
                {
                    if (connect)
                        exitInput.InputChanged.AddListener(ExitByInput);
                    else
                        exitInput.InputChanged.RemoveListener(ExitByInput);
                }
            }

            ExtraInputs(InputSource, connect);
        }

        public virtual void ExtraInputs(IInputSource inputSource, bool connect) { }

        public virtual void Activate()
        {
            if (CheckQueuedState()) { return; }

            animal.LastState = animal.ActiveState;

            animal.Check_Queue_States(ID);

            if (animal.QueueReleased)
            {
                animal.QueueState.ActivateQueued();
                return;
            }
            if (animal.JustActivateState) { return; }

            animal.ActiveState = this;
            SetSpeed();
            MovementAxisMult = Vector3.one;

            CanExit = false;
            CurrentEnterTime = Time.time;


            if (animal.LastState != animal.ActiveState)
            {
                IsPending = true;
                PendingExit = true;
            }
            EnterExitEvent?.OnEnter.Invoke();
        }

        public virtual void ForceActivate()
        {
            animal.LastState = animal.ActiveState;

            animal.ActiveState = this;
            SetSpeed();

            CanExit = false;
            CurrentEnterTime = Time.time;

            if (animal.LastState != animal.ActiveState)
            {
                IsPending = true;
                PendingExit = true;
            }
            EnterExitEvent?.OnEnter.Invoke();
        }

        internal virtual void SetSpeed()
        {
            animal.CustomSpeed = false;
            foreach (var set in SpeedSets)
            {
                animal.CurrentSpeedSet = set;
                return;
            }

            var speedSet = new MSpeedSet()
            { name = this.name, Speeds = new List<MSpeed>(1) { new MSpeed(this.name, animal.CurrentSpeedModifier.Vertical.Value, 4, 4) } };
            animal.CustomSpeed = true;

            animal.CurrentSpeedSet = speedSet;
            animal.CurrentSpeedModifier = speedSet[0];
        }

        public virtual void ResetState()
        {
            IgnoreLowerStates = false;
            IsPersistent = false;
            IsPending = false;
            CanExit = false;
            IsSleepFromMode = false;
            IsSleepFromState = false;
            IsSleepFromStance = false;
            OnQueue = false;
            OnActiveQueue = false;
            CurrentExitTime = Time.time;
            MovementAxisMult = Vector3.one;
        }

        public virtual void RestoreAnimalOnExit() { }

        public virtual void ExitState()
        {
            ResetStateValues();
            ResetState();
            RestoreAnimalOnExit();
        }


        public void SetEnterStatus(int value) => animal.State_SetStatus(value);
        public void SetStatus(int value) => SetEnterStatus(value);
        public void SetFloat(float value) => animal.State_SetFloat(value);
        public void SetFloatSmooth(float value, float time)
        {
            if (animal.State_Float != 0f)
                animal.State_SetFloat(Mathf.MoveTowards(animal.State_Float, 0, time));
        }


        public void SetExitStatus(int value) => animal.State_SetExitStatus(value);

        public virtual void ActivateQueued()
        {
            OnQueue = false;
            OnActiveQueue = false;
            animal.QueueState = null;
            Activate();
        }

        private void SendMessagesTags(List<MesssageItem> msgs)
        {
            if (msgs != null && msgs.Count > 0)
            {
                if (UseSendMessage)
                {
                    foreach (var item in msgs)
                        item.DeliverMessage(animal, IncludeChildren, animal.debugStates && debug);
                }
                else
                {
                    if (listeners != null && listeners.Length > 0)
                    {
                        foreach (var animListeners in listeners)
                        {
                            foreach (var item in msgs)
                                item.DeliverAnimListener(animListeners, animal.debugStates && debug);
                        }
                    }
                }
            }
        }

        public void AnimationTagEnter(int animatorTagHash)
        {

            if (!IsActiveState) return;

            if (MainTagHash == animatorTagHash)
            {
                General.Modify(animal);
                CheckPendingExit();

                EnterCoreAnimation();

                SetExitStatus(0);
                SetEnterStatus(0);
                animal.SprintUpdate();

                SendMessagesTags(GeneralMessage);

                if (IsPending)
                {
                    IsPending = false;
                    animal.OnStateChange.Invoke(ID);
                }
            }
            else
            {
                TagModifier ActiveTag = TagModifiers.Find(tag => tag.TagHash == animatorTagHash);

                if (ActiveTag != null)
                {
                    ActiveTag.modifier.Modify(animal);
                    CheckPendingExit();
                    EnterTagAnimation();
                    animal.SprintUpdate();

                    SendMessagesTags(ActiveTag.tagMessages);

                    if (IsPending)
                    {
                        IsPending = false;
                        animal.OnStateChange.Invoke(ID);
                    }
                }
            }
        }

        private void CheckPendingExit()
        {
            if (IsPending && PendingExit)
            {
                animal.LastState?.PendingAnimationState();
                PendingExit = false;
            }
        }

        public void SetInput(bool value) => InputValue = value;

        public void ReceiveMessages(string message, object value) => this.Invoke(message, value);


        internal void ActivatebyInput(bool InputValue)
        {
            this.InputValue = InputValue;

            if (IsSleep)
            {
                this.InputValue = false;
                animal.InputSource?.SetInput(Input, false);
            }

            if (animal.LockInput) return;
            if (animal.JustActivateState) return;

            if (CanBeActivated)
            {
                StatebyInput();
            }
        }

        internal void ExitByInput(bool exitValue)
        {
            ExitInputValue = exitValue;
            if (IsActiveState == this && CanExit)
            {
                StateExitByInput();
            }
        }


        internal void SetCanExit()
        {
            if (!CanExit && !IsPending && !animal.m_IsAnimatorTransitioning)
            {
                if (MTools.ElapsedTime(CurrentEnterTime, ExitCooldown))
                {
                    if (ExitOnMain)
                    {
                        if (InCoreAnimation) CanExit = true;
                    }
                    else
                    {
                        CanExit = true;
                    }
                }
            }
        }

        public virtual void NewActiveState(StateID newState) { }


        public virtual void SpeedModifierChanged(MSpeed speed, int SpeedIndex) { }


        public bool AllowExit()
        {
            if (CanExit)
            {
                IgnoreLowerStates = false;
                IsPersistent = false;
                AllowStateExit();
            }
            return CanExit;
        }

        public virtual void AllowStateExit() { }

        public void AllowExit(int nextState)
        {
            if (!AllowExitFromAnim && AllowExit())
            {
                if (nextState != -1) animal.State_Activate(nextState);
            }
        }

        public void AllowExit(int nextState, int StateExitStatus)
        {
            SetExitStatus(StateExitStatus);

            if (!AllowExitFromAnim && AllowExit())
            {
                if (nextState != -1) animal.State_Activate(nextState);
            }
        }
        #endregion 

        #region Empty Methods

        public void Enable(bool value) => Active = value;

        public virtual void PendingAnimationState() { }

        public virtual void InitializeState() { }


        public virtual void EnterCoreAnimation() { }


        public virtual void EnterTagAnimation() { }

        public virtual void TryExitState(float DeltaTime) { }

        public virtual bool TryActivate() => InputValue && CanBeActivated;

        public virtual void StatebyInput()
        {
            if (IsSleep) InputValue = false;

            if (InputValue && TryActivate())
                Activate();
        }


        public virtual void StateExitByInput()
        {
            if (ExitInputValue) AllowExit();
        }

        public virtual void ResetStateValues() { }

        public virtual void OnStateMove(float deltatime) { }

        public virtual void OnStatePreMove(float deltatime) { }

        public virtual void OnModeStart(Mode mode) { }

        public virtual void OnModeEnd(Mode mode) { }

        public virtual void StateGizmos(MAnimal animal) { }

        public virtual bool CustomStateInspector() => false;
        #endregion
    }

    [System.Serializable]
    public class TagModifier
    {
        public string AnimationTag;
        public AnimalModifier modifier;
        public int TagHash { get; set; }

        public List<MesssageItem> tagMessages;
    }

    [System.Serializable]
    public struct AnimalModifier
    {
        [Utilities.Flag]
        public modifier modify;

        public bool RootMotion;
        public bool Sprint;
        public bool Gravity;
        public bool Grounded;
        public bool OrientToGround;
        public bool CustomRotation;
        public bool FreeMovement;
        public bool AdditivePosition;
        public bool AdditiveRotation;
        public bool Persistent;

        public bool IgnoreLowerStates;

        public bool LockMovement;

        public bool LockInput;


        public void Modify(MAnimal animal)
        {
            if ((int)modify == 0) return;

            if (Modify(modifier.IgnoreLowerStates)) { animal.ActiveState.IgnoreLowerStates = IgnoreLowerStates; }
            if (Modify(modifier.AdditivePositionSpeed)) { animal.UseAdditivePos = AdditivePosition; }

            if (Modify(modifier.AdditiveRotationSpeed)) { animal.UseAdditiveRot = AdditiveRotation; }
            if (Modify(modifier.RootMotion)) { animal.RootMotion = RootMotion; }
            if (Modify(modifier.Gravity)) { animal.UseGravity = Gravity; }
            if (Modify(modifier.Sprint)) { animal.UseSprintState = Sprint; }

            if (Modify(modifier.Grounded)) { animal.Grounded = Grounded; }
            if (Modify(modifier.OrientToGround)) { animal.UseOrientToGround = OrientToGround; }
            if (Modify(modifier.CustomRotation)) { animal.UseCustomAlign = CustomRotation; }
            if (Modify(modifier.Persistent)) { animal.ActiveState.IsPersistent = Persistent;}
            if (Modify(modifier.LockInput)) { animal.LockInput = LockInput; }
            if (Modify(modifier.LockMovement)) { animal.LockMovement = LockMovement; }
            if (Modify(modifier.FreeMovement)) { animal.FreeMovement = FreeMovement; }
        }

        private bool Modify(modifier modifier) => ((modify & modifier) == modifier);
    }
    public enum modifier
    {
        RootMotion = 1,
        Sprint = 2,
        Gravity = 4,
        Grounded = 8,
        OrientToGround = 16,
        CustomRotation = 32,
        IgnoreLowerStates = 64,
        Persistent = 128,
        LockMovement = 256,
        LockInput = 512,
        AdditiveRotationSpeed = 1024,
        AdditivePositionSpeed = 2048,
        FreeMovement = 4096,
    }



    #region Inspector


#if UNITY_EDITOR

    [CustomEditor(typeof(State), true)]
    public class StateEd : Editor
    {
        SerializedProperty
           ID, Input, ExitInput, Priority, General, GeneralMessage, TryLoop, EnterTag, ExitTag, ExitFrame, ExitOnMain, ExitCooldown, EnterCooldown,
            CanStrafe, MovementStrafe, IdleStrafe, debug, UseSendMessage, IncludeChildren, AllowExitAnimation, IncludeSleepState,
           SleepFromState, SleepFromMode, TagModifiers, QueueFrom, Editor_Tabs1, stances, SleepFromStance;

        State M;

        string[] Tabs = new string[4] { "General", "Tags", "Limits", "" };

        GUIStyle GreatLabel;

        private void OnEnable()
        {
            M = (State)target;
            Tabs[3] = M.ID ? M.ID.name : "Missing ID***";

            Editor_Tabs1 = serializedObject.FindProperty("Editor_Tabs1");

            ID = serializedObject.FindProperty("ID");
            Input = serializedObject.FindProperty("Input");
            ExitInput = serializedObject.FindProperty("ExitInput");
            Priority = serializedObject.FindProperty("Priority");
            TryLoop = serializedObject.FindProperty("TryLoop");
            AllowExitAnimation = serializedObject.FindProperty("AllowExitFromAnim");


            EnterTag = serializedObject.FindProperty("EnterTag");
            ExitTag = serializedObject.FindProperty("ExitTag");
            TagModifiers = serializedObject.FindProperty("TagModifiers");

            General = serializedObject.FindProperty("General");
            GeneralMessage = serializedObject.FindProperty("GeneralMessage");
            UseSendMessage = serializedObject.FindProperty("UseSendMessage");
            IncludeChildren = serializedObject.FindProperty("IncludeChildren");


            ExitFrame = serializedObject.FindProperty("ExitFrame");
            ExitOnMain = serializedObject.FindProperty("ExitOnMain");
            ExitCooldown = serializedObject.FindProperty("ExitCooldown");
            EnterCooldown = serializedObject.FindProperty("EnterCooldown");

            CanStrafe = serializedObject.FindProperty("CanStrafe");
            MovementStrafe = serializedObject.FindProperty("MovementStrafe");
            IdleStrafe = serializedObject.FindProperty("IdleStrafe");


            debug = serializedObject.FindProperty("debug");


            IncludeSleepState = serializedObject.FindProperty("IncludeSleepState");
            SleepFromState = serializedObject.FindProperty("SleepFromState");
            SleepFromMode = serializedObject.FindProperty("SleepFromMode");
            QueueFrom = serializedObject.FindProperty("QueueFrom");
            stances = serializedObject.FindProperty("stances");
            SleepFromStance = serializedObject.FindProperty("SleepFromStance");
        }

        public GUIContent Deb;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();


            if (GreatLabel == null)
                GreatLabel = new GUIStyle(EditorStyles.largeLabel) { fontStyle = FontStyle.Bold, fontSize = 14 };

            Editor_Tabs1.intValue = GUILayout.Toolbar(Editor_Tabs1.intValue, Tabs);


            int Selection = Editor_Tabs1.intValue;
            if (Selection == 0) ShowGeneral();
            else if (Selection == 1) ShowTags();
            else if (Selection == 2) ShowLimits();
            else if (Selection == 3) ShowState();

            serializedObject.ApplyModifiedProperties();

            Deb = new GUIContent((Texture)(AssetDatabase.LoadAssetAtPath("Assets/Malbers Animations/Common/Scripts/Editor/Icons/Debug_Icon.png", typeof(Texture))), "Debug");
        }

        private void ShowGeneral()
        {
            MalbersEditor.DrawDescription($"Common parameters of the State");

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(ID);

                var currentGUIColor = GUI.color;
                GUI.color = debug.boolValue ? Color.red : currentGUIColor;

                if (Deb == null)
                    Deb = new GUIContent((Texture)
                        (AssetDatabase.LoadAssetAtPath("Assets/Malbers Animations/Common/Scripts/Editor/Icons/Debug_Icon.png", typeof(Texture))), "Debug");

                debug.boolValue = GUILayout.Toggle(debug.boolValue, Deb, EditorStyles.miniButton, GUILayout.Width(25));
                GUI.color = currentGUIColor;
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.PropertyField(Input, new GUIContent("Enter Input"));
                EditorGUILayout.PropertyField(ExitInput);
                EditorGUILayout.PropertyField(Priority);
                EditorGUILayout.PropertyField(AllowExitAnimation);
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                EditorGUILayout.PropertyField(ExitFrame);
                EditorGUILayout.PropertyField(ExitOnMain);
                EditorGUILayout.PropertyField(EnterCooldown);
                EditorGUILayout.PropertyField(ExitCooldown);
                EditorGUILayout.PropertyField(TryLoop);
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                EditorGUILayout.PropertyField(CanStrafe);
                if (M.CanStrafe)
                {
                    EditorGUILayout.PropertyField(MovementStrafe);
                    EditorGUILayout.PropertyField(IdleStrafe);
                }
            }
            EditorGUILayout.EndVertical();


            ShowDebug();

        }

        private void ShowTags()
        {
            MalbersEditor.DrawDescription($"Animator Tags will modify the core parameters on the Animal.\nThe core tag value is the name of the ID - [{Tabs[3]}]");


            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                EditorGUILayout.PropertyField(EnterTag);
                EditorGUILayout.PropertyField(ExitTag);
            }
            EditorGUILayout.EndVertical();


            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                EditorGUILayout.PropertyField(General, new GUIContent("Tag [" + Tabs[3] + "]"), true);

                var st = new GUIStyle(EditorStyles.boldLabel);
                st.fontSize += 1;

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Messages", st);

                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                EditorGUIUtility.labelWidth = 85;
                EditorGUILayout.PropertyField(UseSendMessage, new GUIContent("Use SendMsg"));
                EditorGUIUtility.labelWidth = 55;
                EditorGUILayout.PropertyField(IncludeChildren, new GUIContent("Children"));
                EditorGUIUtility.labelWidth = 0;
                EditorGUILayout.EndHorizontal();

                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(GeneralMessage, new GUIContent("Messages [" + Tabs[3] + "]"), true);
                EditorGUI.indentLevel--;
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Animation Tags", st);
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(TagModifiers, new GUIContent(TagModifiers.displayName + " [" + TagModifiers.arraySize + "]"), true);
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndVertical();
        }


        private void ShowDebug()
        {
            if (M.debug && Application.isPlaying && M.animal)
            {
                using (new EditorGUI.DisabledGroupScope(true))
                {
                    using (new GUILayout.VerticalScope(EditorStyles.helpBox))
                    {
                        EditorGUILayout.Toggle("Enabled", M.Active);
                        EditorGUILayout.Toggle("Is Active State", M.IsActiveState);
                        EditorGUILayout.Toggle("Can Exit", M.CanExit);
                        EditorGUILayout.Toggle("OnQueue", M.OnQueue);
                        EditorGUILayout.Toggle("On Active Queue", M.OnActiveQueue);
                        EditorGUILayout.Toggle("Pending", M.IsPending);
                        EditorGUILayout.Toggle("Pending Exit", M.PendingExit);
                        EditorGUILayout.Toggle("Sleep From State", M.IsSleepFromState);
                        EditorGUILayout.Toggle("Sleep From Mode", M.IsSleepFromMode);
                        EditorGUILayout.Toggle("Sleep From Stance", M.IsSleepFromStance);
                        EditorGUILayout.Toggle("In Core Animation", M.InCoreAnimation);
                        EditorGUILayout.Toggle("Ignore Lower States", M.IgnoreLowerStates);
                        EditorGUILayout.Toggle("Is Persistent", M.IsPersistent);
                    }
                    Repaint();
                }
            }
        }
        private void ShowLimits()
        {
            MalbersEditor.DrawDescription($"Set Limitations to the States when another State, Mode or Stance is playing");

            using (new GUILayout.VerticalScope(EditorStyles.helpBox))
            {

                using (new GUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("States", GreatLabel);
                    var AcSleep = IncludeSleepState.boolValue ? "Sleep Inlcude" : "Sleep Exclude";
                    IncludeSleepState.boolValue = GUILayout.Toggle(IncludeSleepState.boolValue, new GUIContent(AcSleep), EditorStyles.miniButton, GUILayout.Width(100));
                }
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(SleepFromState, new GUIContent($"Sleep from States [{SleepFromState.arraySize}]"), true);
                EditorGUILayout.PropertyField(QueueFrom, new GUIContent($"Queue from States [{QueueFrom.arraySize}]"), true);
                EditorGUI.indentLevel--;
            }


            using (new GUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Modes", GreatLabel);
                EditorGUILayout.Space();

                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(SleepFromMode, new GUIContent(SleepFromMode.displayName + " [" + SleepFromMode.arraySize + "]"), true);
                EditorGUI.indentLevel--;
            }


            using (new GUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Stances", GreatLabel);
                EditorGUILayout.Space();
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(SleepFromStance, new GUIContent($"Sleep from Stances [{SleepFromStance.arraySize}]"), true);
                EditorGUILayout.PropertyField(stances, new GUIContent($"Allow Stances [" + stances.arraySize + "]"), true);
                EditorGUI.indentLevel--;
            }
        }

        protected virtual void ShowState()
        {
            MalbersEditor.DrawDescription($"{Tabs[3]} Parameters");

            if (!M.CustomStateInspector())
            {
                var skip = 27;
                var property = serializedObject.GetIterator();
                property.NextVisible(true);

                for (int i = 0; i < skip; i++)
                    property.NextVisible(false);

                do
                {
                    EditorGUILayout.PropertyField(property, true);
                } while (property.NextVisible(false));
            }
        }
    }


    [UnityEditor.CustomPropertyDrawer(typeof(AnimalModifier))]
    public class AnimalModifierDrawer : UnityEditor.PropertyDrawer
    {

        private float Division;
        int activeProperties;

        public override void OnGUI(Rect position, UnityEditor.SerializedProperty property, GUIContent label)
        {
            UnityEditor.EditorGUI.BeginProperty(position, label, property);

            GUI.Box(position, GUIContent.none, UnityEditor.EditorStyles.helpBox);

            position.x += 2;
            position.width -= 2;

            position.y += 2;
            position.height -= 2;


            var indent = UnityEditor.EditorGUI.indentLevel;
            UnityEditor.EditorGUI.indentLevel = 0;

            var height = UnityEditor.EditorGUIUtility.singleLineHeight;

            #region Serialized Properties
            var modify = property.FindPropertyRelative("modify");
            var Colliders = property.FindPropertyRelative("Colliders");
            var RootMotion = property.FindPropertyRelative("RootMotion");
            var Sprint = property.FindPropertyRelative("Sprint");
            var Gravity = property.FindPropertyRelative("Gravity");
            var OrientToGround = property.FindPropertyRelative("OrientToGround");
            var CustomRotation = property.FindPropertyRelative("CustomRotation");
            var IgnoreLowerStates = property.FindPropertyRelative("IgnoreLowerStates");
            var AdditivePositionSpeed = property.FindPropertyRelative("AdditivePosition");
            var AdditiveRotation = property.FindPropertyRelative("AdditiveRotation");
            var Grounded = property.FindPropertyRelative("Grounded");
            var FreeMovement = property.FindPropertyRelative("FreeMovement");
            var Persistent = property.FindPropertyRelative("Persistent");
            var LockInput = property.FindPropertyRelative("LockInput");
            var LockMovement = property.FindPropertyRelative("LockMovement");
            #endregion

            var line = position;
            var lineLabel = line;
            line.height = height;

            var foldout = lineLabel;
            foldout.width = 10;
            foldout.x += 10;

            UnityEditor.EditorGUIUtility.labelWidth = 16;
            UnityEditor.EditorGUIUtility.labelWidth = 0;

            modify.intValue = (int)(modifier)UnityEditor.EditorGUI.EnumFlagsField(line, label, (modifier)(modify.intValue));

            line.y += height + 2;
            Division = line.width / 3;

            activeProperties = 0;
            int ModifyValue = modify.intValue;

            if (Modify(ModifyValue, modifier.RootMotion))
                DrawProperty(ref line, RootMotion, new GUIContent("RootMotion", "Root Motion:\nEnable/Disable the Root Motion on the Animator"));

            if (Modify(ModifyValue, modifier.Sprint))
                DrawProperty(ref line, Sprint, new GUIContent("Sprint", "Sprint:\nEnable/Disable Sprinting on the Animal"));

            if (Modify(ModifyValue, modifier.Gravity))
                DrawProperty(ref line, Gravity, new GUIContent("Gravity", "Gravity:\nEnable/Disable the Gravity on the Animal. Used when is falling or jumping"));

            if (Modify(ModifyValue, modifier.Grounded))
                DrawProperty(ref line, Grounded, new GUIContent("Grounded", "Grounded\nEnable/Disable if the Animal is Grounded (If True it will  calculate  the Alignment for Position with the ground ). If False:  Orient to Ground is also disabled."));

            if (Modify(ModifyValue, modifier.CustomRotation))
                DrawProperty(ref line, CustomRotation, new GUIContent("Custom Rot", "Custom Rotation: \nEnable/Disable the Custom Rotations (Used in Fly, Climb, UnderWater, Swim), This will disable Orient to Ground"));

            UnityEditor.EditorGUI.BeginDisabledGroup(CustomRotation.boolValue || !Grounded.boolValue);
            if (Modify(ModifyValue, modifier.OrientToGround))
                DrawProperty(ref line, OrientToGround, new GUIContent("Orient Ground", "Orient to Ground:\nEnable/Disable the Rotation Alignment while grounded. (If False the Animal will be aligned with the Up Vector)"));
            UnityEditor.EditorGUI.EndDisabledGroup();

            if (Modify(ModifyValue, modifier.IgnoreLowerStates))
                DrawProperty(ref line, IgnoreLowerStates, new GUIContent("Ignore Lower States", "States below will not be able to try to activate themselves"));

            if (Modify(ModifyValue, modifier.Persistent))
                DrawProperty(ref line, Persistent, new GUIContent("Persistent", "Persistent:\nEnable/Disable is Persistent on the Active State ... meaning the Animal will not Try to activate any States"));

            if (Modify(ModifyValue, modifier.LockMovement))
                DrawProperty(ref line, LockMovement, new GUIContent("Lock Move", "Lock Movement:\nLock the Movement on the Animal, does not include Action Inputs for Attack, Jump, Action, etc"));

            if (Modify(ModifyValue, modifier.LockInput))
                DrawProperty(ref line, LockInput, new GUIContent("Lock Input", "Lock Input:\nLock the Inputs, (Jump, Attack, etc) does not include Movement Input (WASD or Axis Inputs)"));

            if (Modify(ModifyValue, modifier.AdditiveRotationSpeed))
                DrawProperty(ref line, AdditiveRotation, new GUIContent("+ Rot Speed", "Additive Rotation Speed:\nEnable/Disable Additive Rotation used on the Speed Modifier"));

            if (Modify(ModifyValue, modifier.AdditivePositionSpeed))
                DrawProperty(ref line, AdditivePositionSpeed, new GUIContent("+ Pos Speed", "Additive Position Speed:\nEnable/Disable Additive Position used on the Speed Modifiers"));


            if (Modify(ModifyValue, modifier.FreeMovement))
                DrawProperty(ref line, FreeMovement, new GUIContent("Free Move", "Free Movement:\nEnable/Disable the Free Movement... This allow to Use the Pitch direction vector and the Rotator Transform"));

            UnityEditor.EditorGUI.indentLevel = indent;
            UnityEditor.EditorGUI.EndProperty();
        }

        private void DrawProperty(ref Rect rect, UnityEditor.SerializedProperty property, GUIContent content)
        {
            Rect splittedLine = rect;
            splittedLine.width = Division - 1;

            splittedLine.x += (Division * (activeProperties % 3)) + 1;

            property.boolValue = UnityEditor.EditorGUI.ToggleLeft(splittedLine, content, property.boolValue);

            activeProperties++;
            if (activeProperties % 3 == 0)
            {
                rect.y += UnityEditor.EditorGUIUtility.singleLineHeight + 2;
            }
        }


        private bool Modify(int modify, modifier modifier)
        {
            return ((modify & (int)modifier) == (int)modifier);
        }

        public override float GetPropertyHeight(UnityEditor.SerializedProperty property, GUIContent label)
        {
            int activeProperties = 0;

            var modify = property.FindPropertyRelative("modify");
            int ModifyValue = modify.intValue;

            if (Modify(ModifyValue, modifier.RootMotion)) activeProperties++;
            if (Modify(ModifyValue, modifier.Sprint)) activeProperties++;
            if (Modify(ModifyValue, modifier.Gravity)) activeProperties++;
            if (Modify(ModifyValue, modifier.Grounded)) activeProperties++;
            if (Modify(ModifyValue, modifier.CustomRotation)) activeProperties++;
            if (Modify(ModifyValue, modifier.OrientToGround)) activeProperties++;
            if (Modify(ModifyValue, modifier.IgnoreLowerStates)) activeProperties++;
            if (Modify(ModifyValue, modifier.AdditivePositionSpeed)) activeProperties++;
            if (Modify(ModifyValue, modifier.AdditiveRotationSpeed)) activeProperties++;
            if (Modify(ModifyValue, modifier.Persistent)) activeProperties++;
            if (Modify(ModifyValue, modifier.FreeMovement)) activeProperties++;
            if (Modify(ModifyValue, modifier.LockMovement)) activeProperties++;
            if (Modify(ModifyValue, modifier.LockInput)) activeProperties++;

            float lines = (int)((activeProperties + 2) / 3) + 1;

            return base.GetPropertyHeight(property, label) * lines + (2 * lines);
        }
    }
#endif
    #endregion
}
