using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace HybridCLRIntegration.Editor
{
    [FilePath("ProjectSettings/HybridCLRIntegrationSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    public class HybridCLRIntegrationProjectSettings: ScriptableSingleton<HybridCLRIntegrationProjectSettings>
    {
        public const string BytesExtension = ".bytes";
        
        public string hotUpdateDllsDestinationPath => this.m_HotUpdateDllsDestinationPath;

        public string assembliesPostIl2CppStripDestinationPath => this.m_AssembliesPostIl2CppStripDestinationPath;
        
        public LauncherConfig launcherConfig => this.m_LauncherConfig;

        public IReadOnlyList<string> patchedAOTAssemblyList => m_PatchedAOTAssemblyList;

        [SerializeField]
        private string m_HotUpdateDllsDestinationPath;

        [SerializeField]
        private string m_AssembliesPostIl2CppStripDestinationPath;

        [SerializeField]
        private List<string> m_PatchedAOTAssemblyList = new List<string>() { "mscorlib.dll" };
        
        [SerializeField]
        private LauncherConfig m_LauncherConfig;
        
        private void OnDisable()
        {
            this.Save();
        }

        internal void Save()
        {
            this.Save(true);
        }

        internal SerializedObject GetSerializedObject()
        {
            return new SerializedObject(this);
        }
    }
    
    internal static class HybridCLRIntegrationSettingsUIElementsRegister
    {
        [SettingsProvider]
        public static SettingsProvider CreateHybridCLRIntegrationSettingsProvider()
        {
            var provider = new SettingsProvider("Project/HybridCLR Integration Settings", SettingsScope.Project)
            {
                label = "HybridCLR Integration Settings",
                activateHandler = (searchContext, rootElement) =>
                {
                    HybridCLRIntegrationProjectSettings.instance.hideFlags = HideFlags.None;
                    var settings = HybridCLRIntegrationProjectSettings.instance.GetSerializedObject();
                    var title = new Label()
                    {
                        text = "HybridCLR Integration Settings",
                        style = 
                        {
                            fontSize = new StyleLength(20),
                            unityFontStyleAndWeight = FontStyle.Bold,
                        }
                    };
                    title.AddToClassList("title");
                    rootElement.Add(title);
    
                    var properties = new VisualElement()
                    {
                        style =
                        {
                            flexDirection = FlexDirection.Column,
                            
                        }
                    };
                    properties.AddToClassList("property-list");
                    rootElement.Add(properties);
                    
                    properties.Add(new InspectorElement(settings));
    
                    rootElement.Bind(settings);
                },
                deactivateHandler = () =>
                {
                    HybridCLRIntegrationProjectSettings.instance.Save();
                },
    
                // Populate the search keywords to enable smart search filtering and label highlighting:
                keywords = new HashSet<string>(new[] { "hybrid", "clr", "copy", "aot" })
            };
    
            return provider;
        }
    }
}