using System;
using UnityEngine;

namespace RidingSystem.Scriptables
{
    [CreateAssetMenu(menuName = "RidingSystem/Variables/Transform", order = 3000)]
    public class TransformVar : ScriptableVar
    {
        [SerializeField] private Transform value;

        public Action<Transform> OnValueChanged = delegate { };

        public virtual Transform Value
        {
            get => value;
            set
            {
                if (value != this.value)
                {
                    this.value = value;
                    OnValueChanged(value);
                }
            }
        }

        public virtual void SetValue(TransformVar var) => Value = var.Value;
        public virtual void SetNull() => Value = null;
        public virtual void SetValue(Transform var) => Value = var;
        public virtual void SetValue(GameObject var) => Value = var.transform;
        public virtual void SetValue(Component var) => Value = var.transform;
    }

    [System.Serializable]
    public class TransformReference
    {
        public bool UseConstant = true;

        public Transform ConstantValue;
        [RequiredField] public TransformVar Variable;

        public TransformReference() => UseConstant = true;
        public TransformReference(Transform value) => Value = value;

        public TransformReference(TransformVar value)
        {
            Variable = value;
            UseConstant = false;
        }

        public Transform Value
        {
            get => UseConstant ? ConstantValue : (Variable != null ? Variable.Value : null);
            set
            {
                if (UseConstant || Variable == null)
                {
                    UseConstant = true;
                    ConstantValue = value;
                }
                else
                    Variable.Value = value;
            }
        }

        public Vector3 position => Value.position;
        public Quaternion rotation => Value.rotation;

        public static implicit operator Transform(TransformReference reference) => reference.Value;
        public static implicit operator TransformReference(Transform reference) => new TransformReference(reference);
    }
}