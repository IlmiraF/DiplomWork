using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using RidingSystem.Utilities;

namespace RidingSystem.Controller
{
    public partial class MAnimal
    {
        void Awake()
        {
            if (Anim == null) Anim = GetComponentInParent<Animator>();
            if (RB == null) RB = GetComponentInParent<Rigidbody>();

            DefaultCameraInput = UseCameraInput;

            if (NoParent) transform.parent = null;


            if (Rotator != null)
            {
                if (RootBone == null)
                {
                    if (Anim.avatar.isHuman)
                        RootBone = Anim.GetBoneTransform(HumanBodyBones.Hips).parent;
                    else
                        RootBone = Rotator.GetChild(0);

                    if (RootBone == null)
                        Debug.LogWarning("Make sure the Root Bone is Set on the Advanced Tab -> Misc -> RootBone. This is the Character's Avatar root bone");
                }

                if (RootBone != null && !RootBone.IsGrandchild(Rotator))
                {
                    if (Rotator.position != RootBone.position)
                    {
                        var offset = new GameObject("Offset");
                        offset.transform.rotation = transform.rotation;
                        offset.transform.position = transform.position;

                        offset.transform.SetParent(Rotator);
                        RootBone.SetParent(offset.transform);

                        offset.transform.localScale = Vector3.one;
                        RootBone.localScale = Vector3.one;
                    }
                    else
                    {
                        RootBone.parent = Rotator;
                    }
                }
            }

            GetHashIDs();

            foreach (var set in speedSets) set.CurrentIndex = set.StartVerticalIndex;

            RB.useGravity = false;
            RB.constraints = RigidbodyConstraints.FreezeRotation;
            RB.drag = 0;

            if (defaultStance == null)
            {
                defaultStance = ScriptableObject.CreateInstance<StanceID>();
                defaultStance.name = "Default";
                defaultStance.ID = 0;
            }

            if (currentStance == null) currentStance = defaultStance;

            GetAnimalColliders();

            for (int i = 0; i < states.Count; i++)
            {
                if (states[i] != null)
                {
                    if (CloneStates)
                    {
                        var instance = ScriptableObject.Instantiate(states[i]);
                        instance.name = instance.name.Replace("(Clone)", "(C)");
                        states[i] = instance;
                    }

                    states[i].AwakeState(this);
                }
            }

            for (int i = 0; i < modes.Count; i++)
            {
                modes[i].Priority = modes.Count - i;
                modes[i].AwakeMode(this);
            }

            SetPivots();
            CalculateHeight();

            currentSpeedSet = defaultSpeedSet;
            AlignUniqueID = UnityEngine.Random.Range(0, 99999);
        }

        public virtual void ResetController()
        {
            if (MainCamera == null)  
            {
                m_MainCamera.UseConstant = true;
                m_MainCamera.Value = MTools.FindMainCamera().transform;
            }

            if (Anim)
            {
                Anim.Rebind();
                Anim.speed = AnimatorSpeed;
                Anim.updateMode = AnimatorUpdateMode.AnimatePhysics;


                var AllModeBehaviours = Anim.GetBehaviours<ModeBehaviour>();

                if (AllModeBehaviours != null)
                {
                    foreach (var ModeB in AllModeBehaviours) ModeB.InitializeBehaviour(this);
                }
                else
                {
                    if (modes != null && modes.Count > 0)
                    {
                        Debug.LogWarning("Please check your Animator Controller. There's no Mode Behaviors Attached to it. Re-import the Animator again");
                    }
                }
            }

            foreach (var state in states)
            {
                state.InitializeState();
                state.InputValue = false;
                state.ResetState();
            }

            if (RB) RB.isKinematic = false;
            EnableColliders(true);



            CheckIfGrounded();
            CalculateHeight();


            activeState =
                OverrideStartState == null ?
                states[states.Count - 1] :
                State_Get(OverrideStartState);


            ActiveStateID = activeState.ID;
            activeState.Activate();
            lastState = activeState;


            activeState.IsPending = false;
            ActiveState.CanExit = true;
            activeState.General.Modify(this);

            JustActivateState = false;

            State_SetFloat(0);

            UsingMoveWithDirection = (UseCameraInput);

            Mode_Stop();

            if (StartWithMode.Value != 0)
            {
                if (StartWithMode.Value / 1000 == 0)
                {
                    Mode_Activate(StartWithMode.Value);
                }
                else
                {
                    var mode = StartWithMode.Value / 1000;
                    var modeAb = StartWithMode.Value % 1000;
                    if (modeAb == 0) modeAb = -99;
                    Mode_Activate(mode, modeAb);
                }
            }


            LastPos = transform.position;

            ForwardMultiplier = 1f;
            GravityMultiplier = 1f;

            MovementAxis =
            MovementAxisRaw =
            AdditivePosition =
            InertiaPositionSpeed =
            MovementAxisSmoothed = Vector3.zero;

            LockMovementAxis = (new Vector3(LockHorizontalMovement ? 0 : 1, LockUpDownMovement ? 0 : 1, LockForwardMovement ? 0 : 1));

            UseRawInput = true;
            UseAdditiveRot = true;
            UseAdditivePos = true;
            Grounded = true;
            Randomizer = true;
            AlwaysForward = AlwaysForward;
            Strafe = Strafe;
            Stance = currentStance;
            GlobalOrientToGround = GlobalOrientToGround;

            SpeedMultiplier = 1;
            CurrentCycle = Random.Range(0, 99999);
            ResetGravityValues();

            UpdateDamagerSet();

            var TypeHash = TryOptionalParameter(m_Type);
            SetOptionalAnimParameter(TypeHash, animalType);
        }

        [ContextMenu("Set Pivots")]
        public void SetPivots()
        {
            Pivot_Hip = pivots.Find(item => item.name.ToUpper() == "HIP");
            Pivot_Chest = pivots.Find(item => item.name.ToUpper() == "CHEST");

            Has_Pivot_Hip = Pivot_Hip != null;
            Has_Pivot_Chest = Pivot_Chest != null;
            Starting_PivotChest = Has_Pivot_Chest;

            CalculateHeight();

#if UNITY_EDITOR
            if (!Application.isPlaying) UnityEditor.EditorUtility.SetDirty(this);
#endif
        }


        public void OnEnable()
        {
            if (Animals == null) Animals = new List<MAnimal>();
            Animals.Add(this);

            ResetInputSource();

            if (isPlayer) SetMainPlayer();

            SetBoolParameter += SetAnimParameter;
            SetIntParameter += SetAnimParameter;
            SetFloatParameter += SetAnimParameter;
            SetTriggerParameter += SetAnimParameter;

            if (!alwaysForward.UseConstant && alwaysForward.Variable != null)
                alwaysForward.Variable.OnValueChanged += Always_Forward;

            ResetController();
            Sleep = false;
        }

        public void OnDisable()
        {
            if (Animals != null) Animals.Remove(this);

            UpdateInputSource(false);

            DisableMainPlayer();

            MTools.ResetFloatParameters(Anim);
            RB.velocity = Vector3.zero;

            SetBoolParameter -= SetAnimParameter;
            SetIntParameter -= SetAnimParameter;
            SetFloatParameter -= SetAnimParameter;
            SetTriggerParameter -= SetAnimParameter;


            if (!alwaysForward.UseConstant && alwaysForward.Variable != null)
                alwaysForward.Variable.OnValueChanged -= Always_Forward;

            if (states != null)
            {
                foreach (var st in states)
                    if (st != null) st.ExitState();
            }


            if (ActiveMode != null) ActiveMode.PlayingMode = false;
            Mode_Stop();
        }


        public void CalculateHeight()
        {
            if (Has_Pivot_Hip)
            {
                if (height == 1) height = Pivot_Hip.position.y;
                Center = Pivot_Hip.position;
            }
            else if (Has_Pivot_Chest)
            {
                if (height == 1) height = Pivot_Chest.position.y;
                Center = Pivot_Chest.position;
            }

            if (Has_Pivot_Chest && Has_Pivot_Hip)
            {
                Center = (Pivot_Chest.position + Pivot_Hip.position) / 2;
            }
        }

        public void UpdateDamagerSet()
        {
            Attack_Triggers = GetComponentsInChildren<IMDamager>(true).ToList();
            foreach (var at in Attack_Triggers)
            {
                at.Owner = (gameObject);
                at.Active = false;
            }
        }

        public void AttackTriggers_Update() => UpdateDamagerSet();

        #region Animator Stuff
        protected virtual void GetHashIDs()
        {
            if (Anim == null) return;


            animatorHashParams = new List<int>();

            foreach (var parameter in Anim.parameters)
            {
                animatorHashParams.Add(parameter.nameHash);
            }

            #region Main Animator Parameters
            hash_Vertical = Animator.StringToHash(m_Vertical);
            hash_Horizontal = Animator.StringToHash(m_Horizontal);
            hash_SpeedMultiplier = Animator.StringToHash(m_SpeedMultiplier);

            hash_Movement = Animator.StringToHash(m_Movement);
            hash_Grounded = Animator.StringToHash(m_Grounded);

            hash_State = Animator.StringToHash(m_State);
            hash_StateEnterStatus = Animator.StringToHash(m_StateStatus);


            hash_LastState = Animator.StringToHash(m_LastState);
            hash_StateFloat = Animator.StringToHash(m_StateFloat);

            hash_Mode = Animator.StringToHash(m_Mode);

            hash_ModeStatus = Animator.StringToHash(m_ModeStatus);
            #endregion

            #region Optional Parameters

            hash_StateExitStatus = TryOptionalParameter(m_StateExitStatus);
            hash_SpeedMultiplier = TryOptionalParameter(m_SpeedMultiplier);

            hash_UpDown = TryOptionalParameter(m_UpDown);
            hash_DeltaUpDown = TryOptionalParameter(m_DeltaUpDown);

            hash_Slope = TryOptionalParameter(m_Slope);


            hash_DeltaAngle = TryOptionalParameter(m_DeltaAngle);
            hash_Sprint = TryOptionalParameter(m_Sprint);

            hash_StateTime = TryOptionalParameter(m_StateTime);


            hash_Strafe = TryOptionalParameter(m_Strafe);
            hash_StrafeAngle = TryOptionalParameter(m_strafeAngle);

            hash_Stance = TryOptionalParameter(m_Stance);

            hash_LastStance = TryOptionalParameter(m_LastStance);

            hash_Random = TryOptionalParameter(m_Random);
            hash_ModePower = TryOptionalParameter(m_ModePower);

            hash_ModeOn = TryOptionalParameter(m_ModeOn);
            hash_StateOn = TryOptionalParameter(m_StateOn);
            #endregion
        }


        private int TryOptionalParameter(string param)
        {
            var AnimHash = Animator.StringToHash(param);

            if (!animatorHashParams.Contains(AnimHash))
                return 0;
            return AnimHash;
        }

        protected virtual void CacheAnimatorState()
        {
            m_PreviousCurrentState = m_CurrentState;
            m_PreviousNextState = m_NextState;
            m_PreviousIsAnimatorTransitioning = m_IsAnimatorTransitioning;

            m_CurrentState = Anim.GetCurrentAnimatorStateInfo(0);
            m_NextState = Anim.GetNextAnimatorStateInfo(0);
            m_IsAnimatorTransitioning = Anim.IsInTransition(0);

            if (m_IsAnimatorTransitioning)
            {
                if (m_NextState.fullPathHash != 0)
                {
                    AnimStateTag = m_NextState.tagHash;
                    AnimState = m_NextState;
                }
            }
            else
            {
                if (m_CurrentState.fullPathHash != AnimState.fullPathHash)
                {
                    AnimStateTag = m_CurrentState.tagHash;
                }

                AnimState = m_CurrentState;
            }

            var lastStateTime = StateTime;
            StateTime = Mathf.Repeat(AnimState.normalizedTime, 1);


            if (lastStateTime > StateTime) StateCycle?.Invoke(ActiveStateID);
        }

        protected virtual void UpdateAnimatorParameters()
        {
            SetFloatParameter(hash_Vertical, VerticalSmooth);
            SetFloatParameter(hash_Horizontal, HorizontalSmooth);

            SetOptionalAnimParameter(hash_UpDown, UpDownSmooth);
            SetOptionalAnimParameter(hash_DeltaUpDown, DeltaUpDown);


            SetOptionalAnimParameter(hash_DeltaAngle, DeltaAngle);
            SetOptionalAnimParameter(hash_Slope, SlopeNormalized);
            SetOptionalAnimParameter(hash_SpeedMultiplier, SpeedMultiplier);
            SetOptionalAnimParameter(hash_StateTime, StateTime);
        }
        #endregion

        #region Inputs 
        internal void InputAxisUpdate()
        {
            if (UseRawInput)
            {
                if (AlwaysForward)
                    RawInputAxis.z = 1;

                var inputAxis = RawInputAxis;

                if (LockMovement || Sleep)
                {
                    MovementAxis = Vector3.zero;
                    return;
                }

                if (MainCamera && UsingMoveWithDirection && !Strafe)
                {
                    var Cam_Forward = Vector3.ProjectOnPlane(MainCamera.forward, UpVector).normalized;
                    var Cam_Right = Vector3.ProjectOnPlane(MainCamera.right, UpVector).normalized;

                    Vector3 UpInput;

                    if (!FreeMovement)
                    {
                        UpInput = Vector3.zero;
                    }
                    else
                    {
                        if (UseCameraUp)
                        {
                            UpInput = (inputAxis.y * MainCamera.up);
                            UpInput += Vector3.Project(MainCamera.forward, UpVector) * inputAxis.z;
                        }
                        else
                        {
                            UpInput = (inputAxis.y * UpVector);

                            if (inputAxis.y != 0 && inputAxis.z == 0)
                                inputAxis.z = 0.01f;
                        }
                    }

                    var m_Move = (inputAxis.z * Cam_Forward) + (inputAxis.x * Cam_Right) + UpInput;

                    MoveFromDirection(m_Move);
                }
                else
                {
                    MoveWorld(inputAxis);
                }
            }
            else
            {
                MoveFromDirection(RawInputAxis);
            }
        }


        public virtual void SetInputAxis(Vector3 inputAxis)
        {
            UseRawInput = true;
            RawInputAxis = inputAxis;
            if (UsingUpDownExternal)
                RawInputAxis.y = UpDownAdditive;
        }

        public virtual void SetInputAxis(Vector2 inputAxis) => SetInputAxis(new Vector3(inputAxis.x, 0, inputAxis.y));

        public virtual void SetInputAxisXY(Vector2 inputAxis) => SetInputAxis(new Vector3(inputAxis.x, inputAxis.y, 0));

        public virtual void SetInputAxisYZ(Vector2 inputAxis) => SetInputAxis(new Vector3(0, inputAxis.x, inputAxis.y));

        private float UpDownAdditive;
        private bool UsingUpDownExternal;

        public virtual void SetUpDownAxis(float upDown)
        {
            UpDownAdditive = upDown;
            UsingUpDownExternal = true;
            SetInputAxis(RawInputAxis);
        }

        protected virtual void MoveWorld(Vector3 move)
        {
            UsingMoveWithDirection = false;

            if (!UseSmoothVertical && move.z > 0) move.z = 1;
            Move_Direction = transform.TransformDirection(move).normalized;

            SetMovementAxis(move);
        }

        private void SetMovementAxis(Vector3 move)
        {
            MovementAxisRaw = move;
            MovementAxisRaw.z *= ForwardMultiplier;

            MovementAxis = MovementAxisRaw;
            MovementDetected = MovementAxisRaw != Vector3.zero;

            MovementAxis.Scale(LockMovementAxis);
            MovementAxis.Scale(ActiveState.MovementAxisMult);
        }

        public virtual void MoveFromDirection(Vector3 move)
        {
            if (LockMovement)
            {
                MovementAxis = Vector3.zero;
                return;
            }

            UsingMoveWithDirection = true;

            if (move.magnitude > 1f) move.Normalize();

            var UpDown = FreeMovement ? move.y : 0;


            if (!FreeMovement)
                move = Quaternion.FromToRotation(UpVector, SurfaceNormal) * move;

            Move_Direction = move;
            move = transform.InverseTransformDirection(move);

            float turnAmount = Mathf.Atan2(move.x, move.z);
            float forwardAmount = move.z < 0 ? 0 : move.z;

            float angleCurrent = Mathf.Atan2(Forward.x, Forward.z) * Mathf.Rad2Deg;

            float targetAngle = Mathf.Atan2(Move_Direction.x, Move_Direction.z) * Mathf.Rad2Deg;
            var Delta = Mathf.DeltaAngle(angleCurrent, targetAngle);

            DeltaAngle = MovementDetected ? Delta : 0;

            if (Mathf.Approximately(Delta, float.NaN))
            {
                DeltaAngle = 0f;
            }

            if (!UseSmoothVertical)
            {
                forwardAmount = Mathf.Abs(move.z);
                forwardAmount = forwardAmount > 0 ? 1 : forwardAmount;
            }
            else
            {
                if (Mathf.Abs(DeltaAngle) < TurnLimit)
                    forwardAmount = Mathf.Clamp01(Move_Direction.magnitude);
            }

            SetMovementAxis(new Vector3(turnAmount, UpDown, forwardAmount));
        }

        public virtual void RotateAtDirection(Vector3 direction)
        {
            if (IsPlayingMode && !ActiveMode.AllowRotation) return;

            RawInputAxis = direction;
            UseRawInput = false;
            Rotate_at_Direction = true;
        }
        #endregion

        #region Additional Speeds (Movement, Turn) 

        public void CalculateTargetSpeed()
        {
            if ((!UseAdditivePos) ||
                 (IsPlayingMode && !ActiveMode.AllowMovement))
            {
                TargetSpeed = Vector3.zero;
                return;
            }

            Vector3 TargetDir = ActiveState.Speed_Direction();

            float Speed_Modifier = Strafe ? CurrentSpeedModifier.strafeSpeed.Value : CurrentSpeedModifier.position.Value;

            if (Strafe)
            {
                TargetDir = (Forward * VerticalSmooth) + (Right * HorizontalSmooth);

                if (FreeMovement)
                    TargetDir += (Up * UpDownSmooth);

            }
            else
            {
                if ((VerticalSmooth < 0) && CurrentSpeedSet != null)
                {
                    TargetDir *= -CurrentSpeedSet.BackSpeedMult.Value;
                    Speed_Modifier = CurrentSpeedSet[0].position;
                }
                if (FreeMovement)
                {
                    float SmoothZYInput = Mathf.Clamp01(Mathf.Max(Mathf.Abs(UpDownSmooth), Mathf.Abs(VerticalSmooth)));
                    TargetDir *= SmoothZYInput;
                }
                else
                {
                    TargetDir *= VerticalSmooth;
                }

            }

            if (TargetDir.magnitude > 1) TargetDir.Normalize();
            TargetSpeed = TargetDir * Speed_Modifier * DeltaTime * ScaleFactor; 

            HorizontalVelocity = Vector3.ProjectOnPlane(Inertia, UpVector);
            HorizontalSpeed = HorizontalVelocity.magnitude;
        }

        private void MoveRotator()
        {
            if (!FreeMovement && Rotator)
            {
                if (PitchAngle != 0 || Bank != 0)
                {
                    float limit = 0.005f;
                    var lerp = DeltaTime * (CurrentSpeedSet.PitchLerpOff);

                    Rotator.localRotation = Quaternion.Slerp(Rotator.localRotation, Quaternion.identity, lerp);

                    PitchAngle = Mathf.Lerp(PitchAngle, 0, lerp);
                    Bank = Mathf.Lerp(Bank, 0, lerp);

                    if (Mathf.Abs(PitchAngle) < limit && Mathf.Abs(Bank) < limit)
                    {
                        Bank = PitchAngle = 0;
                        Rotator.localRotation = Quaternion.identity;
                    }
                }
            }
            else
            {
                CalculatePitchDirectionVector();
            }
        }

        public virtual void FreeMovementRotator(float Ylimit, float bank)
        {
            CalculatePitch(Ylimit);
            CalculateBank(bank);
            CalculateRotator();
        }

        internal virtual void CalculateRotator()
        {
            Rotator.localEulerAngles = new Vector3(PitchAngle, 0, Bank);
        }
        internal virtual void CalculateBank(float bank) => Bank = Mathf.Lerp(Bank, -bank * Mathf.Clamp(HorizontalSmooth, -1, 1), DeltaTime * CurrentSpeedSet.BankLerp);
        internal virtual void CalculatePitch(float Ylimit)
        {
            float NewAngle = 0;

            if (PitchDirection.sqrMagnitude > 0.0001)
            {
                NewAngle = 90 - Vector3.Angle(UpVector, PitchDirection);
                NewAngle = Mathf.Clamp(-NewAngle, -Ylimit, Ylimit);
            }

            var deltatime = DeltaTime * CurrentSpeedSet.PitchLerpOn;


            PitchAngle = Mathf.Lerp(PitchAngle, NewAngle, deltatime);

            DeltaUpDown = Mathf.Lerp(DeltaUpDown, -Mathf.DeltaAngle(PitchAngle, NewAngle), deltatime * 2);

            if (Mathf.Abs(DeltaUpDown) < 0.01f) DeltaUpDown = 0;
        }


        internal virtual void CalculatePitchDirectionVector()
        {
            var dir = Move_Direction != Vector3.zero ? Move_Direction : Forward;
            PitchDirection = Vector3.Lerp(PitchDirection, dir, DeltaTime * CurrentSpeedSet.PitchLerpOn * 2);
        }

        protected virtual void AdditionalSpeed(float time)
        {
            var LerpPos = CurrentSpeedModifier.lerpPosition;

            InertiaPositionSpeed = (LerpPos > 0) ? Vector3.Lerp(InertiaPositionSpeed, TargetSpeed, time * LerpPos) : TargetSpeed;

            AdditivePosition += InertiaPositionSpeed;
        }

        protected virtual void AdditionalTurn(float time)
        {
            float SpeedRotation = CurrentSpeedModifier.rotation;

            if (VerticalSmooth < 0.01 && !CustomSpeed && CurrentSpeedSet != null)
            {
                SpeedRotation = CurrentSpeedSet[0].rotation;
            }

            if (SpeedRotation < 0) return;

            if (MovementDetected)
            {
                float ModeRotation = (IsPlayingMode && !ActiveMode.AllowRotation) ? 0 : 1;

                if (UsingMoveWithDirection)
                {
                    var TargetLocalRot = Quaternion.Euler(0, DeltaAngle, 0);
                    Quaternion targetRotation = Quaternion.Slerp(Quaternion.identity, TargetLocalRot, (SpeedRotation + 1) / 4 * ((TurnMultiplier + 1) * time * ModeRotation));
                    AdditiveRotation *= targetRotation;
                }
                else
                {
                    float Turn = SpeedRotation * 10;
                    float TurnInput = Mathf.Clamp(HorizontalSmooth, -1, 1) * (MovementAxis.z >= 0 ? 1 : -1);
                    AdditiveRotation *= Quaternion.Euler(0, Turn * TurnInput * time * ModeRotation, 0);
                    var TargetGlobal = Quaternion.Euler(0, TurnInput * (TurnMultiplier + 1), 0);
                    var AdditiveGlobal = Quaternion.Slerp(Quaternion.identity, TargetGlobal, time * (SpeedRotation + 1) * ModeRotation);
                    AdditiveRotation *= AdditiveGlobal;
                }
            }
        }


        internal void MovementSystem(float DeltaTime)
        {
            float maxspeedV = CurrentSpeedModifier.Vertical;
            float maxspeedH = 1;

            var LerpUpDown = DeltaTime * CurrentSpeedSet.PitchLerpOn;
            var LerpVertical = DeltaTime * CurrentSpeedModifier.lerpPosAnim;
            var LerpTurn = DeltaTime * CurrentSpeedModifier.lerpRotAnim;
            var LerpAnimator = DeltaTime * CurrentSpeedModifier.lerpAnimator;

            if (Strafe)
            {
                maxspeedH = maxspeedV;
                LerpVertical = LerpTurn = LerpUpDown = DeltaTime * CurrentSpeedModifier.lerpStrafe;
            }

            if (IsPlayingMode && !ActiveMode.AllowMovement)
                MovementAxis = Vector3.zero;

            var Horiz = Mathf.Lerp(HorizontalSmooth, MovementAxis.x * maxspeedH, LerpTurn);

            float v = MovementAxis.z;


            if (Rotate_at_Direction)
            {
                float r = 0;
                v = 0;
                Horiz = Mathf.SmoothDamp(HorizontalSmooth, MovementAxis.x * 4, ref r, inPlaceDamp * DeltaTime);
            }

            VerticalSmooth = LerpVertical > 0 ?
                Mathf.Lerp(VerticalSmooth, v * maxspeedV, LerpVertical) :
                MovementAxis.z * maxspeedV;


            HorizontalSmooth = LerpTurn > 0 ? Horiz : MovementAxis.x * maxspeedH;

            UpDownSmooth = LerpVertical > 0 ?
                Mathf.Lerp(UpDownSmooth, MovementAxis.y, LerpUpDown) :
                MovementAxis.y;


            SpeedMultiplier = (LerpAnimator > 0) ?
                Mathf.Lerp(SpeedMultiplier, CurrentSpeedModifier.animator.Value, LerpAnimator) :
                CurrentSpeedModifier.animator.Value;

            var zero = 0.005f;

            if (Mathf.Abs(VerticalSmooth) < zero) VerticalSmooth = 0;
            if (Mathf.Abs(HorizontalSmooth) < zero) HorizontalSmooth = 0;
            if (Mathf.Abs(UpDownSmooth) < zero) UpDownSmooth = 0;
        }


        #endregion

        #region Platorm movement
        public void SetPlatform(Transform newPlatform)
        {
            platform = newPlatform;
            platform_LastPos = platform.position;
            platform_Rot = platform.rotation;
        }

        public void PlatformMovement()
        {
            if (platform == null) return;
            if (platform.gameObject.isStatic) return;

            var DeltaPlatformPos = platform.position - platform_LastPos;

            transform.position += DeltaPlatformPos;


            Quaternion Inverse_Rot = Quaternion.Inverse(platform_Rot);
            Quaternion Delta = Inverse_Rot * platform.rotation;

            if (Delta != Quaternion.identity)
            {
                var pos = transform.DeltaPositionFromRotate(platform, Delta);

                transform.position += pos;
            }

            transform.rotation *= Delta;

            platform_LastPos = platform.position;
            platform_Rot = platform.rotation;
        }
        #endregion


        #region Terrain Alignment
        internal virtual void AlignRayCasting()
        {
            MainRay = FrontRay = false;
            hit_Chest = new RaycastHit() { normal = Vector3.zero };
            hit_Hip = new RaycastHit();
            hit_Chest.distance = hit_Hip.distance = Height;

            var Direction = -transform.up;

            if (Physics.Raycast(Main_Pivot_Point, Direction, out hit_Chest, Pivot_Multiplier, GroundLayer, QueryTriggerInteraction.Ignore))
            {
                FrontRay = true;

                if (debugGizmos)
                {
                    Debug.DrawRay(hit_Chest.point, hit_Chest.normal * ScaleFactor * 0.2f, Color.green);
                    MTools.DrawWireSphere(Main_Pivot_Point + Direction * (hit_Chest.distance - RayCastRadius), Color.green, RayCastRadius * ScaleFactor);
                }

                MainPivotSlope = Vector3.SignedAngle(hit_Chest.normal, UpVector, Right);


                if (MainPivotSlope > maxAngleSlope)
                {
                    if (MovementAxisRaw.z > 0 && !hit_Chest.transform.gameObject.CompareTag(DebrisTag))
                    {
                        AdditivePosition = Vector3.ProjectOnPlane(AdditivePosition, Forward);
                        MovementAxis.z = 0;
                    }
                }
                else if (MainPivotSlope < -maxAngleSlope)
                {
                    FrontRay = false;
                }
                else
                {
                    if (platform != hit_Chest.transform)
                        SetPlatform(hit_Chest.transform);

                    hit_Chest.collider.attachedRigidbody?.AddForceAtPosition(Gravity * (RB.mass / 2), hit_Chest.point, ForceMode.Force);
                }
            }
            else
            {
                platform = null;
            }

            if (Has_Pivot_Hip && Has_Pivot_Chest)
            {
                var hipPoint = Pivot_Hip.World(transform) + DeltaVelocity;

                if (Physics.Raycast(hipPoint, Direction, out hit_Hip, ScaleFactor * Pivot_Hip.multiplier, GroundLayer, QueryTriggerInteraction.Ignore))
                {

                    var MainPivotSlope = Vector3.SignedAngle(hit_Hip.normal, UpVector, Right);

                    if (MainPivotSlope < -maxAngleSlope)
                    {
                        MainRay = false;
                    }
                    else
                    {
                        MainRay = true;

                        if (debugGizmos)
                        {
                            Debug.DrawRay(hit_Hip.point, hit_Hip.normal * ScaleFactor * 0.2f, Color.green);
                            MTools.DrawWireSphere(hipPoint + Direction * (hit_Hip.distance - RayCastRadius), Color.green, RayCastRadius * ScaleFactor);
                        }

                        if (platform != hit_Hip.transform) SetPlatform(hit_Hip.transform);

                        hit_Hip.collider.attachedRigidbody?.AddForceAtPosition(Gravity * (RB.mass / 2), hit_Hip.point, ForceMode.Force);

                        if (!FrontRay) hit_Chest = hit_Hip;
                    }
                }
                else
                {
                    platform = null;

                    if (FrontRay)
                    {
                        MovementAxis.z = 1;
                        hit_Hip = hit_Chest;
                    }
                }
            }
            else
            {
                MainRay = FrontRay;
                hit_Hip = hit_Chest;
            }

            if (ground_Changes_Gravity)
                Gravity = -hit_Hip.normal;

            CalculateSurfaceNormal();
        }

        internal virtual void CalculateSurfaceNormal()
        {
            if (Has_Pivot_Hip)
            {
                Vector3 TerrainNormal;

                if (Has_Pivot_Chest)
                {
                    Vector3 direction = (hit_Chest.point - hit_Hip.point).normalized;
                    Vector3 Side = Vector3.Cross(UpVector, direction).normalized;
                    SurfaceNormal = Vector3.Cross(direction, Side).normalized;

                    TerrainNormal = SurfaceNormal;
                }
                else
                {
                    SurfaceNormal = TerrainNormal = hit_Hip.normal;
                }

                TerrainSlope = Vector3.SignedAngle(TerrainNormal, UpVector, Right);
            }
            else
            {
                TerrainSlope = Vector3.SignedAngle(hit_Hip.normal, UpVector, Right);
                SurfaceNormal = UpVector;
            }
        }

        internal virtual void AlignRotation(bool align, float time, float smoothness)
        {
            AlignRotation(align ? SurfaceNormal : UpVector, time, smoothness);
        }

        internal virtual void AlignRotation(Vector3 alignNormal, float time, float Smoothness)
        {
            AlignRotLerpDelta = Mathf.Lerp(AlignRotLerpDelta, Smoothness, time * AlignRotDelta * 4);

            Quaternion AlignRot = Quaternion.FromToRotation(transform.up, alignNormal) * transform.rotation; 
            Quaternion Inverse_Rot = Quaternion.Inverse(transform.rotation);
            Quaternion Target = Inverse_Rot * AlignRot;
            Quaternion Delta = Quaternion.Lerp(Quaternion.identity, Target, time * AlignRotLerpDelta);

            transform.rotation *= Delta;
        }

        internal void AlignPosition(float time)
        {
            if (!MainRay && !FrontRay) return;
            AlignPosition(hit_Hip.distance, time, AlignPosLerp * 2);
        }

        internal void AlignPosition(float distance, float time, float Smoothness)
        {
            float difference = Height - distance;

            if (!Mathf.Approximately(distance, Height))
            {
                AlignPosLerpDelta = Mathf.Lerp(AlignPosLerpDelta, Smoothness, time * AlignPosDelta);

                var deltaHeight = difference * time * AlignPosLerpDelta;

                Vector3 align = transform.rotation * new Vector3(0, deltaHeight, 0);
                AdditivePosition += align;

                hit_Hip.distance += deltaHeight;
            }
        }

        internal virtual void AlignPosition_Distance(float distance)
        {
            float difference = Height - distance;
            AdditivePosition += transform.rotation * new Vector3(0, difference, 0);
        }

        internal virtual void AlignPosition()
        {
            float difference = Height - hit_Hip.distance;
            AdditivePosition += transform.rotation * new Vector3(0, difference, 0);
        }
        #endregion

        protected virtual void TryActivateState()
        {
            if (ActiveState.IsPersistent) return;
            if (JustActivateState) return;

            foreach (var trySt in states)
            {
                if (trySt.IsActiveState) continue;
                if (ActiveState.IgnoreLowerStates && ActiveState.Priority > trySt.Priority) return;

                if ((trySt.UniqueID + CurrentCycle) % trySt.TryLoop != 0) continue;

                if (!ActiveState.IsPending && ActiveState.CanExit)
                {
                    if (trySt.Active &&
                        !trySt.OnEnterCoolDown &&
                        !trySt.IsSleep &&
                        !trySt.OnQueue &&
                         trySt.TryActivate())
                    {
                        trySt.Activate();
                        break;
                    }
                }
            }
        }

        protected virtual void TryExitActiveState()
        {
            if (ActiveState.CanExit)
                ActiveState.TryExitState(DeltaTime);
        }

        protected virtual void OnAnimatorMove() => OnAnimalMove();


        protected virtual void OnAnimalMove()
        {
            if (Sleep)
            {
                Anim.ApplyBuiltinRootMotion();
                return;
            }

            CacheAnimatorState();
            ResetValues();

            if (ActiveState == null) return;

            DeltaTime = Time.fixedDeltaTime;

            InputAxisUpdate();

            ActiveState.SetCanExit();

            PreStateMovement(this);

            ActiveState.OnStatePreMove(DeltaTime);

            CalculateTargetSpeed();

            MoveRotator();

            if (IsPlayingMode) ActiveMode.OnAnimatorMove(DeltaTime);

            if (UseAdditivePos) AdditionalSpeed(DeltaTime);
            if (UseAdditiveRot) AdditionalTurn(DeltaTime);

            ApplyExternalForce();

            if (Grounded)
            {
                PlatformMovement();

                if (AlignLoop.Value <= 1 || (AlignUniqueID + CurrentCycle) % AlignLoop.Value == 0)
                    AlignRayCasting();


                AlignPosition(DeltaTime);

                if (!UseCustomAlign)
                    AlignRotation(UseOrientToGround, DeltaTime, AlignRotLerp);
            }
            else
            {
                MainRay = FrontRay = false;
                SurfaceNormal = UpVector;
                AlignPosLerpDelta = 0;
                AlignRotLerpDelta = 0;

                if (!UseCustomAlign)
                    AlignRotation(false, DeltaTime, AlignRotLerp);
                TerrainSlope = 0;
            }

            ActiveState.OnStateMove(DeltaTime);

            PostStateMovement(this);

            TryExitActiveState();
            TryActivateState();

            MovementSystem(DeltaTime);
            GravityLogic();

            LastPos = transform.position;

            if (float.IsNaN(AdditivePosition.x)) return;

            if (!DisablePositionRotation)
            {
                if (RB)
                {
                    if (RB.isKinematic)
                    {
                        transform.position += AdditivePosition;
                    }
                    else
                    {
                        RB.velocity = Vector3.zero;
                        RB.angularVelocity = Vector3.zero;

                        if (DeltaTime > 0)
                        {
                            DesiredRBVelocity = AdditivePosition / DeltaTime;
                            RB.velocity = DesiredRBVelocity;
                        }
                        transform.rotation *= AdditiveRotation;
                    }
                }
                else
                {
                    transform.position += AdditivePosition;
                    transform.rotation *= AdditiveRotation;
                }

                Strafing_Rotation();
            }
            UpdateAnimatorParameters();
        }

        private void Strafing_Rotation()
        {
            if (Strafe)
            {
                var RawDirection = Forward;

                Vector3 HorizontalDir = Vector3.ProjectOnPlane(RawDirection, UpVector);
                Vector3 ForwardDir = Vector3.ProjectOnPlane(Forward, UpVector);

                HorizontalAimAngle_Raw = -Vector3.SignedAngle(HorizontalDir, ForwardDir, UpVector);


                StrafeDeltaValue = Mathf.Lerp(StrafeDeltaValue,
                    MovementDetected ? ActiveState.MovementStrafe : ActiveState.IdleStrafe,
                    DeltaTime * m_StrafeLerp);

                SetOptionalAnimParameter(hash_StrafeAngle, HorizontalAimAngle_Raw);

                transform.rotation *= Quaternion.Euler(0, HorizontalAimAngle_Raw * StrafeDeltaValue, 0);
            }
            else
            {
                HorizontalAimAngle_Raw = 0;
                StrafeDeltaValue = 0;
                SetOptionalAnimParameter(hash_StrafeAngle, HorizontalAimAngle_Raw);
            }
        }

        private void ApplyExternalForce()
        {
            var Acel = ExternalForceAcel > 0 ? (DeltaTime * ExternalForceAcel) : 1;

            CurrentExternalForce = Vector3.Lerp(CurrentExternalForce, ExternalForce, Acel);

            if (CurrentExternalForce.sqrMagnitude <= 0.01f) CurrentExternalForce = Vector3.zero;


            if (CurrentExternalForce != Vector3.zero)
                AdditivePosition += CurrentExternalForce * DeltaTime;
        }

        private void GravityLogic()
        {
            if (UseGravity)
            {
                if (Grounded) return;

                var GTime = DeltaTime * GravityTime;

                GravityStoredVelocity = Gravity * GravityPower * (GTime * GTime / 2);
                AdditivePosition += GravityStoredVelocity * DeltaTime;
                GravityTime++;

                if (LimitGravityTime > 0 && LimitGravityTime < GravityTime) GravityTime--;
            }
        }


        void ResetValues()
        {
            AdditivePosition = RootMotion ? Anim.deltaPosition : Vector3.zero;
            AdditiveRotation = RootMotion ? Anim.deltaRotation : Quaternion.identity;

            DeltaPos = transform.position - LastPos;
            CurrentCycle = (CurrentCycle + 1) % 999999999;

            var DeltaRB = RB.velocity * DeltaTime;
            DeltaVelocity = Grounded ? Vector3.ProjectOnPlane(DeltaRB, UpVector) : DeltaRB;
        }
    }
}