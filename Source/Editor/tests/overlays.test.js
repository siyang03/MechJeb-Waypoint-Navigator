(function(){
let passed=0,failed=0;
const assert=(v,msg='Assertion failed')=>{if(!v)throw Error(msg);};
const near=(a,b)=>assert(Math.abs(a-b)<.000001,`${a} != ${b}`);
function test(name,fn){try{fn();passed++;console.log('PASS OVERLAY '+name);}catch(e){failed++;console.log('FAIL OVERLAY '+name+': '+e.message);}}
const point=(name,p,extra={})=>canvas.events[name]({pointerId:1,button:0,clientX:p.x,clientY:p.y,preventDefault(){},...extra});
test('Location data has unique IDs, valid Kerbin coordinates and source links',()=>{assert(new Set(KerbinLocations.map(s=>s.id)).size===27);for(const s of KerbinLocations){assert(Number.isFinite(s.lat)&&Math.abs(s.lat)<=90);assert(Number.isFinite(s.lon)&&Math.abs(s.lon)<=180);assert(s.source.startsWith('https://'));assert(['launch','anomaly','commnet'].includes(s.kind));}});
test('All three layers start hidden',()=>{assert(visibleLocations().length===0);assert($('locationsPanel').hidden);});
test('Anomaly toggle only reveals anomalies without editing CFG',()=>{const before=snapshot();$('toggleAnomalies').click();assert(visibleLocations().length===9);assert(visibleLocations().every(s=>s.kind==='anomaly'));assert(snapshot()===before);assert(!$('locationsPanel').hidden);});
test('Launch layer includes separate runways, pads and discoverable sites',()=>{$('toggleLaunches').click();assert(visibleLocations().length===20);$('toggleAnomalies').click();assert(visibleLocations().length===11);assert(visibleLocations().every(s=>s.kind==='launch'));for(const id of ['woomerang','cove','glacier','crater','mahi','dessert-runway','ksc-pad','old-ksc'])assert(visibleLocations().some(s=>s.id===id));});
test('Clicking an airfield selects details without creating a waypoint',()=>{const site=KerbinLocations.find(s=>s.id==='island'),p=screen(site),before=waypoints().length;point('pointerdown',p);point('pointerup',p);assert(selectedLocation===site);assert(waypoints().length===before);assert(!$('locationDetails').hidden);assert($('locationSource').href===site.source);});
test('Explicit Add as waypoint uses planning altitude and supports undo',()=>{const n=waypoints().length;$('addLocation').click();assert(waypoints().length===n+1);const w=waypoints().at(-1);near(K.geo(w).lat,selectedLocation.lat);assert(K.geo(w).alt===10000);assert(K.get(w,'name')==='Island Airfield');$('undo').click();assert(waypoints().length===n);});
test('Location list can select a site and expose DLC note',()=>{const dessert=KerbinLocations.find(s=>s.id==='dessert');$('locationList').children.find(b=>b.textContent.includes('Dessert')).click();assert(selectedLocation===dessert);assert($('locationNote').textContent.includes('Making History'));});
test('Hidden layer cannot be hit and clears its selected detail',()=>{$('toggleLaunches').click();assert(!selectedLocation);assert(!hitLocation(screen(KerbinLocations[0])));assert($('locationsPanel').hidden);});
test('CommNet toggle is independent and inspection does not edit the CFG',()=>{
  const before=snapshot();$('toggleCommNet').click();assert(visibleLocations().length===7);assert(visibleLocations().every(s=>s.kind==='commnet'));
  const site=KerbinLocations.find(s=>s.id==='comm-larkes');selectLocation(site);assert($('locationNote').textContent.includes('Separate from the launchpad'));
  assert(snapshot()===before);$('toggleLaunches').click();assert(visibleLocations().length===18);$('toggleLaunches').click();assert(visibleLocations().length===7);
  $('addLocation').click();const w=waypoints().at(-1);near(K.geo(w).lat,site.lat);near(K.geo(w).lon,site.lon);assert(K.geo(w).alt===10000);$('undo').click();assert(snapshot()===before);
  $('toggleCommNet').click();assert(!selectedLocation);assert(visibleLocations().length===0);
});
test('Updated launch references distinguish facilities and cross the seam',()=>{
  const find=id=>KerbinLocations.find(s=>s.id===id);near(find('island').lat,-1.529278);near(find('island').lon,-71.885306);
  assert(Math.abs(find('crater').lon)>179);assert(K.distance(find('woomerang'),find('comm-larkes'))>1000);
  assert(K.distance(find('dessert'),find('dessert-runway'))>100);assert(find('ksc').lat!==find('ksc-pad').lat);
});
test('Hit detection survives zoom and pan',()=>{$('toggleAnomalies').click();zoom(3);view.x-=80;view.y+=25;const site=KerbinLocations.find(s=>s.id==='ufo');assert(hitLocation(screen(site))===site);});
test('Reference markers wrap across longitude seam',()=>{const site=KerbinLocations.find(s=>s.id==='ufo'),q=screen(site);assert(hitLocation({x:q.x+width*view.scale,y:q.y})===site);});
test('Anomaly is not dragged or appended by dragging its marker',()=>{const site=KerbinLocations.find(s=>s.id==='sputnik'),q=screen(site),before=snapshot();point('pointerdown',q);point('pointermove',{x:q.x+30,y:q.y+20});point('pointerup',{x:q.x+30,y:q.y+20});assert(snapshot()===before);});
test('Blank CFG disables Add; overlay data is not exported',()=>{tree=K.node('__root__');active=-1;selected=-1;render();selectLocation(KerbinLocations.find(s=>s.id==='ufo'));assert($('addLocation').disabled);assert(!K.serialize(tree).includes('Flying Saucer'));$('newPlan').click();assert(!$('addLocation').disabled);$('addLocation').click();assert(waypoints().length===1);assert(K.get(waypoints()[0],'Vertical')==='true');assert(!K.get(waypoints()[0],'RW'));});
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
