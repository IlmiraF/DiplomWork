using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Events;
using RidingSystem.Events;
using RidingSystem.Scriptables;
using RidingSystem.Controller;
using System.Collections;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace RidingSystem.HAP
{
    public enum DismountType { Random, Input, Last }
    [AddComponentMenu("RidingSystem/Riding/Rider")]
    public class MRider : MonoBehaviour, IAnimatorListener, IRider
    {
        #region Public Variables
        public BoolReference Parent = new BoolReference(true);

        public GameObjectReference m_MountStored = new GameObjectReference();


        public Mount MountStored
        {
            get => p_MountStored;
            protected set
            {
                p_MountStored = value;
            }
        }
        private Mount p_MountStored;

        public BoolReference StartMounted;

        public bool ReSync = true;

        public Vector3Reference Gravity = new Vector3Reference(Vector3.down);

        [SerializeField] private BoolReference m_CanMount = new BoolReference(false);
        [SerializeField] private BoolReference m_CanDismount = new BoolReference(false);
        [SerializeField] private BoolReference m_CanCallAnimal = new BoolReference(false);

        public DismountType DismountType = DismountType.Random;

        public string MountLayer = "Mounted";

        [ContextMenuItem("Find Right Hand", "FindRHand")]
        public Transform RightHand;

        [ContextMenuItem("Find Left Hand", "FindLHand")]
        public Transform LeftHand;

        public Vector3Reference LeftReinOffset = new Vector3Reference();

        public Vector3Reference RightReinOffset = new Vector3Reference();

        private bool freeRightHand = true;
        private bool freeLeftHand = true;



        public readonly static int IKLeftFootHash = Animator.StringToHash("IKLeftFoot");
        public readonly static int IKRightFootHash = Animator.StringToHash("IKRightFoot");
        public readonly static int MountHash = Animator.StringToHash("Mount");
        public readonly static int MountSideHash = Animator.StringToHash("MountSide");
        public static readonly int EmptyHash = Animator.StringToHash("Empty");

        [Utilities.Flag("Update Type")]
        public UpdateMode LinkUpdate = UpdateMode.Update | UpdateMode.FixedUpdate;

        public FloatReference AlingMountTrigger = new FloatReference(0.2f);

        private Hashtable animatorParams;

        public bool debug;

        #region Call Animal
        public AudioClip CallAnimalA;
        public AudioClip StopAnimalA;
        public AudioSource RiderAudio;
        public bool ToggleCall { get; set; }
        #endregion

        #region ExtraCollider

        public CapsuleCollider MainCollider;
        private OverrideCapsuleCollider Def_CollPropeties;
        [CreateScriptableAsset] public CapsuleColliderPreset MountCollider;


        #endregion

        #region UnityEvents

        public GameObjectEvent OnFindMount = new GameObjectEvent();
        public BoolEvent OnCanMount = new BoolEvent();
        public BoolEvent OnCanDismount = new BoolEvent();
        public BoolEvent CanCallMount = new BoolEvent();

        public UnityEvent OnStartMounting = new UnityEvent();
        public UnityEvent OnEndMounting = new UnityEvent();
        public UnityEvent OnStartDismounting = new UnityEvent();
        public UnityEvent OnEndDismounting = new UnityEvent();
        public UnityEvent OnAlreadyMounted = new UnityEvent();

        #endregion

        public BoolReference DisableComponents;
        public Behaviour[] DisableList;
        #endregion

        #region Auto Properties
        public Mount Montura { get; set; }

        public virtual IInputSource MountInput { get; set; }


        public MountTriggers MountTrigger { get; set; }

        public bool CanMount { get => m_CanMount.Value; protected set => m_CanMount.Value = value; }
        public bool CanDismount { get => m_CanDismount.Value; protected set => m_CanDismount.Value = value; }

        public bool CanCallAnimal { get => m_CanCallAnimal.Value; protected set => m_CanCallAnimal.Value = value; }

        public float SpeedMultiplier { get; set; }

        public float TargetSpeedMultiplier { get; set; }

        public bool ForceLateUpdateLink { get; set; }

        protected MonoBehaviour[] AllComponents { get; set; }
        #endregion

        #region IK VARIABLES    
        protected float L_IKFootWeight = 0f;
        protected float R_IKFootWeight = 0f;
        #endregion

        public Quaternion MountRotation { get; set; }

        public Vector3 MountPosition { get; set; }

        internal int MountLayerIndex = -1;
        protected AnimatorUpdateMode Default_Anim_UpdateMode;

        #region Properties


        protected bool mounted;
        public bool Mounted
        {
            get => mounted;
            set
            {
                mounted = value;
                SetAnimParameter(MountHash, Mounted);
            }
        }
        public bool IsOnHorse { get; protected set; }

        public bool IsRiding => IsOnHorse && Mounted;

        public bool IsMountingDismounting => IsOnHorse || Mounted;

        public bool IsMounting => !IsOnHorse && Mounted;

        public bool IsDismounting => IsOnHorse && !Mounted;


        #region private vars
        protected float SP_Weight;
        protected RigidbodyConstraints DefaultConstraints;
        protected CollisionDetectionMode DefaultCollision;
        #region Re-Sync with Horse
        private float RiderNormalizedTime;
        private float HorseNormalizedTime;
        public float ResyncThreshold = 0.1f;
        #endregion
        #endregion

        #region References

        [SerializeField] private Animator animator;
        [SerializeField] private Rigidbody m_rigidBody;

        public Animator Anim { get => animator; protected set => animator = value; } 
        public Rigidbody RB { get => m_rigidBody; protected set => m_rigidBody = value; }

        public Transform RiderRoot { get => m_root; protected set => m_root = value; }

        [SerializeField] private Transform m_root;


        #region Bones
        public Transform Spine { get; private set; }
        public Transform Chest { get; private set; }

        public ISleepController GroundController { get; protected set; }
        #endregion

        protected List<Collider> colliders;

        #endregion
        #endregion

        private void GetExtraColliders()
        {
            colliders = GetComponentsInChildren<Collider>().ToList();

            var CleanCol = new List<Collider>();

            foreach (var col in colliders)
            {
                if (col.enabled && !col.isTrigger)
                    CleanCol.Add(col);
            }

            colliders = new List<Collider>(CleanCol);


            if (MainCollider)
            {
                Def_CollPropeties = new OverrideCapsuleCollider(MainCollider) { modify = (CapsuleModifier)(-1) };
                colliders.Remove(MainCollider);
            }
        }

        public void Start()
        {
            if (RiderRoot == null) RiderRoot = transform.root;
            if (Anim == null) Anim = this.FindComponent<Animator>();
            if (RB == null) RB = this.FindComponent<Rigidbody>();

            GroundController = GetComponent<ISleepController>();

            animatorParams = new Hashtable();

            if (Anim)
            {
                foreach (AnimatorControllerParameter parameter in Anim.parameters)
                    animatorParams.Add(parameter.nameHash, parameter.name);

                MountLayerIndex = Anim.GetLayerIndex(MountLayer);

                if (MountLayerIndex != -1)
                {
                    Anim.SetLayerWeight(MountLayerIndex, 1);
                    Anim.Play("Empty", MountLayerIndex, 0);
                }
                Spine = Anim.GetBoneTransform(HumanBodyBones.Spine);
                Chest = Anim.GetBoneTransform(HumanBodyBones.Chest);

                Default_Anim_UpdateMode = Anim.updateMode;
            }

            GetExtraColliders();

            IsOnHorse = Mounted = false;
            ForceLateUpdateLink = false;
            SpeedMultiplier = 1f;
            TargetSpeedMultiplier = 1f;

            if ((int)LinkUpdate == 0 || !Parent)
                LinkUpdate = UpdateMode.FixedUpdate | UpdateMode.LateUpdate;


            FindStoredMount();

            if (StartMounted.Value) Start_Mounted();

            UpdateCanMountDismount();
        }

        void Update()
        {
            if ((LinkUpdate & UpdateMode.Update) == UpdateMode.Update) UpdateRiderTransform();
        }


        private void LateUpdate()
        {
            if ((LinkUpdate & UpdateMode.LateUpdate) == UpdateMode.LateUpdate || ForceLateUpdateLink) UpdateRiderTransform();
        }

        private void FixedUpdate()
        {
            if ((LinkUpdate & UpdateMode.FixedUpdate) == UpdateMode.FixedUpdate) UpdateRiderTransform();
        }

        public virtual void UpdateRiderTransform()
        {
            if (IsRiding)
            {
                transform.position = Montura.MountPoint.position;
                transform.rotation = Montura.MountPoint.rotation;

                MountRotation = transform.rotation;
                MountPosition = transform.position;
            }
        }



        public virtual void Mount_TargetTransform()
        {
            transform.position = MountPosition;
            transform.rotation = MountRotation;
        }

        internal void SetMountSide(int side) => SetAnimParameter(MountSideHash, side);

        public virtual void MountAnimal()
        {
            if (!CanMount) return;

            Mounted = true;  
            SetMountSide(MountTrigger.MountID);
        }

        public virtual void DismountAnimal()
        {
            if (!CanDismount) return;

            Montura.Mounted = Mounted = false;
            MountTrigger = GetDismountTrigger();


            SetMountSide(MountTrigger.DismountID);
        }

        protected MountTriggers GetDismountTrigger()
        {
            switch (DismountType)
            {
                case DismountType.Last:
                    if (MountTrigger == null) MountTrigger = Montura.MountTriggers[UnityEngine.Random.Range(0, Montura.MountTriggers.Count)];
                    return MountTrigger;
                case DismountType.Input:
                    var MoveInput = Montura.Animal.MovementAxis;

                    MountTriggers close = MountTrigger;

                    float Diference = Vector3.Angle(MountTrigger.Direction, MoveInput);

                    foreach (var mt in Montura.MountTriggers)
                    {
                        var newDiff = Vector3.Angle(mt.Direction, MoveInput);

                        if (newDiff < Diference)
                        {
                            Diference = newDiff;
                            close = mt;
                        }
                    }

                    return close;

                case DismountType.Random:
                    int Randomindex = UnityEngine.Random.Range(0, Montura.MountTriggers.Count);
                    return Montura.MountTriggers[Randomindex];
                default:
                    return MountTrigger;
            }
        }

        private void FindStoredMount()
        {
            MountStored = m_MountStored.Value != null ? m_MountStored.Value.FindComponent<Mount>() : null;
        }


        public virtual void Set_StoredMount(GameObject newMount)
        {
            m_MountStored.Value = newMount;
            FindStoredMount();
        }

        public virtual void ClearStoredMount()
        {
            m_MountStored.Value = null;
            MountStored = null;
        }


        public void Start_Mounted()
        {
            if (MountStored != null && m_MountStored.Value.activeSelf)
            {
                if (m_MountStored.Value.IsPrefab())
                    m_MountStored.Value = Instantiate(m_MountStored.Value, transform.position - transform.forward, Quaternion.identity);

                Montura = MountStored;

                StopMountAI();

                Montura.Rider = this; 

                if (MountTrigger == null)
                    MountTrigger = Montura.transform.GetComponentInChildren<MountTriggers>();


                Start_Mounting();
                End_Mounting();

                Anim?.Play(Montura.MountIdle, MountLayerIndex);

                Montura.Mounted = Mounted = true; 

                OnAlreadyMounted.Invoke();

                UpdateRiderTransform();
            }
            else
            {
            }
        }

        public virtual void ForceDismount()
        {
            DisconnectWithMount();
            Anim?.Play(EmptyHash, MountLayerIndex);
            SetMountSide(0);
            Start_Dismounting();
            End_Dismounting();
        }

        internal virtual void Start_Mounting()
        {
            Montura.StartMounting(this);    

            IsOnHorse = false;
            Mounted = true;  

            MountInput = Montura.MountInput;


            if (GroundController != null) GroundController.Sleep = true;


            StopMountAI();

            if (RB)
            {
                RB.useGravity = false;
                DefaultConstraints = RB.constraints;
                DefaultCollision = RB.collisionDetectionMode;
                RB.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
                RB.constraints = RigidbodyConstraints.FreezeAll;
                RB.isKinematic = true;
            }

            ToogleColliders(false);

            ToggleCall = false;
            CallAnimal(false);

            m_MountStored.Value = Montura.Animal.gameObject;
            MountStored = Montura;

            if (Parent) RiderRoot.parent = Montura.MountPoint;

            if (!MountTrigger)
                MountTrigger = Montura.GetComponentInChildren<MountTriggers>();

            if (DisableComponents)
                ToggleComponents(false);

            OnStartMounting.Invoke();

            UpdateCanMountDismount();
        }

        public virtual void End_Mounting()
        {
            IsOnHorse = true;
            Montura.End_Mounting();

            if (Parent)
            {
                RiderRoot.localPosition = Vector3.zero;
                RiderRoot.localRotation = Quaternion.identity;
            }


            if (Anim)
            {
                Anim.updateMode = Montura.Anim.updateMode;

                SetAnimParameter(Montura.Animal.hash_Grounded, Montura.Animal.Grounded);
                SetAnimParameter(Montura.Animal.hash_State, Montura.Animal.ActiveStateID.ID);
                SetAnimParameter(Montura.Animal.hash_Mode, Montura.Animal.ModeAbility);
                SetAnimParameter(Montura.Animal.hash_ModeStatus, Montura.Animal.ModeStatus);
                SetAnimParameter(Montura.Animal.hash_Stance, Montura.ID);
                Anim.speed = Montura.Anim.speed;
                ConnectWithMount();
            }
            OnEndMounting.Invoke();

            UpdateCanMountDismount();

            SendMessage("SetIgnoreTransform", Montura.Animal.transform, SendMessageOptions.DontRequireReceiver);
        }

        public virtual void Start_Dismounting()
        {
            RiderRoot.parent = null;
            Montura.Start_Dismounting();
            Mounted = false;

            if (Anim)
            {
                Anim.updateMode = Default_Anim_UpdateMode;

                SetAnimParameter(Montura.Animal.hash_Stance, 0);
                SetAnimParameter(Montura.Animal.hash_Mode, 0);
                SetAnimParameter(Montura.Animal.hash_ModeStatus, 0);

                DisconnectWithMount();
                Anim.speed = 1f;
            }

            OnStartDismounting.Invoke();
            UpdateCanMountDismount();

            SendMessage("ClearIgnoreTransform", SendMessageOptions.DontRequireReceiver);
        }

        public virtual void End_Dismounting()
        {
            IsOnHorse = false; 

            if (Montura) Montura.EndDismounting();

            Montura = null;
            MountTrigger = null;
            ToggleCall = false;

            if (RB)
            {
                RB.isKinematic = false;
                RB.useGravity = true;
                RB.constraints = DefaultConstraints;
                RB.collisionDetectionMode = DefaultCollision;
            }

            if (Anim)
            {
                Anim.speed = 1;
                MTools.ResetFloatParameters(Anim);
            }

            RiderRoot.rotation = Quaternion.FromToRotation(RiderRoot.up, -Gravity.Value) * RiderRoot.rotation;


            ToogleColliders(true);

            if (DisableComponents) ToggleComponents(true);


            OnEndDismounting.Invoke();

            UpdateCanMountDismount();

            if (GroundController != null)
            {
                GroundController.Sleep = false;
                SendMessage("ResetInputAxis", SendMessageOptions.DontRequireReceiver);
            }

        }


        protected virtual void ConnectWithMount()
        {
            Montura.Animal.SetBoolParameter += SetAnimParameter;
            Montura.Animal.SetIntParameter += SetAnimParameter;
            Montura.Animal.SetFloatParameter += SetAnimParameter;
            Montura.Animal.SetTriggerParameter += SetAnimParameter;
            if (ReSync) Montura.Animal.StateCycle += Animators_Locomotion_ReSync;
        }

        protected void DisconnectWithMount()
        {
            Montura.Animal.SetBoolParameter -= SetAnimParameter;
            Montura.Animal.SetIntParameter -= SetAnimParameter;
            Montura.Animal.SetFloatParameter -= SetAnimParameter;
            if (ReSync) Montura.Animal.StateCycle -= Animators_Locomotion_ReSync;
        }

        internal virtual void MountTriggerEnter(Mount mount, MountTriggers mountTrigger)
        {
            Montura = mount;   
            MountTrigger = mountTrigger; 
            OnFindMount.Invoke(mount.Animal.gameObject);

            if (!mountTrigger.AutoMount)
                Montura.OnCanBeMounted.Invoke(Montura.CanBeMountedByState);

            Montura.NearbyRider = this;

            UpdateCanMountDismount();
        }

        internal virtual void MountTriggerExit()
        {
            if (Montura)
                Montura.ExitMountTrigger();

            MountTrigger = null;
            Montura = null;
            MountInput = null;
            OnFindMount.Invoke(null);
            UpdateCanMountDismount();
        }

        internal virtual void UpdateCanMountDismount()
        {
            CanMount = Montura && !Mounted && !IsOnHorse && Montura.CanBeMountedByState;
            OnCanMount.Invoke(CanMount);


            bool canDismount = IsRiding && Montura.CanBeDismountedByState;
            CanDismount = canDismount;
            OnCanDismount.Invoke(CanDismount);


            bool canCallAnimal = !Montura && !Mounted && !IsOnHorse && m_MountStored.Value != null;

            CanCallAnimal = canCallAnimal;
            CanCallMount.Invoke(CanCallAnimal);
        }

        protected virtual void Animators_Locomotion_ReSync(int CurrentState)
        {
            if (!Anim || MountLayerIndex == -1) return;
            if (Montura.Animal.Stance != 0) return;                                                      
            if (Montura.ID != 0) return;                                                    

            if (Anim.IsInTransition(MountLayerIndex) || Montura.Anim.IsInTransition(0)) return;  

            if (MTools.CompareOR(CurrentState, StateEnum.Locomotion, StateEnum.Swim))
            {
                var HorseStateInfo = Montura.Animal.Anim.GetCurrentAnimatorStateInfo(0);
                var RiderStateInfo = Anim.GetCurrentAnimatorStateInfo(MountLayerIndex);

                HorseNormalizedTime = HorseStateInfo.normalizedTime;
                RiderNormalizedTime = RiderStateInfo.normalizedTime;

                var Diff = Mathf.Abs(HorseNormalizedTime - RiderNormalizedTime);

                if (Diff >= ResyncThreshold)
                {
                    Anim.CrossFade(RiderStateInfo.fullPathHash, 0.2f, MountLayerIndex, HorseNormalizedTime);
                }
            }
            else
            {
                RiderNormalizedTime = HorseNormalizedTime = 0;
            }
        }

        public virtual void CallAnimal(bool call)
        {
            if (CanCallAnimal)
            {
                ToggleCall = call;

                if (m_MountStored.Value.IsPrefab())
                {
                    if (MountStored)
                    {
                        MountStored.AI?.ClearTarget();
                        MountStored.AI?.Stop();
                    }

                    MountStored = m_MountStored.Value.FindComponent<Mount>();

                    if (MountStored)
                    {
                    }
                    else
                    {
                        return;
                    }


                    var InsMount = Instantiate(m_MountStored.Value, transform.position - (transform.forward * 4), Quaternion.identity);

                    InsMount.gameObject.name = InsMount.gameObject.name.Replace("(Clone)", "");

                    m_MountStored.UseConstant = true;
                    m_MountStored.Value = InsMount;

                    MountStored = InsMount.FindComponent<Mount>();

                    ToggleCall = true;

                }
                else
                {
                    if (!MountStored)
                    {
                        MountStored = m_MountStored.Value.FindComponent<Mount>();

                        if (!MountStored)
                        {
                            return;
                        }
                    }
                }

                if (MountStored.AI != null && MountStored.AI.Active)
                {
                    if (ToggleCall)
                    {
                        MountStored.AI.SetActive(true);
                        MountStored.AI.SetTarget(RiderRoot, true);
                        MountStored.AI.Move(); 

                        if (CallAnimalA)
                            RiderAudio.PlayOneShot(CallAnimalA);
                    }
                    else
                    {
                        StopMountAI();

                        if (StopAnimalA)
                            RiderAudio.PlayOneShot(StopAnimalA);
                    }
                }
            }
        }


        public virtual void StopMountAI()
        {
            if (MountStored != null && MountStored.AI != null)
            {
                MountStored.AI.Stop();
                MountStored.AI.ClearTarget();
            }
        }


        public virtual void CallAnimalToggle()
        {
            if (CanCallAnimal)
            {
                ToggleCall ^= true;
                CallAnimal(ToggleCall);
            }
        }


        protected virtual void ToogleColliders(bool active)
        {
            MountingCollider(!active);
            foreach (var col in colliders) col.enabled = active;

        }

        private void MountingCollider(bool Mounting)
        {
            if (MainCollider && MountCollider)
            {
                if (Mounting)
                    MountCollider.Modify(MainCollider);
                else
                    Def_CollPropeties.Modify(MainCollider);
            }
        }

        protected virtual void ToggleComponents(bool enabled)
        {
            if (DisableList.Length == 0)
            {
                foreach (var component in AllComponents)
                {
                    if (component is MRider) continue;
                    component.enabled = enabled;
                }
            }
            else
            {
                foreach (var component in DisableList)
                {
                    if (component != null) component.enabled = enabled;
                }
            }
        }

        #region Set Animator Parameters
        public void SetAnimParameter(int hash, int value) { if (Anim && HasParam(hash)) Anim.SetInteger(hash, value); }

        public void SetAnimParameter(int hash, float value) { if (Anim && HasParam(hash)) Anim.SetFloat(hash, value); }

        public void SetAnimParameter(int hash, bool value) { if (Anim && HasParam(hash)) Anim.SetBool(hash, value); }

        public void SetAnimParameter(int hash) { if (Anim && HasParam(hash)) Anim.SetTrigger(hash); }
        #endregion
        private bool HasParam(int hash) => animatorParams.ContainsKey(hash);

        #region Link Animator
        protected virtual void SyncAnimator()
        {
            MAnimal animal = Montura.Animal;

            SetAnimParameter(animal.hash_Vertical, animal.VerticalSmooth);
            SetAnimParameter(animal.hash_Horizontal, animal.HorizontalSmooth);
            SetAnimParameter(animal.hash_Slope, animal.SlopeNormalized);
            SetAnimParameter(animal.hash_Grounded, animal.Grounded);
            SetAnimParameter(animal.hash_ModeStatus, animal.ModeStatus);
            SetAnimParameter(animal.hash_StateFloat, animal.State_Float);

            if (!Montura.UseSpeedModifiers) SpeedMultiplier = animal.SpeedMultiplier;

            if (Anim) Anim.speed = Montura.Anim.speed;

            SpeedMultiplier = Mathf.MoveTowards(SpeedMultiplier, TargetSpeedMultiplier, Time.deltaTime * 5f);
            SetAnimParameter(animal.hash_SpeedMultiplier, SpeedMultiplier);
        }
        #endregion



        public virtual void CheckMountDismount()
        {
            UpdateCanMountDismount();

            if (CanMount) MountAnimal();
            else if (CanDismount) DismountAnimal();
            else if (CanCallAnimal) CallAnimalToggle();
        }

        void OnAnimatorIK()
        {
            if (Anim == null) return;

            IKFeet();

            IK_Reins();

            SolveStraightMount();
        }

        private void IK_Reins()
        {
            if (IsRiding)
            {
                if (Montura && LeftHand && RightHand)
                {
                    var New_L_ReinPos = Montura.Rider.LeftHand.TransformPoint(LeftReinOffset);
                    var New_R_ReinPos = Montura.Rider.RightHand.TransformPoint(RightReinOffset);

                    if (!freeLeftHand && !freeRightHand)
                    {
                        Montura.ResetLeftRein();
                        Montura.ResetRightRein();
                        return;
                    }

                    if (Montura.LeftRein)
                    {
                        if (freeLeftHand)
                        {
                            Montura.LeftRein.position = New_L_ReinPos;
                        }
                        else
                        {
                            if (freeRightHand)
                            {
                                Montura.LeftRein.position = New_R_ReinPos;
                            }
                        }
                    }
                    if (Montura.RightRein)
                    {
                        if (freeRightHand)
                        {
                            Montura.RightRein.position = New_R_ReinPos;
                        }
                        else
                        {
                            if (freeLeftHand)
                            {
                                Montura.RightRein.position = New_L_ReinPos;
                            }
                        }
                    }
                }
            }
        }

        public bool IsAiming { get; set; }

        private void SolveStraightMount()
        {
            if (IsRiding && !IsAiming)
            {
                if (Montura.StraightSpine)
                {
                    SP_Weight = Mathf.MoveTowards(SP_Weight, Montura.StraightSpine ? 1 : 0, Montura.Animal.DeltaTime * Montura.smoothSM / 2);
                }
                else
                {
                    SP_Weight = Mathf.MoveTowards(SP_Weight, 0, Montura.Animal.DeltaTime * Montura.smoothSM / 2);
                }

                if (SP_Weight != 0)
                {
                    Anim.SetLookAtPosition(Montura.MonturaSpineOffset);
                    Anim.SetLookAtWeight(SP_Weight, 0.6f, 1);
                }
            }
        }

        private void IKFeet()
        {
            if (Montura && Montura.HasIKFeet)
            {
                if (IsMountingDismounting)
                {
                    L_IKFootWeight = 1f;
                    R_IKFootWeight = 1f;

                    if (IsMounting || IsDismounting)
                    {
                        L_IKFootWeight = Anim.GetFloat(IKLeftFootHash);
                        R_IKFootWeight = Anim.GetFloat(IKRightFootHash);
                    }

                    Anim.SetIKPositionWeight(AvatarIKGoal.LeftFoot, L_IKFootWeight);
                    Anim.SetIKPositionWeight(AvatarIKGoal.RightFoot, R_IKFootWeight);

                    Anim.SetIKHintPositionWeight(AvatarIKHint.LeftKnee, L_IKFootWeight);
                    Anim.SetIKHintPositionWeight(AvatarIKHint.RightKnee, R_IKFootWeight);

                    Anim.SetIKRotationWeight(AvatarIKGoal.LeftFoot, L_IKFootWeight);
                    Anim.SetIKRotationWeight(AvatarIKGoal.RightFoot, R_IKFootWeight);

                    Anim.SetIKPosition(AvatarIKGoal.LeftFoot, Montura.FootLeftIK.position);
                    Anim.SetIKPosition(AvatarIKGoal.RightFoot, Montura.FootRightIK.position);

                    Anim.SetIKHintPosition(AvatarIKHint.LeftKnee, Montura.KneeLeftIK.position); 
                    Anim.SetIKHintPosition(AvatarIKHint.RightKnee, Montura.KneeRightIK.position);

                    Anim.SetIKHintPositionWeight(AvatarIKHint.LeftKnee, L_IKFootWeight);
                    Anim.SetIKHintPositionWeight(AvatarIKHint.RightKnee, R_IKFootWeight);

                    Anim.SetIKRotation(AvatarIKGoal.LeftFoot, Montura.FootLeftIK.rotation);
                    Anim.SetIKRotation(AvatarIKGoal.RightFoot, Montura.FootRightIK.rotation);
                }
                else
                {
                    Anim.SetIKPositionWeight(AvatarIKGoal.LeftFoot, 0f);
                    Anim.SetIKPositionWeight(AvatarIKGoal.RightFoot, 0f);

                    Anim.SetIKRotationWeight(AvatarIKGoal.LeftFoot, 0f);
                    Anim.SetIKRotationWeight(AvatarIKGoal.RightFoot, 0f);
                }
            }
        }

        public virtual bool OnAnimatorBehaviourMessage(string message, object value) => this.InvokeWithParams(message, value);

        public virtual void EnableMountInput(bool value) => Montura?.EnableInput(value);

        public void DisableMountInput(string input) => MountInput?.DisableInput(input);

        public void EnableMountInput(string input) => MountInput?.EnableInput(input);


        #region IKREINS
        public void FreeRightHand(bool value)
        {
            if (Montura != null)
            {
                freeRightHand = !value;
                if (freeRightHand) Montura.ResetRightRein();
            }
        }

        public void FreeLeftHand(bool value)
        {
            if (Montura != null)
            {
                freeLeftHand = !value;
                if (freeLeftHand) Montura.ResetLeftRein();
            }
        }

        public void FreeBothHands()
        {
            FreeRightHand(false);
            FreeLeftHand(false);
        }


        public void WeaponInHands()
        {
            FreeRightHand(true);
            FreeLeftHand(true);
        }
        #endregion


#if UNITY_EDITOR
        private void OnValidate()
        {
            if (MountCollider == null)
                MountCollider = Resources.Load<CapsuleColliderPreset>("Mount_Capsule");
        }

        private void Reset()
        {
            animator = this.FindComponent<Animator>();
            RB = this.FindComponent<Rigidbody>();
            RiderRoot = transform;

            MainCollider = GetComponent<CapsuleCollider>();
            MountCollider = Resources.Load<CapsuleColliderPreset>("Mount_Capsule");

            if (MainCollider)
                Def_CollPropeties = new OverrideCapsuleCollider(MainCollider) { modify = (CapsuleModifier)(-1) };

            BoolVar CanMountV = MTools.GetInstance<BoolVar>("Can Mount");
            BoolVar CanDismountV = MTools.GetInstance<BoolVar>("Can Dismount");
            BoolVar CanCallMountV = MTools.GetInstance<BoolVar>("Can Call Mount");


            MEvent CanMountE = MTools.GetInstance<MEvent>("Rider Can Mount");
            MEvent CanDismountE = MTools.GetInstance<MEvent>("Rider Can Dismount");
            MEvent RiderMountUI = MTools.GetInstance<MEvent>("Rider Mount UI");

            MEvent CanCallMountE = MTools.GetInstance<MEvent>("Rider Can Call Mount");

            MEvent RiderisRiding = MTools.GetInstance<MEvent>("Rider is Riding");
            MEvent SetCameraSettings = MTools.GetInstance<MEvent>("Set Camera Settings");
            BoolVar RCWeaponInput = MTools.GetInstance<BoolVar>("RC Weapon Input");

            m_CanCallAnimal.Variable = CanCallMountV;
            m_CanCallAnimal.UseConstant = false;

            m_CanMount.Variable = CanMountV;
            m_CanMount.UseConstant = false;

            m_CanDismount.Variable = CanDismountV;
            m_CanDismount.UseConstant = false;



            OnCanMount = new BoolEvent();
            OnCanDismount = new BoolEvent();
            CanCallMount = new BoolEvent();
            OnStartMounting = new UnityEvent();
            OnEndMounting = new UnityEvent();
            OnStartMounting = new UnityEvent();
            OnStartDismounting = new UnityEvent();


            if (CanMountE != null) UnityEditor.Events.UnityEventTools.AddPersistentListener(OnCanMount, CanMountE.Invoke);

            if (CanDismountE != null) UnityEditor.Events.UnityEventTools.AddPersistentListener(OnCanDismount, CanDismountE.Invoke);

            if (CanCallMountE != null) UnityEditor.Events.UnityEventTools.AddPersistentListener(CanCallMount, CanCallMountE.Invoke);

            if (RiderMountUI != null) UnityEditor.Events.UnityEventTools.AddBoolPersistentListener(OnStartMounting, RiderMountUI.Invoke, false);

            if (RiderisRiding != null)
            {
                UnityEditor.Events.UnityEventTools.AddBoolPersistentListener(OnEndMounting, RiderisRiding.Invoke, true);
                UnityEditor.Events.UnityEventTools.AddBoolPersistentListener(OnStartDismounting, RiderisRiding.Invoke, false);
            }

            if (SetCameraSettings != null) UnityEditor.Events.UnityEventTools.AddObjectPersistentListener<Transform>(OnStartDismounting, SetCameraSettings.Invoke, transform);

            if (RCWeaponInput != null)
            {
                UnityEditor.Events.UnityEventTools.AddBoolPersistentListener(OnStartDismounting, RCWeaponInput.SetValue, false);
                UnityEditor.Events.UnityEventTools.AddBoolPersistentListener(OnEndMounting, RCWeaponInput.SetValue, true);
            }


            var malbersinput = GetComponent<MalbersInput>();

            if (malbersinput)
            {
                UnityEditor.Events.UnityEventTools.AddBoolPersistentListener(OnStartMounting, malbersinput.SetMoveCharacter, false);
                UnityEditor.Events.UnityEventTools.AddBoolPersistentListener(OnEndDismounting, malbersinput.SetMoveCharacter, true);
            }
        }

        [HideInInspector] public int Editor_Tabs1;

        [ContextMenu("Create Mount Inputs")]
        void ConnectToInput()
        {
            MInput input = GetComponent<MInput>();

            if (input == null) { input = gameObject.AddComponent<MInput>(); }


            #region Mount Input
            var mountInput = input.FindInput("Mount");

            if (mountInput == null)
            {
                mountInput = new InputRow("Mount", "Mount", KeyCode.F, InputButton.Down, InputType.Key);
                input.inputs.Add(mountInput);

                UnityEditor.Events.UnityEventTools.AddStringPersistentListener(OnStartMounting, input.DisableInput, mountInput.Name);
                UnityEditor.Events.UnityEventTools.AddStringPersistentListener(OnEndDismounting, input.EnableInput, mountInput.Name);

                UnityEditor.Events.UnityEventTools.AddPersistentListener(mountInput.OnInputDown, MountAnimal);


                Debug.Log("<B>Mount</B> Input created and connected to Rider.MountAnimal");
            }
            #endregion

            #region Dismount Input


            var DismountInput = input.FindInput("Dismount");

            if (DismountInput == null)
            {
                DismountInput = new InputRow("Dismount", "Dismount", KeyCode.F, InputButton.LongPress, InputType.Key);

                DismountInput.LongPressTime = 0.2f;

                input.inputs.Add(DismountInput);


                DismountInput.Active = false;

                UnityEditor.Events.UnityEventTools.AddStringPersistentListener(OnEndMounting, input.EnableInput, DismountInput.Name);
                UnityEditor.Events.UnityEventTools.AddStringPersistentListener(OnStartDismounting, input.DisableInput, DismountInput.Name);

                UnityEditor.Events.UnityEventTools.AddPersistentListener(DismountInput.OnLongPress, DismountAnimal);


                var RiderDismountUI = MTools.GetInstance<MEvent>("Rider Dismount UI");

                UnityEditor.Events.UnityEventTools.AddPersistentListener(DismountInput.OnLongPress, DismountAnimal);

                if (RiderDismountUI != null)
                {
                    UnityEditor.Events.UnityEventTools.AddPersistentListener(DismountInput.OnLongPress, RiderDismountUI.Invoke);
                    UnityEditor.Events.UnityEventTools.AddPersistentListener(DismountInput.OnPressedNormalized, RiderDismountUI.Invoke);
                    UnityEditor.Events.UnityEventTools.AddPersistentListener(DismountInput.OnInputUp, RiderDismountUI.Invoke);
                    UnityEditor.Events.UnityEventTools.AddIntPersistentListener(DismountInput.OnInputDown, RiderDismountUI.Invoke, 0);
                }
            }

            #endregion

            #region CanCallMount Input


            var CanCallMount = input.FindInput("Call Mount");

            if (CanCallMount == null)
            {
                CanCallMount = new InputRow("Call Mount", "Call Mount", KeyCode.F, InputButton.Down, InputType.Key);
                input.inputs.Add(CanCallMount);

                UnityEditor.Events.UnityEventTools.AddPersistentListener(CanCallMount.OnInputDown, CallAnimalToggle);
            }

            #endregion

            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.EditorUtility.SetDirty(input);
        }

        [ContextMenu("Create Event Listeners")]
        void CreateEventListeners()
        {
            MEvent RiderSetMount = MTools.GetInstance<MEvent>("Rider Set Mount");
            MEvent RiderSetDismount = MTools.GetInstance<MEvent>("Rider Set Dismount");

            MEventListener listener = GetComponent<MEventListener>();

            if (listener == null)
            {
                listener = gameObject.AddComponent<MEventListener>();
            }

            if (listener.Events == null) listener.Events = new List<MEventItemListener>();

            if (listener.Events.Find(item => item.Event == RiderSetMount) == null)
            {
                var item = new MEventItemListener()
                {
                    Event = RiderSetMount,
                    useVoid = true,
                };

                UnityEditor.Events.UnityEventTools.AddPersistentListener(item.Response, MountAnimal);
                listener.Events.Add(item);

                Debug.Log("<B>Rider Set Mount</B> Added to the Event Listeners");
            }

            if (listener.Events.Find(item => item.Event == RiderSetDismount) == null)
            {
                var item = new MEventItemListener()
                {
                    Event = RiderSetDismount,
                    useVoid = true,
                };

                UnityEditor.Events.UnityEventTools.AddPersistentListener(item.Response, DismountAnimal);
                listener.Events.Add(item);
            }

        }


        private void FindRHand()
        {
            if (animator != null && animator.avatar.isHuman)
            {
                RightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);
                SetDirty();
            }
        }
        private void FindLHand()
        {
            if (animator != null && animator.avatar.isHuman)
            {
                LeftHand = animator.GetBoneTransform(HumanBodyBones.LeftHand);
                SetDirty();
            }
        }


        void SetDirty()
        {
#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
#endif
        }


        void OnDrawGizmos()
        {
            if (Anim && Mounted && Montura.debug && Montura.Animal.ActiveStateID == StateEnum.Locomotion)
            {
                Transform head = Anim.GetBoneTransform(HumanBodyBones.Head);

                Gizmos.color = (int)RiderNormalizedTime % 2 == 0 ? Color.red : Color.white;

                Gizmos.DrawSphere((head.position - transform.root.right * 0.2f), 0.05f);

                Gizmos.color = (int)HorseNormalizedTime % 2 == 0 ? new Color(0.11f, 1f, 0.25f) : Color.white;
                Gizmos.DrawSphere((head.position + transform.root.right * 0.2f), 0.05f);

                UnityEditor.Handles.color = Color.white;
                UnityEditor.Handles.Label(head.position + transform.up * 0.5f, "Sync Status");

            }
        }
#endif
    }

    #region INSPECTOR
#if UNITY_EDITOR
    [CustomEditor(typeof(MRider), true)]
    public class MRiderEd : Editor
    {
        protected MRider M;

        protected SerializedProperty
            MountStored, StartMounted, Parent, animator, m_rigidBody, m_root, gravity, ReSync, ResyncThreshold,
            MountLayer, LayerPath, OnCanMount, OnCanDismount, OnStartMounting, OnEndMounting, m_CanMount, m_CanDismount, m_CanCallAnimal,
            OnStartDismounting, OnEndDismounting, OnFindMount, CanCallMount, OnAlreadyMounted, DisableList, MainCollider,
            CallAnimalA, StopAnimalA, RiderAudio, MountCollider,
            LinkUpdate, debug, AlingMountTrigger, DismountType, DisableComponents, Editor_Tabs1,
            LeftHand, RightHand, RightReinOffset, LeftReinOffset
            ;


        protected virtual void OnEnable()
        {
            M = (MRider)target;

            MountStored = serializedObject.FindProperty("m_MountStored");
            MainCollider = serializedObject.FindProperty("MainCollider");
            MountCollider = serializedObject.FindProperty("MountCollider");


            RightReinOffset = serializedObject.FindProperty("RightReinOffset");
            LeftReinOffset = serializedObject.FindProperty("LeftReinOffset");


            ReSync = serializedObject.FindProperty("ReSync");
            ResyncThreshold = serializedObject.FindProperty("ResyncThreshold");

            m_CanMount = serializedObject.FindProperty("m_CanMount");
            m_CanDismount = serializedObject.FindProperty("m_CanDismount");
            m_CanCallAnimal = serializedObject.FindProperty("m_CanCallAnimal");
            gravity = serializedObject.FindProperty("Gravity");


            animator = serializedObject.FindProperty("animator");
            m_rigidBody = serializedObject.FindProperty("m_rigidBody");
            m_root = serializedObject.FindProperty("m_root");
            StartMounted = serializedObject.FindProperty("StartMounted");
            Parent = serializedObject.FindProperty("Parent");


            Editor_Tabs1 = serializedObject.FindProperty("Editor_Tabs1");

            OnCanMount = serializedObject.FindProperty("OnCanMount");
            OnCanDismount = serializedObject.FindProperty("OnCanDismount");
            OnStartMounting = serializedObject.FindProperty("OnStartMounting");
            OnEndMounting = serializedObject.FindProperty("OnEndMounting");
            OnStartDismounting = serializedObject.FindProperty("OnStartDismounting");
            OnEndDismounting = serializedObject.FindProperty("OnEndDismounting");
            OnFindMount = serializedObject.FindProperty("OnFindMount");
            CanCallMount = serializedObject.FindProperty("CanCallMount");
            OnAlreadyMounted = serializedObject.FindProperty("OnAlreadyMounted");



            CallAnimalA = serializedObject.FindProperty("CallAnimalA");
            StopAnimalA = serializedObject.FindProperty("StopAnimalA");

            RiderAudio = serializedObject.FindProperty("RiderAudio");


            RightHand = serializedObject.FindProperty("RightHand");
            LeftHand = serializedObject.FindProperty("LeftHand");

            LinkUpdate = serializedObject.FindProperty("LinkUpdate");

            debug = serializedObject.FindProperty("debug");
            AlingMountTrigger = serializedObject.FindProperty("AlingMountTrigger");
            DismountType = serializedObject.FindProperty("DismountType");


            DisableComponents = serializedObject.FindProperty("DisableComponents");
            DisableList = serializedObject.FindProperty("DisableList");

        }

        #region GUICONTENT
        private readonly GUIContent G_DisableComponents = new GUIContent("Disable Components", "If some of the components are breaking the Rider Logic, disable them");
        private readonly GUIContent G_DisableList = new GUIContent("Disable List", "Monobehaviours that will be disabled while mounted");
        private readonly GUIContent G_Parent = new GUIContent("Parent to Mount", "Parent the Rider to the Mount Point on the Mountable Animal");
        private readonly GUIContent G_DismountType = new GUIContent("Dismount Type", "Changes the Dismount animation on the Rider.\nRandom: Randomly select a Dismount Animation.\nInput: Select the Dismount Animation by the Horizontal and Vertical Input Axis.\n Last: Uses the Last Mount Animation as a reference for the Dismount Animation.");
        #endregion

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.BeginVertical(MalbersEditor.StyleGray);



            Editor_Tabs1.intValue = GUILayout.Toolbar(Editor_Tabs1.intValue, new string[] { "General", "Events", "Advanced", "Debug" });


            int Selection = Editor_Tabs1.intValue;

            if (Selection == 0) DrawGeneral();
            else if (Selection == 1) DrawEvents();
            else if (Selection == 2) DrawAdvanced();
            else if (Selection == 3) DrawDebug();

            serializedObject.ApplyModifiedProperties();
            EditorGUILayout.EndVertical();
        }


        private void DrawDebug()
        {
            if (Application.isPlaying)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.ToggleLeft("Can Mount", M.CanMount);
                EditorGUILayout.ToggleLeft("Can Dismount", M.CanDismount);
                EditorGUILayout.ToggleLeft("Can Call Animal", M.CanCallAnimal);
                EditorGUILayout.Space();
                EditorGUILayout.ToggleLeft("Mounted", M.Mounted);

                EditorGUILayout.ToggleLeft("Is on Horse", M.IsOnHorse);
                EditorGUILayout.ToggleLeft("Is Mounting", M.IsMounting);
                EditorGUILayout.ToggleLeft("Is Riding", M.IsRiding);
                EditorGUILayout.ToggleLeft("Is Dismounting", M.IsDismounting);
                EditorGUILayout.Space();
                EditorGUILayout.ObjectField("Current Mount", M.Montura, typeof(Mount), false);
                EditorGUILayout.ObjectField("Stored Mount", M.MountStored, typeof(Mount), false);
                EditorGUILayout.ObjectField("Mount Trigger", M.MountTrigger, typeof(MountTriggers), false);
                EditorGUI.EndDisabledGroup();
                EditorGUILayout.EndVertical();

                Repaint();
            }
        }

        private void DrawAdvanced()
        {

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                EditorGUILayout.LabelField("References", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(animator);
                EditorGUILayout.PropertyField(m_rigidBody);
                EditorGUILayout.PropertyField(m_root, new GUIContent("Rider's Root", "Root Gameobject for the Rider Character"));
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                EditorGUILayout.PropertyField(ReSync);
                EditorGUILayout.PropertyField(ResyncThreshold);
                EditorGUILayout.PropertyField(AlingMountTrigger, new GUIContent("Align MTrigger Time", "Time to Align to the Mount Trigger Position while is playing the Mount Animation"));
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawEvents()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                EditorGUILayout.PropertyField(OnCanMount);
                EditorGUILayout.PropertyField(OnCanDismount);

                EditorGUILayout.PropertyField(OnStartMounting);
                EditorGUILayout.PropertyField(OnEndMounting);
                EditorGUILayout.PropertyField(OnStartDismounting);
                EditorGUILayout.PropertyField(OnEndDismounting);

                EditorGUILayout.PropertyField(OnFindMount);
                EditorGUILayout.PropertyField(CanCallMount);

                if (M.StartMounted.Value)
                {
                    EditorGUILayout.PropertyField(OnAlreadyMounted);
                }

            }
            EditorGUILayout.EndVertical();
        }

        private void DrawGeneral()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                var MStoredGUI = "Stored Mount";
                var MStoredTooltip = "If Start Mounted is Active this will be the Animal to mount.";

                if (M.m_MountStored.Value != null && M.m_MountStored.Value.IsPrefab())
                {
                    MStoredGUI += "[Prefab]";
                    MStoredTooltip += "\nThe Stored Mount is a Prefab. It will be instantiated";
                }

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(StartMounted, new GUIContent("Start Mounted", "Set an animal to start mounted on it"));
                MalbersEditor.DrawDebugIcon(debug);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.PropertyField(MountStored, new GUIContent(MStoredGUI, MStoredTooltip));
            }
            EditorGUILayout.EndVertical();


            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                EditorGUILayout.PropertyField(Parent, G_Parent);
                EditorGUILayout.PropertyField(LinkUpdate, new GUIContent("Link Update", "Updates Everyframe the position and rotation of the rider to the Animal Mount Point"));
                EditorGUILayout.PropertyField(DismountType, G_DismountType);
                EditorGUILayout.PropertyField(gravity, new GUIContent("Gravity Dir"));
            }
            EditorGUILayout.EndVertical();



            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                EditorGUILayout.LabelField("Rider Collider", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(MainCollider, new GUIContent("Main Collider", "Main Character collider for the Rider"));
                EditorGUILayout.PropertyField(MountCollider, new GUIContent("Collider Modifier", "When mounting the Collider will change its properties to this preset"));
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                EditorGUILayout.PropertyField(RightHand);
                EditorGUILayout.PropertyField(LeftHand);
                EditorGUILayout.Space();
                EditorGUILayout.PropertyField(LeftReinOffset);
                EditorGUILayout.PropertyField(RightReinOffset);
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                EditorGUILayout.PropertyField(DisableComponents, G_DisableComponents);

                if (M.DisableComponents)
                {
                    MalbersEditor.Arrays(DisableList, G_DisableList);

                    if (M.DisableList != null && M.DisableList.Length == 0)
                    {
                        EditorGUILayout.HelpBox("If 'Disable List' is empty , it will disable all Monovehaviours while riding", MessageType.Info);
                    }
                }
            }
            EditorGUILayout.EndVertical();


            EditorGUI.BeginDisabledGroup(Application.isPlaying);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                EditorGUILayout.LabelField("Exposed Values", EditorStyles.boldLabel);

                EditorGUILayout.PropertyField(m_CanMount, new GUIContent("Can Mount", "It will be enabled when the Rider is near a mount Trigger,\nIt's used on the Active parameter of the Mount Input"));
                EditorGUILayout.PropertyField(m_CanDismount, new GUIContent("Can Dismount", "It will be enabled when the Rider riding a mount,\nIt's used on the Active parameter of the Dismount Input"));
                EditorGUILayout.PropertyField(m_CanCallAnimal, new GUIContent("Can Call Mount", "It will be enabled when the Rider has a Mount Stored and is not near or mounted is near the mount,\nIt's used on the Active parameter of the Can call Mount Input"));
            }
            EditorGUILayout.EndVertical();
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                EditorGUILayout.PropertyField(CallAnimalA, new GUIContent("Call Animal", "Sound to call the Stored Animal"));
                EditorGUILayout.PropertyField(StopAnimalA, new GUIContent("Stop Animal", "Sound to stop calling the Stored Animal"));
                EditorGUILayout.PropertyField(RiderAudio, new GUIContent("Audio Source", "The reference for the audio source"));
            }
            EditorGUILayout.EndVertical();
        }

        public static void AddParametersOnAnimator(UnityEditor.Animations.AnimatorController AnimController, UnityEditor.Animations.AnimatorController Mounted)
        {
            AnimatorControllerParameter[] parameters = AnimController.parameters;
            AnimatorControllerParameter[] Mountedparameters = Mounted.parameters;

            foreach (var param in Mountedparameters)
            {
                if (!SearchParameter(parameters, param.name))
                {
                    AnimController.AddParameter(param);
                }
            }
        }

        public static bool SearchParameter(AnimatorControllerParameter[] parameters, string name)
        {
            foreach (AnimatorControllerParameter item in parameters)
            {
                if (item.name == name) return true;
            }
            return false;
        }
    }
#endif
    #endregion
}