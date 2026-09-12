using UnityEditor;
using UnityEngine;
namespace Ferraris.Editor
{
 public class VisualImporter:AssetPostprocessor
 {
  void OnPreprocessModel()
  {
   if(!assetPath.EndsWith("Visuals/Meadow.fbx"))return;
   var m=(ModelImporter)assetImporter;m.isReadable=true;m.materialImportMode=ModelImporterMaterialImportMode.None;m.importNormals=ModelImporterNormals.Import;
  }
  void OnPreprocessTexture()
  {
   if(!assetPath.Contains("Resources/Visuals/Textures/"))return;
   var t=(TextureImporter)assetImporter;t.mipmapEnabled=true;t.wrapMode=TextureWrapMode.Repeat;t.anisoLevel=8;t.maxTextureSize=2048;
   bool normal=assetPath.Contains("_normal");t.textureType=normal?TextureImporterType.NormalMap:TextureImporterType.Default;t.sRGBTexture=!normal&&!assetPath.Contains("rough")&&!assetPath.Contains("alpha");
   t.SetPlatformTextureSettings(new TextureImporterPlatformSettings{name="Android",overridden=true,maxTextureSize=assetPath.EndsWith(".hdr")?2048:1024,format=assetPath.EndsWith(".hdr")?TextureImporterFormat.ASTC_HDR_6x6:TextureImporterFormat.ASTC_6x6});
  }
 }
}
