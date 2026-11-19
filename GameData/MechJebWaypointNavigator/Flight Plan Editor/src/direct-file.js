/* Browser-authorized file handles only: a typed path cannot grant filesystem access. */
'use strict';
let directHandle=null, directText=null, fileBusy=false;
const directSupported=typeof window.showOpenFilePicker==='function'&&typeof window.showSaveFilePicker==='function';
const cfgTypes=[{description:'KSP flight plans',accept:{'text/plain':['.cfg']}}];
function fileStatus(text,error=false){$('directFileStatus').textContent=text;$('directFileStatus').classList.toggle('error',error);$('directFileStatus').classList.toggle('permission-warning',text.includes('Browser permission is required'));}
function updateFileControls(){
  $('openDirect').disabled=!directSupported||fileBusy;
  $('saveDirect').disabled=!directSupported||fileBusy;
  $('saveAsDirect').disabled=!directSupported||fileBusy;
  $('newCfg').disabled=fileBusy;
  $('cfgFilename').readOnly=!!directHandle;
}
function detachDirectFile(){directHandle=null;directText=null;updateFileControls();fileStatus(directSupported?'No CFG connected. Open a CFG once, then Save CFG writes changes to that file.':'This browser does not support direct file access. Open the editor in Chrome or Edge instead.',!directSupported);}
function cfgForSaving(){
  for(const p of all()){
    K.validate(p);
    if(!K.points(p).length)throw Error(`"${K.get(p,'name')}" has no waypoints. Add a waypoint or delete the empty plan before saving.`);
  }
  return K.serialize(tree);
}
const originalNewCfg=$('newCfg').onclick;
$('newCfg').onclick=()=>{const previousTree=tree;originalNewCfg();if(tree!==previousTree)detachDirectFile();};
async function openDirectCfg(preferred=null){
  if(fileBusy||!directSupported)return;
  if(dirty&&!confirm('Open another CFG and replace this workspace? Save your changes first if needed.'))return;
  const start=snapshot();fileBusy=true;updateFileControls();
  try{
    let handle=preferred;
    if(handle){if(await handle.queryPermission({mode:'read'})!=='granted'&&await handle.requestPermission({mode:'read'})!=='granted')throw Error('Permission to open the default CFG was not granted.');}
    else [handle]=await window.showOpenFilePicker({multiple:false,types:cfgTypes});
    const file=await handle.getFile();
    if(file.size>8*1024*1024)throw Error('CFG exceeds the 8 MB limit.');
    const text=await file.text(),parsed=K.parse(text),warnings=K.inspect(parsed);
    if(warnings.length&&!confirm('Import review\n\n'+warnings.slice(0,18).join('\n')+'\n\nUneditable content will be preserved on save. Continue?'))return;
    if(snapshot()!==start&&!confirm('The workspace changed while opening the file. Replace those changes with the selected CFG?'))return;
    tree=parsed;active=all().length?0:-1;selected=current()&&waypoints().length?0:-1;
    cfgFilename=handle.name;directHandle=handle;directText=text;if(typeof rememberCfg==='function')rememberCfg(handle);undo=[];redo=[];dirty=false;
    $('importStatus').hidden=true;render();fit();
    fileStatus(`Connected: ${handle.name}`);
  }catch(error){if(error.name!=='AbortError')fileStatus('Could not open CFG: '+error.message,true);}
  finally{fileBusy=false;updateFileControls();}
};
$('openDirect').onclick=()=>openDirectCfg();
async function writeDirect(asNew=false){
  if(fileBusy||!directSupported)return;
  fileBusy=true;updateFileControls();let writable=null;
  try{
    // Validate synchronously, before any picker/write, while the click is still a user gesture.
    const output=cfgForSaving();
    let handle=directHandle,baseline=directText;
    if(asNew||!handle){
      handle=await window.showSaveFilePicker({suggestedName:normalizedFilename($('cfgFilename').value),types:cfgTypes});
      // Save As uses the browser's destination selection/overwrite confirmation.
      baseline=await (await handle.getFile()).text();
    }
    const savedSnapshot=snapshot();
    // Edits made while a picker was open should not be silently marked saved.
    const stillCurrent=()=>K.serialize(tree)===output&&snapshot()===savedSnapshot;
    const actual=await (await handle.getFile()).text();
    if(actual!==baseline)throw Error('The CFG changed on disk since it was opened or last saved. Reopen it to load those changes, or use Save CFG As… to keep your edited copy.');
    writable=await handle.createWritable();
    await writable.write(output);await writable.close();writable=null;
    const unchanged=stillCurrent();directHandle=handle;directText=output;cfgFilename=handle.name;if(typeof rememberCfg==='function')rememberCfg(handle);
    dirty=!unchanged;render();
    fileStatus(`Saved directly to ${handle.name}.${unchanged?'':' Newer edits remain unsaved; click Save CFG again.'}`);
  }catch(error){
    if(writable){try{await writable.abort();}catch{}}
    if(error.name!=='AbortError')fileStatus('Save failed: '+error.message,true);
  }finally{fileBusy=false;updateFileControls();}
}
$('saveDirect').onclick=()=>writeDirect(false);
$('saveAsDirect').onclick=()=>writeDirect(true);
detachDirectFile();
