
# 代码热更配置方法

## 加依赖
"com.code-philosophy.hybridclr": "https://gitee.com/focus-creative-games/hybridclr_unity.git#4feac30cb2e105992986c737f7f54992b8300e1a",
"windy.miiasset.dotnethotloader": "https://gitee.com/windyuuy/MiiAsset.git?path=Packages/DotnetHotLoader#d0b737e43e23d537ad98d4196c7b529974ff8aab",

## 配置 HybridCLR
点菜单 HybridCLR/Installer 打开界面, 点击 Install
进Unity项目设置, 切 IL2CPP, .NET Framework, 并关闭 代码剔除()
增加宏 SUPPORT_HYBRIDCLR 以激活代码热更插件
