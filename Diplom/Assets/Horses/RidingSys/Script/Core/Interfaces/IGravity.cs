using UnityEngine;
namespace RidingSystem
{
    public interface IGravity
    {
        Vector3 Gravity { get; set; }
        Vector3 UpVector { get; }
    }
}