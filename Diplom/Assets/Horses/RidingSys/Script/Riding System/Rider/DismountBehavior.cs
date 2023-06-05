using RidingSystem.Utilities;
using UnityEngine;

namespace RidingSystem.HAP
{
    public class DismountBehavior : StateMachineBehaviour
    {
        private MRider rider;
        private Transform MountPoint;
        private TransformAnimation Fix;
        private Vector3 LastRelativeRiderPosition;
        private float ScaleFactor;



        override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            MTools.ResetFloatParameters(animator);

            rider = animator.FindComponent<MRider>();

            rider.SetMountSide(0);
            ScaleFactor = rider.Montura.Animal.ScaleFactor;
            MountPoint = rider.Montura.MountPoint;

            Fix = rider.MountTrigger.Adjustment;

            rider.Start_Dismounting();

            LastRelativeRiderPosition = MountPoint.InverseTransformPoint(rider.transform.position);

        }

        override public void OnStateMove(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            var transition = animator.GetAnimatorTransitionInfo(layerIndex);
            float deltaTime = animator.updateMode == AnimatorUpdateMode.AnimatePhysics ? Time.fixedDeltaTime : Time.deltaTime;

            var TargetRot = animator.rootRotation;

            var TargetPos = MountPoint.TransformPoint(LastRelativeRiderPosition);

            TargetPos += (animator.velocity * deltaTime * ScaleFactor * (Fix ? Fix.delay : 1));

            if (rider.Montura)
            {
                if (Physics.Raycast(rider.transform.position + rider.transform.up, -rider.transform.up, out RaycastHit hit, 1.5f, rider.Montura.Animal.GroundLayer))
                {
                    if (TargetPos.y < hit.point.y)
                        TargetPos = new Vector3(TargetPos.x, hit.point.y, TargetPos.z);
                }

                TargetRot *= rider.Montura.Animal.AdditiveRotation;

                if (stateInfo.normalizedTime > 0.5f && animator.IsInTransition(layerIndex)) 
                {
                    TargetRot = Quaternion.Lerp(TargetRot, Quaternion.FromToRotation(rider.transform.up, Vector3.up) * TargetRot, transition.normalizedTime);
                }
            }

            LastRelativeRiderPosition = MountPoint.InverseTransformPoint(TargetPos);

            rider.MountRotation = TargetRot;
            rider.MountPosition = TargetPos;
            rider.Mount_TargetTransform();
        }

        override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) { rider.End_Dismounting(); }
    }
}