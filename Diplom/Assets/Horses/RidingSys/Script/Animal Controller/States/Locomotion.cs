using RidingSystem.Scriptables;
using RidingSystem.Utilities;
using UnityEngine;
using UnityEngine.Networking.Types;

namespace RidingSystem.Controller
{
    public class Locomotion : State
    {
        public override string StateName => "Locomotion";
        [Header("Locomotion Parameters")]

        [Tooltip("Backward Offset Position of the BackFall Ray")]
        public FloatReference FallRayBackwards = new FloatReference(0.3f);

        [Tooltip("Reset Inertia On Enter")]
        public BoolReference ResetIntertia = new BoolReference(false);

        [Space(10), Tooltip("Makes the Animal Stop Moving when is near a Wall")]
        public bool WallStop = false;
        [Hide("WallStop", true, false)] public float WallRayLength = 1f;
        [Hide("WallStop", true, false)] public LayerMask StopLayer = 1;
        [Hide("WallStop", true, false)] public QueryTriggerInteraction trigger = QueryTriggerInteraction.UseGlobal;


        [Space(10), Tooltip("Makes the Animal avoid ledges, Useful when the Animal without a Fall State, like the Elephant")]
        public bool AntiFall = false;

        [Hide("AntiFall", true, false)] public float frontDistance = 0.5f;
        [Hide("AntiFall", true, false)] public float frontSpace = 0.2f;
        [Space]
        [Hide("AntiFall", true, false)] public float BackDistance = 0.5f;
        [Hide("AntiFall", true, false)] public float BackSpace = 0.2f;
        [Space]
        [Hide("AntiFall", true, false)] public float FallMultiplier = 1f;
        [Hide("AntiFall", true, false)] public Color DebugColor = Color.yellow;

        public bool HasIdle { get; private set; }


        public override void InitializeState()
        {
            HasIdle = animal.HasState(StateEnum.Idle);
        }


        public override bool TryActivate()
        {
            if (animal.Grounded)
            {
                if (!HasIdle) return true;

                if (animal.MovementAxisSmoothed != Vector3.zero || animal.MovementDetected)
                {
                    return true;
                }
            }
            return false;
        }

        public override void Activate()
        {
            base.Activate();
            var speed = (int)animal.CurrentSpeedModifier.Vertical.Value;
            SetEnterStatus(speed);

        }


        public override void EnterCoreAnimation()
        {
            if (ResetIntertia.Value) animal.ResetInertiaSpeed();

        }

        public override void EnterTagAnimation()
        {
            if (CurrentAnimTag == EnterTagHash)
            {
                animal.VerticalSmooth = animal.CurrentSpeedModifier.Vertical;
            }
        }

        public override void OnStatePreMove(float deltatime)
        {
            Wall_Stop();
            Anti_Fall();
        }

        public override void OnStateMove(float deltatime)
        {
            SetFloatSmooth(0, deltatime * CurrentSpeed.lerpPosition);
        }


        private void Wall_Stop()
        {
            if (WallStop)
            {
                var MainPivotPoint = animal.Main_Pivot_Point;
                if (Physics.Raycast(MainPivotPoint, animal.Forward, out _, WallRayLength, StopLayer, trigger))
                {
                    Gizmos.color = Color.red;
                    Debug.DrawRay(MainPivotPoint, animal.Forward * WallRayLength, Color.red);
                    animal.MovementAxis.z = 0;
                }
                else
                {
                    Debug.DrawRay(MainPivotPoint, animal.Forward * WallRayLength, DebugColor);
                }
            }
        }

        private void Anti_Fall()
        {
            if (AntiFall)
            {
                bool BlockForward = false;
                MovementAxisMult = Vector3.one;

                var ForwardMov = MovementRaw.z;
                var Dir = animal.TerrainSlope > 0 ? Gravity : -animal.Up;

                float SprintMultiplier = (animal.CurrentSpeedModifier.Vertical).Value;
                SprintMultiplier += animal.Sprint ? 1f : 0f;


                var RayMultiplier = animal.Pivot_Multiplier * FallMultiplier;

                var MainPivotPoint = animal.Pivot_Chest.World(animal.transform);

                RaycastHit[] hits = new RaycastHit[1];

                Vector3 Center;
                Vector3 Left;
                Vector3 Right;


                if (ForwardMov > 0)
                {
                    Center = MainPivotPoint + (animal.Forward * frontDistance * SprintMultiplier * ScaleFactor);
                    Left = Center + (animal.Right * frontSpace * ScaleFactor);
                    Right = Center + (-animal.Right * frontSpace * ScaleFactor);
                }
                else if (ForwardMov < 0)
                {
                    Center = MainPivotPoint - (animal.Forward * BackDistance * SprintMultiplier * ScaleFactor);
                    Left = Center + (animal.Right * BackSpace * ScaleFactor);
                    Right = Center + (-animal.Right * BackSpace * ScaleFactor);
                }
                else
                { return; }

                Debug.DrawRay(Center, Dir * RayMultiplier, DebugColor);
                Debug.DrawRay(Left, Dir * RayMultiplier, DebugColor);
                Debug.DrawRay(Right, Dir * RayMultiplier, DebugColor);

                var fallHits = Physics.RaycastNonAlloc(Center, Dir, hits, RayMultiplier, GroundLayer, QueryTriggerInteraction.Ignore);

                if (fallHits == 0)
                {
                    BlockForward = true;
                }
                else
                    fallHits = Physics.RaycastNonAlloc(Left, Dir, hits, RayMultiplier, GroundLayer, QueryTriggerInteraction.Ignore);
                if (fallHits == 0)
                {
                    BlockForward = true;
                }
                else
                {
                    fallHits = Physics.RaycastNonAlloc(Right, Dir, hits, RayMultiplier, GroundLayer, QueryTriggerInteraction.Ignore);
                    if (fallHits == 0)
                    {
                        BlockForward = true;
                    }
                }

                if (BlockForward)
                {
                    MovementAxisMult.z = 0;
                    if(Anim.GetFloat("Vertical") == 0f)
                    {
                        animal.State_Activate(StateEnum.Jump);
                        animal.ActiveState.MovementAxisMult.z = 0;
                    }
                }
            }
            else if (!animal.UseCameraInput && MovementRaw.z < 0)
            {
                var MainPivotPoint = animal.Has_Pivot_Hip ? animal.Pivot_Hip.World(transform) : animal.Pivot_Chest.World(transform);
                MainPivotPoint += Forward * -(FallRayBackwards * ScaleFactor);
                RaycastHit[] hits = new RaycastHit[1];

                var RayMultiplier = animal.Pivot_Multiplier;
                Debug.DrawRay(MainPivotPoint, -Up * RayMultiplier, Color.white);

                var fallHits = Physics.RaycastNonAlloc(MainPivotPoint, -Up, hits, RayMultiplier, GroundLayer, QueryTriggerInteraction.Ignore);

                if (fallHits == 0)
                {
                    MovementAxisMult.z = 0;
                }
            }
        }

        public override void StateGizmos(MAnimal animal)
        {
            if (AntiFall) PaintRays(animal);

            if (WallStop)
            {
                var MainPivotPoint = animal.Main_Pivot_Point;
                Debug.DrawRay(MainPivotPoint, animal.Forward * WallRayLength, DebugColor);
            }
        }



        void PaintRays(MAnimal animal)
        {
            float scale = animal.ScaleFactor;
            var Dir = animal.TerrainSlope > 0 ? animal.Gravity : -animal.Up;
            var RayMultiplier = animal.Pivot_Multiplier * FallMultiplier;
            var MainPivotPoint = animal.Pivot_Chest.World(animal.transform);

            var FrontCenter = MainPivotPoint + (animal.Forward * frontDistance * scale);
            var FrontLeft = FrontCenter + (animal.Right * frontSpace * scale);
            var FrontRight = FrontCenter + (-animal.Right * frontSpace * scale);
            var BackCenter = MainPivotPoint - (animal.Forward * BackDistance * scale);
            var BackLeft = BackCenter + (animal.Right * BackSpace * scale);
            var BackRight = BackCenter + (-animal.Right * BackSpace * scale);

            Debug.DrawRay(FrontCenter, Dir * RayMultiplier, DebugColor);
            Debug.DrawRay(FrontLeft, Dir * RayMultiplier, DebugColor);
            Debug.DrawRay(FrontRight, Dir * RayMultiplier, DebugColor);
            Debug.DrawRay(BackCenter, Dir * RayMultiplier, DebugColor);
            Debug.DrawRay(BackLeft, Dir * RayMultiplier, DebugColor);
            Debug.DrawRay(BackRight, Dir * RayMultiplier, DebugColor);
        }


#if UNITY_EDITOR
        void Reset()
        {
            ID = MTools.GetInstance<StateID>("Locomotion");

            General = new AnimalModifier()
            {
                RootMotion = true,
                Grounded = true,
                Sprint = true,
                OrientToGround = true,
                CustomRotation = false,
                IgnoreLowerStates = false,
                AdditivePosition = true,
                AdditiveRotation = true,
                Gravity = false,
                modify = (modifier)(-1),
            };

            EnterTag.Value = "StartLocomotion";
        }
#endif
    }
}