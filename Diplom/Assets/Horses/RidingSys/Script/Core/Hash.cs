using UnityEngine;

namespace RidingSystem
{
    public static class Hash
    {
        public static readonly int Vertical = Animator.StringToHash("Vertical");
        public static readonly int Horizontal = Animator.StringToHash("Horizontal");
        public static readonly int UpDown = Animator.StringToHash("UpDown");

        public static readonly int Stand = Animator.StringToHash("Stand");
        public static readonly int Grounded = Animator.StringToHash("Grounded");

        public static readonly int Jump = Animator.StringToHash("Jump");

        public static readonly int Dodge = Animator.StringToHash("Dodge");
        public static readonly int Fall = Animator.StringToHash("Fall");
        public static readonly int Type = Animator.StringToHash("Type");


        public static readonly int Slope = Animator.StringToHash("Slope");

        public static readonly int Shift = Animator.StringToHash("Shift");

        public static readonly int Fly = Animator.StringToHash("Fly");
        public static readonly int Locomotion = Animator.StringToHash("Locomotion");

        public static readonly int Attack1 = Animator.StringToHash("Attack1");
        public static readonly int Attack2 = Animator.StringToHash("Attack2");

        public static readonly int Death = Animator.StringToHash("Death");

        public static readonly int Damaged = Animator.StringToHash("Damaged");
        public static readonly int Stunned = Animator.StringToHash("Stunned");

        public static readonly int IDInt = Animator.StringToHash("IDInt");
        public static readonly int IDFloat = Animator.StringToHash("IDFloat");

        public static readonly int Swim = Animator.StringToHash("Swim");
        public static readonly int Underwater = Animator.StringToHash("Underwater");

        public static readonly int IDAction = Animator.StringToHash("IDAction");
        public static readonly int Action = Animator.StringToHash("Action");


        public static readonly int Null = Animator.StringToHash("Null");
        public static readonly int Empty = Animator.StringToHash("Empty");


        public static readonly int State = Animator.StringToHash("State");
        public static readonly int Stance = Animator.StringToHash("Stance");
        public static readonly int Mode = Animator.StringToHash("Mode");
        public static readonly int StateTime = Animator.StringToHash("StateTime");

    }

    public static class Int_ID
    {
        public readonly static int Available = 0;
        public readonly static int Interrupted = -2;
        public readonly static int Loop = -1;
        public readonly static int OneTime = 1;
        public readonly static int AllowExit = 1;
    }

    public static class ModeEnum
    {
        public readonly static int Attack1 = 1;
        public readonly static int Attack2 = 2;
        public readonly static int Damage = 3;
        public readonly static int Action = 4;
        public readonly static int Dodge = 5;
        public readonly static int Attack1Air = 6;
        public readonly static int PickUp = 7;
    }

    public static class StateEnum
    {
        public readonly static int Idle = 0;
        public readonly static int Locomotion = 1;
        public readonly static int Jump = 2;
        public readonly static int Fall = 3;
        public readonly static int Swim = 4;
        public readonly static int UnderWater = 5;
        public readonly static int Fly = 6;
        public readonly static int Climb = 7;
        public readonly static int Slide = 8;
        public readonly static int Death = 10;
    }

    public static class StanceEnum
    {
        public readonly static int Default = 0;
        public readonly static int Sneak = 1;
        public readonly static int Combat = 2;
        public readonly static int Wounded = 3;
        public readonly static int Stand = 4;
        public readonly static int Strafe = 5;
        public readonly static int Roll = 6;
        public readonly static int Crouch = 7;
    }
}