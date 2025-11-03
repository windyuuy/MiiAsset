using System.Collections.Generic;
using UnityEngine;

namespace MiiAsset.Runtime.Adapter
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
		internal static readonly Dictionary<UnityEditor.BuildTarget, MiiAssetPlatform> s_BuildTargetMapping =
			new Dictionary<UnityEditor.BuildTarget, MiiAssetPlatform>()
			{
				{ UnityEditor.BuildTarget.XboxOne, MiiAssetPlatform.XboxOne },
				{ UnityEditor.BuildTarget.Switch, MiiAssetPlatform.Switch },
				{ UnityEditor.BuildTarget.PS4, MiiAssetPlatform.PS4 },
				{ UnityEditor.BuildTarget.iOS, MiiAssetPlatform.iOS },
				{ UnityEditor.BuildTarget.Android, MiiAssetPlatform.Android },
				{ UnityEditor.BuildTarget.WebGL, MiiAssetPlatform.WebGL },
				{ UnityEditor.BuildTarget.StandaloneWindows, MiiAssetPlatform.Windows },
				{ UnityEditor.BuildTarget.StandaloneWindows64, MiiAssetPlatform.Windows },
				{ UnityEditor.BuildTarget.StandaloneOSX, MiiAssetPlatform.OSX },
				{ UnityEditor.BuildTarget.StandaloneLinux64, MiiAssetPlatform.Linux },
				{ UnityEditor.BuildTarget.WSAPlayer, MiiAssetPlatform.WindowsUniversal },
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
		internal static MiiAssetPlatform MiiAssetPlatformInternal(UnityEditor.BuildTarget target)
		{
			if (s_BuildTargetMapping.ContainsKey(target))
				return s_BuildTargetMapping[target];
			return MiiAssetPlatform.Unknown;
		}

		internal static string MiiAssetPlatformPathInternal(UnityEditor.BuildTarget target)
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

	#if UNITY_EDITOR
		public static string GetBuildTargetSubFolder(UnityEditor.BuildTarget target)
		{
			return MiiAssetPlatformPathInternal(UnityEditor.EditorUserBuildSettings.activeBuildTarget);
		}
	#endif

		public static string GetRuntimeSubFolder()
		{
			return MiiAssetPlatformPathInternal(Application.platform);
		}

		public static string GetPlatformPathSubFolder()
		{
		#if UNITY_EDITOR
			return MiiAssetPlatformPathInternal(UnityEditor.EditorUserBuildSettings.activeBuildTarget);
		#else
            return MiiAssetPlatformPathInternal(Application.platform);
		#endif
		}
	}
}