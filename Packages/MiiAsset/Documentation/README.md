# MiiAsset 使用说明

## 加载流程:

获取新版本资源url -> AssetLoader.Init -> AssetLoader.UpdateCatalog(传入新版本资源url) -> AssetLoader.CleanUpOldVersionFiles  清理旧版缓存 -> 下载资源 -> 加载资源(未下载则伴随下载) -> AssetLoader.Dispose



## 热更原理:

1. 获取新版本资源url

2. 通过资源url拉取hash文件, 检查是否存在资源更新

3. 有更新则拉取catalog文件, 并在内存中合并到包内catalog中

4. 清理缓存

5. 更新资源

6. 

## 常用模式:

1. addressables 模式
   
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

2. 自制模式
   
   1. 允许可加载的tag(所有tag默认不允许加载)
      
      1. AssetLoader.AllowTags
      
      2. AssetLoader.LoadTags
   
   2. 禁用tag
      
      1. UnloadTags(通过引用计数禁用并卸载)
   
   3. 加载/卸载资源(无引用计数, 通过另外的插件扩展实现引用计数更高效可靠)
      
      1. AssetLoader.LoadAsset
      
      2. AssetLoader.UnLoadAsset
   
   4. 加载/卸载场景(无引用计数)
      
      1. AssetLoader.LoadScene
      
      2. AssetLoader.UnLoadScene

3. 其他API
   
   1. 下载资源
      
      1. AssetLoader.DownloadTags
      
      2. AssetLoader.DownloadBatch
      
      3. AssetLoader.DownloadAll
   
   2. 预加载/卸载资源
      
      1. AssetLoader.LoadTags
      
      2. AssetLoader.UnloadTags
   
   3. 仅加载本地catalog
      
      1. AssetLoader.LoadLocalCatalog
   
   4. 设置最大下载并发数
      
      1. AssetLoader.SetDownloadMaxCount
   
   5. 注册自定义证书许可策略
      
      1. AssetLoader.RegisterCertificateHandler
   
   6. 注册其他平台适配器
      
      1. AssetLoader.Adapt


