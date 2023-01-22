using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using Debug = UnityEngine.Debug;

namespace HybridCLRIntegration
{
    internal static class LogHelper
    {
        const string Conditional = "UNITY_EDITOR";
        
        [Conditional(Conditional)]
        internal static void Log(string message)
        {
            Debug.Log(message);
        }
        
        [Conditional(Conditional)]
        internal static void LogError(string message)
        {
            Debug.LogError(message);
        }
        
        [Conditional(Conditional)]
        internal static void LogWarning(string message)
        {
            Debug.LogWarning(message);
        }

        [Conditional(Conditional)]
        internal static void LogResourceLoadInfo(string assetKey)
        {
            GetResourceLoadInfoAsync(assetKey).ContinueWith(Debug.Log);
        }
        
        private static async Task<string> GetResourceLoadInfoAsync(string key)
        {
            StringBuilder logBuilder = new StringBuilder();

            AsyncOperationHandle<IList<IResourceLocation>> handle = Addressables.LoadResourceLocationsAsync(key);

            await handle.Task;

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                logBuilder.AppendLine($"Successfully loaded resource locations for key: {key}");
            
                foreach (var location in handle.Result)
                {
                    logBuilder.AppendLine($"Resource location: {location.InternalId}");
                
                    if (location.HasDependencies)
                    {
                        foreach (var dependency in location.Dependencies)
                        {
                            logBuilder.AppendLine($"  Dependency: {dependency.InternalId}");
                        }
                    }
                }
            }
            else
            {
                logBuilder.AppendLine($"Failed to load resource locations for key: {key}");
            }

            Addressables.Release(handle);

            return logBuilder.ToString();
        }
    }
}