namespace RidingSystem
{
    [System.Serializable]
    public struct RangedFloat
    {
        public float minValue;
        public float maxValue;

        public RangedFloat(float minValue, float maxValue)
        {
            this.minValue = minValue;
            this.maxValue = maxValue;
        }

        public float RandomValue => UnityEngine.Random.Range(minValue, maxValue);

        public bool IsInRange(float value) => value >= minValue && value <= maxValue;
    }
}