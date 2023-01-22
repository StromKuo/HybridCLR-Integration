using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace HybridCLRIntegration.Editor
{
    public static class HybridCLREditorTools
    {
        [MenuItem("HybridCLR/HybridCLR Integrate/GenerateAll and Copy")]
        public static void GenerateAllAndCopy()
        {
            HybridCLR.Editor.Commands.PrebuildCommand.GenerateAll();
            CopyHotUpdateDlls();
            CopyAssembliesPostIl2CppStrip();
        }

        [MenuItem("HybridCLR/HybridCLR Integrate/Copy HotUpdateDlls")]
        public static void CopyHotUpdateDlls()
        {
            ClearDirectory(HybridCLRIntegrationProjectSettings.instance.hotUpdateDllsDestinationPath);

            var hotUpdateAssemblyAssetKeys = new List<string>();
            foreach (var dllFileName in HybridCLR.Editor.SettingsUtil.HotUpdateAssemblyFilesIncludePreserved)
            {
                var src = GetSourceHotUpdateDllAssetPath(dllFileName);
                var dst = GetTargetHotUpdateDllAssetPath(dllFileName);
                CopyFile(src, dst);

                hotUpdateAssemblyAssetKeys.Add(dst);
            }

            var launcherConfig = GetLauncherConfig();
            if (launcherConfig != null)
            {
                //TODO: Load in the order of dependency.
                launcherConfig.SetHotUpdateAssemblyAssetKeys(hotUpdateAssemblyAssetKeys.ToArray());
                EditorUtility.SetDirty(launcherConfig);
                AssetDatabase.SaveAssets();
            }

            AssetDatabase.Refresh();
        }

        [MenuItem("HybridCLR/HybridCLR Integrate/Copy AssembliesPostIl2CppStrip")]
        public static void CopyAssembliesPostIl2CppStrip()
        {
            ClearDirectory(HybridCLRIntegrationProjectSettings.instance.assembliesPostIl2CppStripDestinationPath);

            var aotMetadataAssetKeys = new List<string>();
            foreach (var dllFileName in HybridCLRIntegrationProjectSettings.instance.patchedAOTAssemblyList)
            {
                var src = GetSourceAssembliesPostIl2CppStripAssetPath(dllFileName);
                var dst = GetTargetAssembliesPostIl2CppStripAssetPath(dllFileName);
                
                HybridCLR.Editor.AOT.AOTAssemblyMetadataStripper.Strip(Path.GetFullPath(src), Path.GetFullPath(dst));
                
                aotMetadataAssetKeys.Add(dst);
            }

            var launcherConfig = GetLauncherConfig();
            if (launcherConfig != null)
            {
                launcherConfig.SetAOTMetadataAssetKeys(aotMetadataAssetKeys.ToArray());
                EditorUtility.SetDirty(launcherConfig);
                AssetDatabase.SaveAssets();
            }
            
            AssetDatabase.Refresh();
        }

        private static LauncherConfig GetLauncherConfig()
        {
            return HybridCLRIntegrationProjectSettings.instance.launcherConfig;
            // var paths = AssetDatabase.FindAssets("t:LauncherConfig")
            //     .Select(AssetDatabase.GUIDToAssetPath)
            //     .ToList();
            // if (paths.Count != 1)
            // {
            //     Debug.LogError($"There can only be one {nameof(LauncherConfig)} asset.");
            // }
            //
            // var p = paths.FirstOrDefault();
            // var ret = string.IsNullOrEmpty(p) ? null : AssetDatabase.LoadAssetAtPath<LauncherConfig>(p);
            // return ret;
        }

        private static string GetSourceHotUpdateDllAssetPath(string dllFileName)
        {
            var ret = Path.Combine(
                    HybridCLR.Editor.SettingsUtil.GetHotUpdateDllsOutputDirByTarget(EditorUserBuildSettings
                        .activeBuildTarget), dllFileName)
                .ToNormalizedPath();
            return ret;
        }

        private static string GetTargetHotUpdateDllAssetPath(string dllFileName)
        {
            var ret = Path.Combine(HybridCLRIntegrationProjectSettings.instance.hotUpdateDllsDestinationPath,
                    $"{dllFileName}{HybridCLRIntegrationProjectSettings.BytesExtension}")
                .ToNormalizedPath();
            return ret;
        }

        private static string GetSourceAssembliesPostIl2CppStripAssetPath(string dllFileName)
        {
            var ret = Path.Combine(
                    HybridCLR.Editor.SettingsUtil.GetAssembliesPostIl2CppStripDir(EditorUserBuildSettings
                        .activeBuildTarget), dllFileName)
                .ToNormalizedPath();
            return ret;
        }

        private static string GetTargetAssembliesPostIl2CppStripAssetPath(string dllFileName)
        {
            var ret = Path.Combine(
                    HybridCLRIntegrationProjectSettings.instance.assembliesPostIl2CppStripDestinationPath,
                    $"{dllFileName}{HybridCLRIntegrationProjectSettings.BytesExtension}")
                .ToNormalizedPath();
            return ret;
        }

        private static void ClearDirectory(string assetPath)
        {
            string fullPath = Path.GetFullPath(assetPath).ToNormalizedPath();

            if (Directory.Exists(fullPath))
            {
                DirectoryInfo directoryInfo = new DirectoryInfo(fullPath);

                foreach (FileInfo file in directoryInfo.GetFiles())
                {
                    file.Delete();
                }

                foreach (DirectoryInfo dir in directoryInfo.GetDirectories())
                {
                    dir.Delete(true);
                }

                Debug.Log($"Cleared directory: {fullPath}");
            }
            else
            {
                Debug.LogWarning($"Directory does not exist: {fullPath}");
            }
        }

        private static void CopyFile(string srcAssetPath, string dstAssetPath, bool isMove = false)
        {
            string srcFullPath = Path.GetFullPath(srcAssetPath).ToNormalizedPath();
            string dstFullPath = Path.GetFullPath(dstAssetPath).ToNormalizedPath();

            if (File.Exists(srcFullPath))
            {
                string dstDirectory = Path.GetDirectoryName(dstFullPath).ToNormalizedPath();
                if (!Directory.Exists(dstDirectory))
                {
                    Directory.CreateDirectory(dstDirectory);
                }

                if (isMove)
                {
                    File.Move(srcFullPath, dstFullPath);
                }
                else
                {
                    File.Copy(srcFullPath, dstFullPath, true);
                }
                Debug.Log($"File copied from {srcFullPath} to {dstFullPath}");
            }
            else
            {
                Debug.LogError($"Source file not found: {srcFullPath}");
            }
        }

        private static string ToNormalizedPath(this string path)
        {
            return path.Replace("\\", "/");
        }
    }
}