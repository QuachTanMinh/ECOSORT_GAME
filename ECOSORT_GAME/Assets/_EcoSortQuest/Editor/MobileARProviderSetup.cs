using System;
using System.Collections;
using System.Reflection;
using UnityEditor;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine;

namespace EcoSortQuest.Editor
{
    /// <summary>Assigns each mobile build target its native AR Foundation provider.</summary>
    [InitializeOnLoad]
    public static class MobileARProviderSetup
    {
        static MobileARProviderSetup() => EditorApplication.delayCall += ConfigureProviders;

        [MenuItem("EcoSort/Configure Android and iOS AR providers")]
        public static void ConfigureProviders()
        {
            Configure(BuildTargetGroup.Android, "UnityEngine.XR.ARCore.ARCoreLoader");
            Configure(BuildTargetGroup.iOS, "UnityEngine.XR.ARKit.ARKitLoader");
            ConfigureMobileRenderer();
            AssetDatabase.SaveAssets();
        }

        static void ConfigureMobileRenderer()
        {
            const string rendererPath = "Assets/Settings/Mobile_Renderer.asset";
            var rendererData = AssetDatabase.LoadMainAssetAtPath(rendererPath) as ScriptableObject;
            var featureType = Type.GetType("UnityEngine.XR.ARFoundation.ARBackgroundRendererFeature, Unity.XR.ARFoundation");
            if (rendererData == null || featureType == null)
            {
                Debug.LogWarning("EcoSort AR: could not locate the Mobile URP renderer or AR background feature yet.");
                return;
            }

            var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            var property = rendererData.GetType().GetProperty("rendererFeatures", flags);
            var features = property != null ? property.GetValue(rendererData) as IList : null;
            if (features == null)
            {
                Debug.LogError("EcoSort AR: the Mobile URP renderer does not expose its renderer features.");
                return;
            }
            foreach (var existing in features)
                if (existing != null && featureType.IsInstanceOfType(existing)) return;

            var feature = ScriptableObject.CreateInstance(featureType);
            feature.name = "AR Background Renderer Feature";
            AssetDatabase.AddObjectToAsset(feature, rendererData);
            features.Add(feature);
            EditorUtility.SetDirty(rendererData);
            Debug.Log("EcoSort AR: added AR Background Renderer Feature to Mobile_Renderer.asset.");
        }

        static void Configure(BuildTargetGroup target, string loaderType)
        {
            var container = GetOrCreateSettingsContainer();
            if (container != null && !container.HasSettingsForBuildTarget(target))
                container.CreateDefaultSettingsForBuildTarget(target);
            if (container != null && !container.HasManagerSettingsForBuildTarget(target))
                container.CreateDefaultManagerSettingsForBuildTarget(target);

            var general = container != null ? container.SettingsForBuildTarget(target) :
                XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(target);
            if (general == null)
            {
                Debug.LogWarning("EcoSort AR: XR settings for " + target + " are not available yet. Open Project Settings > XR Plug-in Management once, then run EcoSort/Configure Android and iOS AR providers.");
                return;
            }

            if (general.Manager == null)
            {
                Debug.LogError("EcoSort AR: XR Manager settings are missing for " + target + ". Open Project Settings > XR Plug-in Management and retry the EcoSort AR setup command.");
                return;
            }

            if (!XRPackageMetadataStore.AssignLoader(general.Manager, loaderType, target))
                Debug.LogError("EcoSort AR: could not assign " + loaderType + " to " + target + ". Confirm the matching AR package finished installing, then retry the setup command.");
            else
            {
                general.InitManagerOnStart = true;
                EditorUtility.SetDirty(general);
                Debug.Log("EcoSort AR: configured " + loaderType + " for " + target + ".");
            }
        }

        static XRGeneralSettingsPerBuildTarget GetOrCreateSettingsContainer()
        {
            var method = typeof(XRGeneralSettingsPerBuildTarget).GetMethod("GetOrCreate", BindingFlags.Static | BindingFlags.NonPublic);
            return method?.Invoke(null, null) as XRGeneralSettingsPerBuildTarget;
        }
    }
}
