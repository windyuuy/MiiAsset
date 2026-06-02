
# 代码热更配置方法

## 加依赖
"com.code-philosophy.hybridclr": "https://gitee.com/focus-creative-games/hybridclr_unity.git#4feac30cb2e105992986c737f7f54992b8300e1a",
"windy.miiasset.dotnethotloader": "https://gitee.com/windyuuy/MiiAsset.git?path=Packages/DotnetHotLoader#d0b737e43e23d537ad98d4196c7b529974ff8aab",

## 配置 HybridCLR
点菜单 HybridCLR/Installer 打开界面, 点击 Install
进Unity项目设置, 切 IL2CPP, .NET Framework, 并关闭 代码剔除()
增加宏 SUPPORT_HYBRIDCLR 以激活代码热更插件
复制 CLRBuildConfig.asset 文件到项目中合适位置(可无需打入包内)

## 自定义构建流程
如果需要自行构建代码热更包, 调用 MiiAsset.Editor.Build.MiiBuildTool.BuildCodeBundles(), 输出路径为 `AssetBundles/${Target}/` , 也就是和普通资源热更包在统一路径下。热更包文件名为 `default_hatill` 和 `default_prehatill`

