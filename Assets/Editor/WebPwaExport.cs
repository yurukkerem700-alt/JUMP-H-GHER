using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

// WebGL çıktısını yüklenebilir, çevrimdışı çalışan uygulamaya (PWA) çevirir:
// manifest.webmanifest, sw.js ve 192/512 ikonlarını üretir.
public static class WebPwaExport
{
    [MenuItem("Cihazlar/Web Klasorune PWA Ekle")]
    public static void ApplyToDefaultFolder(){Apply(Path.GetDirectoryName(Application.dataPath)+"/web");}

    public static void Apply(string folder)
    {
        if(!File.Exists(folder+"/index.html"))throw new Exception("index.html yok: "+folder);
        File.Copy(Application.dataPath+"/WebTemplates/rewarded-ads.js",folder+"/rewarded-ads.js",true);
        string index=File.ReadAllText(folder+"/index.html");
        string clientId=Environment.GetEnvironmentVariable("ADSENSE_CLIENT_ID");
        if(!string.IsNullOrEmpty(clientId)&&clientId.StartsWith("ca-pub-")&&clientId.Length>7&&clientId.Substring(7).All(char.IsDigit))
        {
            string testAttribute=Environment.GetEnvironmentVariable("ADSENSE_TEST_MODE")=="1"?" data-adbreak-test=\"on\"":"";
            string ads="<script async"+testAttribute+" src=\"https://pagead2.googlesyndication.com/pagead/js/adsbygoogle.js?client="+clientId+"\" crossorigin=\"anonymous\"></script>\n"
                +"<script>window.adsbygoogle=window.adsbygoogle||[];window.adBreak=window.adConfig=function(o){window.adsbygoogle.push(o);};window.adConfig({preloadAdBreaks:'on',sound:'off',onReady:function(){window.__jacAdPlacementReady=true;}});</script>\n";
            index=index.Replace("</head>",ads+"</head>");
        }
        File.WriteAllText(folder+"/index.html",index,new UTF8Encoding(false));
        string source=Application.dataPath+"/CarGameArt/GameIcon.png";
        File.Copy(source,folder+"/icon.png",true);
        WriteIcon(source,folder+"/icon-192.png",192,1f);
        WriteIcon(source,folder+"/icon-512.png",512,1f);
        WriteIcon(source,folder+"/icon-maskable-512.png",512,0.78f);
        File.WriteAllText(folder+"/manifest.webmanifest",Manifest,new UTF8Encoding(false));
        File.WriteAllText(folder+"/sw.js",ServiceWorker(folder),new UTF8Encoding(false));
    }

    const string Manifest=@"{
  ""name"": ""Kök Yankısı • Jump Higher"",
  ""short_name"": ""Kök Yankısı"",
  ""description"": ""Her zaman daha yükseğe zıpla. Çevrimdışı oynanabilir sonsuz dikey platform oyunu."",
  ""lang"": ""tr"",
  ""start_url"": ""./index.html"",
  ""scope"": ""./"",
  ""id"": ""./"",
  ""display"": ""fullscreen"",
  ""display_override"": [""fullscreen"", ""standalone""],
  ""background_color"": ""#100e16"",
  ""theme_color"": ""#100e16"",
  ""categories"": [""games""],
  ""icons"": [
    { ""src"": ""icon-192.png"", ""sizes"": ""192x192"", ""type"": ""image/png"", ""purpose"": ""any"" },
    { ""src"": ""icon-512.png"", ""sizes"": ""512x512"", ""type"": ""image/png"", ""purpose"": ""any"" },
    { ""src"": ""icon-maskable-512.png"", ""sizes"": ""512x512"", ""type"": ""image/png"", ""purpose"": ""maskable"" }
  ]
}
";

    static string ServiceWorker(string folder)
    {
        string root=Path.GetFullPath(folder).Replace('\\','/').TrimEnd('/')+"/";
        var files=Directory.GetFiles(folder,"*",SearchOption.AllDirectories)
            .Select(p=>Path.GetFullPath(p).Replace('\\','/').Substring(root.Length))
            .Where(p=>p!="sw.js"&&!p.EndsWith(".map")&&!p.StartsWith("."))
            .OrderBy(p=>p,StringComparer.Ordinal).ToList();
        var list=new StringBuilder();
        foreach(string f in files)list.Append("  ").Append('"').Append(f).Append("\",\n");
        return "// Otomatik üretildi (WebPwaExport). Her derlemede sürüm değişir.\n"
            +"const VERSION='"+DateTime.UtcNow.ToString("yyyyMMddHHmmss")+"';\n"
            +"const CACHE='kok-yankisi-'+VERSION;\n"
            +"const FILES=[\n  \"./\",\n"+list+"];\n"
            +@"
self.addEventListener('install',event=>{
  event.waitUntil((async()=>{
    const cache=await caches.open(CACHE);
    await Promise.all(FILES.map(async url=>{
      const response=await fetch(url,{cache:'reload'});
      if(!response.ok)throw new Error(url+' '+response.status);
      await cache.put(url,response);
    }));
    await self.skipWaiting();
  })());
});

self.addEventListener('activate',event=>{
  event.waitUntil((async()=>{
    for(const key of await caches.keys())if(key.startsWith('kok-yankisi-')&&key!==CACHE)await caches.delete(key);
    await self.clients.claim();
    for(const client of await self.clients.matchAll())client.postMessage({type:'offline-ready'});
  })());
});

self.addEventListener('fetch',event=>{
  const request=event.request;
  if(request.method!=='GET'||new URL(request.url).origin!==location.origin)return;
  event.respondWith((async()=>{
    const cache=await caches.open(CACHE);
    const hit=await cache.match(request,{ignoreSearch:true});
    if(hit)return hit;
    try{return await fetch(request);}
    catch(error){
      if(request.mode==='navigate'){const page=await cache.match('index.html');if(page)return page;}
      throw error;
    }
  })());
});
";
    }

    // Kare ikon üretir; scale<1 ise güvenli alan payı bırakıp koyu zemine yerleştirir (maskable).
    static void WriteIcon(string sourcePath,string targetPath,int size,float scale)
    {
        var src=new Texture2D(2,2,TextureFormat.RGBA32,false);
        src.LoadImage(File.ReadAllBytes(sourcePath));
        int inner=Mathf.RoundToInt(size*scale);
        var rt=RenderTexture.GetTemporary(inner,inner,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);
        var previous=RenderTexture.active;
        Graphics.Blit(src,rt);
        RenderTexture.active=rt;
        var resized=new Texture2D(inner,inner,TextureFormat.RGBA32,false);
        resized.ReadPixels(new Rect(0,0,inner,inner),0,0);resized.Apply();
        RenderTexture.active=previous;RenderTexture.ReleaseTemporary(rt);

        var output=new Texture2D(size,size,TextureFormat.RGBA32,false);
        var background=new Color32(16,14,22,255);
        var pixels=Enumerable.Repeat(background,size*size).ToArray();
        output.SetPixels32(pixels);
        int offset=(size-inner)/2;
        for(int y=0;y<inner;y++)for(int x=0;x<inner;x++)
        {
            Color c=resized.GetPixel(x,y);
            Color under=output.GetPixel(x+offset,y+offset);
            output.SetPixel(x+offset,y+offset,Color.Lerp(under,new Color(c.r,c.g,c.b,1f),c.a));
        }
        output.Apply();
        File.WriteAllBytes(targetPath,output.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(src);UnityEngine.Object.DestroyImmediate(resized);UnityEngine.Object.DestroyImmediate(output);
    }
}
