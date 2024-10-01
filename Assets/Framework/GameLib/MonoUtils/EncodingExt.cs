using System.Text;

namespace Lang.Encoding
{
	public static class EncodingExt
	{
		public static UTF8Encoding UTF8WithoutBom = new UTF8Encoding(false);
	}
}