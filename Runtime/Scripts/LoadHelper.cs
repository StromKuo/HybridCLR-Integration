using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using HybridCLR;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace HybridCLRIntegration
{
    public static class LoadHelper
    {
        public delegate void ProgressCallback(float progress);
        
        public static async Task<(bool, List<Assembly>)> LoadAssembliesAsync(LauncherConfig launcherConfig, ProgressCallback progressCallback, int maxRetryCount, int retryDelayMilliseconds)
        {
            var ret = false;
            var retAssemblies = new List<Assembly>();
            
            int retryCount = 0;
            while (true)
            {
                try
                {
                    IList<TextAsset> loadedAssets = await LoadTextAssetsAsync(launcherConfig.hotUpdateAssemblyAssetKeys, progressCallback);
                    foreach (var textAsset in loadedAssets)
                    {
                        Debug.Log("Loaded TextAsset: " + textAsset.name);

                        // In the Editor environment, *.dll.bytes files have already been automatically loaded, so there's no need to load them again. Repeated loading could actually cause problems.
                        if (!Application.isEditor)
                        {
                            var ass = Assembly.Load(textAsset.bytes);
                            retAssemblies.Add(ass);
                        }
                    }

                    ret = true;
                    break; // Successfully loaded, exit the loop.
                }
                catch (Exception ex)
                {
                    retryCount++;
                    Debug.LogError($"Failed to load TextAssets (Attempt {retryCount}/{maxRetryCount}): {ex.Message}");
                    if (retryCount >= maxRetryCount)
                    {
                        Debug.LogError("Reached maximum retry attempts. Aborting.");
                        ret = false;
                        break;
                    }
                    await Task.Delay(retryDelayMilliseconds); // Wait for a period of time and then retry.
                }
                retAssemblies.Clear();
            }

            return (ret, retAssemblies);
        }

        public static async Task<bool> LoadMetadataForAOTAssemblyAsync(LauncherConfig launcherConfig, ProgressCallback progressCallback, int maxRetryCount, int retryDelayMilliseconds)
        {
            var ret = false;
            
            int retryCount = 0;
            while (true)
            {
                try
                {
                    IList<TextAsset> loadedAssets = await LoadTextAssetsAsync(launcherConfig.aotMetadataAssetKeys, progressCallback);
                    foreach (var textAsset in loadedAssets)
                    {
                        Debug.Log("Loaded TextAsset: " + textAsset.name);

                        // In the Editor environment, *.dll.bytes files have already been automatically loaded, so there's no need to load them again. Repeated loading could actually cause problems.
                        if (!Application.isEditor)
                        {
                            var err = HybridCLR.RuntimeApi.LoadMetadataForAOTAssembly(textAsset.bytes, HomologousImageMode.SuperSet);
                            Debug.Log($"LoadMetadataForAOTAssembly:{textAsset.name}. ret:{err}");
                        }
                    }

                    ret = true;
                    break; // Successfully loaded, exit the loop.
                }
                catch (Exception ex)
                {
                    retryCount++;
                    Debug.LogError($"Failed to load TextAssets (Attempt {retryCount}/{maxRetryCount}): {ex.Message}");
                    if (retryCount >= maxRetryCount)
                    {
                        Debug.LogError("Reached maximum retry attempts. Aborting.");
                        ret = false;
                        break;
                    }
                    await Task.Delay(retryDelayMilliseconds); // Wait for a period of time and then retry.
                }
            }
            
            if (Application.isEditor)
            {
                return true;
            }

            return ret;
        }
        
        async static Task<IList<TextAsset>> LoadTextAssetsAsync(IReadOnlyList<string> assetKeys, ProgressCallback progressCallback, int timeoutMilliseconds = 30000)
        {
            LogHelper.LogResourceLoadInfo(assetKeys[0]);
            
            var tcs = new TaskCompletionSource<IList<TextAsset>>();
            var handle = Addressables.LoadAssetsAsync<TextAsset>(assetKeys, null, Addressables.MergeMode.Union);

            // Handle completion and failure cases
            handle.Completed += (AsyncOperationHandle<IList<TextAsset>> opHandle) =>
            {
                if (opHandle.Status == AsyncOperationStatus.Succeeded)
                {
                    tcs.TrySetResult(opHandle.Result);
                }
                else
                {
                    tcs.TrySetException(new Exception("Failed to load TextAssets"));
                }
            };

            // Implementing timeout
            Task.Delay(timeoutMilliseconds).ContinueWith(_ =>
            {
                tcs.TrySetException(new TimeoutException("Loading TextAssets timed out"));
            });
            
            // Update progress periodically
            Task.Run(async () =>
            {
                while (!handle.IsDone)
                {
                    progressCallback?.Invoke(handle.PercentComplete);
                    await Task.Delay(100); // Update progress every 100ms
                }
            });

            return await tcs.Task;
        }
        
        
    }
}