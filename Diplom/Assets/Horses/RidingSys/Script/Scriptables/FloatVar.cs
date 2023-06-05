using System;
using System.Diagnostics;
using UnityEngine;

namespace RidingSystem.Scriptables
{
    [CreateAssetMenu(menuName = "RidingSystem/Variables/Float", order = 1000)]
    public class FloatVar : ScriptableVar
    {
        [SerializeField, HideInInspector] private float value = 0;

        public Action<float> OnValueChanged = delegate { };

        public virtual float Value
        {
            get => value;
            set
            {
                if (this.value != value)
                {
                    this.value = value;
                    OnValueChanged(value);
                }
            }
        }

        public virtual void SetValue(FloatVar var) => Value = var.Value;

        public virtual void Add(FloatVar var) => Value += var.Value;

        public virtual void Add(float var) => Value += var;

        public static implicit operator float(FloatVar reference) => reference.Value;
    }

    [System.Serializable]
    public class FloatReference
    {
        public bool UseConstant = true;

        public float ConstantValue;
        [RequiredField] public FloatVar Variable;

        public FloatReference() => Value = 0;

        public FloatReference(float value) => Value = value;

        public FloatReference(FloatVar value) => Value = value.Value;

        public float Value
        {
            get => UseConstant || Variable == null ? ConstantValue : Variable.Value;
            set
            {
                if (UseConstant || Variable == null)
                    ConstantValue = value;
                else
                    Variable.Value = value;
            }
        }

        public static implicit operator float(FloatReference reference) => reference.Value;

        public static implicit operator FloatReference(float reference) => new FloatReference(reference);

        public static implicit operator FloatReference(FloatVar reference) => new FloatReference(reference);
    }
}