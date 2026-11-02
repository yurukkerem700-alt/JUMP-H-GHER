using UnityEditor;
public class SkyWildlifeImport : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        if(!assetPath.EndsWith("Resources/Decor/SkyWildlife.png"))return;
        var t=(TextureImporter)assetImporter;
        t.textureType=TextureImporterType.Default;t.textureShape=TextureImporterShape.Texture2D;t.isReadable=true;t.mipmapEnabled=false;
        t.alphaIsTransparency=true;t.maxTextureSize=1024;t.textureCompression=TextureImporterCompression.Uncompressed;
        t.filterMode=UnityEngine.FilterMode.Bilinear;t.wrapMode=UnityEngine.TextureWrapMode.Clamp;
    }
}
