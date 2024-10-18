using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using UnityEngine;

namespace MiiAsset.Runtime.AssetUtils
{
	public static class AssetHelper
	{
		public static string GetInternalBuildPath()
		{
#if UNITY_EDITOR
			var internalBaseUri = Path.GetFullPath(Application.dataPath + "/../Library/MiiAssets/mii/").Replace("\\", "/");
			if (!Directory.Exists(internalBaseUri))
			{
				Directory.CreateDirectory(internalBaseUri);
			}
#else
			var internalBaseUri = Application.dataPath + "/mii/";
#endif
			return internalBaseUri;
		}

		public static async Task<string> LoadCompressedCatalog(Stream stream)
		{
			using var zipArchive = new ZipArchive(stream, ZipArchiveMode.Read);
			var entry = zipArchive.GetEntry("catalog.json");
			Debug.Assert(entry != null, "entry!=null");
			using var streamReader = new StreamReader(entry.Open());
			var text = await streamReader.ReadToEndAsync();
			return text;
		}

#if UNITY_EDITOR
		public static string GetBuildTarget(UnityEditor.BuildTarget buildTarget)
		{
			var name = buildTarget switch
			{
				UnityEditor.BuildTarget.WebGL => "WebGL",
				UnityEditor.BuildTarget.Android => "Android",
				UnityEditor.BuildTarget.StandaloneWindows => "Win32",
				UnityEditor.BuildTarget.StandaloneWindows64 => "Win32",
				UnityEditor.BuildTarget.iOS => "IOS",
				UnityEditor.BuildTarget.StandaloneOSX => "OSX",
				UnityEditor.BuildTarget.EmbeddedLinux => "Linux",
				UnityEditor.BuildTarget.LinuxHeadlessSimulation => "Linux",
				UnityEditor.BuildTarget.StandaloneLinux64 => "Linux",
				_ => Application.platform.ToString(),
			};
			return name;
		}
#endif

		public static string GetRuntimeTarget()
		{
			var name = Application.platform switch
			{
				RuntimePlatform.WebGLPlayer => "WebGL",
				RuntimePlatform.Android => "Android",
				RuntimePlatform.WindowsPlayer => "Win32",
				RuntimePlatform.WindowsEditor => "Win32",
				RuntimePlatform.IPhonePlayer => "IOS",
				RuntimePlatform.OSXPlayer => "IOS",
				RuntimePlatform.OSXEditor => "IOS",
				RuntimePlatform.LinuxPlayer => "Linux",
				RuntimePlatform.LinuxEditor => "Linux",
				RuntimePlatform.LinuxServer => "Linux",
				RuntimePlatform.EmbeddedLinuxArm32 => "Linux",
				RuntimePlatform.EmbeddedLinuxArm64 => "Linux",
				RuntimePlatform.EmbeddedLinuxX64 => "Linux",
				RuntimePlatform.EmbeddedLinuxX86 => "Linux",
				_ => Application.platform.ToString(),
			};
			return name;
		}
	}
}