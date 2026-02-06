// --------------------------------------------------------------------------------------------------------------------
// <copyright file="RemoveMobileSupportWarningWebBuild.cs">
//   Copyright (c) 2021 Johannes Deml. All rights reserved.
// </copyright>
// <author>
//   Johannes Deml
//   public@deml.io
// </author>
// --------------------------------------------------------------------------------------------------------------------

#if UNITY_WEBGL //Not needed anymore in 2020 and above
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace MiiAsset.Editor
{
	/// <summary>
	/// removes a warning popup for mobile builds, that this platform might not be supported:
	/// "Please note that Unity WebGL is not currently supported on mobiles. Press OK if you wish to continue anyway."
	/// </summary>
	public class RemoveMobileSupportWarningWebBuild
	{
		[PostProcessBuild]
		public static void OnPostProcessBuild(BuildTarget target, string targetPath)
		{
			if (target != BuildTarget.WebGL)
			{
				return;
			}

		#if !UNITY_2020_1_OR_NEWER
			var buildFolderPath = Path.Combine(targetPath, "Build");
			var info = new DirectoryInfo(buildFolderPath);
			var files = info.GetFiles("*.js");
		#else
			var info = new DirectoryInfo(targetPath);
			var files = info.GetFiles("index.html");
		#endif
			for (int i = 0; i < files.Length; i++)
			{
				var file = files[i];
				var filePath = file.FullName;
				var text = File.ReadAllText(filePath, Encoding.UTF8);
				var text2 = text.Replace("    unityShowBanner(", "    // unityShowBanner(");

				if (text != text2)
				{
					Debug.Log("Removing mobile warning from " + filePath);
					File.WriteAllText(filePath, text2, Encoding.UTF8);
				}
			}
		}
	}
}
#endif