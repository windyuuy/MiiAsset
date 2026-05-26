namespace HatNetwork
{
	public class HatUpdateState
	{
		public static readonly HatUpdateState Inst = new HatUpdateState();

		/// <summary>
		/// 是否允许加载脚本(不代表是否允许在线更新脚本)
		/// </summary>
		public bool IsHotScriptEnabled = false;
		/// <summary>
		/// 单位 bytes
		/// </summary>
		public long HotUpdateSize = 0;
		/// <summary>
		/// 更新前, 本地资源/脚本版本号
		/// </summary>
		public long LocalVersionCode = -1;
		/// <summary>
		/// 远程资源版本号
		/// </summary>
		public long RemoteVersionCode = -1;
		public string RemoteResourceUrl = null;
		public bool IsPreRemoteLoaded = false;

		public bool IsNeedRestart = true;

		public bool IsNeedRestartIndeed = false;

		/// <summary>
		/// 保留字段, 供极端情况扩展
		/// </summary>
		public string CustomInfoStr = "";

		public bool IsRemoteVersionCodeValid => RemoteVersionCode > 0;
	}
}
