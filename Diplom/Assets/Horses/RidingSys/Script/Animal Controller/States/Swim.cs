using RidingSystem.Utilities;
using System.Collections.Generic;
using UnityEngine;

namespace RidingSystem.Controller
{
    public class Swim : State
    {
        public override string StateName => "Swim";

        [Header("Swim Paramenters")]
        public LayerMask WaterLayer = 16;

        public float AlignSmooth = 10;
        public float Bounce = 5;
        public float TryExitTime = 0.5f;
        protected float EnterWaterTime;
        public bool WaterIsStatic = true;
        public bool KeepInertia = true;
        public float m_Radius = 0.1f;

        public float FrontRayLength = 1;



        public bool PivotAboveWater { get; private set; }

        public bool IsInWater { get; private set; }

        protected MPivots WaterPivot;
        protected Vector3 WaterNormal = Vector3.up;
        protected Vector3 HorizontalInertia;

        protected Collider[] WaterCollider;

        private Vector3 WaterPivot_Dist_from_Water;
        private Vector3 WaterUPPivot => WaterPivotPoint + animal.DeltaVelocity + (animal.UpVector * UpMult);

        private Vector3 UpImpulse;
        const float UpMult = 30;

        public Vector3 WaterPivotPoint => WaterPivot.World(animal.transform) + animal.DeltaVelocity;


        public override void InitializeState()
        {
            WaterPivot = animal.pivots.Find(p => p.name.ToLower().Contains("water"));
            if (WaterPivot == null) Debug.LogError("No Water Pivot Found.. please create a Water Pivot");

            WaterCollider = new Collider[1];
            IsInWater = false;
        }

        public override bool TryActivate()
        {
            CheckWater();

            if (IsInWater)
            {
                var waterCol = WaterCollider[0];

                Ray WaterRay = new Ray(WaterUPPivot, Gravity);

                if (waterCol.Raycast(WaterRay, out RaycastHit WaterHit, 100f))
                {
                    WaterNormal = WaterHit.normal;
                }

                EnterWaterTime = Time.time;

                return true;
            }

            return false;
        }


        public override void Activate()
        {
            base.Activate();

            HorizontalInertia = Vector3.ProjectOnPlane(animal.DeltaPos, animal.UpVector);
            UpImpulse = Vector3.Project(animal.DeltaPos, animal.UpVector);  
            IgnoreLowerStates = true;
            animal.UseGravity = false; 
            animal.InertiaPositionSpeed = Vector3.zero;
            animal.Force_Reset();
        }


        public void CheckWater()
        {
            int WaterFound = Physics.OverlapSphereNonAlloc(WaterPivotPoint, m_Radius * animal.ScaleFactor, WaterCollider, WaterLayer);
            IsInWater = WaterFound > 0;
        }

        public override void TryExitState(float DeltaTime)
        {
            if (!InExitAnimation && MTools.ElapsedTime(EnterWaterTime, TryExitTime))
            {
                CheckWater();
                if (!IsInWater)
                {
                    animal.CheckIfGrounded();
                    AllowExit();
                }
            }
        }


        public override void AllowStateExit()
        {
            IsInWater = false;
        }

        public override void OnStateMove(float deltatime)
        {
            if (IsInWater && !InExitAnimation)
            {
                if (KeepInertia) animal.AddInertia(ref HorizontalInertia, 3);
                if (Bounce > 0) animal.AddInertia(ref UpImpulse, Bounce);

                var waterCol = WaterCollider[0];

                if (!WaterIsStatic)
                {
                    Ray WaterRay = new Ray(WaterUPPivot, Gravity * UpMult);

                    if (waterCol.Raycast(WaterRay, out RaycastHit WaterHit, 100f)) WaterNormal = WaterHit.normal;
                }

                animal.AlignRotation(WaterNormal, deltatime, AlignSmooth > 0 ? AlignSmooth : 5);

                FindWaterLevel();

                var rayColor = (Color.blue + Color.cyan) / 2;

                if (FrontRayLength > 0 &&
                    Physics.Raycast(WaterPivotPoint, Forward, out RaycastHit FrontRayWater, FrontRayLength, GroundLayer, QueryTriggerInteraction.Ignore))
                {
                    var FrontPivot = Vector3.Angle(FrontRayWater.normal, animal.UpVector);

                    rayColor = Color.cyan;

                    if (FrontPivot > animal.maxAngleSlope)
                    {
                        rayColor = Color.black;
                        animal.transform.position += WaterPivot_Dist_from_Water;
                        animal.ResetUPVector();
                    }
                }
                else
                {
                    if (AlignSmooth > 0)
                        animal.AdditivePosition += WaterPivot_Dist_from_Water * (deltatime * AlignSmooth);
                    else
                    {
                        animal.transform.position += WaterPivot_Dist_from_Water;
                        animal.ResetUPVector();
                    }
                }
                if (debug) Debug.DrawRay(WaterPivotPoint, animal.Forward * FrontRayLength, rayColor);
            }
        }

        public void FindWaterLevel()
        {
            if (IsInWater)
            {
                var waterCol = WaterCollider[0];
                var PivotPointDistance = waterCol.ClosestPoint(WaterUPPivot);


                WaterPivot_Dist_from_Water = Vector3.Project((PivotPointDistance - WaterPivotPoint), animal.UpVector);


                PivotAboveWater = Vector3.Dot(WaterPivot_Dist_from_Water, animal.UpVector) < 0;
            }
            else
            {
                PivotAboveWater = true;
            }
        }

        public override void ResetStateValues()
        {
            WaterCollider = new Collider[1];
            IsInWater = false;
            EnterWaterTime = 0;
        }

#if UNITY_EDITOR

        void Reset()
        {
            ID = MTools.GetInstance<StateID>("Swim");

            WaterCollider = new Collider[1];

            ExitCooldown = 1f;
            EnterCooldown = 1f;

            General = new AnimalModifier()
            {
                RootMotion = true,
                Grounded = false,
                Sprint = true,
                OrientToGround = false,
                CustomRotation = true,
                IgnoreLowerStates = true,
                AdditivePosition = true,
                AdditiveRotation = true,
                Gravity = false,
                modify = (modifier)(-1),

            };
        }


        public override void StateGizmos(MAnimal animal)
        {
            if (Application.isPlaying)
            {
                if (IsInWater)
                {
                    var scale = animal.ScaleFactor;
                    Collider WaterCol = WaterCollider[0];

                    Gizmos.color = Color.blue;
                    Gizmos.DrawSphere(WaterPivotPoint, m_Radius * scale);
                    Gizmos.color = Color.red;
                    Gizmos.DrawSphere(animal.transform.position, m_Radius * scale);

                    if (WaterCol)
                    {
                        Gizmos.color = Color.cyan;
                        Gizmos.DrawSphere(WaterCol.ClosestPoint(WaterUPPivot), m_Radius * scale);
                        Gizmos.DrawSphere(WaterUPPivot, m_Radius * scale);
                    }
                }
            }
        }
#endif
    }
}