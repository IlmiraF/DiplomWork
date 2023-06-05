using UnityEngine;
using System.Collections;
using UnityEngine.AI;
using System.Collections.Generic;
using RidingSystem.Events;
using RidingSystem.Scriptables;
using UnityEngine.Events;

#if UNITY_EDITOR
using UnityEditor;
#endif


namespace RidingSystem.Controller.AI
{
    [AddComponentMenu("RidingSystem/Animal Controller/AI/AI Control")]
    public class MAnimalAIControl : MonoBehaviour, IAIControl, IAITarget, IAnimatorListener
    {
        #region Components and References
        [SerializeField] private NavMeshAgent agent;

        [RequiredField] public MAnimal animal;

        public IInputSource InputSource { get; internal set; }

        public IInteractor Interactor { get; internal set; }

        public bool ArriveLookAt => false;

        public virtual bool Active => enabled && gameObject.activeInHierarchy;

        #endregion

        #region Internal Variables
        protected Vector3 TargetLastPosition;

        public virtual float RemainingDistance { get; set; }

        public virtual float AgentRemainingDistance => Agent.remainingDistance;

        public virtual float MinRemainingDistance { get; set; }

        public float SlowMultiplier
        {
            get
            {
                var result = 1f;
                if (CurrentSlowingDistance > CurrentStoppingDistance && RemainingDistance < CurrentSlowingDistance)
                    result = Mathf.Max(RemainingDistance / CurrentSlowingDistance, slowingLimit);

                return result;
            }
        }



        public Transform Transform => transform;

        [ContextMenuItem("Set Default", "SetDefaulStopAgent")]
        public List<StateID> StopAgentOn;

        public Vector3 AIDirection { get; set; }

        public bool InOffMeshLink { get; set; }

        public virtual bool AgentInOffMeshLink => Agent.isOnOffMeshLink;

        public bool StateIsBlockingAgent { get; set; }

        public virtual bool ActiveAgent
        {
            get => agent.enabled && agent.isOnNavMesh;
            set
            {
                agent.enabled = value;
                if (agent.isOnNavMesh) agent.isStopped = !value;
            }
        }

        public bool HasArrived { get; set; }

        public virtual bool UpdateDestinationPosition { get; set; }

        public virtual Vector3 DestinationPosition { get; set; }


        private IEnumerator I_WaitToNextTarget;
        private IEnumerator IFreeMoveOffMesh;
        private IEnumerator IClimbOffMesh;
        #endregion

        #region Public Variables
        [Min(0)] public float UpdateAI = 0.2f;
        private float CurrentTime;

        [Min(0)][SerializeField] protected float stoppingDistance = 0.6f;
        [Min(0)][SerializeField] protected float PointStoppingDistance = 0.6f;

        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("walkDistance")]
        [Min(0)] protected float slowingDistance = 1f;

        [Min(0)] public float OffMeshAlignment = 0.15f;


        [Tooltip("If the difference between the current direction and the desired direction is greater than this value; the animal will stop to turn around.")]
        [Range(0, 180)]
        public float TurnAngle = 90f;

        [Tooltip("Distance from the Animals Root to apply LookAt Target Logic when the Animal arrives to a target.")]
        [Min(0)] public float LookAtOffset = 1;

        [Tooltip("Limit for the Slowing Multiplier to be applied to the Speed Modifier")]
        [Range(0, 1)]
        [SerializeField] private float slowingLimit = 0.3f;

        [SerializeField] private Transform target;
        [SerializeField] private Transform nextTarget;

        public bool AutoNextTarget { get; set; }

        public bool LookAtTargetOnArrival { get; set; }

        public bool debug = false;
        public bool debugGizmos = true;
        public bool debugStatus = true;
        #endregion

        #region Properties 
        public bool FreeMove { get; private set; }

        public virtual float StoppingDistance { get => stoppingDistance; set => stoppingDistance = value; }

        protected float currentStoppingDistance;

        public virtual float CurrentStoppingDistance
        {
            get => currentStoppingDistance;
            set => Agent.stoppingDistance = currentStoppingDistance = value;
        }

        public virtual float SlowingDistance => slowingDistance;

        public virtual float Height => Agent.height * animal.ScaleFactor;

        public virtual float CurrentSlowingDistance { get; set; }

        public bool IsOnMode => animal.IsPlayingMode;

        private bool IsOnNonMovingMode => (IsOnMode && !animal.ActiveMode.AllowMovement);


        public IWayPoint IsWayPoint { get; set; }

        public IAITarget IsAITarget { get; set; }

        public Vector3 AITargetPos => IsAITarget.GetPosition();

        public IInteractable IsTargetInteractable { get; protected set; }
        #endregion 

        #region Events
        [Space]
        public Vector3Event OnTargetPositionArrived = new Vector3Event();
        public TransformEvent OnTargetArrived = new TransformEvent();
        public TransformEvent OnTargetSet = new TransformEvent();

        public TransformEvent TargetSet => OnTargetSet;
        public TransformEvent OnArrived => OnTargetArrived;

        #endregion

        internal bool IsAirDestination => IsAITarget != null && IsAITarget.TargetType == WayPointType.Water;
        internal bool IsGroundDestination => IsAITarget != null && IsAITarget.TargetType == WayPointType.Ground;

        public UnityEvent OnEnabled = new UnityEvent();
        public UnityEvent OnDisabled = new UnityEvent();


        #region Properties 
        public virtual NavMeshAgent Agent => agent;

        public Transform AgentTransform;

        public virtual Vector3 GetPosition() => AgentTransform.position;

        public virtual WayPointType TargetType => animal.FreeMovement ? WayPointType.Water : WayPointType.Ground;


        public virtual bool TargetIsMoving { get; internal set; }


        public virtual bool IsWaiting { get; internal set; }

        public virtual Vector3 LastOffMeshDestination { get; set; }

        public Vector3 NullVector { get; set; }

        public virtual Transform NextTarget { get => nextTarget; set => nextTarget = value; }

        public virtual Transform Target { get => target; set => target = value; }

        protected Vector3 AgentPosition;

        #endregion
        public virtual void SetActive(bool value)
        {
            if (gameObject.activeInHierarchy)
                enabled = value;
        }

        #region Unity Functions 
        public virtual bool OnAnimatorBehaviourMessage(string message, object value) => this.InvokeWithParams(message, value);


        protected virtual void Awake()
        {
            if (animal == null) animal = gameObject.FindComponent<MAnimal>();
            ValidateAgent();

            Interactor = animal.FindInterface<IInteractor>();
            InputSource = animal.FindInterface<IInputSource>();
            animal.UseSmoothVertical = true;

            LookAtTargetOnArrival = true;
            AutoNextTarget = true;
            UpdateDestinationPosition = true;

            NullVector = new Vector3(-998.9999f, -998.9999f, -998.9999f);

            DestinationPosition = NullVector;

            SetAgent();
        }

        protected virtual void SetAgent()
        {
            if (agent == null) AgentTransform.GetComponent<NavMeshAgent>();

            if (agent)
            {
                AgentPosition = Agent.transform.localPosition;
                Agent.angularSpeed = 0;
                Agent.speed = 1;
                Agent.acceleration = 0;
                Agent.autoBraking = false;
                Agent.updateRotation = false;
                Agent.updatePosition = false;
                Agent.autoTraverseOffMeshLink = false;
                Agent.stoppingDistance = StoppingDistance;
            }
        }

        protected virtual void OnEnable()
        {
            animal.OnStateActivate.AddListener(OnState);
            animal.OnModeStart.AddListener(OnModeStart);
            animal.OnModeEnd.AddListener(OnModeEnd);

            IsWaiting = true;

            this.Delay_Action(1, () => StartAI()); 

            if (InputSource != null)
            {
                InputSource.MoveCharacter = false;
                Debuging("Input Move Disabled");
            }

            OnEnabled.Invoke();
        }

        protected virtual void OnDisable()
        {
            animal.OnStateActivate.RemoveListener(OnState); 
            animal.OnModeStart.RemoveListener(OnModeStart); 
            animal.OnModeEnd.RemoveListener(OnModeEnd);
            Stop();
            StopAllCoroutines();
            OnDisabled.Invoke();

            animal.Rotate_at_Direction = false;

            if (InputSource != null)
            {
                InputSource.MoveCharacter = true;
                Debuging("Input Move Enabled");
            }
        }

        protected virtual void Update() { Updating(); }
        #endregion

        #region Animal Events Listen
        public virtual void OnModeStart(int ModeID, int ability)
        {
            Debuging($"has started a Mode: <B>[{animal.ActiveMode.ID.name}]</B>. Ability: <B>[{animal.ActiveMode.ActiveAbility.Name}]</B>");
            if (animal.ActiveMode.AllowMovement) return;

            var Dest = DestinationPosition;
            Stop(); 
            DestinationPosition = Dest;
        }

        public virtual void OnModeEnd(int ModeID, int ability)
        {
            if (StateIsBlockingAgent) return;


            if (!HasArrived)
            {
                CalculatePath();
                Move();
            }

            CompleteOffMeshLink();
            CheckAirTarget();
        }


        public virtual void OnState(int stateID)
        {
            if (IsWaiting) return;

            FreeMove = (animal.ActiveState.General.FreeMovement);
            CheckAirTarget();

            StateIsBlockingAgent = animal.ActiveStateID != 0 && StopAgentOn != null && StopAgentOn.Contains(animal.ActiveStateID);


            if (StateIsBlockingAgent)
            {
                ActiveAgent = false;
            }
            else
            {
                if (!IsOnNonMovingMode)
                {
                    CalculatePath();
                    Move();
                }

                CompleteOffMeshLink();
            }
        }
        #endregion

        public virtual void StartAI()
        {
            FreeMove = (animal.ActiveState.General.FreeMovement);
            if (FreeMove) ActiveAgent = false;
            if (!Agent.isOnNavMesh) ActiveAgent = false;


            HasArrived = false;
            TargetIsMoving = false;
            var targ = target; target = null;
            SetTarget(targ);

            if (AgentTransform == animal.transform)
                Debug.LogError("The Nav Mesh Agent needs to be attached to a child Gameobject, not in the same gameObject as the Animal Component");
        }

        public virtual void Updating()
        {
            ResetAgentPosition();

            if (InOffMeshLink || IsWaiting) return;

            if (FreeMove)
            {
                FreeMovement();
            }
            else
            {
                UpdateAgent();
            }
        }

        protected virtual void ResetAgentPosition()
        {
            AgentTransform.localPosition = AgentPosition;
            Agent.nextPosition = Agent.transform.position; 
        }

        public virtual bool PathPending() => ActiveAgent && Agent.isOnNavMesh && Agent.pathPending;

        public virtual void UpdateAgent()
        {
            if (HasArrived)
            {
                if (LookAtTargetOnArrival && LookAtOffset > 0)
                {
                    if (DestinationPosition == NullVector)
                    {
                        DestinationPosition = (target != null ? target.position : transform.position + transform.forward);
                    }

                    var Origin = (animal.transform.position - animal.transform.forward * LookAtOffset * animal.ScaleFactor);

                    var LookAtDir = (target != null ? target.position : DestinationPosition) - Origin;



                    if (debugGizmos)
                    {
                        MTools.Draw_Arrow(Origin, LookAtDir, Color.magenta);
                        MTools.DrawWireSphere(Origin, Color.magenta, 0.1f);
                    }

                    animal.RotateAtDirection(LookAtDir);
                }
                return;
            }

            if (ActiveAgent)
            {
                if (PathPending()) return;

                SetRemainingDistance(AgentRemainingDistance);

                if (!Arrive_Destination()) 
                {
                    if (!CheckOffMeshLinks())
                    {
                        CalculatePath();
                        Move();
                    }
                }
            }
        }

        public virtual bool Arrive_Destination()
        {
            if (CurrentStoppingDistance >= RemainingDistance)
            {
                if (IsPathIncomplete())
                {
                    Debuging($"[Agent Path Status: {Agent.pathStatus}]. Force Stop");
                    Stop();
                    StopWait();
                    HasArrived = true;
                    RemainingDistance = 0;  
                    AIDirection = Vector3.zero;
                    return true;
                }

                if (!CheckDestinationHeight()) return false;

                HasArrived = true;
                RemainingDistance = 0;
                AIDirection = Vector3.zero;
                Move();

                OnTargetPositionArrived.Invoke(DestinationPosition);

                if (target)
                {
                    OnTargetArrived.Invoke(target);

                    CheckInteractions();

                    if (IsAITarget != null)
                    {
                        IsAITarget.TargetArrived(animal.gameObject);
                        LookAtTargetOnArrival = IsAITarget.ArriveLookAt;
                        if (IsAITarget.TargetType == WayPointType.Ground) FreeMove = false;
                        if (AutoNextTarget) MovetoNextTarget();
                        else Stop();
                    }
                }
                else
                {
                    Stop();
                }
                return true;
            }
            return false;
        }

        protected virtual bool IsPathIncomplete()
        {
            return ActiveAgent && !FreeMove && Agent.pathStatus != NavMeshPathStatus.PathComplete;
        }

        protected virtual bool CheckDestinationHeight()
        {
            if (FreeMove) return true; 

            MTools.DrawWireSphere(DestinationPosition, Color.white, 0.1f);

            var Result = NavMesh.SamplePosition(DestinationPosition, out _, Height, NavMesh.AllAreas);
            return Result;
        }

        public virtual void CheckMovingTarget()
        {
            if (MTools.ElapsedTime(CurrentTime, UpdateAI))
            {
                if (Target)
                {
                    TargetIsMoving = (Target.position - TargetLastPosition).sqrMagnitude > (0.01f / animal.ScaleFactor);
                    TargetLastPosition = Target.position;

                    if (TargetIsMoving) Update_DestinationPosition();
                }
                CurrentTime = Time.time;
            }
        }


        public virtual void CalculatePath()
        {
            if (FreeMove) return;

            if (!ActiveAgent)
            {
                ActiveAgent = true;
                ResetFreeMoveOffMesh();
            }

            if (Agent.isOnNavMesh)
            {
                if (Agent.destination != DestinationPosition) 
                {
                    Agent.SetDestination(DestinationPosition);  

                    if (IsWayPoint != null) DestinationPosition = Agent.destination; 
                }

                if (Agent.desiredVelocity != Vector3.zero) AIDirection = Agent.desiredVelocity.normalized;
            }
        }


        public virtual void Move()
        {
            animal.ForwardMultiplier = Mathf.Abs(animal.DeltaAngle) > TurnAngle ? 0 : 1; 
            animal.Move(AIDirection * SlowMultiplier);
        }

        public virtual void Stop()
        {
            ActiveAgent = false; 
            AIDirection = Vector3.zero;
            DestinationPosition = NullVector;
            animal.StopMoving(); 
        }


        protected virtual void Update_DestinationPosition()
        {
            if (UpdateDestinationPosition)
            {
                DestinationPosition = GetTargetPosition(); 

                var DistanceOnMovingTarget = Vector3.Distance(DestinationPosition, AgentTransform.position);

                if (DistanceOnMovingTarget >= CurrentStoppingDistance)
                {
                    HasArrived = false;
                    CalculatePath();
                    Move();
                }
                else
                {
                    HasArrived = true;
                }
            }
        }

        protected virtual void SetRemainingDistance(float current) => RemainingDistance = current;



        #region Set Assing Target and Next Targets

        public virtual void ResetAIValues()
        {
            StopWait();                                  
            RemainingDistance = float.PositiveInfinity; 
            MinRemainingDistance = float.PositiveInfinity;
            HasArrived = false;
        }

        public virtual void SetTarget(Transform newTarget, bool move)
        {
            target = newTarget;
            OnTargetSet.Invoke(newTarget);

            if (target != null)
            {
                TargetLastPosition = newTarget.position;
                DestinationPosition = newTarget.position;

                IsAITarget = newTarget.gameObject.FindInterface<IAITarget>();
                IsTargetInteractable = newTarget.FindInterface<IInteractable>();
                IsWayPoint = newTarget.FindInterface<IWayPoint>();

                NextTarget = null;

                if (IsWayPoint != null)
                {
                    NextTarget = IsWayPoint.NextTarget();
                }

                CheckAirTarget();

                if (move)
                {

                    ResetAIValues();
                    if (animal.IsPlayingMode) animal.Mode_Interrupt();
                    CurrentStoppingDistance = GetTargetStoppingDistance();
                    CurrentSlowingDistance = GetTargetSlowingDistance();

                    DestinationPosition = GetTargetPosition();

                    CalculatePath();

                    Move();
                }
            }
            else
            {
                IsAITarget = null;
                IsTargetInteractable = null;
                IsWayPoint = null;

                if (move) Stop();
            }
        }

        public virtual void SetTarget(GameObject target) => SetTarget(target, true);
        public virtual void SetTarget(GameObject target, bool move) => SetTarget(target != null ? target.transform : null, move);



        public virtual void ClearTarget() => SetTarget((Transform)null, false);

        public virtual void NullTarget() => target = null;

        public virtual void SetTargetOnly(Transform target) => SetTarget(target, false);
        public virtual void SetTargetOnly(GameObject target) => SetTarget(target, false);
        public virtual void SetTarget(Transform target) => SetTarget(target, true);

        public virtual Vector3 GetTargetPosition()
        {
            var TargetPos = (IsAITarget != null) ? AITargetPos : target.position;
            if (TargetPos == Vector3.zero) TargetPos = target.position;
            return TargetPos;
        }

        public void TargetArrived(GameObject target) {}

        public virtual float GetTargetStoppingDistance() => IsAITarget != null ? IsAITarget.StopDistance() : StoppingDistance;
        public virtual float GetTargetSlowingDistance() => IsAITarget != null ? IsAITarget.SlowDistance() : SlowingDistance;


        public virtual void SetNextTarget(GameObject next)
        {
            NextTarget = next.transform;
            IsWayPoint = next.GetComponent<IWayPoint>();
        }

        public virtual void MovetoNextTarget()
        {
            if (NextTarget == null)
            {
                Stop();
                return;
            }

            if (IsWayPoint != null)
            {
                StopWait();
                I_WaitToNextTarget = C_WaitToNextTarget(IsWayPoint.WaitTime, NextTarget);
                StartCoroutine(I_WaitToNextTarget);
            }
            else
            {
                SetTarget(NextTarget);
            }
        }

        public void StopWait()
        {
            IsWaiting = false;
            if (I_WaitToNextTarget != null) StopCoroutine(I_WaitToNextTarget);
        }

        internal virtual bool CheckAirTarget()
        {
            if (IsAirDestination && !FreeMove)
            {
                if (Target) Debuging($"Target {Target} is in the Air.  Activating Fly State", Target.gameObject);
                animal.State_Activate(StateEnum.Swim);
                FreeMove = true;
            }

            return IsAirDestination;
        }

        #endregion


        public virtual void SetDestination(Vector3 newDestination, bool move)
        {
            LookAtTargetOnArrival = false;

            if (newDestination == DestinationPosition) return;

            CurrentStoppingDistance = PointStoppingDistance;

            ResetAIValues();

            if (IsOnNonMovingMode) animal.Mode_Interrupt();

            IsWayPoint = null;

            if (I_WaitToNextTarget != null)
                StopCoroutine(I_WaitToNextTarget);

            DestinationPosition = newDestination;

            if (move)
            {
                CalculatePath();
                Move();
            }
        }

        public virtual void SetDestination(Vector3Var newDestination) => SetDestination(newDestination.Value);
        public virtual void SetDestination(Vector3 PositionTarget) => SetDestination(PositionTarget, true);

        public virtual void SetDestinationClearTarget(Vector3 PositionTarget)
        {
            target = null;
            SetDestination(PositionTarget, true);
        }



        protected virtual void CheckInteractions()
        {
            if (IsTargetInteractable != null && IsTargetInteractable.Auto)
            {
                if (Interactor != null)
                {
                    Interactor.Interact(IsTargetInteractable);
                }
                else
                {
                    IsTargetInteractable.Interact(0, animal.gameObject);
                }

            }
        }

        protected virtual void FreeMovement()
        {
            AIDirection = (DestinationPosition - animal.transform.position);
            SetRemainingDistance(AIDirection.magnitude);

            AIDirection = AIDirection.normalized * SlowMultiplier;

            animal.Move(AIDirection);
            Arrive_Destination();
        }




        protected virtual bool CheckOffMeshLinks()
        {
            if (AgentInOffMeshLink && !InOffMeshLink)
            {
                InOffMeshLink = true;
                LastOffMeshDestination = DestinationPosition;

                OffMeshLinkData OMLData = Agent.currentOffMeshLinkData;

                if (OMLData.linkType == OffMeshLinkType.LinkTypeManual)
                {
                    var OffMesh_Link = OMLData.offMeshLink;

                    if (OffMesh_Link)
                    {
                        var AnimalLink = OffMesh_Link.GetComponent<MAIAnimalLink>();

                        if (AnimalLink)
                        {
                            AnimalLink.Execute(this, animal);
                            return true;
                        }

                        Zone IsOffMeshZone =
                        OffMesh_Link.FindComponent<Zone>();

                        if (IsOffMeshZone)                                           
                        {
                            IsOffMeshZone.ActivateZone(animal);
                            return true;
                        }





                        var NearTransform = transform.NearestTransform(OffMesh_Link.endTransform, OffMesh_Link.startTransform);
                        var FarTransform = transform.FarestTransform(OffMesh_Link.endTransform, OffMesh_Link.startTransform);

                        AIDirection = NearTransform.forward;
                        animal.Move(AIDirection);


                        if (OffMesh_Link.CompareTag("Fly"))
                        {
                            FlyOffMesh(FarTransform);
                        }
                        else if (OffMesh_Link.CompareTag("Climb"))
                        {
                            ClimbOffMesh();
                        }
                        else if (OffMesh_Link.area == 2)
                        {
                            animal.State_Activate(StateEnum.Jump);
                        }
                    }
                }
                else if (OMLData.linkType == OffMeshLinkType.LinkTypeJumpAcross)
                {
                    animal.State_Activate(StateEnum.Jump);
                }

                return true;
            }
            return false;
        }





        public virtual void CompleteOffMeshLink()
        {
            if (InOffMeshLink)
            {
                CompleteAgentOffMesh();

                InOffMeshLink = false;
                DestinationPosition = LastOffMeshDestination;
                CalculatePath();
                Move();
            }
        }

        protected virtual void CompleteAgentOffMesh()
        {
            if (Agent && Agent.isOnOffMeshLink) Agent.CompleteOffMeshLink();
        }

        protected virtual void FlyOffMesh(Transform target)
        {
            ResetFreeMoveOffMesh();
            IFreeMoveOffMesh = C_FlyMoveOffMesh(target);
            StartCoroutine(IFreeMoveOffMesh);
        }

        protected virtual void ClimbOffMesh()
        {
            if (IClimbOffMesh != null) StopCoroutine(IClimbOffMesh);
            IClimbOffMesh = C_Climb_OffMesh();
            StartCoroutine(IClimbOffMesh);
        }


        protected virtual void ResetFreeMoveOffMesh()
        {
            if (IFreeMoveOffMesh != null)
            {
                InOffMeshLink = false;
                StopCoroutine(IFreeMoveOffMesh);
                IFreeMoveOffMesh = null;
            }
        }

        protected virtual IEnumerator C_WaitToNextTarget(float time, Transform NextTarget)
        {
            IsWaiting = true;

            if (time > 0)
            {
                yield return null;

                animal.Move(AIDirection = Vector3.zero);
                yield return new WaitForSeconds(time);
            }
            SetTarget(NextTarget);
        }

        protected virtual IEnumerator C_FlyMoveOffMesh(Transform target)
        {
            animal.State_Activate(StateEnum.Fly);
            InOffMeshLink = true;
            float distance = float.MaxValue;

            while (distance > StoppingDistance)
            {
                animal.Move((target.position - animal.transform.position).normalized * SlowMultiplier);
                distance = Vector3.Distance(animal.transform.position, target.position);
                yield return null;
            }
            animal.ActiveState.AllowExit();

            Debuging("Exit Fly State Off Mesh");

            InOffMeshLink = false;
        }

        protected virtual IEnumerator C_Climb_OffMesh()
        {
            animal.State_Activate(StateEnum.Climb);
            InOffMeshLink = true;
            yield return null;
            ActiveAgent = false;

            while (animal.ActiveState.ID == StateEnum.Climb)
            {
                animal.SetInputAxis(Vector3.forward);
                yield return null;
            }
            InOffMeshLink = false;

            IClimbOffMesh = null;
        }

        public void ResetStoppingDistance() => CurrentStoppingDistance = StoppingDistance;
        public void ResetSlowingDistance() => CurrentSlowingDistance = SlowingDistance;
        public float StopDistance() => StoppingDistance;
        public float SlowDistance() => SlowingDistance;

        public virtual void ValidateAgent()
        {
            if (agent == null) agent = gameObject.FindComponent<NavMeshAgent>();

            AgentTransform = (agent != null) ? agent.transform : transform;
        }


        protected virtual void Debuging(string Log) { if (debug) Debug.Log($"<B>{animal.name}:</B> " + Log, this); }
        protected virtual void Debuging(string Log, GameObject obj) { if (debug) Debug.Log($"<B>{animal.name}:</B> " + Log, obj); }

#if UNITY_EDITOR
        [HideInInspector] public int Editor_Tabs1;

        protected virtual void OnValidate()
        {
            if (animal == null) animal = gameObject.FindComponent<MAnimal>();
            ValidateAgent();
        }


        void Reset()
        {
            SetDefaulStopAgent();
        }

        void SetDefaulStopAgent()
        {
            StopAgentOn = new List<StateID>(2)
            {
                MTools.GetInstance<StateID>("Fall"),
                MTools.GetInstance<StateID>("Jump")
            };
        }

        private string CheckBool(bool val) => val ? "[X]" : "[  ]";

        protected virtual void OnDrawGizmos()
        {
            var isPlaying = Application.isPlaying;

            if (isPlaying && debugStatus)
            {
                string log = "\nTarget: [" + (Target != null ? Target.name : "-none-") + "]";
                log += "- NextTarget: [" + (NextTarget != null ? NextTarget.name : "-none-") + "]";
                log += "\nRemainingDistance: " + RemainingDistance.ToString("F2");
                log += "\nStopDistance: " + CurrentStoppingDistance.ToString("F2");
                log += "\n" + CheckBool(HasArrived) + " HasArrived";
                log += "\n" + CheckBool(ActiveAgent) + " Agent";
                log += "\n" + CheckBool(TargetIsMoving) + " Target is Moving";
                log += "\n" + CheckBool(IsAITarget != null) + "Target is AITarget";
                log += "\n" + CheckBool(IsWayPoint != null) + "Target is WayPoint";
                log += "\n" + CheckBool(IsWaiting) + " Waiting";
                log += "\n" + CheckBool(IsOnMode) + " On Mode";
                log += "\n" + CheckBool(FreeMove) + " Free Move";
                log += "\n" + CheckBool(InOffMeshLink) + " InOffMeshLink";

                var Styl = new GUIStyle(GUI.skin.box);
                Styl.normal.textColor = Color.white;
                Styl.fontStyle = FontStyle.Bold;
                Styl.alignment = TextAnchor.UpperLeft;


                UnityEditor.Handles.Label(transform.position, "AI Log:" + log, Styl);
            }
            if (!debugGizmos) return;


            if (Agent && ActiveAgent && Agent.path != null)
            {
                Gizmos.color = Color.yellow;
                for (int i = 1; i < Agent.path.corners.Length; i++)
                {
                    Gizmos.DrawLine(Agent.path.corners[i - 1], Agent.path.corners[i]);
                }
            }


            if (isPlaying)
            {
                MTools.Draw_Arrow(AgentTransform.position, AIDirection * 2, Color.white);

                Gizmos.color = Color.white;
                Gizmos.DrawWireSphere(DestinationPosition, stoppingDistance);
            }
            if (AgentTransform)
            {
                var Pos = (isPlaying) ? DestinationPosition : AgentTransform.position;
                var Stop = (isPlaying) ? CurrentStoppingDistance : StoppingDistance;
                var Slow = (isPlaying) ? CurrentSlowingDistance : SlowingDistance;


                Gizmos.color = Color.red;
                Gizmos.DrawSphere(AgentTransform.position, 0.1f);
                if (Slow > Stop)
                {
                    UnityEditor.Handles.color = Color.cyan;
                    UnityEditor.Handles.DrawWireDisc(Pos, Vector3.up, Slow);
                }

                UnityEditor.Handles.color = HasArrived ? Color.green : Color.red;
                UnityEditor.Handles.DrawWireDisc(Pos, Vector3.up, Stop);
            }
        }
#endif
    }

    #region Inspector


#if UNITY_EDITOR

    [CustomEditor(typeof(MAnimalAIControl), true)]
    public class AnimalAIControlEd : Editor
    {
        private MAnimalAIControl M;

        protected SerializedProperty
            stoppingDistance, SlowingDistance, LookAtOffset, targett, UpdateAI, slowingLimit,
            agent, animal, PointStoppingDistance, OnEnabled, OnTargetPositionArrived, OnTargetArrived,
            OnTargetSet, debugGizmos, debugStatus, debug, Editor_Tabs1, nextTarget, OnDisabled, AgentTransform, OffMeshAlignment,
            StopAgentOn, TurnAngle;

        protected virtual void OnEnable()
        {
            M = (MAnimalAIControl)target;

            animal = serializedObject.FindProperty("animal");
            AgentTransform = serializedObject.FindProperty("AgentTransform");
            GetAgentProperty();

            slowingLimit = serializedObject.FindProperty("slowingLimit");
            TurnAngle = serializedObject.FindProperty("TurnAngle");

            OnEnabled = serializedObject.FindProperty("OnEnabled");
            OnDisabled = serializedObject.FindProperty("OnDisabled");

            OnTargetSet = serializedObject.FindProperty("OnTargetSet");
            OnTargetArrived = serializedObject.FindProperty("OnTargetArrived");
            OnTargetPositionArrived = serializedObject.FindProperty("OnTargetPositionArrived");
            stoppingDistance = serializedObject.FindProperty("stoppingDistance");
            PointStoppingDistance = serializedObject.FindProperty("PointStoppingDistance");
            SlowingDistance = serializedObject.FindProperty("slowingDistance");
            LookAtOffset = serializedObject.FindProperty("LookAtOffset");
            targett = serializedObject.FindProperty("target");
            nextTarget = serializedObject.FindProperty("nextTarget");
            OffMeshAlignment = serializedObject.FindProperty("OffMeshAlignment");

            debugGizmos = serializedObject.FindProperty("debugGizmos");
            debugStatus = serializedObject.FindProperty("debugStatus");
            debug = serializedObject.FindProperty("debug");

            Editor_Tabs1 = serializedObject.FindProperty("Editor_Tabs1");
            StopAgentOn = serializedObject.FindProperty("StopAgentOn");

            UpdateAI = serializedObject.FindProperty("UpdateAI");


            if (M.StopAgentOn == null || M.StopAgentOn.Count == 0)
            {
                M.StopAgentOn = new System.Collections.Generic.List<StateID>(1) { MTools.GetInstance<StateID>("Fall") };
                StopAgentOn.isExpanded = true;
                MTools.SetDirty(M);
                serializedObject.ApplyModifiedProperties();
            }
        }

        public virtual void GetAgentProperty()
        {
            agent = serializedObject.FindProperty("agent");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            MalbersEditor.DrawDescription("AI Source. Moves the animal using an AI Agent");

            EditorGUI.BeginChangeCheck();
            {
                EditorGUILayout.BeginVertical(MalbersEditor.StyleGray);

                Editor_Tabs1.intValue = GUILayout.Toolbar(Editor_Tabs1.intValue, new string[] { "General", "Events", "Debug" });

                int Selection = Editor_Tabs1.intValue;

                if (Selection == 0) ShowGeneral();
                else if (Selection == 1) ShowEvents();
                else if (Selection == 2) ShowDebug();


                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(target, "Animal AI Control Changed");
                }
            }

            if (M.Agent != null && M.animal != null && M.Agent.transform == M.animal.transform)
            {
                EditorGUILayout.HelpBox("The NavMesh Agent needs to be attached to a child gameObject. " +
                    "It cannot be in the same gameObject as the Animal Component", MessageType.Error);
            }

            EditorGUILayout.EndVertical();

            serializedObject.ApplyModifiedProperties();
        }
        private void ShowGeneral()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                targett.isExpanded = MalbersEditor.Foldout(targett.isExpanded, "Targets");
                if (targett.isExpanded)
                {
                    EditorGUILayout.PropertyField(targett, new GUIContent("Target", "Target to follow"));
                    EditorGUILayout.PropertyField(nextTarget, new GUIContent("Next Target", "Next Target the animal will go"));
                }
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                EditorGUI.BeginChangeCheck();
                {
                    UpdateAI.isExpanded = MalbersEditor.Foldout(UpdateAI.isExpanded, "AI Parameters");

                    if (UpdateAI.isExpanded)
                    {
                        EditorGUILayout.PropertyField(UpdateAI, new GUIContent("Update Agent", " Recalculate the Path for the Agent every x seconds "));
                        EditorGUILayout.PropertyField(stoppingDistance, new GUIContent("Stopping Distance", "Agent Stopping Distance"));
                        EditorGUILayout.PropertyField(SlowingDistance, new GUIContent("Slowing Distance", "Distance to Start slowing the animal before arriving to the destination"));
                        EditorGUILayout.PropertyField(LookAtOffset);
                        EditorGUILayout.PropertyField(PointStoppingDistance, new GUIContent("Point Stop Distance", "Stop Distance used on the SetDestination method. No Target Assigned"));
                        EditorGUILayout.PropertyField(TurnAngle);
                        EditorGUILayout.PropertyField(slowingLimit);
                        EditorGUILayout.PropertyField(OffMeshAlignment);
                    }
                }
                if (EditorGUI.EndChangeCheck())
                {
                    if (M.Agent)
                    {
                        M.Agent.stoppingDistance = stoppingDistance.floatValue;
                        serializedObject.ApplyModifiedProperties();
                    }
                }
            }
            EditorGUILayout.EndVertical();


            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                animal.isExpanded = MalbersEditor.Foldout(animal.isExpanded, "References");

                if (animal.isExpanded)
                {
                    EditorGUILayout.PropertyField(animal, new GUIContent("Animal", "Reference for the Animal Controller"));
                    EditorGUILayout.PropertyField(AgentTransform, new GUIContent("Agent", "Reference for the AI Agent Transform"));
                    EditorGUI.indentLevel++;

                    EditorGUILayout.PropertyField(StopAgentOn, new GUIContent($"{StopAgentOn.displayName} ({StopAgentOn.arraySize})"), true);

                    if (StopAgentOn.isExpanded && GUILayout.Button(new GUIContent("Set Default Off States", "By Default the AI should not be Active on Fly, Jump or Fall states"), GUILayout.MinWidth(150)))
                    {
                        M.StopAgentOn = new List<StateID>(2)
                    {
                        MTools.GetInstance<StateID>("Fall"),
                        MTools.GetInstance<StateID>("Jump")
                    };
                        serializedObject.ApplyModifiedProperties();

                        Debug.Log("Stop Agent set to default: [Fall,Jump,Fly]");
                        MTools.SetDirty(target);
                    }
                    EditorGUI.indentLevel--;


                    M.ValidateAgent();

                    if (!M.AgentTransform)
                    {
                        EditorGUILayout.HelpBox("There's no Agent found on the hierarchy on this gameobject\nPlease add a NavMesh Agent Component", MessageType.Error);
                    }
                }
            }
            EditorGUILayout.EndVertical();
        }

        private void ShowEvents()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                EditorGUILayout.PropertyField(OnEnabled);
                EditorGUILayout.PropertyField(OnDisabled);
                EditorGUILayout.PropertyField(OnTargetPositionArrived, new GUIContent("On Position Arrived"));
                EditorGUILayout.PropertyField(OnTargetArrived, new GUIContent("On Target Arrived"));
                EditorGUILayout.PropertyField(OnTargetSet, new GUIContent("On New Target Set"));
            }
            EditorGUILayout.EndVertical();
        }

        protected GUIStyle Bold(bool tru) => tru ? EditorStyles.boldLabel : EditorStyles.miniBoldLabel;

        private void ShowDebug()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUIUtility.labelWidth = 50f;
                EditorGUILayout.PropertyField(debug, new GUIContent("Console"));
                EditorGUILayout.PropertyField(debugGizmos, new GUIContent("Gizmos"));
                EditorGUIUtility.labelWidth = 80f;
                EditorGUILayout.PropertyField(debugStatus, new GUIContent("In-Game Log"));
                EditorGUIUtility.labelWidth = 0f;
                EditorGUILayout.EndHorizontal();
                if (Application.isPlaying)
                {

                    Repaint();
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.PropertyField(targett);
                    EditorGUILayout.ObjectField("Next Target", M.NextTarget, typeof(Transform), false);
                    EditorGUILayout.Vector3Field("Destination", M.DestinationPosition);
                    EditorGUILayout.Vector3Field("AI Direction", M.AIDirection);
                    EditorGUILayout.Space();
                    EditorGUILayout.FloatField("Current Stop Distance", M.StoppingDistance);
                    EditorGUILayout.FloatField("Remaining Distance", M.RemainingDistance);
                    EditorGUILayout.FloatField("Slow Multiplier", M.SlowMultiplier);
                    EditorGUILayout.Space();




                    EditorGUIUtility.labelWidth = 70;
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                        EditorGUILayout.ToggleLeft("Target is Moving", M.TargetIsMoving, Bold(M.TargetIsMoving));
                        EditorGUILayout.ToggleLeft("Target is AITarget", M.IsAITarget != null, Bold(M.IsAITarget != null));
                        EditorGUILayout.ToggleLeft("Target is WayPoint", M.IsWayPoint != null, Bold(M.IsWayPoint != null));
                        EditorGUILayout.Space();
                        EditorGUILayout.ToggleLeft("LookAt Target", M.LookAtTargetOnArrival, Bold(M.LookAtTargetOnArrival));
                        EditorGUILayout.ToggleLeft("Auto Next Target", M.AutoNextTarget, Bold(M.AutoNextTarget));
                        EditorGUILayout.ToggleLeft("UpdateDestinationPos", M.UpdateDestinationPosition, Bold(M.UpdateDestinationPosition));
                        EditorGUILayout.EndVertical();

                        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                        EditorGUILayout.ToggleLeft("Is On Mode", M.IsOnMode, Bold(M.IsOnMode));
                        EditorGUILayout.ToggleLeft("Free Move", M.FreeMove, Bold(M.FreeMove));
                        EditorGUILayout.ToggleLeft("In OffMesh Link", M.InOffMeshLink, Bold(M.InOffMeshLink));

                        EditorGUILayout.Space();
                        EditorGUILayout.ToggleLeft("Waiting", M.IsWaiting, Bold(M.IsWaiting));
                        EditorGUILayout.ToggleLeft("Has Arrived to Destination", M.HasArrived, Bold(M.HasArrived));

                        EditorGUILayout.ToggleLeft("Active Agent", M.ActiveAgent, Bold(M.ActiveAgent));
                        if (M.Agent && M.ActiveAgent)
                        {
                            EditorGUILayout.ToggleLeft("Agent in NavMesh", M.Agent.isOnNavMesh, Bold(M.Agent.isOnNavMesh));
                        }
                        EditorGUILayout.EndVertical();

                    }
                    EditorGUILayout.EndHorizontal();
                    EditorGUIUtility.labelWidth = 0;

                    DrawChildDebug();


                    EditorGUI.EndDisabledGroup();
                }
            }
            EditorGUILayout.EndVertical();
        }

        protected virtual void DrawChildDebug()
        { }

    }
#endif
    #endregion
}