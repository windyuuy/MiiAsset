
namespace GameLib.MonoUtils
{
    public class LoomMG
    {
        public static MyLoom SharedLoom;

        public static void Init()
        {
            SharedLoom = SharedLoom ?? MyLoom.CreateOne();
        }
    }
}
