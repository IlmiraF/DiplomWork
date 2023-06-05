namespace RidingSystem
{
    public interface IRider
    {
        bool IsRiding { get; }
        bool Mounted { get; }
        bool IsOnHorse { get; }
        bool CanMount { get; }
        bool CanDismount { get; }
        bool IsMounting { get; }

        bool IsDismounting { get; }

        bool IsAiming { get; set; }

        IInputSource MountInput { get; }
    }
}
