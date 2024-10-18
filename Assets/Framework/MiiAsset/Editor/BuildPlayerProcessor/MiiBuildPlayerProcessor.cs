using System.IO;
using System.Linq;
using MiiAsset.Runtime.AssetUtils;
using MiiAsset.Editor.Build;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace MiiAsset.Editor.BuildPlayerProcessor
{
	public class MiiBuildPlayerProcessor : UnityEditor.Build.BuildPlayerProcessor,IPostprocessBuildWithReport
	{
		/// <summary>
		/// Returns the player build processor callback order.
		/// </summary>
		public override int callbackOrder
		{
			get { return 1; }
		}

		public override void PrepareForBuild(BuildPlayerContext buildPlayerContext)
		{
			MiiBuildTool.BuildAssetBundlesWithPathInfo();

			var internalBuildPath = AssetHelper.GetInternalBuildPath();
			buildPlayerContext.AddAdditionalPathToStreamingAssets(internalBuildPath, "mii");

			LoadLink();
		}

		public static void LoadLink()
		{
			var internalBuildPath = AssetHelper.GetInternalBuildPath();
			string buildPath = internalBuildPath + "/Link/link.xml";
			if (File.Exists(buildPath))
			{
				string projectPath = GetLinkPath(true);
				File.Copy(buildPath, projectPath, true);
				AssetDatabase.ImportAsset(projectPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.DontDownloadFromCacheServer);
			}
		}

		[InitializeOnLoadMethod]
		private static void CleanTemporaryPlayerBuildData()
		{
			RemovePlayerBuildLinkXML();
		}

		internal static void RemovePlayerBuildLinkXML()
		{
			string linkProjectPath = GetLinkPath(false);
			string guid = AssetDatabase.AssetPathToGUID(linkProjectPath);
			if (!string.IsNullOrEmpty(guid))
				AssetDatabase.DeleteAsset(linkProjectPath);
			else if (File.Exists(linkProjectPath))
				File.Delete(linkProjectPath);

			DeleteDirectory(Path.GetDirectoryName(linkProjectPath));
		}

		internal static void DeleteDirectory(string directoryPath, bool onlyIfEmpty = true, bool recursiveDelete = true)
		{
			if (!Directory.Exists(directoryPath))
				return;

			bool isEmpty = !Directory.EnumerateFiles(directoryPath, "*", SearchOption.AllDirectories).Any()
			               && !Directory.EnumerateDirectories(directoryPath, "*", SearchOption.AllDirectories).Any();
			if (!onlyIfEmpty || isEmpty)
			{
				// check if the folder is valid in the AssetDatabase before deleting through standard file system
				string relativePath = directoryPath.Replace("\\", "/").Replace(Application.dataPath, "Assets");
				if (AssetDatabase.IsValidFolder(relativePath))
					AssetDatabase.DeleteAsset(relativePath);
				else
					Directory.Delete(directoryPath, recursiveDelete);
			}
		}

		private static string GetLinkPath(bool createFolder)
		{
			string folderPath;
			folderPath = "Assets/MiiData";

			if (createFolder && !Directory.Exists(folderPath))
			{
				Directory.CreateDirectory(folderPath);
				AssetDatabase.ImportAsset(folderPath);
			}

			return Path.Combine(folderPath, "link.xml");
			;
		}

		public void OnPostprocessBuild(BuildReport report)
		{
			RemovePlayerBuildLinkXML();
		}
	}
}