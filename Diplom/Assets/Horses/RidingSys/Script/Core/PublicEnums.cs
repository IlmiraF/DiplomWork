namespace RidingSystem
{
    public enum InputType { Input, Key }

    public enum WayPointType { Ground, Air, Water, Underwater }

    public enum FieldColor { Red, Green, Blue, Magenta, Cyan, Yellow, Orange, Gray }

    public enum InputButton { Press = 0, Down = 1, Up = 2, LongPress = 3, DoubleTap = 4, Toggle = 5 }

    public enum StateTransition { First = 0, Last = 1 }

    public enum LoopType { Once, PingPong, Repeat }

    public enum TransformRelative { Local = 0, World = 1, }

    public enum ComparerInt { Equal = 0, Greater = 1, Less = 2, NotEqual = 3, }

    public enum ComparerBool { Equal = 0, NotEqual = 1, }

    public enum ComparerString { Equal = 0, NotEqual = 1, Empty = 2, }

    public enum EEnterExit { Enter = 1, Exit = 2, }

    public enum AxisDirection { None, Right, Left, Up, Down, Forward, Backward }

    public enum IncludeExclude { Include, Exclude, }


    public enum TypeMessage
    {
        Bool = 0,
        Int = 1,
        Float = 2,
        String = 3,
        Void = 4,
        IntVar = 5,
        Transform = 6,
        GameObject = 7,
        Component = 8,
    }

    public enum MStatus
    {
        None = 0,
        Prepared = 1,
        Playing = 2,
        Completed = 3,
        Interrupted = 4,
        ForceExit = 5,
    }



    [System.Flags]
    public enum UpdateMode
    {
        Update = 1,
        FixedUpdate = 2,
        LateUpdate = 4,
    }

    public enum UpdateType
    {
        FixedUpdate,
        LateUpdate,
    }

    public enum AimSide { None = 0, Left = 1, Right = 2 }

    public static class WSound
    {
        public static int Equip => 0;
        public static int Store => 1;
        public static int Fire => 2;
        public static int Reload => 3;
        public static int Empty => 4;
        public static int Charge => 5;
    }

    public static class WA
    {
        public static int None => 0;

        public static int Idle => 100;

        public static int Fire_Projectile = 101;

        public static int Release = 101;

        public static int Draw => 99;

        public static int Store => 98;

        public static int Aim => 97;

        public static int Reload => 96;

        public static int Preparing => 95;
        public static int Ready => 95;


        public static string WValue(int v)
        {
            switch (v)
            {
                case 0: return "None";
                case 95: return "Ready|Preparing";
                case 96: return "Reload";
                case 97: return "Aim";
                case 98: return "Store";
                case 99: return "Draw";
                case 100: return "Idle";
                case 101: return "Fire_Projectile";
                default: return v.ToString(); ;
            }
        }
    }
}