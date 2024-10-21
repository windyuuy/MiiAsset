namespace MiiAsset.Editor.CodeHintTemplate
{
	/// <summary>
	/// dep: bb, cc
	/// </summary>
	public sealed class TBundleAA
	{
#if UNITY_EDITOR
		public TBundleBB bb;
#endif
		public string Key = "aa";
	}

	public sealed class TBundleBB
	{
		public string Key = "bb";
	}

	public static class CodeHintTemplate
	{
		public static TBundleAA aa = new();
		public static TBundleBB bb = new();
	}
}