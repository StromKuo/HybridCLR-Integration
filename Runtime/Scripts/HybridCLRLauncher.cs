using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;

namespace HybridCLRIntegration
{
    public class HybridCLRLauncher : MonoBehaviour
    {
        private const int MaxRetryCount = 3;
        private const int RetryDelayMilliseconds = 3000; // 3 seconds

        [SerializeField]
        private LauncherConfig _config;

        [SerializeField]
        private Slider _progress;

        [SerializeField]
        private Text _progressTitle;

        private AssemblyLoader _assemblyLoader;
        private int _assembly0OrMetadata1;

        private void Start()
        {
            _ = this.StartAsync();
        }

        private async Task StartAsync()
        {
            this._assemblyLoader = new AssemblyLoader(this._config);
            this._assembly0OrMetadata1 = 0;
            var (isOk, assemblies) =
                await this._assemblyLoader.LoadAssembliesAsyncWithRetry(MaxRetryCount, RetryDelayMilliseconds);

            if (!isOk)
            {
                Debug.LogError("Failed to load assemblies.");
                await Task.Delay(TimeSpan.FromMilliseconds(RetryDelayMilliseconds));
                _ = this.StartAsync();
                return;
            }

            this._assembly0OrMetadata1 = 1;
            await this._assemblyLoader.LoadMetadataForAOTAssemblyAsyncWithRetry(MaxRetryCount, RetryDelayMilliseconds);

            var methods = GetRuntimeInitializeMethods(assemblies);
            foreach (var method in methods.SelectMany(x => x.Value))
            {
                method.Invoke(null, null);
            }

            Addressables.LoadSceneAsync(this._config.targetLaunchScene);
        }

        private void Update()
        {
            if (this._assemblyLoader == null || !this._assemblyLoader.loadStatus.HasValue) return;
            if (this._progressTitle == null && this._progress == null) return;

            var loadStatus = this._assemblyLoader.loadStatus.Value;
            var suffixStr = this._assembly0OrMetadata1 == 0 ? "Assemblies" : "Metadata";

            var titleStr = string.Empty;
            var progressValue = 0f;

            switch (loadStatus.loadPhase)
            {
                case LoadPhase.Loading:
                    var sizeSuffix = loadStatus.totalBytes > 0 ? $"({BytesToMegabytes(loadStatus.downloadedBytes)}MB/{BytesToMegabytes(loadStatus.totalBytes)}MB)" : string.Empty;
                    titleStr = loadStatus.isDownloadDone
                        ? $"Loading {suffixStr}..."
                        : $"Downloading {suffixStr}...{sizeSuffix}";
                    progressValue = loadStatus.isDownloadDone
                        ? loadStatus.percentComplete
                        : loadStatus.downloadedPercent;
                    break;
                case LoadPhase.WaitingRetry:
                    titleStr = $"Failed to load {suffixStr} and waiting for retry...";
                    break;
                case LoadPhase.Failed:
                    titleStr = $"Failed to load {suffixStr}, will retry...";
                    break;
            }

            this._progressTitle.text = titleStr;
            this._progress.value = progressValue;
        }

        private static Dictionary<RuntimeInitializeLoadType, List<MethodInfo>> GetRuntimeInitializeMethods(
            IEnumerable<Assembly> assemblies)
        {
            var result = assemblies
                .SelectMany(assembly => assembly.GetTypes())
                .SelectMany(type => type.GetMethods(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public))
                .SelectMany(method => method.GetCustomAttributes<RuntimeInitializeOnLoadMethodAttribute>(false),
                    (method, attribute) => new { method, attribute })
                .GroupBy(x => x.attribute.loadType)
                .ToDictionary(g => g.Key, g => g.Select(x => x.method).ToList());

            var sortedLoadTypes = new List<RuntimeInitializeLoadType>
            {
                RuntimeInitializeLoadType.SubsystemRegistration,
                RuntimeInitializeLoadType.AfterAssembliesLoaded,
                RuntimeInitializeLoadType.BeforeSplashScreen,
                RuntimeInitializeLoadType.BeforeSceneLoad,
                RuntimeInitializeLoadType.AfterSceneLoad
            };

            return result.OrderBy(kv => sortedLoadTypes.IndexOf(kv.Key)).ToDictionary(kv => kv.Key, kv => kv.Value);
        }

        private static double BytesToMegabytes(long bytes)
        {
            const double bytesPerMegabyte = 1024 * 1024;
            double megabytes = bytes / bytesPerMegabyte;
            return Math.Round(megabytes, 2); 
        }
    }
}