using RidingSystem.Events;
using RidingSystem.Scriptables;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace RidingSystem.Controller
{
    [System.Serializable]
    public class Mode
    {
        #region Public Variables
        [SerializeField] private bool active = true;

        [SerializeField] private bool ignoreLowerModes = false;

        protected int ModeTagHash;
        public string Input;
        [SerializeField] public ModeID ID;


        [CreateScriptableAsset]
        public ModeModifier modifier;

        public FloatReference CoolDown = new FloatReference(0);

        public List<Ability> Abilities;
        [SerializeField]
        private IntReference m_AbilityIndex = new IntReference(-99);
        public IntReference DefaultIndex = new IntReference(0);
        public IntEvent OnAbilityIndex = new IntEvent();
        public bool ResetToDefault = false;

        [SerializeField] private bool allowRotation = false;
        [SerializeField] private bool allowMovement = false;

        public UnityEvent OnEnterMode = new UnityEvent();
        public UnityEvent OnExitMode = new UnityEvent();

        public AudioSource m_Source;
        #endregion

        #region Properties

        public bool PlayingMode { get; set; }

        public bool IsInTransition { get; set; }

        public bool Active { get => active; set => active = value; }

        public int Priority { get; internal set; }

        public bool AllowRotation { get => allowRotation; set => allowRotation = value; }

        public bool AllowMovement { get => allowMovement; set => allowMovement = value; }

        public string Name => ID != null ? ID.name : string.Empty;

        public bool HasCoolDown => (CoolDown == 0) || InCoolDown;

        public bool InCoolDown { get; internal set; }


        public float ActivationTime;

        public bool IgnoreLowerModes { get => ignoreLowerModes; set => ignoreLowerModes = value; }

        public int AbilityIndex
        {
            get => m_AbilityIndex;
            set
            {
                m_AbilityIndex.Value = value;
                OnAbilityIndex.Invoke(value);
            }
        }

        public void SetAbilityIndex(int index) => AbilityIndex = index;


        public MAnimal Animal { get; private set; }

        public Ability ActiveAbility { get; private set; }
       
        public bool InputValue { get; internal set; }


        #endregion


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

            foreach (var ability in Abilities)
            {
                if (!string.IsNullOrEmpty(ability.Input))
                {
                    var input = InputSource.GetInput(ability.Input);

                    if (input != null)
                    {
                        if (ability.InputListener == null)
                            ability.InputListener = (x) => ActivateAbilitybyInput(ability, x);

                        if (connect)
                            input.InputChanged.AddListener(ability.InputListener);
                        else
                            input.InputChanged.RemoveListener(ability.InputListener);
                    }
                }
            }
        }

        public virtual void AwakeMode(MAnimal animal)
        {
            Animal = animal;
            OnAbilityIndex.Invoke(AbilityIndex);
            ActivationTime = -CoolDown * 2;
            InCoolDown = false;
        }

        public virtual void ResetMode()
        {
            if (Animal.ActiveMode == this)
            {
                Animal.Set_State_Sleep_FromMode(false);
            }

            PlayingMode = false;

            modifier?.OnModeExit(this);
            ActiveAbility.modifier?.OnModeExit(this);

            if (ActiveAbility.m_stopAudio)
            {
                if (ActiveAbility.audioSource != null) ActiveAbility.audioSource.Stop();
                if (m_Source != null) m_Source.Stop();
            }


            if (ResetToDefault && !InputValue)
                m_AbilityIndex.Value = DefaultIndex.Value;

            OnExitInvoke();
            ActiveAbility = null;
        }

        public virtual void ModeExit()
        {
            Animal.ActiveMode = null;
            Animal.ModeTime = 0;
            Animal.SetModeStatus(Animal.ModeAbility = 0);
        }

        public virtual void ResetAbilityIndex()
        {
            if (!Animal.Zone) SetAbilityIndex(DefaultIndex);
        }

        public bool HasAbilityIndex(int index) => Abilities.Find(ab => ab.Index == index) != null;

        public void SetActive(bool value) => Active = value;

        public void ActivatebyInput(bool Input_Value)
        {
            if (!Active) return;
            if (Animal != null && !Animal.enabled) return;
            if (Animal.LockInput) return;

            if (InputValue != Input_Value)
            {
                InputValue = Input_Value;

                if (InputValue)
                {
                    if (Animal.Zone && Animal.Zone.IsMode)
                        Animal.Zone.ActivateZone(Animal);
                    else
                        TryActivate();
                }
                else
                {
                    if (PlayingMode && CheckStatus(AbilityStatus.Charged))
                    {
                        Animal.Mode_Interrupt();
                    }
                }
            }
        }


        public void ActivateAbilitybyInput(Ability ability, bool Input_Value)
        {
            if (!Active) return;
            if (!Animal.enabled) return;
            if (Animal.LockInput) return;

            if (InputValue != Input_Value)
            {
                InputValue = Input_Value;

                if (InputValue)
                {
                    TryActivate(ability);
                }
                else
                {
                    if (PlayingMode && ActiveAbility.Index == ability.Index && CheckStatus(AbilityStatus.Charged))
                    {
                        Animal.Mode_Interrupt();
                    }
                }
            }
        }

        private void Activate(Ability newAbility, int modeStatus, string deb)
        {
            ActiveAbility = newAbility;
            Animal.SetModeParameters(this, modeStatus);

            ActiveAbility.modifier?.OnModeEnter(this);

            AudioSource source = ActiveAbility.audioSource != null ? ActiveAbility.audioSource : m_Source;
            if (source && source.isActiveAndEnabled)
            {
                if (!ActiveAbility.audioClip.NullOrEmpty())
                    source.clip = ActiveAbility.audioClip.GetValue();

                if (source.isPlaying) source.Stop();
                source.PlayDelayed(ActiveAbility.ClipDelay);
            }
        }

        public bool ForceActivate(int abilityIndex)
        {
            if (abilityIndex != 0) AbilityIndex = abilityIndex;

            if (!Animal.IsPreparingMode)
            {
                if (Animal.IsPlayingMode)
                {
                    Animal.ActiveMode.ResetMode();
                    Animal.ActiveMode.ModeExit();
                }

                return TryActivate();

            }
            return false;
        }


        public virtual bool TryActivate() => TryActivate(AbilityIndex);

        public virtual bool TryActivate(int index) => TryActivate(GetTryAbility(index));

        public virtual bool TryActivate(int index, AbilityStatus status, float time = 0)
        {
            var TryNextAbility = GetTryAbility(index);

            if (TryNextAbility != null)
            {
                TryNextAbility.Status = status;

                if (status == AbilityStatus.PlayOneTime)
                    TryNextAbility.AbilityTime = time;

                return TryActivate(TryNextAbility);
            }
            return false;
        }

        public virtual bool TryActivate(Ability newAbility)
        {
            int ModeStatus = 0;
            string deb = "";

            if (newAbility == null)
            {
                return false;
            }

            if (Animal.IsPreparingMode)
            {
                return false;
            }

            if (!newAbility.Active)
            {
                return false;
            }

            if (StateCanInterrupt(Animal.ActiveState.ID, newAbility))
            {
                return false;
            }

            if (PlayingMode)
            {
                if (ActiveAbility.Index == newAbility.Index && CheckStatus(AbilityStatus.Toggle))
                {
                    InputValue = false;
                    Animal.Mode_Interrupt();
                    return false;
                }
                else if (newAbility.HasTransitionFrom && newAbility.Limits.TransitionFrom.Contains(ActiveAbility.Index))
                {
                    ModeStatus = ActiveAbility.Index;
                    deb = ($"Last Ability [{ModeStatus}] is allowing it. <Check ModeBehaviour>");
                    ResetMode();
                }
                else if (HasCoolDown)
                {
                    return false;
                }
                else if (!InCoolDown)
                {
                    ResetMode();
                    ModeExit();
                    deb = ($"No Longer in Cooldown [Same Mode]");
                }
            }
            else if (Animal.IsPlayingMode)
            {
                var ActiveMode = Animal.ActiveMode;

                if (Priority > ActiveMode.Priority && IgnoreLowerModes && !InCoolDown)
                {
                    ActiveMode.ResetMode();
                    ActiveMode.InputValue = false;
                    ActiveMode.ModeExit();

                    deb = ($"Has Interrupted [{ActiveMode.ID.name}] Mode, because it had lower Priority");
                }
                else
                {
                    if (ActiveMode.HasCoolDown)
                    {
                        return false;
                    }
                    else if (!ActiveMode.InCoolDown)
                    {
                        ActiveMode.ResetMode();
                        ActiveMode.ModeExit();
                        deb = ($"No Longer in Cooldown [Different Mode]");
                    }
                }
            }

            Activate(newAbility, ModeStatus, deb);

            return true;
        }


        public void AnimationTagEnter()
        {
            if (ActiveAbility != null && !PlayingMode)
            {
                PlayingMode = true;
                Animal.IsPreparingMode = false;

                Animal.ActiveMode = this;

                Animal.Set_State_Sleep_FromMode(true);

                OnEnterInvoke();

                ActivationTime = Time.time;

                if (!AllowMovement) Animal.InertiaPositionSpeed = Vector3.zero;

                var AMode = ActiveAbility.Status;

                var AModeName = AMode.ToString();

                int ModeStatus = Int_ID.Loop;

                if (AMode == AbilityStatus.PlayOneTime)
                {
                    ModeStatus = Int_ID.OneTime;
                }
                else if (AMode == AbilityStatus.ActiveByTime)
                {
                    float HoldByTime = ActiveAbility.AbilityTime;

                    Animal.StartCoroutine(Ability_By_Time(HoldByTime));
                    AModeName += ": " + HoldByTime;
                    InputValue = false;
                }
                else if (AMode == AbilityStatus.Toggle)
                {
                    AModeName += " On";
                    InputValue = false;
                }

                if (CoolDown > 0) Animal.StartCoroutine(C_SetCoolDown(CoolDown));

                Animal.SetModeStatus(ModeStatus);
            }

        }

        internal void OnAnimatorMove(float deltaTime)
        {
            if (ActiveAbility.Status == AbilityStatus.Charged && ActiveAbility.AbilityTime > 0)
            {
                var currentTime = (Time.time - ActivationTime) / ActiveAbility.AbilityTime;
                var curve = ActiveAbility.ChargeCurve.Evaluate(currentTime);
                var Char_Value = curve * ActiveAbility.ChargeValue;
                Animal.Mode_SetPower(curve);
                ActiveAbility.OnCharged.Invoke(Char_Value);
            }
        }

        public void AnimationTagExit(Ability exitingAbility, int ExitTransitionAbility)
        {
            string ExitTagLogic = "[Skip Exit Logic]";


            if (Animal.ActiveMode == this && ActiveAbility != null && ActiveAbility.Index.Value == exitingAbility.Index.Value)
            {
                ExitTagLogic = $"[Mode Reseted] AcAb:[{ActiveAbility.Index.Value}] ExAb:[{exitingAbility.Index.Value}]";

                ResetMode();
                ModeExit();


                if (ExitTransitionAbility != -1)
                {
                    IsInTransition = false;

                    if (TryActivate(ExitTransitionAbility))
                    {
                        ExitTagLogic = "[Exit to another Ability]";
                        AnimationTagEnter();
                    }
                }
                else
                {
                    if (InputValue && !InCoolDown) TryActivate();
                }
            }
        }




        public virtual Ability GetTryAbility(int index)
        {
            if (!Active) return null;
            if (index == 0) return null; 

            AbilityIndex = index;
            modifier?.OnModeEnter(this);

            if (Abilities == null || Abilities.Count == 0)
            {
                return null;
            }


            if (AbilityIndex == -99)
                return GetAbility(Abilities[Random.Range(0, Abilities.Count)].Index.Value);



            return GetAbility(AbilityIndex);
        }

        public virtual Ability GetAbility(int NewIndex) => Abilities.Find(item => item.Index == NewIndex);

        public virtual Ability GetAbility(string abilityName) => Abilities.Find(item => item.Name == abilityName);


        public virtual void OnModeStateMove(AnimatorStateInfo stateInfo, Animator anim, int Layer)
        {
            IsInTransition = anim.IsInTransition(Layer) &&
            (anim.GetNextAnimatorStateInfo(Layer).fullPathHash != anim.GetCurrentAnimatorStateInfo(Layer).fullPathHash);

            if (Animal.ActiveMode == this)
            {
                Animal.ModeTime = stateInfo.normalizedTime;
                modifier?.OnModeMove(this, stateInfo, anim, Layer);
                ActiveAbility.modifier?.OnModeMove(this, stateInfo, anim, Layer);
            }
        }

        public virtual bool StateCanInterrupt(StateID ID, Ability ability = null)
        {
            if (ability == null) ability = ActiveAbility;

            var properties = ability.Limits;

            if (properties.affect == AffectStates.None) return false;

            if (ability.HasAffectStates)
            {
                if (properties.affect == AffectStates.Exclude && HasState(properties, ID)
                || (properties.affect == AffectStates.Include && !HasState(properties, ID)))
                {
                    return true;
                }
            }
            return false;
        }


        public virtual bool StanceCanInterrupt(StanceID ID, Ability ability = null)
        {
            if (ability == null) ability = ActiveAbility;

            var properties = ability.Limits;

            if (properties.affect_Stance == AffectStates.None) return false;

            if (ability.HasAffectStances)
            {
                if (properties.affect_Stance == AffectStates.Exclude && HasStance(properties, ID)
                || (properties.affect_Stance == AffectStates.Include && !HasStance(properties, ID)))
                {
                    return true;
                }
            }
            return false;
        }

        protected static bool HasState(ModeProperties properties, StateID ID) => properties.affectStates.Exists(x => x.ID == ID.ID);
        protected static bool HasStance(ModeProperties properties, StanceID ID) => properties.Stances.Exists(x => x.ID == ID.ID);


        public IEnumerator C_SetCoolDown(float time)
        {
            InCoolDown = true;
            yield return new WaitForSeconds(time);
            InCoolDown = false;

            if (InputValue)
            {
                ResetMode();
                ModeExit();
                TryActivate(AbilityIndex);
            }
        }

        protected IEnumerator Ability_By_Time(float time)
        {
            yield return new WaitForSeconds(time);
            Animal.Mode_Interrupt();
        }

        private void OnExitInvoke()
        {
            ActiveAbility.OnExit.Invoke();
            OnExitMode.Invoke();
        }

        private void OnEnterInvoke()
        {
            ActiveAbility.OnEnter.Invoke();
            OnEnterMode.Invoke();
        }


        private bool CheckStatus(AbilityStatus status)
        {
            if (ActiveAbility == null) return false;
            return ActiveAbility.Status == status;
        }

        public virtual void Disable()
        {
            Active = false;
            InputValue = false;

            if (PlayingMode)
            {
                if (!CheckStatus(AbilityStatus.PlayOneTime))
                {
                    Animal.Mode_Interrupt();
                }
                else {}
            }
        }

        public virtual void Enable() => Active = true;
    }

    [System.Serializable]
    public class Ability
    {
        public BoolReference active = new BoolReference(true);
        public string Name;
        public IntReference Index = new IntReference(0);

        [Tooltip("Unique Input to play for each Ability")]
        public StringReference Input;

        [Tooltip("Clip to play when the ability is played")]
        public AudioClipReference audioClip;

        [Tooltip("Clip Sound Delay")]
        public FloatReference ClipDelay = new FloatReference(0);

        [Tooltip("Local AudioSource for an specific Ability")]
        public AudioSource audioSource;

        [Tooltip("Stop the Audio sound on Ability Exit")]
        public bool m_stopAudio = true;

        [Tooltip("Local Mode Modifier to Add to the Ability")]
        [CreateScriptableAsset]
        public ModeModifier modifier;

        [UnityEngine.Serialization.FormerlySerializedAs("Properties")]
        public ModeProperties Limits;

        [Tooltip("The Ability can Stay Active until it finish the Animation, by Holding the Input Down, by x time ")]
        public AbilityStatus Status = AbilityStatus.PlayOneTime;


        [Tooltip("The Ability will be completely charged after x seconds. If the value is zero, the charge logic will be ignored")]
        public FloatReference abilityTime = new FloatReference(3);

        [Tooltip("Curve value for the charged ability")]
        public AnimationCurve ChargeCurve = new AnimationCurve(MTools.DefaultCurve);

        [Tooltip("Charge maximun value for the Charged ability")]
        public FloatReference ChargeValue = new FloatReference(1);


        public float AbilityTime { get => abilityTime.Value; set => abilityTime.Value = value; }

        public bool HasAffectStates => Limits.affectStates != null && Limits.affectStates.Count > 0;

        public bool HasAffectStances => Limits.Stances != null && Limits.Stances.Count > 0;
        public bool HasTransitionFrom => Limits.TransitionFrom != null && Limits.TransitionFrom.Count > 0;

        public bool Active { get => active.Value; set => active.Value = value; }

        public UnityAction<bool> InputListener;



        public UnityEvent OnEnter = new UnityEvent();
        public UnityEvent OnExit = new UnityEvent();
        public FloatEvent OnCharged = new FloatEvent();
    }

    public enum AbilityStatus
    {
        PlayOneTime = 0,
        Charged = 1,
        ActiveByTime = 2,
        Toggle = 3,
        Forever = 4,
    }
    public enum AffectStates
    {
        None,
        Include,
        Exclude,
    }

    [System.Serializable]
    public class ModeProperties
    {
        public AffectStates affect = AffectStates.None;

        public List<StateID> affectStates = new List<StateID>();

        public AffectStates affect_Stance = AffectStates.None;

        public List<StanceID> Stances = new List<StanceID>();

        public List<int> TransitionFrom = new List<int>();

        public ModeProperties(ModeProperties properties)
        {
            affect = properties.affect;
            affect_Stance = properties.affect_Stance;
            affectStates = new List<StateID>(properties.affectStates);
            Stances = new List<StanceID>(properties.Stances);
            TransitionFrom = new List<int>();
        }
    }
}