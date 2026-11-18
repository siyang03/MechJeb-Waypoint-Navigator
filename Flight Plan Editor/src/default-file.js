'use strict';
let defaultCfgHandle=null;
function updateDefaultCfg(){
 $('defaultCfg').value=defaultCfgHandle?.name||'';
 $('openDefaultCfg').disabled=!defaultCfgHandle||!directSupported;
}
function cfgDatabase(){return new Promise((resolve,reject)=>{
 if(!window.indexedDB){reject(Error('Persistent file storage unavailable'));return;}
 const request=window.indexedDB.open('kerbin-editor-files',1);
 request.onupgradeneeded=()=>request.result.createObjectStore('files');
 request.onsuccess=()=>resolve(request.result);request.onerror=()=>reject(request.error);
});}
async function rememberCfg(handle){
 defaultCfgHandle=handle;updateDefaultCfg();
 try{const db=await cfgDatabase();try{await new Promise((resolve,reject)=>{const tx=db.transaction('files','readwrite');tx.objectStore('files').put(handle,'default');tx.oncomplete=resolve;tx.onerror=()=>reject(tx.error);tx.onabort=()=>reject(tx.error);});}finally{db.close();}}
 catch{ $('defaultCfg').title='Remembered for this tab only; browser persistent storage is unavailable.'; }
}
$('openDefaultCfg').onclick=()=>{if(defaultCfgHandle)openDirectCfg(defaultCfgHandle);};
updateDefaultCfg();
const startupCfgSnapshot=snapshot();
async function autoOpenDefaultCfg(handle){
  // Never replace edits made while IndexedDB or the permission check was pending.
  if(!directSupported||directHandle||dirty||snapshot()!==startupCfgSnapshot)return;
  try{
    const permission=await handle.queryPermission({mode:'read'});
    if(directHandle||dirty||snapshot()!==startupCfgSnapshot)return;
    if(permission==='granted')await openDirectCfg(handle);
    else fileStatus(`Default CFG: ${handle.name}. Browser permission is required; click Open Default CFG to authorize access.`);
  }catch{fileStatus('Could not open the default CFG automatically. Click Open Default CFG to retry, or Open Another CFG to choose a file.',true);}
}
(async()=>{
  try{
    const db=await cfgDatabase();let handle;
    try{handle=await new Promise((resolve,reject)=>{const r=db.transaction('files').objectStore('files').get('default');r.onsuccess=()=>resolve(r.result);r.onerror=()=>reject(r.error);});}
    finally{db.close();}
    if(!defaultCfgHandle&&handle){defaultCfgHandle=handle;updateDefaultCfg();await autoOpenDefaultCfg(handle);}
  }catch{}
})();
