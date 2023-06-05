using System.Collections.Generic;
using UnityEngine;
using RidingSystem.Scriptables;

namespace RidingSystem.Controller
{
    public class Jump : State
    {
        public override string StateName => "Jump";


        [Header("Jump Parameters")]
        public BoolReference JumpPressed;
        public BoolReference ForwardPressed;

        public float JumpPressedLerp = 5;
        private float JumpPressHeight_Value = 1;
        private float JumpPressForward = 1;
        private float JumpPressForwardAdditive = 0;

        public BoolReference AirControl = new BoolReference(true);
        public FloatReference AirRotation = new FloatReference(10);
        public List<JumpProfile> jumpProfiles = new List<JumpProfile>();
        protected MSpeed JumpSpeed;

        protected bool OneCastingFall_Ray = false;

        protected JumpProfile activeJump;
        private RaycastHit JumpRay;

        private bool CanJumpAgain;
        private Vector3 JumpStartDirection;

        public override bool TryActivate() => InputValue && CanJumpAgain;

        public override void ResetStateValues()
        {
            CanJumpAgain = true;
            JumpPressHeight_Value = 1;
            JumpPressForward = 1;
            JumpPressForwardAdditive = 0;
            OneCastingFall_Ray =
            GoingDown = false;
        }

        public override void AwakeState()
        {
            if (string.IsNullOrEmpty(EnterTag)) EnterTag.Value = "JumpStart";
            if (string.IsNullOrEmpty(ExitTag)) ExitTag.Value = "JumpEnd";

            base.AwakeState();
        }


        public override void Activate()
        {
            base.Activate();

            IgnoreLowerStates = true;

            animal.currentSpeedModifier.animator = 1;
            General.CustomRotation = true;
            CanJumpAgain = false;
        }

        public override void EnterTagAnimation()
        {
            if (CurrentAnimTag == EnterTagHash)
            {
                animal.DeltaPos = Vector3.zero;

                if (!animal.RootMotion)
                {
                    var JumpStartSpeed = new MSpeed(animal.CurrentSpeedModifier)
                    {
                        name = "JumpStartSpeed",
                        position = animal.HorizontalSpeed,
                        Vertical = animal.CurrentSpeedModifier.Vertical.Value,
                        animator = 1,
                        rotation = AirRotation.Value,
                    };

                    animal.SetCustomSpeed(JumpStartSpeed, true);

                }

                JumpStartDirection = animal.Forward;

                if (animal.TerrainSlope > 0)
                    animal.UseCustomAlign = true;
            }
            else if (CurrentAnimTag == ExitTagHash && (animal.hash_StateOn == 0))
            {
                AllowExit();
            }
        }

        public override Vector3 Speed_Direction()
        {
            if (animal.HasExternalForce)
            {
                return Vector3.ProjectOnPlane(animal.ExternalForce, animal.UpVector);
            }
            else if (AirControl)
            {
                return base.Speed_Direction();
            }
            else
            {
                return JumpStartDirection;
            }
        }


        public override void EnterCoreAnimation()
        {
            FindJumpProfile();

            OneCastingFall_Ray = false;
            JumpPressHeight_Value = 1;
            JumpPressForward = 1;
            JumpPressForwardAdditive = 0;
            animal.UseGravity = false;
            animal.ResetGravityValues();

            JumpSpeed = new MSpeed(animal.CurrentSpeedModifier)
            {
                name = $"Jump [{activeJump.name}]",
                position = animal.RootMotion ? 0 : animal.HorizontalSpeed * activeJump.ForwardMultiplier,
                animator = 1,
                rotation = (AirRotation.Value),
                lerpPosAnim = JumpPressedLerp,
                lerpPosition = JumpPressedLerp,
            };


            animal.SetCustomSpeed(JumpSpeed);
            JumpStartDirection = animal.Forward;

            if (animal.TerrainSlope > 0)
                animal.UseCustomAlign = true;
        }

        private void FindJumpProfile()
        {
            activeJump = jumpProfiles != null ? jumpProfiles[0] : new JumpProfile();

            foreach (var jump in jumpProfiles)
            {
                if (jump.LastState == null)
                {
                    if (jump.VerticalSpeed <= animal.VerticalSmooth)
                    {
                        activeJump = jump;
                    }
                }
                else
                {
                    if (jump.VerticalSpeed <= animal.VerticalSmooth && jump.LastState == animal.LastState.ID)
                    {
                        activeJump = jump;
                    }
                }
            }
        }

        public override void OnStateMove(float deltaTime)
        {
            if (InCoreAnimation)
            {
                if (activeJump.JumpLandDistance == 0) return;

                if (JumpPressed)
                {
                    JumpPressHeight_Value = Mathf.MoveTowards(JumpPressHeight_Value, InputValue ? 1 : 0, deltaTime * JumpPressedLerp);
                }

                if (ForwardPressed)
                {
                    JumpPressForward = Mathf.MoveTowards(JumpPressForward, animal.MovementAxis.z, deltaTime * JumpPressedLerp);
                    JumpPressForwardAdditive = Mathf.MoveTowards(JumpPressForwardAdditive, animal.MovementAxis.z, deltaTime * JumpPressedLerp);
                }

                if (!General.RootMotion)
                {
                    Vector3 ExtraJumpHeight = (animal.UpVector * activeJump.HeightMultiplier);
                    animal.AdditivePosition += ExtraJumpHeight * deltaTime * JumpPressHeight_Value;
                }
                else
                {
                    Vector3 RootMotionUP = Vector3.Project(Anim.deltaPosition, animal.UpVector);

                    bool isGoingUp = Vector3.Dot(RootMotionUP, animal.Up) > 0;

                    if (isGoingUp)
                    {
                        animal.AdditivePosition -= RootMotionUP;
                        animal.AdditivePosition +=
                            (RootMotionUP * activeJump.HeightMultiplier * JumpPressHeight_Value);
                    }

                    Vector3 RootMotionForward = Anim.deltaPosition - RootMotionUP;

                    animal.AdditivePosition -= RootMotionForward;

                    if (!AirControl.Value)
                    {
                        animal.AdditivePosition +=
                            JumpStartDirection * RootMotionForward.magnitude * activeJump.ForwardMultiplier;

                    }
                    else
                        animal.AdditivePosition +=
                                 (RootMotionForward * activeJump.ForwardMultiplier * JumpPressForward) +
                                 (animal.Forward * activeJump.ForwardPressed * JumpPressForwardAdditive * deltaTime);
                }
            }
        }

        private bool GoingDown;


        public override void TryExitState(float DeltaTime)
        {
            if (activeJump.ExitTime > activeJump.fallingTime && animal.StateTime >= activeJump.ExitTime)
            {
                animal.CheckIfGrounded();
                AllowExit();
            }
            else if (animal.StateTime >= activeJump.fallingTime && !OneCastingFall_Ray)
                Check_for_Falling();

            CheckForGround(animal.StateTime);
        }


        private void CheckForGround(float normalizedTime)
        {
            if (normalizedTime > 0.33f)
            {
                GoingDown = Vector3.Dot(DeltaPos, Gravity) > 0;
                {
                    var MainPivot = animal.Main_Pivot_Point;

                    var RayLength = activeJump.CliffLandDistance * ScaleFactor;

                    if (debug)
                        Debug.DrawRay(MainPivot, -animal.Up * RayLength, Color.black, 0.1f);

                    if (Physics.Raycast(MainPivot, -animal.Up, out JumpRay, RayLength, GroundLayer, IgnoreTrigger))
                    {
                        if (debug) MTools.DebugTriangle(JumpRay.point, 0.1f, Color.black);

                        var TerrainSlope = Vector3.Angle(JumpRay.normal, animal.UpVector);
                        var DeepSlope = TerrainSlope > animal.maxAngleSlope;

                        if (!DeepSlope)
                        {
                            AllowExit();
                            animal.CheckIfGrounded();
                            GoingDown = false;
                        }
                    }
                }
            }
        }

        private void Check_for_Falling()
        {
            AllowExit();
            OneCastingFall_Ray = true;

            if (activeJump.JumpLandDistance == 0)
            {
                animal.CheckIfGrounded();
                return;
            }

            float RayLength = animal.ScaleFactor * activeJump.JumpLandDistance;
            var MainPivot = animal.Main_Pivot_Point;
            var Direction = -animal.Up;


            if (activeJump.JumpLandDistance > 0)
            {
                if (debug)
                    Debug.DrawRay(MainPivot, Direction * RayLength, Color.red, 0.25f);

                if (Physics.Raycast(MainPivot, Direction, out JumpRay, RayLength, GroundLayer, IgnoreTrigger))
                {
                    if (debug) MTools.DebugTriangle(JumpRay.point, 0.1f, Color.yellow);

                    var GroundSlope = Vector3.Angle(JumpRay.normal, animal.UpVector);



                    if (GroundSlope > animal.maxAngleSlope)
                    {
                        animal.UseGravity = General.Gravity;
                        return;
                    }


                    if (JumpRay.distance < animal.Height)
                    {
                        AllowExit();
                        return;
                    }


                    IgnoreLowerStates = true;
                }
                else
                {
                    animal.UseGravity = General.Gravity;
                }
            }
        }


        public override void NewActiveState(StateID newState)
        {
            if (newState.ID <= 1) CanJumpAgain = true;
        }



#if UNITY_EDITOR
        internal void Reset()
        {
            ID = MTools.GetInstance<StateID>("Jump");
            Input = "Jump";

            SleepFromState = new List<StateID>() { MTools.GetInstance<StateID>("Fall"), MTools.GetInstance<StateID>("Fly") };
            SleepFromMode = new List<ModeID>() { MTools.GetInstance<ModeID>("Action"), MTools.GetInstance<ModeID>("Attack1") };


            EnterTag.Value = "JumpStart";
            ExitTag.Value = "JumpEnd";

            General = new AnimalModifier()
            {
                RootMotion = true,
                Grounded = false,
                Sprint = false,
                OrientToGround = false,
                CustomRotation = true,
                IgnoreLowerStates = true,
                Persistent = false,
                AdditivePosition = true,
                AdditiveRotation = true,
                Gravity = false,
                modify = (modifier)(-1),
            };

            ExitFrame = false;

            jumpProfiles = new List<JumpProfile>()
            { new JumpProfile()
            { name = "Jump",  fallingTime = 0.7f, ForwardMultiplier = 1,  HeightMultiplier =  1, JumpLandDistance = 1.7f}
            };
        }
#endif
    }



    [System.Serializable]
    public struct JumpProfile
    {
        public string name;

        public float VerticalSpeed;

        public float JumpLandDistance;

        [Range(0, 1)]
        public float fallingTime;

        [Range(0, 1)]
        public float ExitTime;

        [MinMaxRange(0, 1)]
        public RangedFloat CliffTime;

        public float CliffLandDistance;

        public float HeightMultiplier;

        public float ForwardMultiplier;

        public float ForwardPressed;

        public StateID LastState;
    }
}
