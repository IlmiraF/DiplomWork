using UnityEngine;

namespace RidingSystem
{
    public interface IMLayer
    {
        LayerMask Layer { get; set; }

        QueryTriggerInteraction TriggerInteraction { get; set; }
    }
}