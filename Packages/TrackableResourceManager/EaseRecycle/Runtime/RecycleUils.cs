using System;

namespace EaseRecycle
{
	public class RecycleUils
	{
		public static string GenUID(string baseName)
		{
			return $"{baseName}-{Guid.NewGuid()}";
		}
	}
}