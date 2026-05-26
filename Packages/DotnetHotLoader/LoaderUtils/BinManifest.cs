using System;

namespace HatNetwork
{
	[Serializable]
	public class BinManifest
	{
		[Serializable]
		public class FileInfo
		{
			public string name;
			public string hash;

			public string DisplayName{
				get{
					return name.Replace(".dll",".egg");
				}
			}
		}
		public long versionCode;
		public long hotSize=0;
		public string ResUpdateTunnel;
		public FileInfo[] aotDlls;
		public FileInfo[] hotDlls;
	}
}
