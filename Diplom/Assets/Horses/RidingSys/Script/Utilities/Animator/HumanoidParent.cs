using RidingSystem.Scriptables;
using UnityEngine;

namespace RidingSystem.Utilities
{
    public class HumanoidParent : MonoBehaviour
    {
        public Animator animator;

        [SearcheableEnum]
        public HumanBodyBones parent = HumanBodyBones.Spine;

        public BoolReference LocalPos;

        public BoolReference LocalRot;

        public Vector3Reference PosOffset;

        public Vector3Reference RotOffset;

        private void Awake()
        {
            if (animator != null)
            {
                var boneParent = animator.GetBoneTransform(parent);

                if (boneParent != null && transform.parent != boneParent)
                {
                    transform.parent = boneParent;

                    if (LocalPos.Value) transform.localPosition = Vector3.zero;
                    if (LocalRot.Value) transform.localRotation = Quaternion.identity;

                    transform.localPosition += PosOffset;
                    transform.localRotation *= Quaternion.Euler(RotOffset);
                }
            }
        }

        private void OnValidate()
        {
            if (animator == null) animator = gameObject.FindComponent<Animator>();
        }
    }
}
