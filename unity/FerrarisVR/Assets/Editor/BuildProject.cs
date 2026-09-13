using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.Management;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine.XR.OpenXR;
using UnityEditor.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features.Interactions;
using UnityEngine.XR.OpenXR.Features.MetaQuestSupport;

namespace Ferraris.Editor
{
    public static class BuildProject
    {
        const string ScenePath="Assets/Scenes/FerrarisMapScene.unity";
        [InitializeOnLoadMethod] static void Initialize()
        {
            EditorApplication.delayCall+=()=>{if(!File.Exists(ScenePath)&&!EditorApplication.isPlayingOrWillChangePlaymode)Configure();};
        }
        [MenuItem("Ferraris/Configure project")]
        public static void Configure()
        {
            Directory.CreateDirectory("Assets/Scenes");
            if(!File.Exists(ScenePath))
            {
                var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
                EditorSceneManager.SaveScene(scene,ScenePath);
            }
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(ScenePath,true)};
            PlayerSettings.companyName="FerrarisView";PlayerSettings.productName="Winksele 1775";PlayerSettings.bundleVersion="0.1.0";
            PlayerSettings.defaultScreenWidth=1440;PlayerSettings.defaultScreenHeight=1000;PlayerSettings.fullScreenMode=FullScreenMode.Windowed;PlayerSettings.runInBackground=true;
            PlayerSettings.colorSpace=ColorSpace.Linear;
            var player=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
            var handling=player.FindProperty("activeInputHandler");if(handling!=null){handling.intValue=1;player.ApplyModifiedPropertiesWithoutUndo();}
            IncludeShaders();AssetDatabase.SaveAssets();Debug.Log("FERRARIS_CONFIGURED");
        }
        static void IncludeShaders()
        {
            var graphics=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0]);
            var included=graphics.FindProperty("m_AlwaysIncludedShaders");
            foreach(string name in new[]{"Unlit/Texture","Unlit/Color","Standard","Ferraris/MobileLandscape","Ferraris/WorldSurface","Ferraris/TerrainSurface","Ferraris/Foliage","Ferraris/ChurchPBR","Skybox/Panoramic"})
            {
                Shader shader=Shader.Find(name);if(shader==null)throw new InvalidOperationException("Missing shader "+name);
                bool found=false;for(int i=0;i<included.arraySize;i++)if(included.GetArrayElementAtIndex(i).objectReferenceValue==shader)found=true;
                if(!found){included.InsertArrayElementAtIndex(included.arraySize);included.GetArrayElementAtIndex(included.arraySize-1).objectReferenceValue=shader;}
            }
            var instancing=graphics.FindProperty("m_InstancingStripping");if(instancing!=null)instancing.intValue=2;
            graphics.ApplyModifiedPropertiesWithoutUndo();
        }
        [MenuItem("Ferraris/Build desktop")]
        public static void Desktop()
        {
            Configure();Build(Path.GetFullPath("../../builds/Winksele1775.app"),BuildTarget.StandaloneOSX);
        }
        [MenuItem("Ferraris/Capture website cover")]
        public static void WebCover()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var area=JsonUtility.FromJson<AreaData>(Resources.Load<TextAsset>("Winksele/area").text);
            var data=JsonUtility.FromJson<WorldData>(Resources.Load<TextAsset>("Winksele/world").text);
            new GameObject("Sun").AddComponent<Light>().type=LightType.Directional;
            var world=new GameObject("Landscape").AddComponent<HistoricalWorld>();world.Build(area,data);
            RenderSettings.fog=true;RenderSettings.fogMode=FogMode.Linear;
            var camera=new GameObject("Cover camera").AddComponent<Camera>();camera.farClipPlane=1800;camera.clearFlags=CameraClearFlags.Skybox;
            camera.transform.position=new Vector3(42,area.Height(42,-22)+14,-22);camera.transform.LookAt(new Vector3(2,15,26));
            var target=new RenderTexture(1440,1000,24);camera.targetTexture=target;camera.Render();
            RenderTexture.active=target;var image=new Texture2D(1440,1000,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1440,1000),0,0);image.Apply();
            File.WriteAllBytes("../../web/cover.jpg",image.EncodeToJPG(90));RenderTexture.active=null;camera.targetTexture=null;
            UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(target);
            Debug.Log("LAND_VAN_WELEER_COVER_CAPTURED");
        }
        [MenuItem("Ferraris/Build website")]
        public static void Web()
        {
            Configure();
            PlayerSettings.WebGL.compressionFormat=WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback=false;
            PlayerSettings.WebGL.dataCaching=true;
            PlayerSettings.WebGL.template="PROJECT:LandVanWeleer";
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.WebGL,false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.WebGL,new[]{GraphicsDeviceType.OpenGLES3});
            foreach(string guid in AssetDatabase.FindAssets("t:Texture2D",new[]{"Assets/WinkseleChurch/Textures","Assets/Resources/Visuals/Textures"}))
            {
                string path=AssetDatabase.GUIDToAssetPath(guid);var importer=(TextureImporter)AssetImporter.GetAtPath(path);
                var settings=importer.GetPlatformTextureSettings("WebGL");int size=path.Contains("WinkseleChurch")||path.EndsWith(".hdr")?2048:1024;
                if(settings.overridden&&settings.maxTextureSize==size)continue;
                settings.name="WebGL";settings.overridden=true;settings.maxTextureSize=size;settings.format=TextureImporterFormat.Automatic;
                importer.SetPlatformTextureSettings(settings);importer.SaveAndReimport();
            }
            // The website consults the public WMS directly. Never redistribute
            // the local copyrighted map raster inside the downloadable data file.
            const string raster="Assets/Resources/Winksele/ferraris.png",excluded="Assets/WebMapExcluded.png";
            string error=AssetDatabase.MoveAsset(raster,excluded);
            if(!string.IsNullOrEmpty(error))throw new InvalidOperationException(error);
            try
            {
                Build(Path.GetFullPath("../../builds/web"),BuildTarget.WebGL);
                File.Copy("../../web/vercel.json","../../builds/web/vercel.json",true);
                File.Copy("../../web/cover.jpg","../../builds/web/cover.jpg",true);
            }
            finally
            {
                error=AssetDatabase.MoveAsset(excluded,raster);
                if(!string.IsNullOrEmpty(error))throw new InvalidOperationException("Restore local map: "+error);
            }
        }
        [MenuItem("Ferraris/Configure Quest OpenXR")]
        public static void ConfigureQuest()
        {
            Configure();Directory.CreateDirectory("Assets/XR");
            const string path="Assets/XR/XRGeneralSettingsPerBuildTarget.asset";
            var settings=AssetDatabase.LoadAssetAtPath<XRGeneralSettingsPerBuildTarget>(path);
            if(settings==null){settings=ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();AssetDatabase.CreateAsset(settings,path);}
            EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey,settings,true);
            if(!settings.HasSettingsForBuildTarget(BuildTargetGroup.Android))settings.CreateDefaultSettingsForBuildTarget(BuildTargetGroup.Android);
            if(!settings.HasManagerSettingsForBuildTarget(BuildTargetGroup.Android))settings.CreateDefaultManagerSettingsForBuildTarget(BuildTargetGroup.Android);
            var general=settings.SettingsForBuildTarget(BuildTargetGroup.Android);general.InitManagerOnStart=true;
            if(!XRPackageMetadataStore.AssignLoader(general.Manager,"UnityEngine.XR.OpenXR.OpenXRLoader",BuildTargetGroup.Android))throw new InvalidOperationException("Cannot assign OpenXR loader");
            var xr=OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);
            if(xr==null)throw new InvalidOperationException("OpenXR settings not initialized");
            var quest=xr.GetFeature<MetaQuestFeature>();var touch=xr.GetFeature<OculusTouchControllerProfile>();
            if(quest==null||touch==null)throw new InvalidOperationException("Quest / Touch OpenXR features unavailable");
            quest.enabled=true;touch.enabled=true;xr.renderMode=OpenXRSettings.RenderMode.SinglePassInstanced;
            EditorUtility.SetDirty(xr);EditorUtility.SetDirty(quest);EditorUtility.SetDirty(touch);EditorUtility.SetDirty(settings);EditorUtility.SetDirty(general);
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android,"be.ferrarisview.winksele");
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android,ScriptingImplementation.IL2CPP);PlayerSettings.Android.targetArchitectures=AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion=AndroidSdkVersions.AndroidApiLevel29;PlayerSettings.Android.targetSdkVersion=AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android,false);PlayerSettings.SetGraphicsAPIs(BuildTarget.Android,new[]{GraphicsDeviceType.Vulkan});
            AssetDatabase.SaveAssets();Debug.Log("FERRARIS_QUEST_CONFIGURED");
        }
        [MenuItem("Ferraris/Build Quest APK")]
        public static void Quest()
        {
            // Stale sync copies in stripped assemblies break IL2CPP; Gradle also
            // packages obsolete staged assets. Recreate generated Android output.
            foreach(string relative in new[]{"Library/Bee/Android/Prj","Library/Bee/artifacts/Android"})
            {
                string generated=Path.GetFullPath(relative);
                if(Directory.Exists(generated))Directory.Delete(generated,true);
            }
            ConfigureQuest();
            if(!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android,BuildTarget.Android))throw new InvalidOperationException("Install Android Build Support, SDK/NDK and OpenJDK in Unity Hub");
            Build(Path.GetFullPath("../../builds/Winksele1775.apk"),BuildTarget.Android);
        }
        static void Build(string path,BuildTarget target)
        {
            if(Resources.Load<TextAsset>("Winksele/world")==null)throw new InvalidOperationException("Run GIS export before building");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{ScenePath},locationPathName=path,target=target,options=target==BuildTarget.WebGL?BuildOptions.None:BuildOptions.Development});
            if(report.summary.result!=BuildResult.Succeeded)throw new InvalidOperationException("Build failed: "+report.summary.result);
            if(target==BuildTarget.WebGL)foreach(var pack in report.packedAssets)foreach(var asset in pack.contents)
                if(asset.sourceAssetPath.EndsWith("/ferraris.png")||asset.sourceAssetPath.EndsWith("/WebMapExcluded.png"))throw new InvalidOperationException("Local map raster must not be included in the website");
            Debug.Log("FERRARIS_BUILD_SUCCESS "+path);
        }
    }
    public class RasterImporter : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if(!assetPath.Contains("Resources/Winksele/"))return;
            var t=(TextureImporter)assetImporter;t.textureType=TextureImporterType.Default;t.mipmapEnabled=true;t.wrapMode=TextureWrapMode.Clamp;t.maxTextureSize=2048;t.textureCompression=TextureImporterCompression.Uncompressed;
            t.SetPlatformTextureSettings(new TextureImporterPlatformSettings{name="Android",overridden=true,maxTextureSize=2048,format=TextureImporterFormat.ASTC_6x6});
        }
    }
    public class SoundImporter : AssetPostprocessor
    {
        void OnPreprocessAudio()
        {
            if(!assetPath.Contains("Discovery/Audio/"))return;
            var importer=(AudioImporter)assetImporter;importer.forceToMono=true;
            var settings=importer.defaultSampleSettings;settings.loadType=AudioClipLoadType.DecompressOnLoad;settings.compressionFormat=AudioCompressionFormat.Vorbis;settings.quality=.75f;importer.defaultSampleSettings=settings;
        }
    }

}
