(function(){
let passed=0,failed=0;
const assert=(v,msg='Assertion failed')=>{if(!v)throw Error(msg);};
const near=(a,b)=>assert(Math.abs(a-b)<.000001,`${a} != ${b}`);
function test(name,fn){try{fn();passed++;console.log('PASS OVERLAY '+name);}catch(e){failed++;console.log('FAIL OVERLAY '+name+': '+e.message);}}
const point=(name,p,extra={})=>canvas.events[name]({pointerId:1,button:0,clientX:p.x,clientY:p.y,preventDefault(){},...extra});
test('Location data has unique IDs, valid Kerbin coordinates and source links',()=>{assert(new Set(KerbinLocations.map(s=>s.id)).size===27);for(const s of KerbinLocations){assert(Number.isFinite(s.lat)&&Math.abs(s.lat)<=90);assert(Number.isFinite(s.lon)&&Math.abs(s.lon)<=180);assert(s.source.startsWith('https://'));assert(['launch','anomaly','commnet'].includes(s.kind));}});
test('Stock layers start hidden and remain independent without editing CFG or history',()=>{
 assert(visibleLocations().length===0);const before=JSON.stringify({cfg:snapshot(),undo,redo,dirty});
 $('toggleAnomalies').click();assert(visibleLocations().length===9);assert($('toggleAnomalies').getAttribute('aria-pressed')==='true');
 $('toggleLaunches').click();assert(visibleLocations().length===20);$('toggleCommNet').click();assert(visibleLocations().length===27);
 $('toggleAnomalies').click();assert(visibleLocations().length===18);$('toggleLaunches').click();assert(visibleLocations().length===7);$('toggleCommNet').click();assert(visibleLocations().length===0);
 assert(JSON.stringify({cfg:snapshot(),undo,redo,dirty})===before);
});
test('Launch references include separate stock facilities',()=>{
 for(const id of ['woomerang','cove','glacier','crater','mahi','dessert-runway','ksc-pad','old-ksc'])assert(KerbinLocations.some(s=>s.id===id));
 const find=id=>KerbinLocations.find(s=>s.id===id);near(find('island').lat,-1.529278);near(find('island').lon,-71.885306);assert(Math.abs(find('crater').lon)>179);
 assert(K.distance(find('woomerang'),find('comm-larkes'))>1000);assert(K.distance(find('dessert'),find('dessert-runway'))>100);assert(find('ksc').lat!==find('ksc-pad').lat);
});
test('Stock reference icons have no selection or details behavior',()=>{assert(typeof hitLocation==='undefined');assert(typeof selectLocation==='undefined');assert(typeof selectedLocation==='undefined');});
test('Clicking a stock icon creates an ordinary waypoint; clicking that waypoint selects it',()=>{
 fit();$('toggleLaunches').click();const before=snapshot(),q=screen(KerbinLocations.find(s=>s.id==='island')),n=waypoints().length;
 point('pointerdown',q);assert(gesture.hit===-1);assert(!('location' in gesture));point('pointerup',q);assert(waypoints().length===n+1);
 const w=waypoints().at(-1);assert(K.get(w,'name').startsWith('WP '));near(K.geo(w).lat,-1.529278);
 selected=-1;point('pointerdown',q);point('pointerup',q);assert(selected===n);assert(waypoints().length===n+1);
 $('undo').click();restore(before);$('toggleLaunches').click();
});
test('Dragging reference icons never moves reference data or creates a waypoint',()=>{
 fit();$('toggleAnomalies').click();const site=KerbinLocations.find(s=>s.id==='sputnik'),q=screen(site),before=snapshot(),data=JSON.stringify(site);
 point('pointerdown',q);point('pointermove',{x:q.x+30,y:q.y+20});point('pointerup',{x:q.x+30,y:q.y+20});assert(snapshot()===before);assert(JSON.stringify(site)===data);$('toggleAnomalies').click();
});
test('Reference symbols retain wrapping and zoom labels',()=>{
 const oldArc=ctx.rect,oldText=ctx.fillText,rects=[],texts=[];
 try{$('toggleLaunches').click();fit();ctx.rect=(...args)=>rects.push(args);ctx.fillText=(...args)=>texts.push(args);drawLocations();assert(rects.length===33);assert(texts.length===0);
 const W=width*view.scale,q=screen(KerbinLocations.find(s=>s.id==='island'));near(rects[1][0]+5,q.x);near(rects[2][0]-rects[1][0],W);
 zoom(1.6);texts.length=0;drawLocations();assert(texts.length===33);
 }finally{ctx.rect=oldArc;ctx.fillText=oldText;$('toggleLaunches').click();fit();}
});
test('Ordinary map clicks and right-drag still work with overlays',()=>{fit();const q=screen({lat:0,lon:0}),n=waypoints().length;point('pointerdown',q);point('pointerup',q);assert(waypoints().length===n+1);const x=view.x;point('pointerdown',q,{button:2});point('pointermove',{x:q.x+20,y:q.y+10},{button:2});point('pointerup',{x:q.x+20,y:q.y+10},{button:2});near(view.x,x+20);assert(waypoints().length===n+1);});
test('Insert before/after preserves ordering and supports edit and undo',()=>{
  const before=snapshot();selected=0;render();const anchor=waypoints()[0],n=waypoints().length;
  $('insertBefore').click();assert(waypoints().length===n+1);assert(waypoints()[1]===anchor);assert(selected===0);
  near(K.geo(waypoints()[0]).lat,K.geo(anchor).lat);near(K.geo(waypoints()[0]).lon,K.wrap(K.geo(anchor).lon-5));assert(K.geo(waypoints()[0]).alt===defaultAlt);
  $('undo').click();selected=waypoints().length-1;render();const last=waypoints()[selected];
  $('insertAfter').click();assert(waypoints().at(-2)===last);assert(selected===waypoints().length-1);near(K.geo(waypoints()[selected]).lon,K.wrap(K.geo(last).lon+5));
  assert(K.get(waypoints()[selected],'Vertical')==='true');assert(!K.get(waypoints()[selected],'RW'));
  $('undo').click();assert(waypoints().length===n);restore(before);
});
globalThis.overlayResult={passed,failed};console.log(`\nOVERLAY: ${passed} passed, ${failed} failed`);
})();
