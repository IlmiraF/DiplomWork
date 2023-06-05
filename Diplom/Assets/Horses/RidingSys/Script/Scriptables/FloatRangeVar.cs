using System;
using UnityEngine;

namespace RidingSystem.Scriptables
{
    [CreateAssetMenu(menuName = "RidingSystem/Variables/Float Range", order = 1000)]
    public class FloatRangeVar : FloatVar
    {
        public FloatReference minValue;
        public FloatReference maxValue;

        public override float Value
        {
            get => UnityEngine.Random.Range(minValue, maxValue);
            set {}
        }
    }
}