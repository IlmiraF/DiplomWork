using UnityEngine;

namespace RidingSystem
{
    public interface IAIControl
    {
        Transform Transform { get; }

        Transform Target { get; set; }

        Vector3 DestinationPosition { get; set; }

        Vector3 AIDirection { get; set; }

        IAITarget IsAITarget { get; set; }

        Vector3 GetTargetPosition();

        float StoppingDistance { get; set; }

        void ResetStoppingDistance();


        float Height { get; }


        float SlowingDistance { get; }

        float RemainingDistance { get; set; }

        void SetTarget(Transform target, bool move);

        void ClearTarget();

        void MovetoNextTarget();

        void SetDestination(Vector3 PositionTarget, bool move);

        void Stop();

        void StopWait();

        void Move();

        void SetActive(bool value);

        bool HasArrived { get; set; }

        bool InOffMeshLink { get; set; }

        void CompleteOffMeshLink();

        bool TargetIsMoving { get; }

        bool AutoNextTarget { get; set; }

        bool Active { get; }

        bool LookAtTargetOnArrival { get; set; }

        bool UpdateDestinationPosition { get; set; }

        RidingSystem.Events.TransformEvent TargetSet { get; }

        RidingSystem.Events.TransformEvent OnArrived { get; }
    }

    public interface IAITarget
    {
        float StopDistance();

        float Height { get; }

        bool ArriveLookAt { get; }

        float SlowDistance();

        Vector3 GetPosition();

        WayPointType TargetType { get; }

        void TargetArrived(GameObject target);
    }
}