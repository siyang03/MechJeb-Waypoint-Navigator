/* Runs against either real editor through run-macos.py [editor directory]. */
(function(){
let passed=0,failed=0;
const assert=(v,msg='Assertion failed')=>{if(!v)throw Error(msg);};
const near=(a,b)=>assert(Math.abs(a-b)<1e-9,`${a} != ${b}`);
function test(name,fn){try{fn();passed++;console.log('PASS KSR '+name);}catch(e){failed++;console.log('FAIL KSR '+name+': '+e.message);}}
const sites=KerbinSide.airfields,find=id=>sites.find(s=>s.id===id);
const record=fn=>{const calls=[],saved={};for(const key of ['arc','moveTo','lineTo','fillText','strokeText','clip','rect','stroke']){saved[key]=ctx[key];ctx[key]=(...args)=>calls.push({key,args});}try{fn();return calls;}finally{for(const key in saved)ctx[key]=saved[key];}};
const arcs=calls=>calls.filter(c=>c.key==='arc');
const labels=calls=>calls.filter(c=>c.key==='fillText');
const history=()=>JSON.stringify({cfg:K.serialize(tree),snapshot:snapshot(),undo,redo,dirty});
const point=(name,p,extra={})=>canvas.events[name]({pointerId:1,button:0,clientX:p.x,clientY:p.y,preventDefault(){},...extra});
test('20 unique immutable valid airports, excluding stock airports',()=>{
 assert(sites.length===20&&new Set(sites.map(s=>s.id)).size===20&&Object.isFrozen(sites));
 for(const s of sites){assert(Object.isFrozen(s)&&Object.isFrozen(s.axes));assert(Number.isFinite(s.lat)&&Math.abs(s.lat)<=90);assert(Number.isFinite(s.lon)&&Math.abs(s.lon)<=180);assert(s.axes.length>=1);for(const axis of s.axes){assert(Object.isFrozen(axis)&&axis.length===2);for(const h of axis)assert(Number.isFinite(h)&&h>=0&&h<360);}assert(!/^(KSC|Island Airfield|Desert Airfield)$/.test(s.name));}
});
test('Crossing axes and recorded nonreciprocal polar headings preserved',()=>{
 assert(JSON.stringify(sites.filter(s=>s.axes.length===2).map(s=>s.id).sort())==='["dununda","meeda","south-lake"]');
 assert(JSON.stringify(find('meeda').axes)==='[[73,253],[133,313]]');assert(JSON.stringify(find('dununda').axes)==='[[26,206],[87,266]]');assert(JSON.stringify(find('south-lake').axes)==='[[41,221],[71,251]]');
 assert(KerbinSide.label(find('kermundsen'))==='Kermundsen (019/185)');assert(KerbinSide.label(find('kola-island'))==='Kola Island (016/196)');assert(KerbinSide.label(find('dununda'))==='Dununda (026/206, 087/266)');
});
test('Cardinal screen directions',()=>{for(const [h,x,y] of [[0,0,-1],[90,1,0],[180,0,1],[270,-1,0]]){const e=KerbinSide.runwayDirection(0,h,2048,1022);near(e.x,x);near(e.y,y);}});
test('45 degrees at latitude 60 uses equirectangular distortion and natural dimensions',()=>{const e=KerbinSide.runwayDirection(60,45,2048,1022),ratio=2048/(2*1022*.5);near(e.x/-e.y,ratio);near(e.x,ratio/Math.hypot(ratio,1));});
test('All bearings normalize finitely, including exact poles',()=>{for(const lat of [-90,-89.87486267,0,60,90])for(let h=0;h<360;h++){const e=KerbinSide.runwayDirection(lat,h,8192,4096);assert(Number.isFinite(e.x)&&Number.isFinite(e.y));near(Math.hypot(e.x,e.y),1);}for(const s of sites)for(const a of s.axes){const e=KerbinSide.runwayDirection(s.lat,a[0],width,height);near(Math.hypot(e.x,e.y),1);}});
test('Defaults off and renders no KSR symbols',()=>{assert(!showKerbinSide);assert(record(drawKerbinSideAirfields).length===0);});
test('Toggle draws 20 airports with three wrapped copies, without history changes',()=>{const before=history();fit();$('toggleKerbinSide').click();assert(showKerbinSide);assert($('toggleKerbinSide').getAttribute('aria-pressed')==='true');assert(arcs(record(drawKerbinSideAirfields)).length===60);assert(history()===before);assert(visibleLocations().length===0);});
test('Labels switch at 1.6 times fit, using three-digit headings',()=>{fit();zoom(1.599);assert(labels(record(drawKerbinSideAirfields)).length===0);fit();zoom(1.6);const text=labels(record(drawKerbinSideAirfields));assert(text.length===60);assert(text.some(c=>c.args[0]==='Kermundsen (019/185)'));});
test('Each crossing axis renders independently with fixed screen length',()=>{const calls=record(drawKerbinSideAirfields),moves=calls.filter(c=>c.key==='moveTo'),lines=calls.filter(c=>c.key==='lineTo');assert(moves.length===23*3&&lines.length===23*3);moves.forEach((c,i)=>near(Math.hypot(c.args[0]-lines[i].args[0],c.args[1]-lines[i].args[1]),16));});
test('Symbols follow pan, resize, map offset and longitude seam',()=>{
 const saved={...view},oldOffset=offset,w=canvas.clientWidth,h=canvas.clientHeight;
 try{view.x+=37;view.y-=11;offset=find('kola-island').lon+180;const calls=arcs(record(drawKerbinSideAirfields)),q=screen(sites[0]),W=width*view.scale;near(calls[0].args[0],q.x-W);near(calls[1].args[0],q.x);near(calls[2].args[0],q.x+W);near(calls[1].args[1],q.y);near(K.wrap(K.toGeo((q.x-view.x)/view.scale,(q.y-view.y)/view.scale,width,height,offset).lon-sites[0].lon),0);
 canvas.clientWidth=400;canvas.clientHeight=200;fit();const resized=arcs(record(drawKerbinSideAirfields));near(resized[1].args[0],screen(sites[0]).x);assert(resized.every(c=>c.args.every(Number.isFinite)));
 }finally{offset=oldOffset;canvas.clientWidth=w;canvas.clientHeight=h;Object.assign(view,saved);}
});
test('Draw clips KSR symbols inside the existing map rectangle',()=>{const calls=record(draw),clip=calls.findIndex(c=>c.key==='clip'),first=calls.findIndex(c=>c.key==='arc');assert(clip>=0&&first>clip);const rect=calls.find(c=>c.key==='rect');near(rect.args[0],view.x);near(rect.args[2],width*view.scale);});
test('Stock toggles and Show Waypoints remain independent',()=>{
 const before=history();$('toggleAnomalies').click();assert(visibleLocations().length===9);assert(arcs(record(drawKerbinSideAirfields)).length===60);$('toggleAnomalies').click();
 if($('showWaypoints').onchange){
   const saved=snapshot();append(0,0);const shown=arcs(record(draw)).length;
   $('showWaypoints').checked=false;$('showWaypoints').onchange();assert(arcs(record(draw)).length===shown-3);assert(arcs(record(drawKerbinSideAirfields)).length===60);
   const q=screen({lat:0,lon:0});point('pointerdown',q);assert(gesture.hit===-1);canvas.events.pointercancel();
   $('undo').click();restore(saved);$('showWaypoints').checked=true;$('showWaypoints').onchange();
   // The edit/undo above intentionally changes history; restore it for the independence assertion.
   const previous=JSON.parse(before);undo=previous.undo;redo=previous.redo;dirty=previous.dirty;
 }

 assert(history()===before);
});
test('Clicking KSR symbol is an ordinary map click; waypoint drag and pan still work',()=>{
 fit();const before=snapshot(),q=screen(find('kojave-sands')),n=waypoints().length;point('pointerdown',q);point('pointerup',q);assert(waypoints().length===n+1);assert(typeof selectLocation==='undefined');
 const original=K.geo(waypoints().at(-1));point('pointerdown',q);point('pointermove',{x:q.x+15,y:q.y+8});point('pointerup',{x:q.x+15,y:q.y+8});assert(K.geo(waypoints().at(-1)).lon!==original.lon);$('undo').click();near(K.geo(waypoints().at(-1)).lon,original.lon);
 const x=view.x;point('pointerdown',q,{button:2});point('pointermove',{x:q.x+20,y:q.y+10},{button:2});point('pointerup',{x:q.x+20,y:q.y+10},{button:2});near(view.x,x+20);restore(before);fit();const scale=view.scale;canvas.events.wheel({clientX:450,clientY:250,deltaY:-100,preventDefault(){}});assert(view.scale>scale);fit();
});
test('Toggle OFF hides symbols without modifying CFG, dirty flag or undo/redo',()=>{const before=history();$('toggleKerbinSide').click();assert(!showKerbinSide);assert($('toggleKerbinSide').getAttribute('aria-pressed')==='false');assert(record(drawKerbinSideAirfields).length===0);assert(history()===before);});
globalThis.kerbinSideResult={passed,failed};console.log(`\nKSR: ${passed} passed, ${failed} failed`);
})();
