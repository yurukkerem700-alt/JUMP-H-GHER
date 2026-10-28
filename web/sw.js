// Otomatik üretildi (WebPwaExport). Her derlemede sürüm değişir.
const VERSION='20261028072225';
const CACHE='kok-yankisi-'+VERSION;
const FILES=[
  "./",
  "Build/web.data.br",
  "Build/web.framework.js.br",
  "Build/web.loader.js",
  "Build/web.wasm.br",
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
  "icon-192.png",
  "icon-512.png",
  "icon-maskable-512.png",
  "icon.png",
  "index.html",
  "manifest.webmanifest",
  "rewarded-ads.js",
];

self.addEventListener('install',event=>{
  event.waitUntil((async()=>{
    const cache=await caches.open(CACHE);
    await Promise.all(FILES.map(async url=>{
      const response=await fetch(url,{cache:'no-cache'});
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
