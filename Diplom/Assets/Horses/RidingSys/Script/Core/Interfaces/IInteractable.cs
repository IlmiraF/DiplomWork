using RidingSystem.Events;
using UnityEngine;

namespace RidingSystem
{
    public interface IInteractable
    {
        void Restart();

        bool Interact(IInteractor interactor);

        bool Interact(int InteracterID, GameObject interactor);

        void Interact();

        bool SingleInteraction { get; }

        bool Active { get; set; }

        bool Auto { get; set; }

        bool Focused { get; set; }

        IInteractor CurrentInteractor { get; set; }

        int Index { get; }

        GameObject Owner { get; }
    }

    public interface IInteractor
    {
        int ID { get; }

        bool Active { get; set; }

        GameObject Owner { get; }

        bool Interact(IInteractable interactable);

        void UnFocus(IInteractable interactable);

        void Focus(IInteractable interactable);
        void Restart();
    }

    [System.Serializable]
    public class InteractionEvents
    {
        public GameObjectEvent OnInteractWithGO = new GameObjectEvent();
        public IntEvent OnInteractWith = new IntEvent();
    }

    public interface ICharacterAction
    {
        bool PlayAction(int Set, int Index);

        bool ForceAction(int Set, int Index);

        bool IsPlayingAction { get; }
    }
}