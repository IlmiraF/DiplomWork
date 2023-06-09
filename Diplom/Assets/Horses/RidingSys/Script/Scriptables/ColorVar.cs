using UnityEngine;

namespace RidingSystem.Scriptables
{
    [CreateAssetMenu(menuName = "RidingSystem/Variables/Color", order = 2000)]
    public class ColorVar : ScriptableVar
    {
        [SerializeField] private Color value = Color.white;

        public virtual Color Value
        {
            get => value;
            set
            {
                this.value = value;
            }
        }

        public virtual void SetValue(ColorVar var) => Value = var.Value;

        public static implicit operator Color(ColorVar reference) => reference.Value;
    }

    [System.Serializable]
    public class ColorReference
    {
        public bool UseConstant = true;

        public Color ConstantValue = Color.white;
        public ColorVar Variable;

        public ColorReference()
        {
            UseConstant = true;
            ConstantValue = Color.white;
        }

        public ColorReference(bool variable = false)
        {
            UseConstant = !variable;

            if (!variable)
            {
                ConstantValue = Color.white;
            }
            else
            {
                Variable = ScriptableObject.CreateInstance<ColorVar>();
                Variable.Value = Color.white;
            }
        }

        public ColorReference(Color value) => Value = value;

        public Color Value
        {
            get => UseConstant ? ConstantValue : Variable.Value;
            set
            {
                if (UseConstant)
                    ConstantValue = value;
                else
                    Variable.Value = value;
            }
        }

        #region Operators
        public static implicit operator Color(ColorReference reference) => reference.Value;
        #endregion
    }
}