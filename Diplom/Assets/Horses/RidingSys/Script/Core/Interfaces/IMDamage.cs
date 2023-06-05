using UnityEngine;
using RidingSystem.Controller.Reactions;

namespace RidingSystem
{
    public interface IMDamage
    {
        Vector3 HitDirection { get; set; }

        GameObject Damager { get; set; }

        GameObject Damagee { get; }

        void ReceiveDamage(Vector3 Direction, GameObject Damager, bool Default_react);
    }

    public interface IMDamager : IMLayer
    {
        int Index { get; }

        bool Active { get; set; }

        GameObject Owner { get; set; }

        void DoDamage(bool value);
    }

    public interface IMDamagerSet
    {
        void ActivateDamager(int ID);

        void UpdateDamagerSet();
    }
}