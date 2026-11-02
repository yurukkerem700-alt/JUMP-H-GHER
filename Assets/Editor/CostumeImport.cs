using UnityEditor;
using UnityEngine;

// Resources/Costumes içindeki kostüm sayfaları kod tarafından piksel piksel okunur (PlayerVisual2D),
// bu yüzden okunabilir, sıkıştırmasız ve mipmap'siz içe aktarılır.
public class CostumeImport : AssetPostprocessor
{
    const string Folder = "Assets/Resources/Costumes/";

    void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith(Folder)) return;
        var importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Default;
        importer.alphaIsTransparency = true;
        importer.isReadable = true;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Bilinear;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.npotScale = TextureImporterNPOTScale.None;
        string id=System.IO.Path.GetFileNameWithoutExtension(assetPath);
        importer.maxTextureSize = Costumes.FrameRows(id)==3 ? 1024 : 2048;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
    }
}
