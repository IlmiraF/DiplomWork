using RidingSystem.Utilities;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RidingSystem.Scriptables;

namespace RidingSystem.Controller
{
    public class Idle : State
    {
        public override string StateName => "Idle";
        public bool HasLocomotion { get; private set; }

        public override void InitializeState()
        {
            HasLocomotion = animal.HasState(StateEnum.Locomotion);
        }

        public override void Activate()
        {
            base.Activate();
            CanExit = true;
        }

        public override bool TryActivate()
        {
            if (HasLocomotion)
            {
                return (
                    animal.MovementAxisSmoothed == Vector3.zero &&
                    !animal.MovementDetected &&
                    General.Grounded == animal.Grounded
                    );
            }
            else
            {
                return (General.Grounded == animal.Grounded);
            }
        }


#if UNITY_EDITOR
        void Reset()
        {
            ID = MTools.GetInstance<StateID>("Idle");

            General = new AnimalModifier()
            {
                RootMotion = true,
                Grounded = true,
                Sprint = false,
                OrientToGround = true,
                CustomRotation = false,
                FreeMovement = false,
                AdditivePosition = true,
                AdditiveRotation = true,
                Gravity = false,
                modify = (modifier)(-1),
            };
        }
#endif
    }
}