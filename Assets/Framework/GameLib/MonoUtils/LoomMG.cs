
namespace GameLib.MonoUtils
{
    public class LoomMG
    {
        public static MyLoom sharedLoom;

        public static void Init()
        {
            sharedLoom = sharedLoom ?? MyLoom.CreateOne();
        }
    }
}
