using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;

namespace HybridCLRIntegration
{
    public class HybridCLRLauncher : MonoBehaviour
    {
        private const int MaxRetryCount = 3;
        private const int RetryDelayMilliseconds = 3000; // 5 seconds
        
        [SerializeField]
        LauncherConfig _config;

        [SerializeField]
        private Slider _progress;

        [SerializeField]
        private Text _progressTitle;
        
        async void Start()
        {
            var (isOk, assemblies) = await LoadHelper.LoadAssembliesAsync(this._config, this.OnLoadAssemblyProgress, MaxRetryCount, RetryDelayMilliseconds);
            await LoadHelper.LoadMetadataForAOTAssemblyAsync(this._config, this.OnLoadMetadataProgress, MaxRetryCount, RetryDelayMilliseconds);

            var methods = CollectRuntimeInitializeMethods(assemblies);
            foreach (var m in methods.SelectMany(x => x.Value))
            {
                m.Invoke(null, null);
            }
            Addressables.LoadSceneAsync(this._config.targetLaunchScene);
        }

        void OnLoadAssemblyProgress(float progress)
        {
            if (this._progressTitle != null) this._progressTitle.text = "Loading Assemblies...";
            
            if (this._progress != null) this._progress.value = progress;
        }
        
        void OnLoadMetadataProgress(float progress)
        {
            if (this._progressTitle != null) this._progressTitle.text = "Loading AOT Metadata...";
            if (this._progress != null) this._progress.value = progress;
        }
        
        static Dictionary<RuntimeInitializeLoadType, List<MethodInfo>> CollectRuntimeInitializeMethods(IEnumerable<Assembly> assemblies)
        {
            var result = new Dictionary<RuntimeInitializeLoadType, List<MethodInfo>>();

            foreach (var assembly in assemblies)
            {
                foreach (var type in assembly.GetTypes())
                {
                    foreach (var method in type.GetMethods(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public))
                    {
                        var attributes = method.GetCustomAttributes(typeof(RuntimeInitializeOnLoadMethodAttribute), false);
                        foreach (RuntimeInitializeOnLoadMethodAttribute attribute in attributes)
                        {
                            if (!result.ContainsKey(attribute.loadType))
                            {
                                result[attribute.loadType] = new List<MethodInfo>();
                            }
                            result[attribute.loadType].Add(method);
                        }
                    }
                }
            }

            // Define the sorting order
            var sortedLoadTypes = new List<RuntimeInitializeLoadType>
            {
                RuntimeInitializeLoadType.SubsystemRegistration,
                RuntimeInitializeLoadType.AfterAssembliesLoaded,
                RuntimeInitializeLoadType.BeforeSplashScreen,
                RuntimeInitializeLoadType.BeforeSceneLoad,
                RuntimeInitializeLoadType.AfterSceneLoad
            };

            // Sort according to the defined order
            var sortedResult = result.OrderBy(kv => sortedLoadTypes.IndexOf(kv.Key))
                .ToDictionary(kv => kv.Key, kv => kv.Value);

            return sortedResult;
        }
    }
}
