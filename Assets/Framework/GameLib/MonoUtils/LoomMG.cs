
namespace MonoExtLib.Loom
{
    public class LoomMG
	{
		private static MyLoom _sharedLoom;

		public static MyLoom SharedLoom
		{
			get
			{
				if (_sharedLoom == null)
				{
					_sharedLoom = MyLoom.CreateOne();
				}

				return _sharedLoom;
			}
		}

		public static void Init()
		{
		}
	}
}
