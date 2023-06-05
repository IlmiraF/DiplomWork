using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using RidingSystem.Scriptables;
using UnityEngine.Events;

namespace RidingSystem.Controller
{
    public partial class MAnimal
    {
        #region INPUTS

        public virtual void ResetInputSource()
        {
            UpdateInputSource(false);
            UpdateInputSource(true);
        }

        public virtual void UpdateInputSource(bool connect)
        {
            if (InputSource == null)
                InputSource = gameObject.FindInterface<IInputSource>();

            if (InputSource != null)
            {
                foreach (var state in states)
                    state.ConnectInput(InputSource, connect);

                foreach (var mode in modes)
                    mode.ConnectInput(InputSource, connect);
            }
        }


        #endregion

        #region Player
        public virtual void SetMainPlayer()
        {
            if (MainAnimal)
            {
                MainAnimal.isPlayer.Value = false;
            }

            this.isPlayer.Value = true;
            MainAnimal = this;
        }

        public void DisableMainPlayer()
        {
            if (MainAnimal == this) MainAnimal = null;
        }
        #endregion

        #region Gravity
        public void ResetGravityDirection() => Gravity = Vector3.down;

        internal void ResetGravityValues()
        {
            GravityTime = m_gravityTime;
            GravityStoredVelocity = Vector3.zero;
        }
        internal void ResetUPVector()
        {
            RB.velocity = Vector3.ProjectOnPlane(RB.velocity, UpVector);
            AdditivePosition = Vector3.ProjectOnPlane(AdditivePosition, UpVector);
            DeltaPos = Vector3.ProjectOnPlane(DeltaPos, UpVector);
        }

        public void GroundChangesGravity(bool value) => ground_Changes_Gravity.Value = value;

        public void AlignToGravity()
        {
            Quaternion AlignRot = Quaternion.FromToRotation(transform.up, UpVector) * transform.rotation;
            base.transform.rotation = AlignRot;
        }
        #endregion

        #region Stances

        public void Stance_Toggle(StanceID NewStance) => Stance = (Stance.ID == NewStance.ID) ? DefaultStance : NewStance;

        public void Stance_Set(StanceID id) => Stance = id;

        public void Stance_Set(int id)
        {
            var NewStance = ScriptableObject.CreateInstance<StanceID>();
            NewStance.name = "Stance(" + id + ")";
            NewStance.ID = id;
            Stance = NewStance;
        }


        public void Stance_SetLast(int id)
        {
            LastStance = id;
            SetOptionalAnimParameter(hash_LastStance, LastStance);
        }



        public void Stance_Reset() => Stance = defaultStance;

        #endregion

        #region Animator Methods

        public virtual bool OnAnimatorBehaviourMessage(string message, object value)
        {
            foreach (var state in states) state.ReceiveMessages(message, value);

            return this.InvokeWithParams(message, value);
        }

        public void SetAnimatorSpeed(float value) => AnimatorSpeed = value;


        public virtual void SetAnimParameter(int hash, int value) => Anim.SetInteger(hash, value);

        public virtual void SetAnimParameter(int hash, float value) => Anim.SetFloat(hash, value);

        public virtual void SetAnimParameter(int hash, bool value) => Anim.SetBool(hash, value);

        public virtual void SetAnimParameter(int hash) => Anim.SetTrigger(hash);

        public virtual void SetOptionalAnimParameter(int Hash, float value)
        {
            if (Hash != 0) SetFloatParameter(Hash, value);
        }

        public virtual void SetOptionalAnimParameter(int Hash, int value)
        {
            if (Hash != 0) SetIntParameter(Hash, value);
        }

        public virtual void SetOptionalAnimParameter(int Hash, bool value)
        {
            if (Hash != 0) SetBoolParameter(Hash, value);
        }

        public virtual void SetOptionalAnimParameter(int Hash)
        {
            if (Hash != 0) SetTriggerParameter(Hash);
        }

        public void SetRandom(int value, int priority)
        {
            if (!enabled || Sleep) return;

            if (priority >= RandomPriority)
            {
                RandomPriority = priority;
                RandomID = Randomizer ? value : 0;
                SetOptionalAnimParameter(hash_Random, RandomID);
            }
        }

        public void ResetRandomPriority(int priority)
        {
            if (priority >= RandomPriority)
            {
                RandomPriority = 0;
            }
        }



        public virtual void EnterTag(string tag) => AnimStateTag = Animator.StringToHash(tag);
        #endregion

        #region States
        public void State_SetFloat(float value)
        {
            State_Float = value;
            SetFloatParameter(hash_StateFloat, State_Float);
        }


        public void State_SetFloat(float value, float smoothValue)
        {
            State_Float = Mathf.Lerp(State_Float, value, smoothValue * DeltaTime);
            SetFloatParameter(hash_StateFloat, State_Float);
        }


        public void State_Replace(State NewState)
        {
            if (CloneStates)
            {
                State instance = (State)ScriptableObject.CreateInstance(NewState.GetType());
                instance = ScriptableObject.Instantiate(NewState);
                instance.name = instance.name.Replace("(Clone)", "(C)");
                NewState = instance;
            }

            var oldState = states.Find(s => s.ID == NewState.ID);

            if (oldState)
            {
                var index = states.IndexOf(oldState);
                var oldStatePriority = oldState.Priority;

                if (CloneStates) Destroy(oldState);

                oldState = NewState;
                oldState.AwakeState(this);
                oldState.Priority = oldStatePriority;
                oldState.InitializeState();
                oldState.ExitState();


                states[index] = oldState;

                UpdateInputSource(true);
            }
        }

        public virtual void State_Force(StateID ID) => State_Force(ID.ID);

        public bool HasState(StateID ID) => HasState(ID.ID);


        public bool HasState(int ID) => State_Get(ID) != null;

        public bool HasState(string statename) => states.Exists(s => s.name == statename);

        public virtual void State_SetStatus(int status)
        {
            SetIntParameter(hash_StateEnterStatus, status);
        }

        public virtual void State_SetExitStatus(int ExitStatus) => SetOptionalAnimParameter(hash_StateExitStatus, ExitStatus);

        public virtual void State_Enable(StateID ID) => State_Enable(ID.ID);
        public virtual void State_Disable(StateID ID) => State_Disable(ID.ID);

        public virtual void State_Enable(int ID) => State_Get(ID)?.Enable(true);

        public virtual void State_Disable(int ID) => State_Get(ID)?.Enable(false);

        public virtual void State_Force(int ID)
        {
            State state = State_Get(ID);

            if (state == ActiveState)
            {
                state.ForceActivate();

                StartCoroutine(C_EnterCoreAnim(state));
            }
            else
                state.ForceActivate();
        }

        IEnumerator C_EnterCoreAnim(State state)
        {
            state.IsPending = true;
            yield return null;
            state.AnimationTagEnter(AnimStateTag);
        }

        public virtual void State_AllowExit(StateID ID) => State_AllowExit(ID.ID);

        public virtual void State_AllowExit(int ID)
        {
            State state = State_Get(ID);
            if (state && state != ActiveState) return;
            state?.AllowExit();
        }

        public virtual void State_Allow_Exit(int nextState)
        {
            if (ActiveState.AllowExit())
            {
                if (nextState != -1) State_Activate(nextState);
            }
        }
        public virtual void State_Allow_Exit(int nextState, int exitStatus)
        {
            if (ActiveState.AllowExit())
            {
                State_SetExitStatus(exitStatus);
                if (nextState != -1) State_Activate(nextState);
            }
        }

        public virtual void State_InputTrue(StateID ID) => State_Get(ID)?.SetInput(true);
        public virtual void State_InputFalse(StateID ID) => State_Get(ID)?.SetInput(false);
        public virtual void ActiveStateAllowExit() => ActiveState.AllowExit();


        public virtual void State_Activate(StateID ID) => State_Activate(ID.ID);


        public virtual bool State_TryActivate(int ID)
        {
            State NewState = State_Get(ID);
            if (NewState && NewState.CanBeActivated)
            {
                return NewState.TryActivate();
            }
            return false;
        }

        public virtual void State_Activate(int ID)
        {
            State NewState = State_Get(ID);

            if (NewState && NewState.CanBeActivated)
            {
                NewState.Activate();
            }
        }

        public virtual State State_Get(int ID) => states.Find(s => s.ID == ID);

        public virtual State State_Get(StateID ID)
        {
            if (ID == null) return null;
            return State_Get(ID.ID);
        }

        public virtual void State_Reset(int ID) => State_Get(ID)?.ResetState();

        public virtual void State_Reset(StateID ID) => State_Reset(ID.ID);

        public virtual void State_Pin(StateID stateID) => State_Pin(stateID.ID);

        public virtual void State_Pin(int stateID) => Pin_State = State_Get(stateID);

        public virtual void State_Pin_ByInput(bool input) => Pin_State?.ActivatebyInput(input);
        public virtual void State_Pin_ByInputToggle() => Pin_State?.ActivatebyInput(!Pin_State.InputValue);

        public virtual void State_Activate_by_Input(StateID stateID, bool input) => State_Activate_by_Input(stateID.ID, input);

        public virtual void State_Activate_by_Input(int stateID, bool input)
        {
            State_Pin(stateID);
            State_Pin_ByInput(input);
        }

        public virtual void State_Pin_ExitStatus(int stateExitStatus)
        {
            if (Pin_State != null && Pin_State.IsActiveState)
                State_SetExitStatus(stateExitStatus);
        }

        #endregion

        #region Modes
        public bool HasMode(ModeID ID) => HasMode(ID.ID);

        public bool HasMode(int ID) => Mode_Get(ID) != null;

        public virtual Mode Mode_Get(ModeID ModeID) => Mode_Get(ModeID.ID);

        public virtual Mode Mode_Get(int ModeID) => modes.Find(m => m.ID == ModeID);

        public void SetModeStatus(int value)
        {
            SetIntParameter?.Invoke(hash_ModeStatus, ModeStatus = value);
        }

        public void Mode_SetPower(float value) => SetOptionalAnimParameter(hash_ModePower, ModePower = value);

        public virtual void Mode_Activate(ModeID ModeID) => Mode_Activate(ModeID.ID, -99);

        public virtual void Mode_Activate(ModeID ModeID, int AbilityIndex) => Mode_Activate(ModeID.ID, AbilityIndex);

        public virtual void Mode_Activate_By_Input(ModeID ModeID, bool InputValue) => Mode_Get(ModeID.ID).ActivatebyInput(InputValue);

        #region INTERFACE ICHARACTER ACTION
        public bool PlayAction(int Set, int Index) => Mode_TryActivate(Set, Index);

        public bool ForceAction(int Set, int Index) => Mode_ForceActivate(Set, Index);

        public bool IsPlayingAction => IsPlayingMode;
        #endregion

        public virtual void Mode_Activate(int ModeID)
        {
            if (ModeID == 0) return;

            var id = Mathf.Abs(ModeID / 1000);

            if (id == 0)
            {
                Mode_Activate(ModeID, -99);
            }
            else
            {
                Mode_Activate(id, ModeID % 100);
            }
        }

        public virtual void Mode_Activate(int ModeID, int AbilityIndex)
        {
            var mode = Mode_Get(ModeID);

            if (mode != null)
            {
                Pin_Mode = mode;
                Pin_Mode.TryActivate(AbilityIndex);
            }
            else
            {
                Debug.LogWarning("You are trying to Activate a Mode but here's no Mode with the ID or is Disabled: " + ModeID);
            }
        }

        public virtual void Mode_Activate(int ModeID, int AbilityIndex, AbilityStatus status)
        {
            var mode = Mode_Get(ModeID);

            if (mode != null)
            {
                Pin_Mode = mode;

                var ability = Pin_Mode.GetAbility(AbilityIndex);

                if (ability != null)
                {
                    ability.Status = status;
                }

                Pin_Mode.TryActivate(AbilityIndex);
            }
            else
            {
                Debug.LogWarning("You are trying to Activate a Mode but here's no Mode with the ID or is Disabled: " + ModeID);
            }
        }


        public virtual bool Mode_ForceActivate(ModeID ModeID, int AbilityIndex) => Mode_ForceActivate(ModeID.ID, AbilityIndex);

        public virtual void Mode_ForceActivate(ModeID ModeID) => Mode_ForceActivate(ModeID.ID, 0);

        public virtual bool Mode_ForceActivate(int ModeID, int AbilityIndex)
        {
            var mode = Mode_Get(ModeID);

            if (mode != null)
            {
                Pin_Mode = mode;
                return Pin_Mode.ForceActivate(AbilityIndex);
            }
            return false;
        }


        public bool Mode_TryActivate(int ModeID, int AbilityIndex = -99)
        {
            var mode = Mode_Get(ModeID);

            if (mode != null)
            {
                Pin_Mode = mode;
                return Pin_Mode.TryActivate(AbilityIndex);
            }
            return false;
        }

        public bool Mode_TryActivate(int ModeID, int AbilityIndex, AbilityStatus status, float time = 0)
        {
            var mode = Mode_Get(ModeID);

            if (mode != null)
            {
                Pin_Mode = mode;
                return Pin_Mode.TryActivate(AbilityIndex, status, time);
            }
            return false;
        }


        public virtual void Mode_Stop()
        {
            if (IsPlayingMode)
            {
                activeMode.InputValue = false;
                Mode_Interrupt();
            }
            else
            {
                ModeAbility = 0;
                SetModeStatus(Int_ID.Available);
                return;
            }
            ActiveMode = null;
            ModeTime = 0; 
        }

        public virtual void SprintUpdate() => Sprint = sprint;
        public virtual void Sprint_Set(bool value) => Sprint = value;



        public virtual void Mode_Interrupt() => SetModeStatus(Int_ID.Interrupted);

        public virtual void Mode_Disable_All()
        {
            foreach (var mod in modes) mod.Disable();
        }

        public virtual void Mode_Enable_All()
        {
            foreach (var mod in modes) mod.Enable();
        }

        public virtual void Mode_Disable(ModeID id) => Mode_Disable((int)id);

        public virtual void Mode_Disable(int id) => Mode_Get(id)?.Disable();

        public virtual void Mode_Disable(string mod)
        {
            foreach (var M in modes)
            {
                if (mod.Contains(M.Name))
                    M.Disable();
            }
        }

        public virtual void Mode_Enable(string mod)
        {
            foreach (var M in modes)
            {
                if (mod.Contains(M.Name))
                    M.Enable();
            }
        }

        public virtual void Mode_ActiveAbilityIndex(int Mode, int ActiveAbility) => Mode_Get(Mode).SetAbilityIndex(ActiveAbility);

        public virtual void Mode_Enable(ModeID id) => Mode_Enable(id.ID);

        public virtual void Mode_Enable(int id) => Mode_Get(id)?.Enable();

        public virtual void Mode_Pin(ModeID ID)
        {
            if (Pin_Mode != null && Pin_Mode.ID == ID) return;

            var pin = Mode_Get(ID);

            Pin_Mode = null;
            if (pin != null && pin.Active) Pin_Mode = pin;
        }

        public virtual void Mode_Pin_Ability(int AbilityIndex)
        {
            if (AbilityIndex == 0) return;
            if (Pin_Mode != null) Pin_Mode.SetAbilityIndex(AbilityIndex);
        }


        public virtual bool Mode_Ability_Enable(int ModeID, int AbilityID, bool enable)
        {
            var mode = Mode_Get(ModeID);
            if (mode != null)
            {
                var ability = mode.GetAbility(AbilityID);
                if (ability != null)
                {
                    ability.Active = enable;
                    return true;
                }
            }
            return false;
        }

        public virtual void Mode_Pin_Ability_Enable(int AbilityIndex)
        {
            if (AbilityIndex == 0) return;
            var ability = Pin_Mode?.GetAbility(AbilityIndex);
            if (ability != null) ability.Active = true;
        }

        public virtual void Mode_Pin_Disable_Ability(int AbilityIndex)
        {
            if (AbilityIndex == 0) return;
            var ability = Pin_Mode?.GetAbility(AbilityIndex);
            if (ability != null) ability.Active = false;
        }


        public virtual void Mode_Pin_Status(int aMode)
        {
            if (Pin_Mode != null)
            {
                foreach (var ability in Pin_Mode.Abilities)
                {
                    ability.Status = (AbilityStatus)aMode;
                }
            }
        }

        public virtual void Mode_Pin_Time(float time)
        {
            if (Pin_Mode != null)
                foreach (var ab in Pin_Mode.Abilities)
                    ab.AbilityTime = time;
        }

        public virtual void Mode_Pin_Enable(bool value) => Pin_Mode?.SetActive(value);
        public virtual void Mode_Pin_EnableInvert(bool value) => Pin_Mode?.SetActive(!value);

        public virtual void Mode_Pin_Input(bool value) => Pin_Mode?.ActivatebyInput(value);

        public virtual void Mode_Pin_Activate() => Pin_Mode?.TryActivate();

        public virtual void Mode_Pin_AbilityActivate(int AbilityIndex) => Pin_Mode?.TryActivate(AbilityIndex);

        #endregion

        #region Movement
        public virtual void Strafe_Toggle() => Strafe ^= true;

        public virtual void Move(Vector3 move)
        {
            UseRawInput = false;
            RawInputAxis = move;
            Rotate_at_Direction = false;
            DeltaAngle = 0;
        }

        public virtual void Move(Vector2 move) => Move(new Vector3(move.x, 0, move.y));

        public virtual void MoveWorld(Vector2 move) => MoveWorld(new Vector3(move.x, 0, move.y));

        public virtual void StopMoving()
        {
            RawInputAxis = Vector3.zero;
            DeltaAngle = 0;
        }

        public virtual void AddInertia(ref Vector3 Inertia, float speed = 1f)
        {
            AdditivePosition += Inertia;
            Inertia = Vector3.Lerp(Inertia, Vector3.zero, DeltaTime * speed);
        }
        #endregion

        #region Speeds
        public virtual void SpeedUp() => Speed_Add(+1);

        public virtual void SpeedDown() => Speed_Add(-1);

        public virtual MSpeedSet SpeedSet_Get(string name) => speedSets.Find(x => x.name == name);

        public virtual MSpeed Speed_GetModifier(string name, int index)
        {
            var set = SpeedSet_Get(name);

            if (set != null && index < set.Speeds.Count)
                return set[index - 1];

            return MSpeed.Default;
        }

        public virtual void SetCustomSpeed(MSpeed customSpeed, bool keepInertiaSpeed = false)
        {
            CustomSpeed = true;
            CurrentSpeedModifier = customSpeed;

            if (keepInertiaSpeed)
            {
                CalculateTargetSpeed();
                InertiaPositionSpeed = TargetSpeed;
            }
        }

        private void Speed_Add(int change) => CurrentSpeedIndex += change;

        public virtual void Speed_CurrentIndex_Set(int speedIndex) => CurrentSpeedIndex = speedIndex;

        public virtual void Speed_CurrentIndex_Set(IntVar speedIndex) => CurrentSpeedIndex = speedIndex;

        public virtual void Speed_Change_Lock(bool lockSpeed) => SpeedChangeLocked = lockSpeed;

        public virtual void SpeedSet_Set_Active(string SpeedSetName, int activeIndex)
        {
            var speedSet = SpeedSet_Get(SpeedSetName);

            if (speedSet != null)
            {
                speedSet.CurrentIndex = activeIndex;

                if (CurrentSpeedSet == speedSet)
                {
                    CurrentSpeedIndex = activeIndex;
                    speedSet.StartVerticalIndex = activeIndex; 
                }
            }
        }

        public virtual void Speed_Update_Current() => CurrentSpeedIndex = CurrentSpeedIndex;

        public virtual void Speed_SetTopIndex(int topIndex)
        {
            CurrentSpeedSet.TopIndex = topIndex;
            Speed_Update_Current();
        }

        public virtual void Speed_SetTopIndex(string SpeedSetName, int topIndex)
        {
            var speedSet = SpeedSet_Get(SpeedSetName);
            if (speedSet != null)
            {
                speedSet.TopIndex = topIndex;
                Speed_Update_Current();
            }
        }


        public virtual void SpeedSet_Set_Active(string SpeedSetName, string activeSpeed)
        {
            var speedSet = speedSets.Find(x => x.name.ToLower() == SpeedSetName.ToLower());

            if (speedSet != null)
            {
                var mspeedIndex = speedSet.Speeds.FindIndex(x => x.name.ToLower() == activeSpeed.ToLower());

                if (mspeedIndex != -1)
                {
                    speedSet.CurrentIndex = mspeedIndex + 1;

                    if (CurrentSpeedSet == speedSet)
                    {
                        CurrentSpeedIndex = mspeedIndex + 1;
                        speedSet.StartVerticalIndex = CurrentSpeedIndex; 
                    }
                }
            }
            else
            {
                Debug.LogWarning("There's no Speed Set called : " + SpeedSetName);
            }
        }
        #endregion

        #region Extrass



        public virtual void Force_Add(
            Vector3 Direction, float Force, float Aceleration,
            bool ResetGravity, bool ForceAirControl = true, float LimitForce = 0)
        {
            var CurrentForce = CurrentExternalForce + GravityStoredVelocity;

            if (LimitForce > 0 && CurrentForce.magnitude > LimitForce)
                CurrentForce = CurrentForce.normalized * LimitForce;

            CurrentExternalForce = CurrentForce;
            ExternalForce = Direction.normalized * Force;
            ExternalForceAcel = Aceleration;

            if (ActiveState.ID == StateEnum.Fall)
            {
                var fall = ActiveState as Fall;
                fall.FallCurrentDistance = 0;
            }

            if (ResetGravity) GravityTime = 0;

            ExternalForceAirControl = ForceAirControl;

        }

        public virtual void Force_Remove(float Aceleration = 0)
        {
            ExternalForceAcel = Aceleration;
            ExternalForce = Vector3.zero;
        }

        internal void Force_Reset()
        {
            CurrentExternalForce = Vector3.zero;
            ExternalForce = Vector3.zero;
            ExternalForceAcel = 0;
        }

        public virtual void DisableSelf(float time) => this.Delay_Action(time, () => enabled = false);



        public bool CheckIfGrounded()
        {
            AlignRayCasting();

            if (MainRay && FrontRay && !DeepSlope)
            {
                return Grounded = true;
            }

            return false;
        }

        public void Always_Forward(bool value) => AlwaysForward = value;

        public virtual void ActivateDamager(int ID)
        {
            if (Sleep) return;

            if (ID == -1)
            {
                foreach (var dam in Attack_Triggers) dam.DoDamage(true);
            }
            else if (ID == 0)
            {
                foreach (var dam in Attack_Triggers) dam.DoDamage(false);
            }
            else
            {
                var Att_T = Attack_Triggers.FindAll(x => x.Index == ID);

                if (Att_T != null)
                    foreach (var dam in Att_T) dam.DoDamage(true);
            }
        }

        internal void GetAnimalColliders()
        {
            var colls = GetComponentsInChildren<Collider>(true).ToList();

            colliders = new List<Collider>();

            foreach (var item in colls)
            {
                if (!item.isTrigger) colliders.Add(item);
            }
        }

        public virtual void EnableColliders(bool active)
        {
            foreach (var item in colliders)
            {
                if (item) item.enabled = active;
            }
        }


        public virtual void DisableAnimal()
        {
            enabled = false;
            MalbersInput MI = GetComponent<MalbersInput>();
            if (MI) MI.enabled = false;
        }

        public void SetTimeline(bool isonTimeline)
        {
            Sleep = isonTimeline;

            if (Rotator != null) RootBone.parent = isonTimeline ? null : Rotator;
        }


        public void ResetInertiaSpeed() => InertiaPositionSpeed = TargetSpeed;

        public void UseCameraBasedInput() => UseCameraInput = true;



        private void OnDestroy()
        {
            OnDisable();
        }
        #endregion
    }
}