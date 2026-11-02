using System;
using System.IO;
using System.Security.Cryptography;

// Unique asset URLs also bypass Unity's IndexedDB cache of older builds.
public static class VersionWebBuild
{
    public static void Apply(string folder)
    {
        string html=File.ReadAllText(Path.Combine(folder,"index.html"));
        string sw=File.ReadAllText(Path.Combine(folder,"sw.js"));
        foreach(string name in new[]{"web.data","web.wasm","web.framework.js","web.loader.js"})
        {
            string path=Path.Combine(folder,"Build",name);
            if(!File.Exists(path)) continue;
            string hash;
            using(var sha=SHA256.Create()) using(var stream=File.OpenRead(path))
                hash=BitConverter.ToString(sha.ComputeHash(stream)).Replace("-","").Substring(0,12).ToLowerInvariant();
            string versioned="web."+hash+name.Substring(3);
            string dest=Path.Combine(folder,"Build",versioned);
            if(File.Exists(dest)) File.Delete(dest);
            File.Move(path,dest);
            html=html.Replace("Build/"+name,"Build/"+versioned);
            sw=sw.Replace("Build/"+name,"Build/"+versioned);
        }
        File.WriteAllText(Path.Combine(folder,"index.html"),html);
        File.WriteAllText(Path.Combine(folder,"sw.js"),sw);
    }
}
