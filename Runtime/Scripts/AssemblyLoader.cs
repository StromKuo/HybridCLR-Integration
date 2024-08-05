using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using HybridCLR;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace HybridCLRIntegration
{
    public class AssemblyLoader
    {
        public LoadStatus? loadStatus => _currentOperationHandle.HasValue
            ? new LoadStatus(this._currentLoadPhase, this._currentOperationHandle.Value.GetDownloadStatus(),
                this._currentOperationHandle.Value.PercentComplete)
            : null;

        public readonly LauncherConfig launcherConfig;

        private AsyncOperationHandle<IList<TextAsset>>? _currentOperationHandle;
        private LoadPhase _currentLoadPhase;

        public AssemblyLoader(LauncherConfig launcherConfig)
        {
            this.launcherConfig = launcherConfig;
        }

        public async Task<(bool, IList<Assembly>)> LoadAssembliesAsyncWithRetry(int maxRetryCount,
            int retryDelayMilliseconds)
        {
            var retAssemblies = new List<Assembly>();
            bool success = await this.ExecuteWithRetryAsync(async () =>
            {
                var result = await this.LoadAssembliesAsync();
                retAssemblies.AddRange(result);
            }, maxRetryCount, retryDelayMilliseconds);

            return (success, retAssemblies);
        }

        public async Task<IList<Assembly>> LoadAssembliesAsync()
        {
            var retAssemblies = new List<Assembly>();
            this._currentLoadPhase = LoadPhase.Loading;
            var handle = LoadTextAssetsAsync(this.launcherConfig.hotUpdateAssemblyAssetKeys);
            this._currentOperationHandle = handle;
            var loadedAssets = await handle.Task;
            foreach (var textAsset in loadedAssets)
            {
                LogHelper.Log("Loaded TextAsset: " + textAsset.name);

                // In the Editor environment, *.dll.bytes files have already been automatically loaded, so there's no need to load them again. Repeated loading could actually cause problems.
                if (!Application.isEditor)
                {
                    var ass = Assembly.Load(textAsset.bytes);
                    retAssemblies.Add(ass);
                }
            }

            return retAssemblies;
        }

        public async Task<bool> LoadMetadataForAOTAssemblyAsyncWithRetry(int maxRetryCount, int retryDelayMilliseconds)
        {
            bool success = await this.ExecuteWithRetryAsync(
                async () => { await this.LoadMetadataForAOTAssemblyAsync(); }, maxRetryCount,
                retryDelayMilliseconds);

            if (Application.isEditor)
            {
                return true;
            }

            return success;
        }

        public async Task LoadMetadataForAOTAssemblyAsync()
        {
            this._currentLoadPhase = LoadPhase.Loading;
            var handle = LoadTextAssetsAsync(this.launcherConfig.aotMetadataAssetKeys);
            this._currentOperationHandle = handle;
            var loadedAssets = await handle.Task;
            foreach (var textAsset in loadedAssets)
            {
                LogHelper.Log("Loaded TextAsset: " + textAsset.name);

                // In the Editor environment, *.dll.bytes files have already been automatically loaded, so there's no need to load them again. Repeated loading could actually cause problems.
                if (!Application.isEditor)
                {
                    var err = RuntimeApi.LoadMetadataForAOTAssembly(textAsset.bytes,
                        HomologousImageMode.SuperSet);
                    LogHelper.Log($"LoadMetadataForAOTAssembly:{textAsset.name}. ret:{err}");
                }
            }
        }

        private async Task<bool> ExecuteWithRetryAsync(Func<Task> action, int maxRetryCount,
            int retryDelayMilliseconds)
        {
            int retryCount = 0;
            while (true)
            {
                try
                {
                    await action();
                    return true; // Successfully executed, exit the loop.
                }
                catch (Exception ex)
                {
                    retryCount++;
                    LogHelper.LogError($"Operation failed (Attempt {retryCount}/{maxRetryCount}): {ex.Message}");
                    if (retryCount >= maxRetryCount)
                    {
                        LogHelper.LogError("Reached maximum retry attempts. Aborting.");
                        this._currentLoadPhase = LoadPhase.Failed;
                        return false;
                    }

                    this._currentLoadPhase = LoadPhase.WaitingRetry;
                    await Task.Delay(retryDelayMilliseconds); // Wait for a period of time and then retry.
                }
            }
        }

        static AsyncOperationHandle<IList<TextAsset>> LoadTextAssetsAsync(IReadOnlyList<string> assetKeys)
        {
            LogHelper.LogResourceLoadInfo(assetKeys[0]);
            return Addressables.LoadAssetsAsync<TextAsset>(assetKeys, null, Addressables.MergeMode.Union);
        }
    }
}