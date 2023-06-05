namespace RidingSystem
{
    public interface ITag
    {
        bool HasTag(Tag tag);

        bool HasTag(int key);
    }
}