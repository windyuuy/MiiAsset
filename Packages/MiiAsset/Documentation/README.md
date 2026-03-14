# MiiAsset 使用说明

## 基本加载流程:

## 热更流程:

***
`基本加载流程:`
获取新版本资源url -> AssetLoader.Init -> AssetLoader.UpdateCatalog(传入新版本资源url) -> AssetLoader.CleanUpOldVersionFiles  清理旧版缓存 -> 下载资源 -> 加载资源(未下载则伴随下载) -> AssetLoader.Dispose
***

1. 调用方法 `AssetLoader.Init()` 初始化，检测本地资源清单，后续会动态维护此清单。
2. 获取新版本资源url。
3. 调用方法 `AssetLoader.UpdateCatalog` 通过资源url拉取hash文件, 检查是否存在catalog更新, catalog为资源索引文件。
4. 有更新则拉取catalog文件, 同时加载包内catalog和下载远程catalog到本地缓存, 并在内存中将缓存的最新catalog合并到包内catalog中。
5. 调用方法 `AssetLoader.CleanUpOldVersionFiles()` 清理旧版缓存资源文件，为新版本资源腾出空间。
6. 调用方法 `AssetLoader.DownloadAll()` 下载本地不存在的新版本资源。
7. 启动时加载资源过程，如果本地资源清单中不存在所需资源，则会自动从远程下载。
8. 调用方法 `AssetLoader.Dispose()` 释放资源加载状态。

## 配置方法

1. 创建配置: 使用Assets目录下任意位置右键菜单 Create/MiiConfig/AssetConsumerConfig, 创建配置 AssetConsumerConfig.asset, 如果不创建改配置则使用默认配置。
2. 创建和设置 `AAPathConfig` 。
3. 参数说明:
   1. UpdateTunnel 热更通道, 如果集成了AAUpdateSettings, 那么直接从AAUpdateSettings面板修改即可。
   2. LoadType 加载模式(仅编辑器中生效):
      1. LoadFromEditor: 直接通过编辑器加载资源。
      2. LoadFromBundle: 从发布的AssetBundle加载资源，用于模拟测试复现问题。

## 如何构建和上传资源

### 自动构建资源
1. 默认配置下，在Unity构建时, 会自动触发构建资源到 `AssetBundles` 目录下
2. 构建资源后可以通过外部上传工具包上传热更资源或手动上传。
- 目前上传工具已支持 S3、AliOSS、COS 等多个云存储服务，并已适配多个发行渠道。

### 手动构建资源，发布热更
1. 点击菜单 `Tools/MiiAsset/BuildAssetBundlesWithPathInfo` 会自动构建资源到 `AssetBundles` 目录下，通过上传工具上传热更资源。

## 常用资源引用模式

1. 半自由管理模式
   1. 允许可加载的tag(所有tag默认不允许加载)
      1. AssetLoader.AllowTags
   2. 加载tag对应的所有资源，并增加引用计数
      1. AssetLoader.LoadTags
   3. 卸载tag
      1. UnloadTags(通过引用计数禁用并卸载)
   4. 加载/卸载资源(无引用计数, 通过另外的插件扩展实现引用计数更高效可靠)
      1. AssetLoader.LoadAsset
      2. AssetLoader.UnLoadAsset
   5. 加载/卸载场景(无引用计数)
      1. AssetLoader.LoadScene
      2. AssetLoader.UnLoadScene
2. addressables 完全引用计数模式
   1. 加载资源
      1. AssetLoader.LoadAssetByReferWrapped
   2. 释放资源
      1. AssetLoader.UnLoadAssetByReferWrapped
   3. 加载对象
      1. AssetLoader.InstantiateAsync
   4. 释放对象
      1. AssetLoader.ReleaseInstance
   5. 加载场景
      1. AssetLoader.LoadSceneByReferWrapped
   6. 释放场景
      1. AssetLoader.UnLoadSceneByReferWrapped

## 分批下载模式

该模式主要基于菜单工具: `Tools/MiiAsset/BundleBatchTools`

1. 编辑器模式下运行游戏，每到一个阶段都使用菜单 `PushIndex`，记录每个阶段所需的资源清单，最后使用菜单 `SaveRecords` 保存全部记录，保存在文件 `Assets/Bundles/GameConfigs/AALoaderConfigs/AALoadConfig.json.txt` 中。之后可手动调整该配置。
2. 构建时，会自动按照配置文件 `Assets/Bundles/GameConfigs/AALoaderConfigs/AALoadConfig.json.txt` 中的配置，给对应资源加分批tag，格式为 `batch{index}`。
3. 使用 `AssetLoader.DownloadBatch({index})` 方法下载对应阶段的资源，0为第一阶段。

## 扩展功能
### 支持资源防刷机制
使用 `AssetLoader.SetRemoteSourceAdapter` 设置自定义的资源加载器，用于调整加载Url，以支持自定义缓存策略，可参考 `YueCloudCdnAdapter` 包。

## 其他API

1. 下载资源
   1. AssetLoader.DownloadTags
   2. AssetLoader.DownloadBatch
   3. AssetLoader.DownloadAll
2. 获取所需下载大小
   1. AssetLoader.GetDownloadSize
3. 预加载/卸载资源
   1. AssetLoader.LoadTags
   2. AssetLoader.UnloadTags
      - 获取下载/加载进度
        ```csharp
        var status = new AssetLoadStatusGroup();
        AssetLoader.LoadTags(tags, status);
        // 获取下载进度
        var downloadProgress = status.DownloadProgress;
        // 获取加载进度
        var progress = status.Progress;
        ```
4. 仅加载本地catalog
   1. AssetLoader.LoadLocalCatalog
5. 设置最大下载并发数
   1. AssetLoader.SetDownloadMaxCount
6. 注册自定义证书许可策略
   1. AssetLoader.RegisterCertificateHandler
7. 注册其他平台适配器
   1. AssetLoader.Adapt

