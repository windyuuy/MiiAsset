using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Adapter
{
	/// <summary>
	/// Options for the MiiAsset build platform.
	/// </summary>
	public enum MiiAssetPlatform
	{
		/// <summary>
		/// Use to indicate that the build platform is unknown.
		/// </summary>
		Unknown,

		/// <summary>
		/// Use to indicate that the build platform is Windows.
		/// </summary>
		Windows,

		/// <summary>
		/// Use to indicate that the build platform is OSX.
		/// </summary>
		OSX,

		/// <summary>
		/// Use to indicate that the build platform is Linux.
		/// </summary>
		Linux,

		/// <summary>
		/// Use to indicate that the build platform is PS4.
		/// </summary>
		PS4,

		/// <summary>
		/// Use to indicate that the build platform is PS4.
		/// </summary>
		Switch,

		/// <summary>
		/// Use to indicate that the build platform is XboxOne.
		/// </summary>
		XboxOne,

		/// <summary>
		/// Use to indicate that the build platform is WebGL.
		/// </summary>
		WebGL,

		/// <summary>
		/// Use to indicate that the build platform is iOS.
		/// </summary>
		iOS,

		/// <summary>
		/// Use to indicate that the build platform is Android.
		/// </summary>
		Android,

		/// <summary>
		/// Use to indicate that the build platform is WindowsUniversal.
		/// </summary>
		WindowsUniversal
	}

	public static class PlatformAdapter
	{
	#if UNITY_EDITOR
		internal static readonly Dictionary<BuildTarget, MiiAssetPlatform> s_BuildTargetMapping =
			new Dictionary<BuildTarget, MiiAssetPlatform>()
			{
				{ BuildTarget.XboxOne, MiiAssetPlatform.XboxOne },
				{ BuildTarget.Switch, MiiAssetPlatform.Switch },
				{ BuildTarget.PS4, MiiAssetPlatform.PS4 },
				{ BuildTarget.iOS, MiiAssetPlatform.iOS },
				{ BuildTarget.Android, MiiAssetPlatform.Android },
				{ BuildTarget.WebGL, MiiAssetPlatform.WebGL },
				{ BuildTarget.StandaloneWindows, MiiAssetPlatform.Windows },
				{ BuildTarget.StandaloneWindows64, MiiAssetPlatform.Windows },
				{ BuildTarget.StandaloneOSX, MiiAssetPlatform.OSX },
				{ BuildTarget.StandaloneLinux64, MiiAssetPlatform.Linux },
				{ BuildTarget.WSAPlayer, MiiAssetPlatform.WindowsUniversal },
			};
	#endif
		internal static readonly Dictionary<RuntimePlatform, MiiAssetPlatform> s_RuntimeTargetMapping =
			new Dictionary<RuntimePlatform, MiiAssetPlatform>()
			{
				{ RuntimePlatform.XboxOne, MiiAssetPlatform.XboxOne },
				{ RuntimePlatform.Switch, MiiAssetPlatform.Switch },
				{ RuntimePlatform.PS4, MiiAssetPlatform.PS4 },
				{ RuntimePlatform.IPhonePlayer, MiiAssetPlatform.iOS },
				{ RuntimePlatform.Android, MiiAssetPlatform.Android },
				{ RuntimePlatform.WebGLPlayer, MiiAssetPlatform.WebGL },
				{ RuntimePlatform.WindowsPlayer, MiiAssetPlatform.Windows },
				{ RuntimePlatform.OSXPlayer, MiiAssetPlatform.OSX },
				{ RuntimePlatform.LinuxPlayer, MiiAssetPlatform.Linux },
				{ RuntimePlatform.WindowsEditor, MiiAssetPlatform.Windows },
				{ RuntimePlatform.OSXEditor, MiiAssetPlatform.OSX },
				{ RuntimePlatform.LinuxEditor, MiiAssetPlatform.Linux },
				{ RuntimePlatform.WSAPlayerARM, MiiAssetPlatform.WindowsUniversal },
				{ RuntimePlatform.WSAPlayerX64, MiiAssetPlatform.WindowsUniversal },
				{ RuntimePlatform.WSAPlayerX86, MiiAssetPlatform.WindowsUniversal },
			};

	#if UNITY_EDITOR
		internal static MiiAssetPlatform MiiAssetPlatformInternal(BuildTarget target)
		{
			if (s_BuildTargetMapping.ContainsKey(target))
				return s_BuildTargetMapping[target];
			return MiiAssetPlatform.Unknown;
		}

		internal static string MiiAssetPlatformPathInternal(BuildTarget target)
		{
			if (s_BuildTargetMapping.ContainsKey(target))
				return s_BuildTargetMapping[target].ToString();
			return target.ToString();
		}

	#endif
		internal static MiiAssetPlatform MiiAssetPlatformInternal(RuntimePlatform platform)
		{
			if (s_RuntimeTargetMapping.ContainsKey(platform))
				return s_RuntimeTargetMapping[platform];
			return MiiAssetPlatform.Unknown;
		}

		internal static string MiiAssetPlatformPathInternal(RuntimePlatform platform)
		{
			if (s_RuntimeTargetMapping.ContainsKey(platform))
				return s_RuntimeTargetMapping[platform].ToString();
			return platform.ToString();
		}

		public static string GetBuildTargetSubFolder(BuildTarget target)
		{
			return MiiAssetPlatformPathInternal(EditorUserBuildSettings.activeBuildTarget);
		}

		public static string GetRuntimeSubFolder()
		{
			return MiiAssetPlatformPathInternal(Application.platform);
		}

		public static string GetPlatformPathSubFolder()
		{
		#if UNITY_EDITOR
			return MiiAssetPlatformPathInternal(EditorUserBuildSettings.activeBuildTarget);
		#else
            return MiiAssetPlatformPathInternal(Application.platform);
		#endif
		}
	}
}