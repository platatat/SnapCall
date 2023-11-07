namespace SnapCall
{
    public interface IDeck
    {
        ulong Draw(int count);
        void RefillAndShuffle();
    }
}