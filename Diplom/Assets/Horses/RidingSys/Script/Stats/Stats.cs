using RidingSystem.Events;
using RidingSystem.Scriptables;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;


#if UNITY_EDITOR
using UnityEditor;
#endif

namespace RidingSystem
{
    [AddComponentMenu("RidingSystem/Stats/Stats Manager")]
    public class Stats : MonoBehaviour, IAnimatorListener
    {
        public List<Stat> stats = new List<Stat>();
        public Dictionary<int, Stat> stats_D;

        public Stat PinnedStat;

        public virtual bool OnAnimatorBehaviourMessage(string message, object value) => this.InvokeWithParams(message, value);

        public void Initialize()
        {
            StopAllCoroutines();

            stats_D = new Dictionary<int, Stat>();

            foreach (var stat in stats)
            {
                if (stat.ID == null)
                {
                    Debug.LogError("One of the Stats has an Empty ID", gameObject);
                    break;
                }
                stat.InitializeStat(this);

                if (!stats_D.ContainsKey(stat.ID))
                {
                    stats_D.Add(stat.ID, stat);
                }
                else
                {
                    stats_D[stat.ID] = stat;
                }
            }
        }

        private void OnEnable()
        {
            Initialize();
        }
        private void OnDisable()
        {
            StopAllCoroutines();
        }


        public virtual void Stats_Update()
        {
            foreach (var s in stats) s.UpdateStat();
        }

        public virtual void Stats_Update(StatID iD) => Stats_Update(iD.ID);

        public virtual void Stats_Update(int iD) => Stat_Get(iD)?.UpdateStat();

        public virtual void Stat_Reset_to_Max(StatID iD) => Stat_Get(iD)?.Reset_to_Max();

        public virtual void Stat_Reset_to_Min(StatID iD) => Stat_Get(iD)?.Reset_to_Min();

        public virtual void Stat_Disable(StatID iD) => Stat_Get(iD)?.SetActive(false);

        public virtual void Stat_Degenerate_Off(StatID ID) => Stat_Get(ID)?.SetDegeneration(false);

        public virtual void Stat_Degenerate_On(StatID ID) => Stat_Get(ID)?.SetDegeneration(true);

        public virtual void Stat_Regenerate_Off(StatID ID) => Stat_Get(ID)?.SetRegeneration(false);

        public virtual void Stat_Regenerate_On(StatID ID) => Stat_Get(ID)?.SetRegeneration(true);


        #region Callbacks with StatID parameters
        public virtual void Stat_Enable(StatID iD) => Stat_Get(iD)?.SetActive(true);

        public virtual void Stat_Pin(StatID ID) => Stat_Get(ID.ID);

        public virtual Stat Stat_Get(StatID ID) => Stat_Get(ID.ID);

        public virtual void Stat_Inmune_Activate(StatID ID) => Stat_Get(ID)?.SetInmune(true);

        public virtual void Stat_Inmune_Deactivate(StatID ID) => Stat_Get(ID)?.SetInmune(false);

        #endregion


        public virtual void Stat_Pin(string name) => Stat_Get(name);

        public virtual void Stat_Pin(int ID) => Stat_Get(ID);


        public virtual Stat Stat_Get(string name) => PinnedStat = stats.Find(item => item.Name == name);

        public virtual Stat Stat_Get(int ID)
        {
            if (stats_D != null && stats_D.TryGetValue(ID, out PinnedStat))
                return PinnedStat;
            return null;
        }
        public virtual Stat Stat_Get(IntVar ID) => Stat_Get(ID.Value);

        public virtual void Stat_ModifyValue(StatID ID, float modifyvalue) => Stat_Get(ID)?.Modify(modifyvalue);
        public virtual void Stat_ModifyValue(int ID, float modifyvalue) => Stat_Get(ID)?.Modify(modifyvalue);
        public virtual void Stat_ModifyValue(string name, float modifyvalue) => Stat_Get(name)?.Modify(modifyvalue);

        public virtual void Stat_ModifyValue(StatID ID, float modifyvalue, StatOption modifyType) => Stat_Get(ID)?.Modify(modifyvalue, modifyType);
        public virtual void Stat_ModifyValue(string name, float modifyvalue, StatOption modifyType) => Stat_Get(name)?.Modify(modifyvalue, modifyType);

        public virtual void Stat_Pin_ModifyValue(float value) => PinnedStat?.Modify(value);

        public virtual void Stat_Pin_ModifyValue(FloatVar value) => PinnedStat?.Modify(value.Value);

        public virtual void Stat_Pin_SetMult(float value) => PinnedStat?.SetMultiplier(value);

        public virtual void Stat_Pin_SetMult(FloatVar value) => PinnedStat?.SetMultiplier(value.Value);

        public virtual void Stat_Pin_ModifyValue(float value, float time) => PinnedStat?.Modify(value, time);

        public virtual void Stat_Pin_ModifyValue_1Sec(float value) => PinnedStat?.Modify(value, 1);

        public virtual void Stat_Pin_SetValue(float value) => PinnedStat.SetValue(value);

        public virtual void Stat_Pin_ModifyMaxValue(float value) => PinnedStat?.ModifyMAX(value);

        public virtual void Stat_Pin_SetMaxValue(float value) => PinnedStat?.SetMAX(value);

        public virtual void Stat_Pin_Modify_RegenRate(float value) => PinnedStat?.ModifyRegenRate(value);

        public virtual void Stat_Pin_Degenerate(bool value) => PinnedStat?.SetDegeneration(value);

        public virtual void Stat_Pin_SetInmune(bool value) => PinnedStat?.SetInmune(value);



        public virtual void Stat_Pin_Regenerate(bool value) => PinnedStat?.SetRegeneration(value);

        public virtual void Stat_Pin_Enable(bool value) => PinnedStat?.SetActive(value);

        public virtual void Stat_Pin_ModifyValue(float newValue, int ticks, float timeBetweenTicks) => PinnedStat?.Modify(newValue, ticks, timeBetweenTicks);

        public virtual void Stat_Pin_CleanCoroutines() => PinnedStat?.CleanRoutines();




        [Obsolete("Use Stat_Degenerate_Off instead")]
        public virtual void DegenerateOff(StatID ID) => Stat_Degenerate_Off(ID);

        [Obsolete("Use Stat_Degenerate_On instead")]
        public virtual void DegenerateOn(StatID ID) => Stat_Degenerate_On(ID);






#if UNITY_EDITOR

        [ContextMenu("Create/Stamina")]
        private void ConnectStamina()
        {
            if (stats == null) stats = new List<Stat>();


            var staminaID = MTools.GetInstance<StatID>("Stamina");


            if (staminaID != null)
            {
                var staminaStat = Stat_Get(staminaID);

                if (staminaStat == null)
                {
                    staminaStat = new Stat()
                    {
                        ID = staminaID,
                        value = new FloatReference(100),
                        InmuneTime = new FloatReference(0.5f),
                        regenerate = new BoolReference(true),
                        RegenRate = new FloatReference(40),
                        DegenRate = new FloatReference(20),
                        RegenWaitTime = new FloatReference(2),
                        Above = 25f,
                    };
                    stats.Add(staminaStat);
                }

                var method = this.GetUnityAction<bool>("MAnimal", "UseSprint");
                if (method != null)
                {
                    UnityEditor.Events.UnityEventTools.AddBoolPersistentListener(staminaStat.OnStatBelow, method, false);
                    UnityEditor.Events.UnityEventTools.AddBoolPersistentListener(staminaStat.OnStatAbove, method, true);
                }

                MEvent UIStamina = MTools.GetInstance<MEvent>("UI Stamina Stat");

                if (UIStamina)
                {
                    UnityEditor.Events.UnityEventTools.AddPersistentListener(staminaStat.OnValueChangeNormalized, UIStamina.Invoke);
                    UnityEditor.Events.UnityEventTools.AddPersistentListener(staminaStat.OnStatFull, UIStamina.Invoke);
                }


                var onSprintEnable = this.GetFieldClass<BoolEvent>("MAnimal", "OnSprintEnabled");

                if (onSprintEnable != null)
                {
                    UnityEditor.Events.UnityEventTools.AddObjectPersistentListener<StatID>(onSprintEnable, Stat_Pin, staminaID);
                    UnityEditor.Events.UnityEventTools.AddPersistentListener(onSprintEnable, Stat_Pin_Degenerate);
                }


                MTools.SetDirty(this);
            }
        }

        [ContextMenu("Create/Health")]
        void CreateHealth()
        {
            var health = MTools.GetInstance<StatID>("Health");

            if (health != null)
            {

                var HealthStat = new Stat()
                {
                    ID = health,
                    value = new FloatReference(100),
                    DisableOnEmpty = new BoolReference(true),
                    InmuneTime = new FloatReference(0.1f)
                };
                stats.Add(HealthStat);


                var deathID = MTools.GetInstance<StateID>("Death");

                var method = this.GetUnityAction<StateID>("MAnimal", "State_Activate");

                if (method != null) UnityEditor.Events.UnityEventTools.AddObjectPersistentListener<StateID>(HealthStat.OnStatEmpty, method, deathID);

                MTools.SetDirty(this);
            }
        }

        private void Reset()
        {
            if (stats == null) stats = new List<Stat>();

            CreateHealth();
        }
#endif
    }


    [Serializable]
    public class Stat
    {
        #region Variables 

        [Tooltip("Enable/Disable the Stat. Disable Stats cannot be modified")]
        public bool active = true;
        [Tooltip("Key Idendifier for the Stat")]
        public StatID ID;
        [Tooltip("Current Value of the Stat")]
        public FloatReference value = new FloatReference(0);
        [Tooltip("Maximun Value of the Stat")]
        public FloatReference maxValue = new FloatReference(100);
        [Tooltip("Minimum Value of the Stat")]
        public FloatReference minValue = new FloatReference();
        [Tooltip("If the Stat is Empty it will be disabled to avoid future changes")]
        public BoolReference DisableOnEmpty = new BoolReference();

        [SerializeField] internal FloatReference multiplier = new FloatReference(1);

        [SerializeField] internal BoolReference regenerate = new BoolReference(false);
        public FloatReference RegenRate;
        public FloatReference RegenWaitTime = new FloatReference(0);
        public FloatReference DegenWaitTime = new FloatReference(0);
        [SerializeField] internal BoolReference degenerate = new BoolReference(false);
        public FloatReference DegenRate;
        public FloatReference InmuneTime;
        public ResetTo resetTo = ResetTo.MaxValue;
        private bool regenerate_LastValue;
        private bool degenerate_LastValue;
        private bool isBelow = false;
        private bool isAbove = false;
        #endregion

        #region Events
        public UnityEvent OnStatFull = new UnityEvent();
        public UnityEvent OnStatEmpty = new UnityEvent();
        public UnityEvent OnStat = new UnityEvent();
        public float Below;
        public float Above;
        public UnityEvent OnStatBelow = new UnityEvent();
        public UnityEvent OnStatAbove = new UnityEvent();
        public FloatEvent OnValueChangeNormalized = new FloatEvent();
        public FloatEvent OnValueChange = new FloatEvent();
        public BoolEvent OnDegenerate = new BoolEvent();
        public BoolEvent OnRegenerate = new BoolEvent();
        public BoolEvent OnActive = new BoolEvent();
        #endregion

        #region Properties
        public bool Active
        {
            get => active;
            set
            {
                active = value;

                OnActive.Invoke(value);
                if (value)
                    StartRegeneration();
                else
                    StopRegeneration();
            }
        }

        public string Name
        {
            get
            {
                if (ID != null)
                {
                    return ID.name;
                }
                return string.Empty;
            }
        }

        public float Value
        {
            get => value;
            set => SetValue(value);
        }

        public bool IsFull => Value == MaxValue;
        public bool IsEmpty => Value == MinValue;

        public float Multiplier { get => multiplier.Value; set => multiplier.Value = value; }

        public float NormalizedValue => Value / MaxValue;

        public bool IsInmune { get; set; }

        public float MaxValue { get => maxValue.Value; set => maxValue.Value = value; }

        public float MinValue { get => minValue.Value; set => minValue.Value = value; }

        public bool IsRegenerating { get; private set; }

        public bool IsDegenerating { get; private set; }

        [SerializeField] internal int EditorTabs = 0;

        public bool Regenerate
        {
            get => regenerate.Value;
            set
            {
                regenerate.Value = value;
                regenerate_LastValue = regenerate;
                OnRegenerate.Invoke(value);

                if (regenerate)
                {
                    degenerate.Value = false;
                    StopDegeneration();
                    StartRegeneration();
                }
                else
                {
                    degenerate.Value = degenerate_LastValue;
                    StopRegeneration();
                    StartDegeneration();
                }
            }
        }

        public bool Degenerate
        {
            get => degenerate.Value;
            set
            {
                degenerate.Value = value;
                degenerate_LastValue = degenerate; 
                OnDegenerate.Invoke(value);

                if (degenerate)
                {
                    regenerate.Value = false;
                    StartDegeneration();
                    StopRegeneration();
                }
                else
                {
                    regenerate.Value = regenerate_LastValue;
                    StopDegeneration();
                    StartRegeneration();
                }
            }
        }

        #endregion

        [NonSerialized] private WaitForSeconds InmuneWait;

        internal void InitializeStat(Stats holder)
        {
            isAbove = isBelow = false;
            Owner = holder;

            if (value.Value >= Above) isAbove = true;
            else if (value.Value <= Below) isBelow = true;

            regenerate_LastValue = Regenerate;

            if (MaxValue < Value) MaxValue = Value;


            I_Regeneration = null;
            I_Degeneration = null;
            I_ModifyPerTicks = null;

            InmuneWait = new WaitForSeconds(InmuneTime);

            if (Active)
            {
                StartRegeneration();
                StartDegeneration();
            }

            holder.Delay_Action(3, () => ValueEvents());
        }

        internal void SetMultiplier(float value) => multiplier.Value = value;


        internal void ValueEvents()
        {
            OnValueChangeNormalized.Invoke(NormalizedValue);
            OnValueChange.Invoke(value);


            if (this.value == minValue.Value)
            {
                this.value.Value = minValue.Value;
                OnStatEmpty.Invoke();

                if (DisableOnEmpty.Value)
                {
                    SetActive(false);
                    return;
                }

            }
            else if (this.value == maxValue.Value)
            {
                this.value.Value = maxValue.Value;
                OnStatFull.Invoke();
            }


            if (this.value >= Above && !isAbove)
            {
                OnStatAbove.Invoke();
                isAbove = true;
                isBelow = false;
            }
            else if (this.value <= Below && !isBelow)
            {
                OnStatBelow.Invoke();
                isBelow = true;
                isAbove = false;
            }
        }

        internal void SetValue(float value)
        {
            var RealValue = Mathf.Clamp(value * Multiplier, MinValue, maxValue);

            if ((!Active) || 
                (this.value.Value == RealValue)) return;

            this.value.Value = RealValue;

            ValueEvents();
        }

        public void SetActive(bool value) => Active = value;
        public void SetRegeneration(bool value) => Regenerate = value;
        public void SetDegeneration(bool value) => Degenerate = value;
        public void SetInmune(bool value) => IsInmune = value;

        public virtual void Modify(float newValue)
        {
            if (!IsInmune && Active)
            {
                Value += newValue;
                StartRegeneration();
                if (!Regenerate)
                    StartDegeneration();

                SetInmune();
            }
        }

        public virtual void UpdateStat()
        {
            SetValue(value);
            StartRegeneration();
            if (!Regenerate)
                StartDegeneration();
        }

        public virtual void Modify(float newValue, float time)
        {
            if (!IsInmune && Active)
            {
                StopSlowModification();
                Owner.StartCoroutine(out I_ModifySlow, C_SmoothChangeValue(newValue, time));
                SetInmune();
            }
        }

        public virtual void Modify(float newValue, int ticks, float timeBetweenTicks)
        {
            if (!Active) return;
            StopCoroutine(I_ModifyPerTicks);

            Owner.StartCoroutine(out I_ModifyPerTicks, C_ModifyTicksValue(newValue, ticks, timeBetweenTicks));
        }

        public virtual void ModifyMAX(float newValue)
        {
            if (!Active) return;
            MaxValue += newValue;
            StartRegeneration();
        }

        public virtual void SetMAX(float newValue)
        {
            if (!Active) return;
            MaxValue = newValue;
            StartRegeneration();
        }


        public virtual void ModifyRegenRate(float newValue)
        {
            if (!Active) return;

            RegenRate.Value += newValue;
            StartRegeneration();
        }

        public virtual void SetRegenerationWait(float newValue)
        {
            if (!Active) return;

            RegenWaitTime.Value = newValue;

            if (RegenWaitTime < 0) RegenWaitTime.Value = 0;
        }

        public virtual void SetRegenerationRate(float newValue)
        {
            if (!Active) return;
            RegenRate.Value = newValue;
        }

        public virtual void Reset() => Value = (resetTo == ResetTo.MaxValue) ? MaxValue : MinValue;

        public virtual void Reset_to_Max() => Value = MaxValue;

        public virtual void Reset_to_Min() => Value = MinValue;
        internal void CleanRoutines()
        {
            StopDegeneration();
            StopRegeneration();
            StopTickDamage();
            StopSlowModification();
        }


        public virtual void RegenerateOverTime(float time)
        {
            if (time <= 0)
            {
                StartRegeneration();
            }
            else
            {
                Owner.StartCoroutine(C_RegenerateOverTime(time));
            }
        }

        protected virtual void SetInmune()
        {
            if (InmuneTime > 0)
            {
                StopCoroutine(I_IsInmune);
                Owner.StartCoroutine(out I_IsInmune, C_InmuneTime());
            }
        }



        private void StopCoroutine(IEnumerator Cor)
        {
            if (Cor != null) Owner.StopCoroutine(Cor);
        }

        protected virtual void StartRegeneration()
        {
            StopRegeneration();

            if (RegenRate == 0 || !Regenerate) return;

            Owner.StartCoroutine(out I_Regeneration, C_Regenerate());
        }


        protected virtual void StartDegeneration()
        {
            StopDegeneration();
            if (DegenRate == 0 || !Degenerate) return;

            Owner.StartCoroutine(out I_Degeneration, C_Degenerate());
        }

        protected virtual void StopRegeneration()
        {
            StopCoroutine(I_Regeneration);

            I_Regeneration = null;
            IsRegenerating = false;
        }

        protected virtual void StopDegeneration()
        {
            StopCoroutine(I_Degeneration);

            I_Degeneration = null;
            IsDegenerating = false;
        }

        protected virtual void StopTickDamage()
        {
            StopCoroutine(I_ModifyPerTicks);
            I_ModifyPerTicks = null;
        }

        protected virtual void StopSlowModification()
        {
            StopCoroutine(I_ModifySlow);
            I_ModifySlow = null;
        }

        public void Modify(float Value, StatOption modify)
        {
            switch (modify)
            {
                case StatOption.AddValue:
                    Modify(Value);
                    break;
                case StatOption.SetValue:
                    this.Value = Value;
                    break;
                case StatOption.SubstractValue:
                    Modify(-Value);
                    break;
                case StatOption.ModifyMaxValue:
                    ModifyMAX(Value);
                    break;
                case StatOption.SetMaxValue:
                    MaxValue = Value;
                    break;
                case StatOption.Degenerate:
                    DegenRate = Value;
                    Degenerate = true;
                    break;
                case StatOption.StopDegenerate:
                    DegenRate = Value;
                    Degenerate = false;
                    break;
                case StatOption.Regenerate:
                    Regenerate = true;
                    RegenRate = Value;
                    break;
                case StatOption.StopRegenerate:
                    Regenerate = false;
                    RegenRate = Value;
                    break;
                case StatOption.Reset:
                    Reset();
                    break;
                case StatOption.ReduceByPercent:
                    Modify(-(MaxValue * Value / 100));
                    break;
                case StatOption.IncreaseByPercent:
                    Modify(MaxValue * Value / 100);
                    break;
                case StatOption.Multiplier:
                    Multiplier = Value;
                    break;
                case StatOption.ResetToMax:
                    Reset_to_Max();
                    break;
                case StatOption.ResetToMin:
                    Reset_to_Min();
                    break;
                case StatOption.None:
                    break;
                default:
                    break;
            }
        }


        #region Coroutines
        public Stats Owner { get; private set; }
        private IEnumerator I_Regeneration;
        private IEnumerator I_Degeneration;
        private IEnumerator I_ModifyPerTicks;
        private IEnumerator I_ModifySlow;
        private IEnumerator I_IsInmune;


        protected IEnumerator C_RegenerateOverTime(float time)
        {
            float ReachValue = RegenRate > 0 ? MaxValue : MinValue;
            bool Positive = RegenRate > 0;
            float currentTime = Time.time;

            while (Value != ReachValue || currentTime > time)
            {
                Value += (RegenRate * Time.deltaTime);

                if (Positive && Value > MaxValue)
                {
                    Value = MaxValue;
                }
                else if (!Positive && Value < 0)
                {
                    Value = MinValue;
                }
                currentTime += Time.deltaTime;

                yield return null;
            }
            yield return null;
        }

        protected IEnumerator C_InmuneTime()
        {
            IsInmune = true;
            yield return InmuneWait;
            IsInmune = false;
        }

        protected IEnumerator C_Regenerate()
        {
            yield return null;

            if (RegenWaitTime > 0)
                yield return new WaitForSeconds(RegenWaitTime);

            IsRegenerating = true;



            while (Regenerate && Value < MaxValue)
            {
                Value += (RegenRate * Time.deltaTime);
                yield return null;
            }

            IsRegenerating = false;
            yield return null;
        }

        protected IEnumerator C_Degenerate()
        {
            yield return null;

            if (DegenWaitTime > 0)
                yield return new WaitForSeconds(DegenWaitTime);

            IsDegenerating = true;

            while (Degenerate && Value > MinValue)
            {
                Value -= (DegenRate * Time.deltaTime);
                yield return null;
            }
            IsDegenerating = false;
            yield return null;
        }

        protected IEnumerator C_ModifyTicksValue(float value, int Ticks, float time)
        {
            var WaitForTicks = new WaitForSeconds(time);

            for (int i = 0; i < Ticks; i++)
            {
                Value += value;
                if (Value <= MinValue)
                {
                    Value = MinValue;
                    break;
                }
                yield return WaitForTicks;
            }

            yield return null;

            StartRegeneration();
        }

        protected IEnumerator C_SmoothChangeValue(float newvalue, float time)
        {
            StopRegeneration();
            float currentTime = 0;
            float currentValue = Value;
            newvalue = Value + newvalue;

            yield return null;
            while (currentTime <= time)
            {

                Value = Mathf.Lerp(currentValue, newvalue, currentTime / time);
                currentTime += Time.deltaTime;

                yield return null;
            }
            Value = newvalue;

            yield return null;
            StartRegeneration();
        }
        #endregion

        public enum ResetTo
        {
            MinValue,
            MaxValue
        }
    }


}