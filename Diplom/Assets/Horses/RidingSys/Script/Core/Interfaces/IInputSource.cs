using RidingSystem.Events;
using UnityEngine;

namespace RidingSystem
{
    public interface IInputSource
    {
        void Enable(bool val);

        bool MoveCharacter { get; set; }

        IInputAction GetInput(string input);
        void EnableInput(string input);
        void DisableInput(string input);

        void SetInput(string input, bool value);
    }

    public interface IInputAction
    {
        bool Active { get; set; }

        bool GetValue { get; }

        string Name { get; }

        BoolEvent InputChanged { get; }
    }



    public interface IInputSystem
    {
        float GetAxis(string Axis);
        float GetAxisRaw(string Axis);
        bool GetButtonDown(string button);
        bool GetButtonUp(string button);
        bool GetButton(string button);
    }

    public interface ICharacterMove
    {
        void Move(Vector3 move);

        void SetInputAxis(Vector3 inputAxis);

        void SetInputAxis(Vector2 inputAxis);
    }


    public class DefaultInput : IInputSystem
    {
        public float GetAxis(string Axis) => Input.GetAxis(Axis);

        public float GetAxisRaw(string Axis) => Input.GetAxisRaw(Axis);

        public bool GetButton(string button) => Input.GetButton(button);

        public bool GetButtonDown(string button) => Input.GetButtonDown(button);

        public bool GetButtonUp(string button) => Input.GetButtonUp(button);

        public static IInputSystem GetInputSystem(string PlayerID = "")
        {
            IInputSystem Input_System = null;
            Input_System = new DefaultInput();

#if REWIRED
           Rewired.Player player = Rewired.ReInput.players.GetPlayer(PlayerID);
            if (player != null)
                Input_System = new RewiredInput(player);
            else
                Debug.LogError("NO REWIRED PLAYER WITH THE ID:" + PlayerID + " was found");
            return Input_System;
#endif
#if OOTII_EI
            Input_System = new EasyInput();
#endif
            return Input_System;
        }
    }

#if REWIRED
    public class RewiredInput : IInputSystem
    {
        Rewired.Player player;

        public float GetAxis(string Axis)
        {
            return player.GetAxis(Axis);
        }

        public float GetAxisRaw(string Axis)
        {
            return player.GetAxisRaw(Axis);
        }

        public bool GetButton(string button)
        {
            return player.GetButton(button);
        }

        public bool GetButtonDown(string button)
        {
            return player.GetButtonDown(button);
        }

        public bool GetButtonUp(string button)
        {
            return player.GetButtonUp(button);
        }

        public RewiredInput(Rewired.Player player)
        {
            this.player = player;
        }
    }
#endif

#if OOTII_EI
    public class EasyInput : IInputSystem
    {
        public float GetAxis(string Axis)
        {
            return com.ootii.Input.InputManager.GetValue(Axis);
        }

        public float GetAxisRaw(string Axis)
        {
           return GetAxis(Axis);
        }

        public bool GetButton(string button)
        {
            return com.ootii.Input.InputManager.IsPressed(button);
        }

        public bool GetButtonDown(string button)
        {
            return com.ootii.Input.InputManager.IsJustPressed(button);
        }

        public bool GetButtonUp(string button)
        {
            return com.ootii.Input.InputManager.IsJustReleased(button);
        }
    }
#endif

}