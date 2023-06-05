using System;
using UnityEngine;

namespace RidingSystem.Scriptables
{
    [CreateAssetMenu(menuName = "RidingSystem/Variables/Bool", order = 1000)]
    public class BoolVar : ScriptableVar
    {
        [SerializeField] private bool value;

        public Action<bool> OnValueChanged = delegate { };

        public virtual bool Value
        {
            get => value;
            set
            {
                if (this.value != value)
                {
                    this.value = value;
                    OnValueChanged(value);                }
            }
        }

        public virtual void SetValue(BoolVar var) => SetValue(var.Value);

        public virtual void SetValue(bool var) => Value = var;
        public virtual void SetValueInverted(bool var) => Value = !var;
        public virtual void Toggle() => Value ^= true;
        public virtual void UpdateValue() => OnValueChanged?.Invoke(value);

        public static implicit operator bool(BoolVar reference) => reference.Value;
    }

    [System.Serializable]
    public class BoolReference
    {
        public bool UseConstant = true;

        public bool ConstantValue;
        [RequiredField] public BoolVar Variable;

        public BoolReference() => Value = false;

        public BoolReference(bool value) => Value = value;

        public BoolReference(BoolVar value) => Value = value.Value;

        public bool Value
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

        #region Operators
        public static implicit operator bool(BoolReference reference) => reference.Value;
        #endregion
    }
}