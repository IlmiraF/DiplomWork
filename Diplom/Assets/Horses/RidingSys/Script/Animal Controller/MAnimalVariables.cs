using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Events;
using RidingSystem.Events;
using RidingSystem.Scriptables;
using System;

namespace RidingSystem.Controller
{
    public partial class MAnimal
    {

        public System.Action<int, bool> SetBoolParameter { get; set; } = delegate { };
        public System.Action<int, float> SetFloatParameter { get; set; } = delegate { };

        public System.Action<int, int> SetIntParameter { get; set; } = delegate { };

        public System.Action<int> SetTriggerParameter { get; set; } = delegate { };

        public System.Action<int> StateCycle { get; set; }

        public System.Action<MAnimal> PreStateMovement = delegate { };

        public System.Action<MAnimal> PostStateMovement = delegate { };

        private List<int> animatorHashParams;

        #region Static Properties
        public static List<MAnimal> Animals;
        public static MAnimal MainAnimal;
        #endregion

        #region States
        public bool CloneStates = true;

        public bool NoParent = true;

        public List<State> states = new List<State>();

        public StateID OverrideStartState;

        internal State activeState;
        internal State lastState;

        internal State queueState;

        public bool QueueReleased => QueueState != null && QueueState.OnActiveQueue && !QueueState.OnQueue;

        public State QueueState
        {
            get => queueState;
            internal set
            {
                queueState = value;
            }
        }


        public State LastState
        {
            get => lastState;
            internal set
            {
                {
                    lastState = value;
                    LastState.EnterExitEvent?.OnExit.Invoke();

                    LastState.ExitState();

                    var LastStateID = (QueueState == null) ? lastState.ID.ID : QueueState.ID.ID;

                    SetIntParameter(hash_LastState, LastStateID);
                }
            }
        }

        protected State Pin_State;

        public bool JustActivateState { get; internal set; }

        public StateID ActiveStateID { get; private set; }

        public float State_Float { get; private set; }

        public State ActiveState
        {
            get => activeState;
            internal set
            {
                activeState = value;

                if (value == null) return;

                JustActivateState = true;

                if (LastState != null && LastState.ExitFrame) LastState.OnStateMove(DeltaTime);

                this.Delay_Action(() => { JustActivateState = false; });
                ActiveStateID = activeState.ID;

                OnStateActivate.Invoke(activeState.ID);

                SetIntParameter(hash_State, activeState.ID.ID); 

                SetOptionalAnimParameter(hash_StateOn);
                Strafe = Strafe;

                foreach (var st in states) st.NewActiveState(activeState.ID);

                Set_Sleep_FromStates(activeState);
                Check_Queue_States(activeState.ID);


                if (IsPlayingMode && ActiveMode.StateCanInterrupt(ActiveStateID))
                {
                    Mode_Interrupt();
                }
            }
        }

        internal void Set_Sleep_FromStates(State state)
        {
            foreach (var st in states)
            {
                st.IsSleepFromState = st.SleepFromState.Contains(state.ID);
                st.IsSleepFromState ^= !st.IncludeSleepState;
            }
        }

        internal virtual void Set_State_Sleep_FromMode(bool playingMode)
        {
            foreach (var state in states)
                state.IsSleepFromMode = playingMode && state.SleepFromMode.Contains(ActiveMode.ID);
        }


        internal virtual void Set_State_Sleep_FromStance()
        {
            foreach (var state in states)
                state.IsSleepFromStance = state.SleepFromStance.Contains(Stance);
        }

        internal virtual void Check_Queue_States(StateID ID)
        {
            foreach (var st in states)
            {
                st.OnQueue = st.QueueFrom.Contains(ID);
            }
        }

        #endregion

        #region General  
        [SerializeField] private LayerReference groundLayer = new LayerReference(1);

        public LayerMask GroundLayer => groundLayer.Value;

        public float height = 1f;
        public float Height => (height) * ScaleFactor;

        public float ScaleFactor => transform.localScale.y;

        public IInputSource InputSource;

        [SerializeField] private Vector3 center;
        public Vector3 Center
        {
            private set => center = value;
            get => transform.TransformPoint(center);
        }
        #endregion

        #region Stance

        [SerializeField] private StanceID currentStance;
        [SerializeField] private StanceID defaultStance;


        public int LastStance { get; private set; }

        public StanceID DefaultStance { get => defaultStance; set => defaultStance = value; }


        public StanceID Stance
        {
            get => currentStance;
            set
            {
                if (Sleep || !enabled) return;
                if (!ActiveState.ValidStance(value)) return;

                LastStance = currentStance;
                currentStance = value;

                var exit = OnEnterExitStances.Find(st => st.ID.ID == LastStance);
                exit?.OnExit.Invoke();
                OnStanceChange.Invoke(value);
                var enter = OnEnterExitStances.Find(st => st.ID.ID == value);
                enter?.OnEnter.Invoke();

                Set_State_Sleep_FromStance();

                if (debugStances)
                {
                    Debug.Log($"<B>[{name}]</B> → <B>[Set Stance] → <color=yellow>{value.name} [{value.ID}]</color></B>");
                }

                SetOptionalAnimParameter(hash_Stance, currentStance.ID);
                SetOptionalAnimParameter(hash_LastStance, LastStance);
                SetOptionalAnimParameter(hash_StateOn);

                if (!JustActivateState) SetIntParameter(hash_LastState, ActiveStateID);

                ActiveState.SetSpeed();

                if (IsPlayingMode && ActiveMode.StanceCanInterrupt(currentStance))
                {
                    Mode_Interrupt();
                }
            }
        }
        #endregion

        #region Movement
        public FloatReference AnimatorSpeed = new FloatReference(1);

        [SerializeField] private BoolReference alwaysForward = new BoolReference(false);

        [SerializeField] private BoolReference lockForwardMovement = new BoolReference(false);
        [SerializeField] private BoolReference lockHorizontalMovement = new BoolReference(false);
        [SerializeField] private BoolReference lockUpDownMovement = new BoolReference(false);

        public float ForwardMultiplier { get; set; }

        public Vector3 MovementAxis;

        public Vector3 MovementAxisRaw;

        public Vector3 RawInputAxis;

        public bool UseRawInput { get; internal set; }

        public Vector3 MovementAxisSmoothed;

        public bool AlwaysForward
        {
            get => alwaysForward.Value;
            set
            {
                alwaysForward.Value = value;
                MovementAxis.z = alwaysForward.Value ? 1 : 0;
                MovementDetected = AlwaysForward;
            }
        }

        public Vector3 Move_Direction;

        private bool movementDetected;

        public bool MovementDetected
        {
            get => movementDetected;
            internal set
            {
                if (movementDetected != value)
                {
                    movementDetected = value;
                    OnMovementDetected.Invoke(value);
                    SetBoolParameter(hash_Movement, MovementDetected);
                }
            }
        }

        public BoolReference useCameraInput = new BoolReference(true);

        public BoolReference useCameraUp = new BoolReference();

        public bool DefaultCameraInput { get; set; }

        public void ResetCameraInput() => UseCameraInput = DefaultCameraInput;

        public bool UseCameraUp { get => useCameraUp.Value; set => useCameraUp.Value = value; }

        public bool UseCameraInput
        {
            get => useCameraInput.Value;
            set { useCameraInput.Value = UsingMoveWithDirection = value; }
        }

        public bool UsingMoveWithDirection { set; get; }

        public bool Rotate_at_Direction { set; get; }

        public TransformReference m_MainCamera = new TransformReference();

        public Transform MainCamera => m_MainCamera.Value;


        [SerializeField] private bool additivePosLog;
        [SerializeField] private bool additiveRotLog;


        private void DebLogAdditivePos()
        {
            additivePosLog ^= true;
            MTools.SetDirty(this);
        }

        private void DebLogAdditiveRot()
        {
            additiveRotLog ^= true;
            MTools.SetDirty(this);
        }
        [ContextMenuItem("Debug AdditivePos", nameof(DebLogAdditivePos))]
        [ContextMenuItem("Debug AdditiveRot", nameof(DebLogAdditiveRot))]

        public BoolReference isPlayer = new BoolReference(true);


        public Vector3 AdditivePosition
        {
            get => additivePosition;
            set
            {
                additivePosition = value;
                if (additivePosLog)
                    Debug.Log($"Additive Pos:  {(additivePosition / DeltaTime)} ");
            }
        }
        internal Vector3 additivePosition;

        public Quaternion AdditiveRotation
        {
            get => additiveRotation;
            set
            {
                additiveRotation = value;
                if (additiveRotLog) Debug.Log($"Additive ROT:  {(additiveRotation):F3} ");
            }
        }
        Quaternion additiveRotation;




        public Vector3 InertiaPositionSpeed { get; internal set; }


        [SerializeField] private BoolReference SmoothVertical = new BoolReference(true);

        [Tooltip("Global turn multiplier to increase rotation on the animal")]
        public FloatReference TurnMultiplier = new FloatReference(0f);

        [Tooltip("Smooth Damp Value to Turn in place, when using LookAt Direction Instead of Move()")]
        public FloatReference inPlaceDamp = new FloatReference(2f);

        public Vector3 DeltaPos { get; internal set; }

        public Vector3 LastPos { get; internal set; }

        public Vector3 Inertia => DeltaPos / DeltaTime;

        public float DeltaAngle { get; internal set; }

        public Vector3 PitchDirection { get; internal set; }
        public float PitchAngle { get; internal set; }
        public float Bank { get; internal set; }

        public float VerticalSmooth { get => MovementAxisSmoothed.z; internal set => MovementAxisSmoothed.z = value; }

        public float HorizontalSmooth { get => MovementAxisSmoothed.x; internal set => MovementAxisSmoothed.x = value; }

        public float UpDownSmooth
        {
            get => MovementAxisSmoothed.y;
            internal set
            {
                MovementAxisSmoothed.y = value;
            }
        }


        public float DeltaUpDown { get; internal set; }


        public bool UseSmoothVertical { get => SmoothVertical.Value; set => SmoothVertical.Value = value; }

        public float DeltaTime { get; private set; }

        #endregion

        #region Alignment Ground
        public FloatReference AlignPosLerp = new FloatReference(15f);
        public FloatReference AlignPosDelta = new FloatReference(2.5f);
        public FloatReference AlignRotDelta = new FloatReference(2.5f);

        public float AlignPosLerpDelta { get; private set; }

        public float AlignRotLerpDelta { get; private set; }

        public FloatReference AlignRotLerp = new FloatReference(15f);


        public IntReference AlignLoop = new IntReference(1);

        [Tooltip("Tag your small rocks, debris,steps and stair objects  with this Tag. It will help the animal to recognize better the Terrain")]
        public StringReference DebrisTag = new StringReference("Stair");

        [Range(1f, 90f), Tooltip("Maximun angle on the terrain the animal can walk")]
        public float maxAngleSlope = 45f;

        public float MainPivotSlope { get; private set; }

        public Transform Rotator;
        public Transform RootBone;

        public bool DeepSlope => TerrainSlope < -maxAngleSlope;

        public bool isinSlope => Mathf.Abs(TerrainSlope) > maxAngleSlope;

        public float HorizontalSpeed { get; private set; }

        public Vector3 HorizontalVelocity { get; private set; }


        public Vector3 SurfaceNormal { get; internal set; }

        public float SlopeNormalized => TerrainSlope / maxAngleSlope;

        public float TerrainSlope { get; private set; }


        [SerializeField] private BoolReference grounded = new BoolReference(false);
        public bool Grounded
        {
            get => grounded.Value;
            set
            {
                if (grounded.Value != value)
                {
                    grounded.Value = value;

                    if (!value)
                    {
                        platform = null;
                    }
                    else
                    {
                        ResetGravityValues();
                        Force_Reset();

                        UpDownAdditive = 0; 
                        UsingUpDownExternal = false;
                        GravityMultiplier = 1;
                        ExternalForceAirControl = true;
                    }

                    SetBoolParameter(hash_Grounded, grounded.Value);
                }
                OnGrounded.Invoke(value);
            }
        }
        #endregion

        #region External Force

        public Vector3 ExternalForce { get; set; }

        public Vector3 CurrentExternalForce { get; set; }

        public float ExternalForceAcel { get; set; }

        public bool ExternalForceAirControl { get; set; }

        public bool HasExternalForce => ExternalForce != Vector3.zero;
        #endregion

        #region References
        [RequiredField] public Animator Anim;
        [RequiredField] public Rigidbody RB; 

        public Vector3 Up => transform.up;
        public Vector3 Right => transform.right;
        public Vector3 Forward => transform.forward;


        #endregion

        #region Modes
        public IntReference StartWithMode = new IntReference(0);

        public int ModeStatus { get; private set; }

        public float ModePower { get; set; }

        private Mode activeMode;

        public List<Mode> modes = new List<Mode>();

        public bool IsPlayingMode => activeMode != null;

        public bool IsPreparingMode { get; set; }

        public Zone Zone { get; internal set; }

        public int LastModeID { get; internal set; }

        public int LastAbilityIndex { get; internal set; }

        public Mode ActiveMode
        {
            get => activeMode;
            internal set
            {
                var lastMode = activeMode;
                activeMode = value;
                ModeTime = 0;

                if (value != null)
                {
                    OnModeStart.Invoke(ActiveModeID = value.ID, value.AbilityIndex);
                    ActiveState.OnModeStart(activeMode);
                }
                else
                {
                    ActiveModeID = 0;
                }

                if (lastMode != null)
                {
                    LastModeID = lastMode.ID;
                    LastAbilityIndex = lastMode.AbilityIndex;
                    OnModeEnd.Invoke(lastMode.ID, LastAbilityIndex);
                    ActiveState.OnModeEnd(lastMode);
                }
            }
        }

        internal virtual void SetModeParameters(Mode value, int status)
        {
            if (value != null)
            {
                var ability = (value.ActiveAbility != null ? (int)value.ActiveAbility.Index : 0);

                int mode = Mathf.Abs(value.ID * 1000) + Mathf.Abs(ability);

                ModeAbility = (value.ID < 0 || ability < 0) ? -mode : mode;

                SetOptionalAnimParameter(hash_ModeOn);

                if (hash_ModeOn != 0 && status != 0)
                    SetModeStatus(status);
                else
                    SetModeStatus(status);

                IsPreparingMode = true;
                ModeTime = 0;
            }
            else
            {
                SetModeStatus(Int_ID.Available);
                ModeAbility = 0;
            }
        }

        public int ModeAbility
        {
            get => m_ModeIDAbility;
            internal set
            {
                m_ModeIDAbility = value;
                SetIntParameter?.Invoke(hash_Mode, m_ModeIDAbility);
            }
        }
        private int m_ModeIDAbility;

        public float ModeTime { get; internal set; }

        public int ActiveModeID { get; private set; }

        public Mode Pin_Mode { get; private set; }

        #endregion

        #region Sleep
        [SerializeField] private BoolReference sleep = new BoolReference(false);

        public bool Sleep
        {
            get => sleep.Value;
            set
            {
                if (!value && Sleep)
                {
                    MTools.ResetFloatParameters(Anim);
                    ResetController();
                }
                sleep.Value = value;

                LockInput = LockMovement = value;
                if (Sleep) SetOptionalAnimParameter(hash_Random, 0);
            }
        }
        #endregion 

        #region Strafe
        public BoolEvent OnStrafe = new BoolEvent();

        [SerializeField] private BoolReference m_strafe = new BoolReference(false);
        [SerializeField] private BoolReference m_CanStrafe = new BoolReference(false);
        [SerializeField] private BoolReference m_StrafeNormalize = new BoolReference(false);
        [SerializeField] private FloatReference m_StrafeLerp = new FloatReference(5f);


        public bool StrafeNormalize => m_StrafeNormalize.Value;

        public bool Strafe
        {
            get => m_CanStrafe.Value && m_strafe.Value && ActiveState.CanStrafe;
            set
            {
                if (sleep) return;

                var newStrafe = value && m_CanStrafe.Value && ActiveState.CanStrafe;

                if (newStrafe != m_strafe.Value)
                {
                    m_strafe.Value = newStrafe;
                    OnStrafe.Invoke(m_strafe.Value);
                    SetOptionalAnimParameter(hash_Strafe, m_strafe.Value);
                    SetOptionalAnimParameter(hash_StateOn);

                    if (!JustActivateState) SetIntParameter(hash_LastState, ActiveStateID);

                    if (!m_strafe.Value)
                    {
                        ResetCameraInput();
                    }
                }
            }
        }

        public bool CanStrafe { get => m_CanStrafe.Value; set => m_CanStrafe.Value = value; }

        private float StrafeDeltaValue;
        private float HorizontalAimAngle_Raw;

        #endregion

        #region Pivots

        internal RaycastHit hit_Hip;
        internal RaycastHit hit_Chest;

        public List<MPivots> pivots = new List<MPivots>
            { new MPivots("Hip", new Vector3(0,0.7f,-0.7f), 1), new MPivots("Chest", new Vector3(0,0.7f,0.7f), 1), new MPivots("Water", new Vector3(0,1,0), 0.05f) };

        public MPivots Pivot_Hip;
        public MPivots Pivot_Chest;

        public int AlignUniqueID { get; private set; }

        public bool Has_Pivot_Hip;

        public bool Has_Pivot_Chest;

        public bool MainRay { get; private set; }
        public bool FrontRay { get; private set; }

        public Vector3 Main_Pivot_Point
        {
            get
            {
                Vector3 pivotPoint;
                if (Has_Pivot_Chest)
                {
                    pivotPoint = Pivot_Chest.World(transform);
                }
                else if (Has_Pivot_Hip)
                {
                    pivotPoint = Pivot_Hip.World(transform);
                }
                else
                {
                    pivotPoint = transform.TransformPoint(new Vector3(0, Height, 0));
                }

                return pivotPoint + DeltaVelocity;
            }
        }

        public Vector3 DeltaVelocity { get; private set; }

        private bool Starting_PivotChest;

        public void DisablePivotChest() => Has_Pivot_Chest = false;

        public void ResetPivotChest() => Has_Pivot_Chest = Starting_PivotChest;
        public void UsePivotChest(bool value) => Has_Pivot_Chest = value;


        public bool NoPivot => !Has_Pivot_Chest && !Has_Pivot_Hip;

        public float Pivot_Multiplier
        {
            get
            {
                float multiplier = Has_Pivot_Chest ? Pivot_Chest.multiplier : (Has_Pivot_Hip ? Pivot_Hip.multiplier : 1f);
                return multiplier * ScaleFactor * (NoPivot ? 1.5f : 1f);
            }
        }
        #endregion

        #region Speed Modifiers

        public Vector3 TargetSpeed { get; internal set; }
        public Vector3 DesiredRBVelocity { get; internal set; }

        public bool SpeedChangeLocked { get; private set; }

        public List<MSpeedSet> speedSets;
        private MSpeedSet currentSpeedSet = new MSpeedSet();
        internal MSpeedSet defaultSpeedSet = new MSpeedSet()
        { name = "Default Set", Speeds = new List<MSpeed>(1) { new MSpeed("Default", 1, 4, 4) } };

        public bool CustomSpeed;

        public MSpeed currentSpeedModifier = MSpeed.Default;
        internal MSpeed SprintSpeed = MSpeed.Default;

        protected int speedIndex;

        public MSpeed CurrentSpeedModifier
        {
            get
            {
                if (Sprint && !CustomSpeed) return SprintSpeed;
                return currentSpeedModifier;
            }
            internal set
            {
                if (currentSpeedModifier.name != value.name)
                {
                    currentSpeedModifier = value;
                    OnSpeedChange.Invoke(CurrentSpeedModifier);
                    EnterSpeedEvent(CurrentSpeedIndex);
                    ActiveState?.SpeedModifierChanged(CurrentSpeedModifier, CurrentSpeedIndex);
                }
            }
        }


        public int CurrentSpeedIndex
        {
            get => speedIndex;
            internal set
            {
                if (CustomSpeed || SpeedChangeLocked || CurrentSpeedSet == null) return;

                var speedModifiers = CurrentSpeedSet.Speeds;

                var newValue = Mathf.Clamp(value, 1, speedModifiers.Count);
                if (newValue > CurrentSpeedSet.TopIndex) newValue = CurrentSpeedSet.TopIndex;

                newValue = Mathf.Clamp(value, 1, newValue);


                if (speedIndex != newValue)
                {
                    speedIndex = newValue;

                    var sprintSpeed = Mathf.Clamp(speedIndex + 1, 1, speedModifiers.Count);

                    CurrentSpeedModifier = speedModifiers[speedIndex - 1];

                    SprintSpeed = speedModifiers[sprintSpeed - 1];

                    if (CurrentSpeedSet != null)
                        CurrentSpeedSet.CurrentIndex = speedIndex;
                }
            }
        }

        public MSpeedSet CurrentSpeedSet
        {
            get => currentSpeedSet;
            internal set
            {
                if (value.name != currentSpeedSet.name)
                {
                    currentSpeedSet = value;
                    speedIndex = -1;
                    JustChangedSpeedSet = true;
                    CurrentSpeedIndex = currentSpeedSet.CurrentIndex;
                    JustChangedSpeedSet = false;

                    EnterSpeedEvent(CurrentSpeedIndex);
                }
            }
        }

        bool JustChangedSpeedSet;

        private void EnterSpeedEvent(int index)
        {
            if (JustChangedSpeedSet) return;

            if (OnEnterExitSpeeds != null)
            {
                var SpeedEnterEvent = OnEnterExitSpeeds.Find(s => s.SpeedIndex == index && s.SpeedSet == CurrentSpeedSet.name);

                if (OldEnterExitSpeed != null && SpeedEnterEvent != OldEnterExitSpeed)
                {
                    OldEnterExitSpeed.OnExit.Invoke();
                    OldEnterExitSpeed = null;
                }


                if (SpeedEnterEvent != null)
                {
                    SpeedEnterEvent.OnEnter.Invoke();
                    OldEnterExitSpeed = SpeedEnterEvent;
                }
            }
        }

        private OnEnterExitSpeed OldEnterExitSpeed;

        public void ResetSpeedSet() => CurrentSpeedSet = defaultSpeedSet;

        internal float SpeedMultiplier { get; set; }

        internal bool sprint;
        internal bool realSprint;

        public bool Sprint
        {
            get => UseSprintState && sprint && UseSprint && MovementDetected && !SpeedChangeLocked;
            set
            {
                var newRealSprint = UseSprintState && value && UseSprint && MovementDetected && !SpeedChangeLocked; 

                sprint = value;

                if (realSprint != newRealSprint)
                {
                    realSprint = newRealSprint;

                    OnSprintEnabled.Invoke(realSprint);
                    SetOptionalAnimParameter(hash_Sprint, realSprint);

                    int currentPI = CurrentSpeedIndex;
                    var speed = CurrentSpeedModifier;

                    if (realSprint)
                    {
                        speed = SprintSpeed;
                        currentPI++;
                    }

                    OnSpeedChange.Invoke(speed);
                    EnterSpeedEvent(currentPI);

                    ActiveState?.SpeedModifierChanged(speed, currentPI);
                }
            }
        }

        public void SetSprint(bool value) => Sprint = value;

        internal int CurrentCycle { get; private set; }

        #endregion 

        #region Gravity
        [SerializeField] private Vector3Reference m_gravityDir = new Vector3Reference(Vector3.down);

        [SerializeField] private FloatReference m_gravityPower = new FloatReference(9.8f);

        [SerializeField] private IntReference m_gravityTime = new IntReference(10);
        [SerializeField] private IntReference m_gravityTimeLimit = new IntReference(0);


        public int StartGravityTime { get => m_gravityTime.Value; internal set => m_gravityTime.Value = value; }
        public int LimitGravityTime { get => m_gravityTimeLimit.Value; internal set => m_gravityTimeLimit.Value = value; }

        public float GravityMultiplier { get; internal set; }


        public int GravityTime { get; internal set; }


        public float GravityPower { get => m_gravityPower.Value * GravityMultiplier; set => m_gravityPower.Value = value; }


        public Vector3 GravityStoredVelocity { get; internal set; }

        public Vector3 Gravity { get => m_gravityDir.Value; set => m_gravityDir.Value = value; }

        public Vector3 UpVector => -m_gravityDir.Value;

        public BoolReference ground_Changes_Gravity = new BoolReference(false);

        #endregion

        #region Advanced Parameters

        [Range(0, 180), Tooltip("Slow the Animal when the Turn Angle is ouside this limit")]
        public float TurnLimit = 120;

        public BoolReference rootMotion = new BoolReference(true);

        public FloatReference rayCastRadius = new FloatReference(0.05f);

        public float RayCastRadius => rayCastRadius.Value + 0.001f;
        public IntReference animalType = new IntReference(0);
        #endregion

        #region Use Stuff Properties  
        public bool UseAdditivePos
        {
            get => useAdditivePos;
            set
            {
                useAdditivePos = value;
                if (!useAdditivePos) ResetInertiaSpeed();
            }
        }
        private bool useAdditivePos;

        public bool UseAdditiveRot { get; internal set; }

        public bool UseSprintState { get; internal set; }

        public bool UseCustomAlign { get; set; }
        public bool FreeMovement { get; set; }
        public bool UseSprint
        {
            get => useSprintGlobal;
            set
            {
                useSprintGlobal.Value = value;
                Sprint = sprint;
            }
        }
        public bool CanSprint { get => UseSprint; set => UseSprint = value; }

        public bool LockInput
        {
            get => lockInput.Value;
            set
            {
                lockInput.Value = value;
                OnInputLocked.Invoke(lockInput);
            }
        }

        public bool RootMotion
        {
            get => rootMotion;
            set => Anim.applyRootMotion = rootMotion.Value = value;
        }

        private bool useGravity;
        public bool UseGravity
        {
            get => useGravity;
            set
            {
                useGravity = value;

                if (!useGravity) ResetGravityValues();
            }
        }

        public bool LockMovement
        {
            get => lockMovement;
            set
            {
                lockMovement.Value = value;
                OnMovementLocked.Invoke(lockMovement);
            }
        }


        public bool LockForwardMovement
        {
            get => lockForwardMovement;
            set
            {
                lockForwardMovement.Value = value;
                LockMovementAxis.z = value ? 0 : 1;
            }
        }

        public bool LockHorizontalMovement
        {
            get => lockHorizontalMovement;
            set
            {
                lockHorizontalMovement.Value = value;
                LockMovementAxis.x = value ? 0 : 1;
            }
        }

        public bool LockUpDownMovement
        {
            get => lockUpDownMovement;
            set
            {
                lockUpDownMovement.Value = value;
                LockMovementAxis.y = value ? 0 : 1;
            }
        }

        private Vector3 LockMovementAxis;
        private bool useOrientToGround;

        public bool UseOrientToGround
        {
            get => useOrientToGround && m_OrientToGround.Value;
            set => useOrientToGround = value;
        }


        public bool GlobalOrientToGround
        {
            get => m_OrientToGround.Value;
            set
            {
                m_OrientToGround.Value = value;
                Has_Pivot_Chest = value ? Pivot_Chest != null : false;
            }
        }



        [SerializeField, Tooltip("Global Orient to ground. Disable This for Humanoids")]
        private BoolReference m_OrientToGround = new BoolReference(true);


        [SerializeField, Tooltip("Locks Input on the Animal, Ignore inputs like Jumps, Attacks, Actions etc")]
        private BoolReference lockInput = new BoolReference(false);

        [SerializeField, Tooltip("Locks the Movement entries on the animal. (Horizontal, Vertical,Up Down)")]
        private BoolReference lockMovement = new BoolReference(false);

        [SerializeField]
        private BoolReference useSprintGlobal = new BoolReference(true);
        #endregion

        #region Animator States Info
        internal AnimatorStateInfo m_CurrentState;
        internal AnimatorStateInfo m_NextState;
        internal AnimatorStateInfo m_PreviousCurrentState;
        internal AnimatorStateInfo m_PreviousNextState;

        internal bool m_IsAnimatorTransitioning;
        internal bool FirstAnimatorTransition;
        protected bool m_PreviousIsAnimatorTransitioning;


        public AnimatorStateInfo AnimState { get; set; }

        public int currentAnimTag;
        public int AnimStateTag
        {
            get => currentAnimTag;
            private set
            {
                if (value != currentAnimTag)
                {
                    currentAnimTag = value;
                    activeState.AnimationTagEnter(value);

                    if (ActiveState.IsPending)
                        LastState.AnimationTagEnter(value);
                }
            }
        }
        #endregion

        #region Platform
        public Transform platform;
        protected Vector3 platform_LastPos;
        protected Quaternion platform_Rot;
        #endregion  

        #region Extras
        internal bool DisablePositionRotation = false;

        protected List<IMDamager> Attack_Triggers;


        public List<Collider> colliders = new List<Collider>();

        public float StateTime { get; private set; }

        #endregion

        #region Events
        public IntEvent OnAnimationChange;
        public BoolEvent OnInputLocked = new BoolEvent();
        public BoolEvent OnMovementLocked = new BoolEvent();
        public BoolEvent OnSprintEnabled = new BoolEvent();
        public BoolEvent OnGrounded = new BoolEvent();
        public BoolEvent OnMovementDetected = new BoolEvent();

        public IntEvent OnStateActivate = new IntEvent();
        public IntEvent OnStateChange = new IntEvent();
        public Int2Event OnModeStart = new Int2Event();
        public Int2Event OnModeEnd = new Int2Event();
        public IntEvent OnStanceChange = new IntEvent();
        public SpeedModifierEvent OnSpeedChange = new SpeedModifierEvent();
        public Vector3Event OnTeleport = new Vector3Event();


        public List<OnEnterExitState> OnEnterExitStates;
        public List<OnEnterExitStance> OnEnterExitStances;
        public List<OnEnterExitSpeed> OnEnterExitSpeeds;


        #endregion

        #region Random
        public int RandomID { get; private set; }
        public int RandomPriority { get; private set; }

        public bool Randomizer { get; set; }
        #endregion 

        #region Animator Parameters

        [SerializeField, Tooltip("Forward (Z) Movement for the Animator")] private string m_Vertical = "Vertical";
        [SerializeField, Tooltip("Horizontal (X) Movement for the Animator")] private string m_Horizontal = "Horizontal";
        [SerializeField, Tooltip("Vertical (Y) Movement for the Animator")] private string m_UpDown = "UpDown";
        [SerializeField, Tooltip("Vertical (Y) Difference between Target and Current UpDown")] private string m_DeltaUpDown = "DeltaUpDown";

        [SerializeField, Tooltip("Is the animal on the Ground? ")] private string m_Grounded = "Grounded";
        [SerializeField, Tooltip("Is the animal moving?")] private string m_Movement = "Movement";

        [SerializeField, Tooltip("Active/Current State the animal is")]
        private string m_State = "State";

        [SerializeField, Tooltip("Trigger to Notify the Activation of a State")]
        private string m_StateOn = "StateOn";

        [SerializeField, Tooltip("Trigger to Notify the Activation of a Mode")]
        private string m_ModeOn = "ModeOn";


        [SerializeField, Tooltip("The Active State can have multiple status to change inside the State itself")]
        private string m_StateStatus = "StateEnterStatus";
        [SerializeField, Tooltip("The Active State can use this parameter to activate exiting animations")]
        private string m_StateExitStatus = "StateExitStatus";
        [SerializeField, Tooltip("Float value for the States to be used when needed")]
        private string m_StateFloat = "StateFloat";
        [SerializeField, Tooltip("Last State the animal was")]
        private string m_LastState = "LastState";

        [SerializeField, Tooltip("Active State Time for the States Animations")]
        private string m_StateTime = "StateTime";

        [SerializeField, Tooltip("Speed Multiplier for the Animations")]
        private string m_SpeedMultiplier = "SpeedMultiplier";


        [SerializeField, Tooltip("Active Mode the animal is... The Value is the Mode ID plus the Ability Index. Example Action Eat = 4002")]
        private string m_Mode = "Mode";



        [SerializeField, Tooltip("Store the Modes Status (Available=0  Started=1  Looping=-1 Interrupted=-2)")]
        private string m_ModeStatus = "ModeStatus";
        [SerializeField, Tooltip("Mode Float Value, Used to have a float Value for the modes to be used when needed")]
        private string m_ModePower = "ModePower";

        [SerializeField, Tooltip("Sprint Value")]
        private string m_Sprint = "Sprint";

        [SerializeField, Tooltip("Active/Current stance of the animal")] private string m_Stance = "Stance";
        [SerializeField, Tooltip("Previus/Last stance of the animal")] private string m_LastStance = "LastStance";
        [SerializeField, Tooltip("Normalized value of the Slope of the Terrain")] private string m_Slope = "Slope";
        [SerializeField, Tooltip("Type of animal for the Additive corrective pose")] private string m_Type = "Type";

        [SerializeField, Tooltip("Random Value for Animations States with multiple animations")] private string m_Random = "Random";
        [SerializeField, Tooltip("Target Angle calculated from the current forward  direction to the desired direction")] private string m_DeltaAngle = "DeltaAngle";
        [SerializeField, Tooltip("Does the Animal Uses Strafe")] private string m_Strafe = "Strafe";
        [SerializeField, Tooltip("Horizontal Angle for Strafing.")] private string m_strafeAngle = "StrafeAngle";

        internal int hash_Vertical;
        internal int hash_Horizontal;
        internal int hash_UpDown;

        internal int hash_DeltaUpDown;

        internal int hash_Movement;
        internal int hash_Grounded;
        internal int hash_SpeedMultiplier;

        internal int hash_DeltaAngle;

        internal int hash_State;
        internal int hash_StateOn;
        internal int hash_StateEnterStatus;
        internal int hash_StateExitStatus;
        internal int hash_StateFloat;
        internal int hash_StateTime;
        internal int hash_LastState;

        internal int hash_Mode;
        internal int hash_ModeOn;
        internal int hash_ModeStatus;
        internal int hash_ModePower;

        internal int hash_Stance;
        internal int hash_LastStance;

        internal int hash_Slope;
        internal int hash_Sprint;
        internal int hash_Random;
        internal int hash_Strafe;
        internal int hash_StrafeAngle;
        #endregion
    }


    [System.Serializable]
    public class OnEnterExitSpeed
    {
        [Tooltip("Which is the Speed Set (By its Name) changed. Case Sensitive")]
        public string SpeedSet;
        [Tooltip("Which is the Speed Modifier (By its Name) changed. This is Ignored if is set to 1. Case Sensitive")]
        public int SpeedIndex;
        public UnityEvent OnEnter;
        public UnityEvent OnExit;
    }


    [System.Serializable]
    public class OnEnterExitState
    {
        public StateID ID;
        public UnityEvent OnEnter;
        public UnityEvent OnExit;
    }

    [System.Serializable]
    public class OnEnterExitStance
    {
        public StanceID ID;
        public UnityEvent OnEnter;
        public UnityEvent OnExit;
    }

    [System.Serializable]
    public class SpeedModifierEvent : UnityEvent<MSpeed> { }
}
