using UnityEngine;
using RidingSystem.Scriptables;
using UnityEngine.Serialization;

namespace RidingSystem.Controller
{
    public class Fall : State
    {
        public override string StateName => "Fall";
        public enum FallBlending { DistanceNormalized, Distance, VerticalVelocity }

        [Header("Fall Parameters")]
        public BoolReference AirControl = new BoolReference(true);
        public FloatReference AirRotation = new FloatReference(10);
        public FloatReference AirMovement = new FloatReference(0);
        public FloatReference AirSmooth = new FloatReference(2);

        [Space]

        public FloatReference Offset = new FloatReference();

        [FormerlySerializedAs("FallRayForward")]
        public FloatReference MoveMultiplier = new FloatReference(0.1f);

        [FormerlySerializedAs("fallRayMultiplier")]
        public FloatReference lengthMultiplier = new FloatReference(1f);

        public IntReference RayHits = new IntReference(3);

        [FormerlySerializedAs("AirDrag")]
        public float UpDrag = 1;

        [Space]
        public FallBlending BlendFall = FallBlending.DistanceNormalized;

        public FloatReference LowerBlendDistance;

        [Space, Header("Fall Damage")]
        public StatID AffectStat;

        public FloatReference FallMinDistance = new FloatReference(5f);

        public FloatReference FallMaxDistance = new FloatReference(15f);

        public Vector2[] landStatus;

        public bool StuckAnimal = true;

        public FloatReference PushForward = new FloatReference(2);
        public float MaxHeight { get; set; }

        public float FallCurrentDistance { get; set; }

        protected Vector3 fall_Point;
        private RaycastHit[] FallHits;
        private RaycastHit FallRayCast;

        private GameObject GameObjectHit;
        private bool IsDebree;

        private float DistanceToGround;

        float Fall_Float;
        public Vector3 UpImpulse { get; set; }

        private MSpeed FallSpeed = MSpeed.Default;

        public Vector3 FallPoint { get; private set; }

        public bool Has_UP_Impulse { get; private set; }

        private bool GoingDown;
        private int Hits;

        public override void AwakeState()
        {
            base.AwakeState();
            animalStats = animal.FindComponent<Stats>();
        }

        public override bool TryActivate()
        {
            float SprintMultiplier = (animal.VerticalSmooth);
            var fall_Pivot = animal.Main_Pivot_Point + (animal.Forward * Offset * ScaleFactor) +
                (animal.Forward * SprintMultiplier * MoveMultiplier * ScaleFactor);

            fall_Pivot += animal.DeltaPos;

            float Multiplier = animal.Pivot_Multiplier * lengthMultiplier;
            return TryFallRayCasting(fall_Pivot, Multiplier);
        }

        private bool TryFallRayCasting(Vector3 fall_Pivot, float Multiplier)
        {
            FallHits = new RaycastHit[RayHits];

            var Direction = animal.TerrainSlope < 0 ? Gravity : -transform.up;

            var Radius = animal.RayCastRadius * ScaleFactor;
            Hits = Physics.SphereCastNonAlloc(fall_Pivot, Radius, Direction, FallHits, Multiplier, GroundLayer, IgnoreTrigger);

            if (debug)
            {
                Debug.DrawRay(fall_Pivot, Direction * Multiplier, Color.magenta);
                Debug.DrawRay(FallRayCast.point, FallRayCast.normal * ScaleFactor * 0.2f, Color.magenta);
            }

            var TerrainSlope = 0f;

            if (Hits > 0)
            {
                if (animal.Grounded)
                {
                    foreach (var hit in FallHits)
                    {
                        if (hit.collider != null)
                        {
                            TerrainSlope = Vector3.SignedAngle(hit.normal, animal.UpVector, animal.Right);
                            MTools.DrawWireSphere(fall_Pivot + Direction * DistanceToGround, Color.magenta, Radius);
                            FallRayCast = hit;



                            if (TerrainSlope > -animal.maxAngleSlope)
                                break;
                        }
                    }

                    if (FallRayCast.transform.gameObject != GameObjectHit)
                    {
                        GameObjectHit = FallRayCast.transform.gameObject;
                        IsDebree = GameObjectHit.CompareTag(animal.DebrisTag);
                    }

                    if (animal.DeepSlope || (TerrainSlope < -animal.maxAngleSlope && !IsDebree))
                    {
                        return true;
                    }
                }
                else
                {
                    FallRayCast = FallHits[0];
                    DistanceToGround = FallRayCast.distance;

                    var FallSlope = Vector3.Angle(FallRayCast.normal, animal.UpVector);

                    if (FallSlope > animal.maxAngleSlope)
                    {
                        return true;
                    }
                    if (Height >= DistanceToGround)
                    {

                        if (animal.ExternalForce != Vector3.zero) return true;

                        animal.CheckIfGrounded();
                        return false;
                    }
                }
            }
            else
            {
                return true;
            }

            //Debug.Log("fa");
            return false;
        }

        public override void Activate()
        {
            StartingSpeedDirection = animal.ActiveState.Speed_Direction();
            base.Activate();
            ResetStateValues();
            Fall_Float = animal.State_Float;
        }

        public override void EnterCoreAnimation()
        {
            SetEnterStatus(0);

            UpImpulse = Vector3.Project(animal.DeltaPos, animal.UpVector);  

            IgnoreLowerStates = false;

            var Speed = animal.HorizontalSpeed / ScaleFactor;
            var passInertia = true;

            if (animal.HasExternalForce)
            {
                var HorizontalForce = Vector3.ProjectOnPlane(animal.ExternalForce, animal.UpVector);
                var HorizontalInertia = Vector3.ProjectOnPlane(animal.Inertia, animal.UpVector);

                var HorizontalSpeed = HorizontalInertia - HorizontalForce;
                Speed = HorizontalSpeed.magnitude / ScaleFactor;
                passInertia = false;
            }

            if (!animal.ExternalForceAirControl) Speed = 0;

            FallSpeed = new MSpeed(animal.CurrentSpeedModifier)
            {
                name = "FallSpeed",
                position = Speed,
                animator = 1,
                rotation = AirRotation.Value,
                lerpPosition = AirSmooth.Value,
            };


            animal.SetCustomSpeed(FallSpeed, passInertia);

            if (animal.HasExternalForce && animal.Zone) animal.UseGravity = false;

            Has_UP_Impulse = Vector3.Dot(UpImpulse, animal.UpVector) > 0;

            if (MTools.CompareOR(animal.LastState.ID, 0, 1, StateEnum.Swim, StateEnum.Climb) && Has_UP_Impulse || animal.HasExternalForce)
                UpImpulse = Vector3.zero;
        }

        public override Vector3 Speed_Direction()
        {
            return AirControl.Value ? (base.Speed_Direction() + animal.ExternalForce).normalized : StartingSpeedDirection;
        }


        Vector3 StartingSpeedDirection;
        private Stats animalStats;

        public override void OnStateMove(float deltaTime)
        {
            if (InCoreAnimation)
            {
                if (animal.Zone && animal.HasExternalForce) animal.GravityTime = 0;

                animal.AdditivePosition += UpImpulse;

                if (Has_UP_Impulse)
                    UpImpulse = Vector3.Lerp(UpImpulse, Vector3.zero, deltaTime * UpDrag);

                if (AirControl.Value && AirMovement > 0 && AirMovement > CurrentSpeedPos)
                {
                    if (!animal.ExternalForceAirControl) return;

                    CurrentSpeedPos = Mathf.Lerp(CurrentSpeedPos, AirMovement, (AirSmooth != 0 ? (deltaTime * AirSmooth) : 1));
                }
            }
        }

        public override void ExitState()
        {
            var status = 0;
            if (landStatus != null && landStatus.Length >= 1)
            {

                foreach (var ls in landStatus)
                    if (ls.x < FallCurrentDistance) status = (int)ls.y;

            }
            SetExitStatus(status);

            if (AffectStat != null && animalStats != null
                && FallCurrentDistance > FallMinDistance.Value && animal.Grounded)
            {
                var StatFallValue = (FallCurrentDistance) * 100 / FallMaxDistance;
                animalStats.Stat_ModifyValue(AffectStat, StatFallValue, StatOption.ReduceByPercent);
            }
            base.ExitState();
        }


        public override void TryExitState(float DeltaTime)
        {
            var Radius = animal.RayCastRadius * ScaleFactor;
            FallPoint = animal.Main_Pivot_Point;
            float DeltaDistance = 0;

            GoingDown = Vector3.Dot(DeltaPos, Gravity) > 0;

            if (GoingDown)
            {
                DeltaDistance = Vector3.Project(DeltaPos, Gravity).magnitude;
                FallCurrentDistance += DeltaDistance;
            }

            if (animal.debugGizmos && debug)
            {
                MTools.DrawWireSphere(FallPoint, Color.magenta, Radius);
                MTools.DrawWireSphere(FallPoint + Gravity * Height, (Color.red + Color.blue) / 2, Radius);
                Debug.DrawRay(FallPoint, Gravity * 100f, Color.magenta);
            }

            var FoundGround = (Physics.Raycast(FallPoint, Gravity, out FallRayCast, 100f, GroundLayer, IgnoreTrigger));


            if (FoundGround)
            {
                DistanceToGround = FallRayCast.distance;

                if (animal.debugGizmos && debug)
                {
                    MTools.DrawWireSphere(FallRayCast.point, (Color.blue + Color.red) / 2, Radius);
                    MTools.DrawWireSphere(FallPoint, (Color.red), Radius);
                }

                switch (BlendFall)
                {
                    case FallBlending.DistanceNormalized:
                        {
                            var realDistance = DistanceToGround - Height;

                            if (MaxHeight < realDistance)
                            {
                                MaxHeight = realDistance;
                                Fall_Float = Mathf.Lerp(Fall_Float, 0, DeltaTime * 5);
                                animal.State_SetFloat(Fall_Float);
                            }
                            else
                            {
                                realDistance -= LowerBlendDistance;
                                Fall_Float = Mathf.Lerp(Fall_Float, 1 - realDistance / MaxHeight, DeltaTime * 10);
                                animal.State_SetFloat(Fall_Float);
                            }
                        }
                        break;
                    case FallBlending.Distance:
                        animal.State_SetFloat(FallCurrentDistance);
                        break;
                    case FallBlending.VerticalVelocity:
                        var UpInertia = Vector3.Project(animal.DeltaPos, animal.UpVector).magnitude;   
                        animal.State_SetFloat(UpInertia / animal.DeltaTime * (GoingDown ? 1 : -1));
                        break;
                    default:
                        break;
                }

                if (Height > DistanceToGround || ((DistanceToGround - DeltaDistance) < 0))
                {
                    var FallRayAngle = Vector3.Angle(FallRayCast.normal, animal.UpVector);
                    var DeepSlope = FallRayAngle > animal.maxAngleSlope;

                    if (!DeepSlope)
                    {
                        AllowExit();
                        animal.CheckIfGrounded();

                        animal.Grounded = true;
                        animal.UseGravity = false;

                        if (DeltaDistance > 0.1f)
                        {
                            animal.ResetUPVector();
                        }
                        animal.InertiaPositionSpeed = Vector3.ProjectOnPlane(animal.RB.velocity * DeltaTime, animal.UpVector);
                        return;
                    }
                    else
                    {
                        FallCurrentDistance = 0;
                        return;
                    }
                }
            }

            ResetRigidbody(DeltaTime, Gravity);
        }

        private void ResetRigidbody(float DeltaTime, Vector3 Gravity)
        {
            if (StuckAnimal && GoingDown)
            {
                var RBOldDown = Vector3.Project(animal.RB.velocity, Gravity);
                var RBNewDown = Vector3.Project(animal.DesiredRBVelocity, Gravity);
                var NewDMagn = RBNewDown.magnitude;
                var Old_DMagn = RBOldDown.magnitude;

                MTools.Draw_Arrow(animal.Main_Pivot_Point + Forward * 0.02f, RBOldDown * 0.5f, Color.red);
                MTools.Draw_Arrow(animal.Main_Pivot_Point + Forward * 0.04f, RBNewDown * 0.5f, Color.green);

                ResetCount++;

                if (NewDMagn > (Old_DMagn * Old_DMagn) && 
                    Old_DMagn < 0.1f &&
                    ResetCount > 5)
                {
                    if (animal.DesiredRBVelocity.magnitude > Height)
                    {
                        animal.ResetUPVector();
                        animal.GravityTime = animal.StartGravityTime;

                        if (PushForward > 0)
                            animal.InertiaPositionSpeed = animal.Forward * animal.ScaleFactor * DeltaTime * PushForward;

                        ResetCount = 0;
                    }
                }
            }
        }

        private int ResetCount;

        public override void ResetStateValues()
        {
            DistanceToGround = float.PositiveInfinity;
            GoingDown = false;
            IsDebree = false;
            FallSpeed = new MSpeed();
            FallRayCast = new RaycastHit();
            GameObjectHit = null;
            FallHits = new RaycastHit[RayHits];
            UpImpulse = Vector3.zero;
            MaxHeight = float.NegativeInfinity;
            FallCurrentDistance = 0;
            Fall_Float = 0;
        }


        public override void StateGizmos(MAnimal animal)
        {
            if (!Application.isPlaying)
            {
                var fall_Pivot = animal.Main_Pivot_Point + (animal.Forward * Offset * animal.ScaleFactor);
                float Multiplier = animal.Pivot_Multiplier * lengthMultiplier;

                Debug.DrawRay(fall_Pivot, animal.Gravity.normalized * Multiplier, Color.magenta);
            }
        }

#if UNITY_EDITOR

        private void Reset()
        {
            ID = MTools.GetInstance<StateID>("Fall");
            General = new AnimalModifier()
            {
                RootMotion = false,
                AdditivePosition = true,
                AdditiveRotation = true,
                Grounded = false,
                Sprint = false,
                OrientToGround = false,

                Gravity = true,
                CustomRotation = false,
                modify = (modifier)(-1),
            };

            LowerBlendDistance = 0.1f;
            MoveMultiplier = 0.1f;
            lengthMultiplier = 1f;

            FallSpeed.name = "FallSpeed";

            ExitFrame = false;
        }
#endif
    }
}