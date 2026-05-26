
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

namespace HatNetwork
{
    // TODO: compat app-v1.6.x 之后的版本强更前, 只能在prehatill中使用
    public static class HatLoaderUtils
    {
        public static void RemoveAddressablesCatalog()
        {
            var addDir = $"{Application.persistentDataPath}/com.unity.addressables";
            if (Directory.Exists(addDir))
            {
                Directory.Delete(addDir, true);
            }
        }

        public static string GetHLtLibDir()
        {
            return $"{Application.persistentDataPath}/HLtLib/";
        }
        public static string GetVersionTextPath()
        {
            var cachePath = $"{Application.persistentDataPath}/HLtLib/version.txt";
            return cachePath;
        }

        public static bool IsVersionTextExist()
        {
            return File.Exists(GetVersionTextPath());
        }

        private static bool? isVersionTextExistOverTime;
        public static bool IsVersionTextExistOverTime()
        {
            if (isVersionTextExistOverTime == null)
            {
                isVersionTextExistOverTime = File.Exists(GetVersionTextPath());
            }
            return isVersionTextExistOverTime.Value;
        }

        public static void ClearOldVersionCachesBefore(long versionCode)
        {
            var dirs=new DirectoryInfo(GetHLtLibDir()).GetDirectories();
            foreach (var dir in dirs)
            {
                if(long.TryParse(dir.Name,out var folderVersion))
                {
                    if (folderVersion < versionCode)
                    {
                        MyAddressablesUtils.ClearOldVersionCache(folderVersion);
                    }
                }
            }
        }

        public static Task<MyAddressablesUtils.GetVersionResp> GetRemoteVersionCode(MonoBehaviour comp)
        {
            return MyAddressablesUtils.GetRemoteVersionCode(comp);
        }
    }
}