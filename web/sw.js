// Otomatik üretildi (WebPwaExport). Her derlemede sürüm değişir.
const VERSION='20261104150322';
const CACHE='kok-yankisi-'+VERSION;
const FILES=[
  "./",
  "Build/web.1ea131d0ff84.data.br",
  "Build/web.2f59e9079719.framework.js.br",
  "Build/web.f4042b8b0b19.loader.js",
  "Build/web.098d16b98ca0.wasm.br",
  "TemplateData/MemoryProfiler.png",
  "TemplateData/favicon.ico",
  "TemplateData/fullscreen-button.png",
  "TemplateData/progress-bar-empty-dark.png",
  "TemplateData/progress-bar-empty-light.png",
  "TemplateData/progress-bar-full-dark.png",
  "TemplateData/progress-bar-full-light.png",
  "TemplateData/style.css",
  "TemplateData/unity-logo-dark.png",
  "TemplateData/unity-logo-light.png",
  "TemplateData/unity-logo-title-footer.png",
  "TemplateData/webmemd-icon.png",
  "build-info.json",
  "icon-192.png",
  "icon-512.png",
  "icon-maskable-512.png",
  "icon.png",
  "index.html",
  "manifest.webmanifest",
];

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
    let updated=false;
    for(const key of await caches.keys())if(key.startsWith('kok-yankisi-')&&key!==CACHE){await caches.delete(key);updated=true;}
    await self.clients.claim();
    for(const client of await self.clients.matchAll()){
      client.postMessage({type:'offline-ready'});
      if(updated)client.navigate(client.url).catch(()=>{}); // eski sürüm açıkken yeni sürüm gelirse sayfa kendini yeniler
    }
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
