using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HybridCLRIntegration
{
    public class LauncherConfig : ScriptableObject
    {
        public AssetReferenceScene targetLaunchScene => this._targetLaunchScene;
        
        public IReadOnlyList<string> hotUpdateAssemblyAssetKeys => this._hotUpdateAssemblyAssetKeys;
        
        public IReadOnlyList<string> aotMetadataAssetKeys => this._aotMetadataAssetKeys;
        
        [SerializeField]
        private AssetReferenceScene _targetLaunchScene;

        [Tooltip("This field will be generated automatically.")]
        [ReadOnly]
        [SerializeField]
        private string[] _hotUpdateAssemblyAssetKeys;
        
        [Tooltip("This field will be generated automatically.")]
        [ReadOnly]
        [SerializeField]
        private string[] _aotMetadataAssetKeys;

        public static LauncherConfig configInstance;

#if UNITY_EDITOR
        [UnityEditor.MenuItem("Assets/Create/HybridCLRLauncher Config")]
        public static void CreateAsset()
        {
            var path = UnityEditor.EditorUtility.SaveFilePanelInProject("Save Config", "HybridCLRLauncherConfig", "asset", string.Empty);
            if (string.IsNullOrEmpty(path))
                return;

            var configObject = CreateInstance<LauncherConfig>();
            UnityEditor.AssetDatabase.CreateAsset(configObject, path);

            // Add the config asset to the build
            var preloadedAssets = UnityEditor.PlayerSettings.GetPreloadedAssets().ToList();
            preloadedAssets.Add(configObject);
            UnityEditor.PlayerSettings.SetPreloadedAssets(preloadedAssets.ToArray());
        }

        public void SetHotUpdateAssemblyAssetKeys(string[] assetKeys)
        {
            this._hotUpdateAssemblyAssetKeys = assetKeys;
        }
        
        public void SetAOTMetadataAssetKeys(string[] assetKeys)
        {
            this._aotMetadataAssetKeys = assetKeys;
        }
#endif

        void OnEnable()
        {
            configInstance = this;
        }
    }
}
