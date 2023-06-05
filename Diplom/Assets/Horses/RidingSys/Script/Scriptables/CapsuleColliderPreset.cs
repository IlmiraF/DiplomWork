using UnityEngine;

namespace RidingSystem.Scriptables
{
    [CreateAssetMenu(menuName = "RidingSystem/Preset/Capsule Collider", order = 200)]
    public class CapsuleColliderPreset : ScriptableObject
    {
        public OverrideCapsuleCollider modifier;

        public void Modify(CapsuleCollider collider) => modifier.Modify(collider);
    }
}