using UnityEngine;

namespace RidingSystem
{
    public interface IWayPoint : IAITarget
    {
        Transform NextTarget();

        Transform WPTransform { get; }

        float WaitTime { get; }
    }
}