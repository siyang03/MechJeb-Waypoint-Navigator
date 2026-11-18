var directResult={passed:0,failed:0};
var directTests=(async()=>{
const assert=(v,msg='Assertion failed')=>{if(!v)throw Error(msg);};
async function test(name,fn){try{await fn();directResult.passed++;console.log('PASS FILE '+name);}catch(e){directResult.failed++;console.log('FAIL FILE '+name+': '+e.message);}}
const data=K.node('__root__'),plan=K.plan('File route');K.children(plan,'WayPoints')[0].children.push(K.waypoint(1,2));K.addPlan(data,plan);
const file=memoryFile('Original.cfg',K.serialize(data));nextOpen=file;
await test('Opening binds a file and loads its plans',async()=>{dirty=false;await $('openDirect').onclick();assert(directHandle===file);assert(K.get(current(),'name')==='File route');assert(!dirty);assert(undo.length===0);assert($('cfgFilename').readOnly);});
await test('Save updates same handle without a download',async()=>{const n=downloads.length;change(()=>K.set(current(),'description','Edited'));await $('saveDirect').onclick();assert(file.writes===1);assert(file.text.includes('description = Edited'));assert(downloads.length===n);assert(!dirty);});
await test('External file changes prevent overwrite',async()=>{file.text+='// External change\n';const before=file.text;change(()=>K.set(current(),'description','Pending'));await $('saveDirect').onclick();assert(file.text===before);assert(file.writes===1);assert(dirty);assert($('directFileStatus').textContent.includes('changed on disk'));});
await test('Save As chooses another handle and filename',async()=>{nextSave=memoryFile('Renamed.cfg','');await $('saveAsDirect').onclick();assert(directHandle===nextSave);assert(cfgFilename==='Renamed.cfg');assert(nextSave.text.includes('Pending'));assert(!dirty);assert(file.writes===1);});
await test('Failed writing is aborted and edits remain unsaved',async()=>{const handle=directHandle,before=handle.text;handle.fail=true;change(()=>K.set(current(),'description','Denied'));await $('saveDirect').onclick();assert(handle.text===before);assert(handle.aborted);assert(dirty);handle.fail=false;});
await test('Edits during writing remain dirty',async()=>{directHandle.onWrite=()=>{K.set(current(),'description','Newer edit');};await $('saveDirect').onclick();assert(dirty);assert(directHandle.text.includes('Denied'));assert(K.get(current(),'description')==='Newer edit');directHandle.onWrite=null;await $('saveDirect').onclick();assert(!dirty);assert(directHandle.text.includes('Newer edit'));});
await test('Cancelled picker preserves binding and workspace',async()=>{const before=snapshot(),handle=directHandle;pickerError={name:'AbortError',message:'Cancelled'};await $('openDirect').onclick();assert(snapshot()===before);assert(directHandle===handle);pickerError=null;});
await test('New CFG disconnects old file; blank CFG can be saved',async()=>{const previous=directHandle,text=previous.text;$('newCfg').click();assert(!directHandle);assert(!$('cfgFilename').readOnly);nextSave=memoryFile('Blank.cfg','');await $('saveDirect').onclick();assert(directHandle===nextSave);assert(K.plans(K.parse(nextSave.text)).length===0);assert(previous.text===text);});
console.log(`\nDIRECT FILE: ${directResult.passed} passed, ${directResult.failed} failed`);
})();
