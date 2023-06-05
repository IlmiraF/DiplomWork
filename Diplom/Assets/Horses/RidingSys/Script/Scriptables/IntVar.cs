using System;
using UnityEngine;

namespace RidingSystem.Scriptables
{
    [CreateAssetMenu(menuName = "RidingSystem/Variables/Integer", order = 1000)]
    public class IntVar : ScriptableVar
    {
        [SerializeField] private int value = 0;

        public Action<int> OnValueChanged = delegate { };

        public virtual int Value
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


        public virtual void SetValue(IntVar var) => Value = var.Value;

        public virtual void Add(IntVar var) => Value += var.Value;

        public virtual void Add(int var) => Value += var;
        public virtual void Multiply(int var) => Value *= var;
        public virtual void Multiply(IntVar var) => Value *= var;
        public virtual void Divide(IntVar var) => Value /= var;

        public static implicit operator int(IntVar reference) => reference.Value;
    }


    [System.Serializable]
    public class IntReference
    {
        public bool UseConstant = true;

        public int ConstantValue;
        [RequiredField] public IntVar Variable;

        public IntReference() => Value = 0;

        public IntReference(int value) => Value = value;

        public IntReference(IntVar value) => Value = value.Value;

        public int Value
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
        public static implicit operator int(IntReference reference) => reference.Value;

        public static implicit operator IntReference(int reference) => new IntReference(reference);

        public static implicit operator IntReference(IntVar reference) => new IntReference(reference);
        #endregion
    }
}