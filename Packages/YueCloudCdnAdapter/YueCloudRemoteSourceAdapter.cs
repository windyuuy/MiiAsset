using System;
using System.Diagnostics;
using System.Net;
using System.Text.RegularExpressions;
using MiiAsset.Runtime.Adapter;

namespace YueCloudCdnAdapter
{
	public class YueCloudRemoteSourceAdapter : IRemoteSourceAdapter
	{
		private readonly string PKey;

		public YueCloudRemoteSourceAdapter(string pKey)
		{
			PKey = pKey;
		}

		private static readonly Regex RegionRegex = new Regex(@"https?\://[^/]+(/.+)");

		public string ConvertRemoteUri(string remoteBaseUri, string fileName)
		{
			var match = RegionRegex.Match(remoteBaseUri);
			if (match.Success)
			{
				var pKey = PKey;
				var keyPart = match.Groups[1].Value;
				return ToHashedUri(remoteBaseUri, keyPart, fileName, pKey, DateTime.Now);
			}
			else
			{
				MyLogger.LogError($"invalid url format: {remoteBaseUri}");
				return remoteBaseUri + fileName;
			}
		}

		// [MenuItem("Tools/Test/TestHashUrl")]
		[Conditional("UNITY_EDITOR")]
		static void Test()
		{
			var url = ToHashedUri("http://test.com/", "/", "test.jpg", "m85elvswafo1zbdygdtmqru2mdgsv7e4",
				DateTime.Parse("2020/02/27 16:10:32").ToUniversalTime());
			MyLogger.Log(url);
		}

		private static string ToHashedUri(string remoteBaseUri, string keyPart, string fileName, string pKey,
			DateTime dateTime)
		{
			// var timestamp = (Date.Now() / 1000).ToString("x");
			// var timestamp = ((long)(DateTime.Parse("2020/02/27 16:10:32").ToUniversalTime() -
			//                         DateTime.Parse("1970/01/01 00:00:00")).TotalSeconds).ToString("x");
			var timestamp = ((long)(dateTime - new DateTime(1970, 01, 01, 0, 0, 0)).TotalSeconds).ToString("x");
			// pKey = "m85elvswafo1zbdygdtmqru2mdgsv7e4";//test
			var fileName2 = WebUtility.UrlEncode(fileName);
			var pkeyuritimestamp = $"{pKey}{keyPart}{fileName2}{timestamp}";
			// var pkeyuritimestamp = $"{pKey}/test.jpg{timestamp}";
			if (MonoUtils.Encrypt.Md5Utils.TryComputeMd5Hash4096(pkeyuritimestamp, out var md5hash))
			{
				var uri = $"{remoteBaseUri}{fileName2}?sign={md5hash}&t={timestamp}";
				return uri;
			}
			else
			{
				MyLogger.LogError($"compute hash failed: {remoteBaseUri}, {timestamp}");
				return remoteBaseUri + fileName;
			}
		}
	}
}