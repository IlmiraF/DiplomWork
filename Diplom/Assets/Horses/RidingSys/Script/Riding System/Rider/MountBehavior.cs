using UnityEngine;
using System;
using RidingSystem.Utilities;

namespace RidingSystem.HAP
{
    public class MountBehavior : StateMachineBehaviour
    {
        public AnimationCurve MovetoMountPoint;

        protected MRider rider;
        protected Transform MountTrigger;
        protected bool AlingWithY;

        private float alignTime;
        public float AnimationMult = 1f;

        float AnimalScaleFactor = 1;

        TransformAnimation Fix;

        override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            rider = animator.FindComponent<MRider>();

            rider.SetMountSide(0);

            alignTime = rider.AlingMountTrigger;
            MountTrigger = rider.MountTrigger.transform;   

            rider.MountRotation = rider.transform.rotation;
            rider.MountPosition = rider.transform.position;

            AnimalScaleFactor = rider.Montura.Animal.ScaleFactor;

            MTools.ResetFloatParameters(animator);

            Fix = rider.MountTrigger.Adjustment;

            rider.Start_Mounting();
        }

        override public void OnStateMove(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            float DeltaTime = animator.updateMode == AnimatorUpdateMode.AnimatePhysics ? Time.fixedDeltaTime : Time.deltaTime;
            var TargetRot = animator.rootRotation;
            var TargetPos = rider.RiderRoot.position += (animator.velocity * DeltaTime * AnimalScaleFactor * (Fix ? Fix.time : 1) * AnimationMult);

            float norm_time = stateInfo.normalizedTime;

            var Mount_Position = rider.Montura.MountPoint.position;
            var Mount_Rotation = rider.Montura.MountPoint.rotation;

            if (norm_time < alignTime)
            {
                var lerp = norm_time / alignTime;

                TargetPos = Vector3.Lerp(TargetPos, MountTrigger.position, lerp);
                TargetRot = Quaternion.Lerp(TargetRot, MountTrigger.rotation, lerp);
            }


            if (Fix)
            {
                if (Fix.UsePosition)
                {
                    if (!Fix.SeparateAxisPos)
                    {
                        TargetPos = Vector3.LerpUnclamped(TargetPos, Mount_Position, Fix.PosCurve.Evaluate(norm_time));
                    }
                    else
                    {
                        float x = Mathf.LerpUnclamped(TargetPos.x, Mount_Position.x, Fix.PosXCurve.Evaluate(norm_time) * Fix.Position.x);
                        float y = Mathf.LerpUnclamped(TargetPos.y, Mount_Position.y, Fix.PosYCurve.Evaluate(norm_time) * Fix.Position.y);
                        float z = Mathf.LerpUnclamped(TargetPos.z, Mount_Position.z, Fix.PosZCurve.Evaluate(norm_time) * Fix.Position.z);

                        Vector3 newPos = new Vector3(x, y, z);

                        TargetPos = newPos;
                    }
                }
                else
                {
                    TargetPos = Vector3.Lerp(TargetPos, Mount_Position, MovetoMountPoint.Evaluate(norm_time));
                }


                if (Fix.UseRotation) TargetRot = Quaternion.Lerp(TargetRot, Mount_Rotation, Fix.RotCurve.Evaluate(norm_time));
                else
                    TargetRot = Quaternion.Lerp(TargetRot, Mount_Rotation, MovetoMountPoint.Evaluate(norm_time));
            }
            else
            {
                TargetPos = Vector3.Lerp(TargetPos, Mount_Position, MovetoMountPoint.Evaluate(norm_time));
                TargetRot = Quaternion.Lerp(TargetRot, Mount_Rotation, MovetoMountPoint.Evaluate(norm_time));
            }

            rider.MountRotation = TargetRot;
            rider.MountPosition = TargetPos;
            rider.Mount_TargetTransform();
        }

        override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        { rider.End_Mounting(); }
    }
}

