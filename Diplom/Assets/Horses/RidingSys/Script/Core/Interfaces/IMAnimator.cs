using System;

namespace RidingSystem
{
    public interface IMAnimator
    {
        Action<int, bool> SetBoolParameter { get; set; }
        Action<int, float> SetFloatParameter { get; set; }

        Action<int, int> SetIntParameter { get; set; }

        Action<int> SetTriggerParameter { get; set; }

        void SetAnimParameter(int hash, int value);

        void SetAnimParameter(int hash, float value);

        void SetAnimParameter(int hash, bool value);

        void SetAnimParameter(int hash);
    }


    public interface IAnimatorListener
    {
        bool OnAnimatorBehaviourMessage(string message, object value);
    }

    public interface IAnimatorStateCycle
    {
        System.Action<int> StateCycle { get; set; }
    }
}