using System;
using System.IO;
using System.Security.Cryptography;
using System.Collections.Generic;
using System.Text.RegularExpressions;

// Unique asset URLs also bypass Unity's IndexedDB cache of older builds.
public static class VersionWebBuild
{
    public static void Apply(string folder)
    {
        string html=File.ReadAllText(Path.Combine(folder,"index.html"));
        var keep=new HashSet<string>();
        foreach(string name in new[]{"web.data","web.wasm","web.framework.js","web.loader.js"})
        {
            string path=Path.Combine(folder,"Build",name);
            if(!File.Exists(path)) continue;
            string hash;
            using(var sha=SHA256.Create()) using(var stream=File.OpenRead(path))
                hash=BitConverter.ToString(sha.ComputeHash(stream)).Replace("-","").Substring(0,12).ToLowerInvariant();
            string versioned="web."+hash+name.Substring(3);
            string dest=Path.Combine(folder,"Build",versioned);
            keep.Add(dest);
            if(File.Exists(dest)) File.Delete(dest);
            File.Move(path,dest);
            html=html.Replace("Build/"+name,"Build/"+versioned);
        }
        if(keep.Count==0) return;
        foreach(string old in Directory.GetFiles(Path.Combine(folder,"Build")))
            if(Regex.IsMatch(Path.GetFileName(old),@"^web\.[a-f0-9]{12}\.(data|wasm|framework\.js|loader\.js)(\.br)?$") && !keep.Contains(old)) File.Delete(old);
        File.WriteAllText(Path.Combine(folder,"index.html"),html);
    }
}
