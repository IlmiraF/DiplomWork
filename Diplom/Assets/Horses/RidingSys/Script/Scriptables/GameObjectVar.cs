using System;
using UnityEngine;

namespace RidingSystem.Scriptables
{
    [CreateAssetMenu(menuName = "RidingSystem/Variables/Game Object", order = 3000)]
    public class GameObjectVar : ScriptableVar
    {
        [SerializeField] private GameObject value;

        public Action<GameObject> OnValueChanged;

        public virtual GameObject Value
        {
            get => value;
            set
            {
                this.value = value;
                OnValueChanged?.Invoke(value);
            }
        }

        public virtual void SetValue(GameObjectVar var) => Value = var.Value;
        public virtual void SetNull(GameObjectVar var) => Value = null;
        public virtual void SetValue(GameObject var) => Value = var;
        public virtual void SetValue(Component var) => Value = var.gameObject;

    }

    [System.Serializable]
    public class GameObjectReference
    {
        public bool UseConstant = true;

        public GameObject ConstantValue;
        [RequiredField] public GameObjectVar Variable;

        public GameObjectReference() => UseConstant = true;
        public GameObjectReference(GameObject value) => Value = value;

        public GameObjectReference(GameObjectVar value)
        {
            Variable = value;
            UseConstant = false;
        }

        public GameObject Value
        {
            get => UseConstant ? ConstantValue : (Variable != null ? Variable.Value : null);
            set
            {
                if (UseConstant || Variable == null)
                {
                    ConstantValue = value;
                    UseConstant = true;
                }
                else
                {
                    Variable.Value = value;
                }
            }
        }
    }
}