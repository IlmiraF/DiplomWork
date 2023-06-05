using UnityEngine;
using RidingSystem.Scriptables;

namespace RidingSystem.HAP
{
    [AddComponentMenu("RidingSystem/Riding/Mount Trigger")]
    public class MountTriggers : MonoBehaviour
    {
        [SerializeField, RequiredField]
        protected Mount Montura;

        public BoolReference AutoMount = new BoolReference(false);

        public BoolReference Dismount = new BoolReference(true);

        public bool WasAutomounted { get; internal set; }

        [UnityEngine.Serialization.FormerlySerializedAs("MountID")]
        public IntReference MountID;
        [UnityEngine.Serialization.FormerlySerializedAs("DismountID")]
        public IntReference m_DismountID;

        public int DismountID => m_DismountID == 0 ? MountID : m_DismountID;

        public Vector3Reference Direction;

        [CreateScriptableAsset] public TransformAnimation Adjustment;

        public MRider NearbyRider
        {
            get => Montura.NearbyRider;
            internal set => Montura.NearbyRider = value;
        }

        void OnEnable()
        {
            if (Montura == null)
                Montura = GetComponentInParent<Mount>();
        }

        void OnTriggerEnter(Collider other)
        {
            if (!gameObject.activeInHierarchy || other.isTrigger) return;
            GetAnimal(other);
        }

        protected virtual void GetAnimal(Collider other)
        {
            if (!Montura)
            {
                Debug.LogError("No Mount Script Found... please add one");
                return;
            }

            if (!Montura.Mounted && Montura.CanBeMounted)
            {
                var newRider = other.FindComponent<MRider>();

                if (newRider != null)
                {
                    if (newRider.IsMountingDismounting) return;
                    if (newRider.MainCollider != other) return;


                    if (NearbyRider == null || NearbyRider.MountTrigger != this)
                    {
                        newRider.MountTriggerEnter(Montura, this);

                        if (AutoMount.Value && !WasAutomounted)
                        {
                            newRider.MountAnimal();
                        }
                    }
                }
            }
        }



        void OnTriggerExit(Collider other)
        {
            if (!gameObject.activeInHierarchy || other.isTrigger) return;

            var newRider = other.FindComponent<MRider>();

            if (newRider != null && NearbyRider == newRider)
            {
                if (NearbyRider.IsMountingDismounting) return;

                if (NearbyRider.MountTrigger == this
                    && !Montura.Mounted &&
                    newRider.MainCollider == other)
                {
                    NearbyRider.MountTriggerExit();
                }
            }
        }

        private void Reset()
        {
            if (Montura == null)
                Montura = GetComponentInParent<Mount>();
        }
    }
}