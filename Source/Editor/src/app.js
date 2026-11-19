/* Local-file entry point: ordinary scripts intentionally avoid file:// module restrictions. */
'use strict';
const K=window.Kerbin, $=id=>document.getElementById(id);
let tree=K.node('__root__'), active=0, selected=-1, undo=[], redo=[], dirty=false, cfgFilename='FlightPlans.cfg';
let offset=0, defaultAlt=10000, speed=300, image=null;
const view={scale:1,x:0,y:0}, canvas=$('map'), ctx=canvas.getContext('2d');
let width=2048,height=1022,gesture=null;
const layers={anomaly:false,launch:false,commnet:false};
const locationStyles={anomaly:{symbol:'◆',color:'#ffd27a',label:'Anomalies'},launch:{symbol:'■',color:'#90d9ff',label:'Launch sites / runways'},commnet:{symbol:'▲',color:'#c6acff',label:'CommNet groundstations'}};
let selectedLocation=null;
K.addPlan(tree,K.plan('Untitled route'));
const all=()=>K.plans(tree), current=()=>all()[active], waypoints=()=>current()?K.points(current()):[];
const snapshot=()=>JSON.stringify({tree,active,selected,offset,defaultAlt,speed,cfgFilename});
function checkpoint(before=snapshot()){undo.push(before);if(undo.length>80)undo.shift();redo=[];dirty=true;}
function restore(s){const data=JSON.parse(s);({tree,active,selected,offset,defaultAlt,speed}=data);cfgFilename=data.cfgFilename||'FlightPlans.cfg';render();}
function change(fn){const before=snapshot();try{fn();if(snapshot()!==before)checkpoint(before);render();}catch(e){restore(before);message(e.message,true);}}
function message(text,error=false){if(error){$('importStatus').hidden=false;$('importStatus').textContent=text;$('importStatus').classList.toggle('error',true);}}
function uniqueName(base,used=all().map(p=>K.get(p,'name'))){let name=base,i=2;while(used.includes(name))name=`${base} (${i++})`;return name;}
function km(n){return (n/1000).toLocaleString(undefined,{maximumFractionDigits:1,minimumFractionDigits:1})+' km';}
function duration(seconds){if(!Number.isFinite(seconds))return '—';const mins=Math.round(seconds/60);return `${Math.floor(mins/60)}h ${String(mins%60).padStart(2,'0')}m`;}
function field(id,value){$(id).value=value;}
function render(){
  const plans=all();active=Math.min(Math.max(0,active),plans.length-1);const p=current(),pts=waypoints();if(selected>=pts.length)selected=pts.length-1;
  $('plans').replaceChildren(...plans.map((p,i)=>{const o=document.createElement('option');o.value=i;o.textContent=K.get(p,'name');return o;}));$('plans').value=active;
  ['duplicate','deletePlan','planName','description'].forEach(id=>$(id).disabled=!p);
  field('cfgFilename',cfgFilename);field('planName',p?K.get(p,'name'):'');field('description',p?K.get(p,'description'):'');field('defaultAlt',defaultAlt);field('speed',speed);
  const s=p?K.stats(p):{total:0,rows:[]};$('total').textContent=km(s.total);$('duration').textContent=duration(s.total/speed);$('count').textContent=pts.length;$('routeCount').textContent=String(pts.length).padStart(2,'0');
  $('waypoints').replaceChildren(...pts.map((w,i)=>{const li=document.createElement('li'),b=document.createElement('button'),number=document.createElement('span'),title=document.createElement('span'),coords=document.createElement('small'),dist=document.createElement('span'),cum=document.createElement('small');b.classList.toggle('active',i===selected);b.setAttribute('aria-pressed',String(i===selected));number.className='wp-number';number.textContent=i+1;title.className='wp-title';title.textContent=K.get(w,'name')||'Unnamed';const g=K.geo(w);coords.textContent=`${g.lat.toFixed(3)}°, ${g.lon.toFixed(3)}° · ${g.alt} m`;title.append(coords);dist.className='wp-distance';dist.textContent=i?km(s.rows[i].leg):'START';cum.textContent=km(s.rows[i].cumulative);dist.append(cum);b.append(number,title,dist);b.onclick=()=>{selected=i;render();};li.append(b);return li;}));
  $('insertBefore').disabled=$('insertAfter').disabled=!pts[selected];
  $('noPoints').hidden=pts.length>0;$('waypointEditor').hidden=selected<0||!pts[selected];
  if(pts[selected]){const w=pts[selected],g=K.geo(w);$('editorTitle').textContent=`Waypoint ${selected+1}`;field('wpName',K.get(w,'name'));field('lat',g.lat);field('lon',g.lon);field('alt',g.alt);$('flags').textContent='Preserved flags: '+(w.children.filter(e=>e.type==='value'&&!['lat','lon','alt','name'].includes(e.key)).map(e=>`${e.key} = ${e.value}`).join(' · ')||'none');}
  $('up').disabled=selected<=0;$('down').disabled=selected<0||selected>=pts.length-1;$('undo').disabled=!undo.length;$('redo').disabled=!redo.length;
  renderLocations();if(typeof schedulePrediction==='function')schedulePrediction();draw();
}
function append(lat,lon,name){if(!current()){message('Create a Kerbin flight plan first.',true);return;}change(()=>{K.children(current(),'WayPoints')[0].children.push(K.waypoint(lat,lon,defaultAlt,name||`WP ${waypoints().length+1}`));selected=waypoints().length-1;});}
function insertWaypoint(after){
  const anchor=waypoints()[selected];if(!anchor)return;
  change(()=>{
    const entries=K.children(current(),'WayPoints')[0].children;
    const position=entries.indexOf(anchor)+(after?1:0),coordinates=K.geo(anchor);
    const names=waypoints().map(w=>K.get(w,'name'));
    let number=waypoints().length+1;while(names.includes(`WP ${number}`))number++;
    entries.splice(position,0,K.waypoint(coordinates.lat,K.wrap(coordinates.lon+(after?5:-5)),defaultAlt,`WP ${number}`));
    selected+=after?1:0;
  });
}
$('insertBefore').onclick=()=>insertWaypoint(false);
$('insertAfter').onclick=()=>insertWaypoint(true);
function normalizedFilename(raw){
  let name=raw.trim();
  if(!name||/[<>:"/\\|?*\x00-\x1f]/.test(name))throw Error('Enter a filename without folder paths or special characters.');
  if(!/\.cfg$/i.test(name))name+='.cfg';
  if(!name.slice(0,-4).trim()||name==='..cfg'||name==='...cfg')throw Error('Enter a name before .cfg.');
  return name;
}
$('cfgFilename').onchange=()=>change(()=>{cfgFilename=normalizedFilename($('cfgFilename').value);});
$('newCfg').onclick=()=>{
  if(tree.children.length&&!confirm('Start a new blank CFG file? Your current workspace will be replaced. Download it first if needed; Undo can restore it.'))return;
  change(()=>{tree=K.node('__root__',[K.node('KramaxAutoPilotPlans',[K.node('Kerbin')])]);active=-1;selected=-1;cfgFilename='FlightPlans.cfg';});
  $('importStatus').hidden=true;
  message('Created a blank CFG file. Rename it in CFG filename, then add a New flight plan or export the blank file.');
};
$('newPlan').onclick=()=>change(()=>{const p=K.plan(uniqueName('Untitled route'));K.addPlan(tree,p);active=all().indexOf(p);selected=-1;});
$('duplicate').onclick=()=>change(()=>{const copy=K.clone(current());K.set(copy,'name',uniqueName(K.get(copy,'name')+' copy'));K.addPlan(tree,copy);active=all().indexOf(copy);selected=-1;});
$('deletePlan').onclick=()=>{if(confirm(`Delete plan "${K.get(current(),'name')}"? You can undo this.`))change(()=>{K.removeNode(tree,current());selected=-1;});};
$('plans').onchange=()=>{active=Number($('plans').value);selected=-1;render();};
$('planName').onchange=()=>change(()=>{const name=K.safeText($('planName').value.trim());if(!name)throw Error('A flight plan needs a name.');if(all().some(p=>p!==current()&&K.get(p,'name')===name))throw Error('A plan with that name already exists. Choose a different name; existing plans will be kept.');K.set(current(),'name',name);});
$('description').onchange=()=>change(()=>K.set(current(),'description',K.safeText($('description').value)));

$('editForm').onsubmit=e=>{e.preventDefault();change(()=>{const lat=Number($('lat').value),lon=Number($('lon').value),alt=Number($('alt').value),name=K.safeText($('wpName').value.trim());if(!name||!Number.isFinite(lat)||Math.abs(lat)>90||!Number.isFinite(lon)||Math.abs(lon)>180||!Number.isFinite(alt))throw Error('Enter a name, latitude from −90 to 90, longitude from −180 to 180, and a finite altitude.');const w=waypoints()[selected];for(const [key,v] of Object.entries({lat,lon,alt,name}))K.set(w,key,v);});};
$('applyAllAlt').onclick=()=>{
  const raw=$('alt').value.trim(),alt=Number(raw);
  if(!raw||!Number.isFinite(alt)){message('Enter a finite altitude in meters ASL before applying it to all waypoints.',true);return;}
  if(!current()||!waypoints().length)return;
  const count=waypoints().length;
  change(()=>{for(const w of waypoints())K.set(w,'alt',alt);});
  message(`Set all ${count} waypoints in the current plan to ${alt} m ASL. Undo restores their previous altitudes.`);
};
function reorder(delta){change(()=>{const pts=waypoints(),a=pts[selected],b=pts[selected+delta],entries=K.children(current(),'WayPoints')[0].children;if(!a||!b)return;const i=entries.indexOf(a),j=entries.indexOf(b);[entries[i],entries[j]]=[entries[j],entries[i]];selected+=delta;});}
$('up').onclick=()=>reorder(-1);$('down').onclick=()=>reorder(1);$('deleteWp').onclick=()=>change(()=>{const w=waypoints()[selected];if(w)K.removeNode(current(),w);});
$('undo').onclick=()=>{if(!undo.length)return;redo.push(snapshot());restore(undo.pop());dirty=true;message('Undid the last edit.');};$('redo').onclick=()=>{if(!redo.length)return;undo.push(snapshot());restore(redo.pop());dirty=true;message('Redid the last edit.');};
for(const [id,min,max] of [['defaultAlt',-Infinity,Infinity],['speed',1,Infinity]])$(id).onchange=()=>change(()=>{const v=Number($(id).value);if(!$(id).value.trim()||!Number.isFinite(v)||v<min||v>max)throw Error(`Invalid ${id} setting.`);if(id==='defaultAlt')defaultAlt=v;else speed=v;});
window.addEventListener('keydown',e=>{if(/INPUT|TEXTAREA|SELECT/.test(e.target.tagName)||e.target.isContentEditable)return;if(e.key==='Backspace'&&!e.ctrlKey&&!e.metaKey&&!e.altKey&&waypoints()[selected]){e.preventDefault();if(!e.repeat)$('deleteWp').click();return;}if((e.ctrlKey||e.metaKey)&&e.key.toLowerCase()==='z'){e.preventDefault();$(e.shiftKey?'redo':'undo').click();}if((e.ctrlKey||e.metaKey)&&e.key.toLowerCase()==='y'){e.preventDefault();$('redo').click();}});
window.addEventListener('beforeunload',e=>{if(dirty){e.preventDefault();e.returnValue='';}});
function size(){const b=canvas.getBoundingClientRect(),dpr=window.devicePixelRatio||1;canvas.width=Math.round(b.width*dpr);canvas.height=Math.round(b.height*dpr);ctx.setTransform(dpr,0,0,dpr,0,0);return b;}
function fit(){const b=canvas.getBoundingClientRect();view.scale=Math.min(b.width/width,b.height/height);view.x=(b.width-width*view.scale)/2;view.y=(b.height-height*view.scale)/2;draw();}
function zoom(factor,x=canvas.clientWidth/2,y=canvas.clientHeight/2){const base=Math.min(canvas.clientWidth/width,canvas.clientHeight/height),next=Math.max(base*.3,Math.min(base*30,view.scale*factor)),r=next/view.scale;view.x=x-(x-view.x)*r;view.y=y-(y-view.y)*r;view.scale=next;draw();}
$('zoomIn').onclick=()=>zoom(1.4);$('zoomOut').onclick=()=>zoom(1/1.4);$('fit').onclick=fit;
function screen(p){const q=K.toPixel(p.lat,p.lon,width,height,offset);return {x:view.x+q.x*view.scale,y:view.y+q.y*view.scale};}
function local(e){const b=canvas.getBoundingClientRect();return {x:e.clientX-b.left,y:e.clientY-b.top};}
function at(p){const x=(p.x-view.x)/view.scale,y=(p.y-view.y)/view.scale;if(x<0||x>width||y<0||y>height)return null;return K.toGeo(x,y,width,height,offset);}
// Keep markers compact at normal/high zoom and shrink with the map when zooming out.
function markerScale(){return Math.min(1,view.scale/Math.min(canvas.clientWidth/width,canvas.clientHeight/height));}
function markerRadius(index){return (index===selected?8:7)*markerScale();}
function visibleLocations(){return KerbinLocations.filter(site=>layers[site.kind]);}
function selectLocation(site){selectedLocation=site;renderLocations();draw();}
function renderLocations(){
  const sites=visibleLocations();
  if(selectedLocation&&!sites.includes(selectedLocation))selectedLocation=null;
  $('locationsPanel').hidden=!sites.length;
  $('toggleAnomalies').setAttribute('aria-pressed',String(layers.anomaly));
  $('toggleAnomalies').textContent='Anomalies';
  $('toggleLaunches').setAttribute('aria-pressed',String(layers.launch));
  $('toggleLaunches').textContent='Launch Sites / Runways';
  $('toggleCommNet').setAttribute('aria-pressed',String(layers.commnet));
  $('toggleCommNet').textContent='CommNet Groundstations';
  $('locationsLegend').textContent=Object.entries(locationStyles).filter(([kind])=>layers[kind]).map(([kind,style])=>`${kind==='anomaly'?'◆ / ▮':style.symbol} ${style.label} (${KerbinLocations.filter(site=>site.kind===kind).length})`).join(' · ');
  $('locationList').replaceChildren(...sites.map(site=>{
    const button=document.createElement('button');button.className=site.kind;
    button.textContent=(site.shape==='monolith'?'▮':locationStyles[site.kind].symbol)+' '+site.name;
    button.setAttribute('aria-pressed',String(site===selectedLocation));
    button.onclick=()=>selectLocation(site);return button;
  }));
  $('locationDetails').hidden=!selectedLocation;
  $('addLocation').disabled=!current();
  if(selectedLocation){
    $('locationName').textContent=selectedLocation.name;
    $('locationCoordinates').textContent=`Latitude ${selectedLocation.lat.toFixed(6)}° · Longitude ${selectedLocation.lon.toFixed(6)}°`;
    $('locationNote').textContent=selectedLocation.note+` Adding this site uses the planning altitude (${defaultAlt} m ASL), not ground elevation.`;
    $('locationSource').href=selectedLocation.source;
  }
}
function hitLocation(p){
  const W=width*view.scale,r=10*markerScale();let nearest=null,best=r;
  for(const site of visibleLocations()){
    const q=screen(site),distance=Math.min(...[-W,0,W].map(dx=>Math.hypot(q.x+dx-p.x,q.y-p.y)));
    if(distance<best){best=distance;nearest=site;}
  }
  return nearest;
}
function drawLocations(){
  const W=width*view.scale,scale=markerScale();ctx.save();
  for(const site of visibleLocations()){
    const q=screen(site),r=(site===selectedLocation?7:5)*scale;
    for(const dx of [-W,0,W]){
      const x=q.x+dx,y=q.y;ctx.beginPath();
      if(site.shape==='monolith')ctx.rect(x-r*.5,y-r*1.0125,r,r*2.025);
      else if(site.kind==='launch')ctx.rect(x-r,y-r,r*2,r*2);
      else if(site.kind==='commnet'){ctx.moveTo(x,y-r-1);ctx.lineTo(x+r+1,y+r);ctx.lineTo(x-r-1,y+r);ctx.closePath();}
      else{ctx.moveTo(x,y-r-1);ctx.lineTo(x+r+1,y);ctx.lineTo(x,y+r+1);ctx.lineTo(x-r-1,y);ctx.closePath();}
      ctx.fillStyle='#0b1117';ctx.fill();ctx.strokeStyle=locationStyles[site.kind].color;ctx.lineWidth=1.5*scale;ctx.stroke();
      const relativeZoom=view.scale/Math.min(canvas.clientWidth/width,canvas.clientHeight/height);
      if(site===selectedLocation||relativeZoom>=1.6){
        ctx.font='11px sans-serif';ctx.textAlign='left';ctx.textBaseline='bottom';
        ctx.lineWidth=3;ctx.lineJoin='round';ctx.miterLimit=2;ctx.strokeStyle='#09141d';ctx.strokeText(site.name,x+r+4,y-r);ctx.fillStyle=locationStyles[site.kind].color;ctx.fillText(site.name,x+r+4,y-r);
      }
    }
  }
  ctx.restore();
}
$('toggleAnomalies').onclick=()=>{layers.anomaly=!layers.anomaly;renderLocations();draw();};
$('toggleLaunches').onclick=()=>{layers.launch=!layers.launch;renderLocations();draw();};
$('toggleCommNet').onclick=()=>{layers.commnet=!layers.commnet;renderLocations();draw();};
$('addLocation').onclick=()=>{if(selectedLocation)append(selectedLocation.lat,selectedLocation.lon,selectedLocation.name);};
function draw(){
  const b=size(),s=view.scale,W=width*s,H=height*s;ctx.clearRect(0,0,b.width,b.height);ctx.fillStyle='#09141d';ctx.fillRect(0,0,b.width,b.height);
  ctx.save();ctx.beginPath();ctx.rect(view.x,view.y,W,H);ctx.clip();ctx.fillStyle='#102936';ctx.fillRect(view.x,view.y,W,H);if(image)ctx.drawImage(image,view.x,view.y,W,H);
  ctx.strokeStyle=image?'#d9eaff35':'#315365';ctx.lineWidth=1;
  for(let lat=-90;lat<=90;lat+=30){const y=view.y+(90-lat)/180*H;ctx.beginPath();ctx.moveTo(view.x,y);ctx.lineTo(view.x+W,y);ctx.stroke();}
  for(let lon=-180;lon<180;lon+=30){const x=screen({lat:0,lon}).x;ctx.beginPath();ctx.moveTo(x,view.y);ctx.lineTo(x,view.y+H);ctx.stroke();ctx.fillStyle='#bdd3dc';ctx.font='10px monospace';ctx.fillText(`${lon}°`,x+4,view.y+H-8);}
  drawLocations();
  const pts=waypoints();ctx.strokeStyle='#80bfff';ctx.lineWidth=2.2;
  for(let i=1;i<pts.length;i++){
    const path=K.samples(K.geo(pts[i-1]),K.geo(pts[i])).map(p=>screen(p));
    for(let j=1;j<path.length;j++){while(path[j].x-path[j-1].x>W/2)path[j].x-=W;while(path[j].x-path[j-1].x< -W/2)path[j].x+=W;}
    for(let copy=-2;copy<=2;copy++){ctx.beginPath();path.forEach((p,j)=>j?ctx.lineTo(p.x+copy*W,p.y):ctx.moveTo(p.x+copy*W,p.y));ctx.stroke();}
  }
  if(typeof drawPrediction==='function')drawPrediction();
  const markerSize=markerScale(),ksc=screen({lat:-0.050185,lon:-74.490867}),arm=6*markerSize;ctx.strokeStyle='#ffd27a';ctx.lineWidth=1.2*markerSize;ctx.beginPath();ctx.moveTo(ksc.x-arm,ksc.y);ctx.lineTo(ksc.x+arm,ksc.y);ctx.moveTo(ksc.x,ksc.y-arm);ctx.lineTo(ksc.x,ksc.y+arm);ctx.stroke();if(markerSize>=.6){ctx.fillStyle='#ffd27a';ctx.font=`${10*markerSize}px monospace`;ctx.fillText('KSC',ksc.x+8*markerSize,ksc.y-8*markerSize);}
  for(let i=0;i<pts.length;i++){const p=screen(K.geo(pts[i]));for(const dx of [-W,0,W]){ctx.beginPath();ctx.arc(p.x+dx,p.y,markerRadius(i),0,Math.PI*2);ctx.fillStyle=i===selected?'#c5e3ff':'#102944';ctx.fill();ctx.strokeStyle='#9bceff';ctx.lineWidth=1.3*markerSize;ctx.stroke();ctx.fillStyle=i===selected?'#123353':'#d2eaff';ctx.font=`bold ${9*markerSize}px sans-serif`;ctx.textAlign='center';ctx.textBaseline='middle';if(markerSize>=.6)ctx.fillText(i+1,p.x+dx,p.y);}}ctx.restore();ctx.textAlign='start';ctx.textBaseline='alphabetic';
  $('zoomLabel').textContent=Math.round(s/Math.min(canvas.clientWidth/width,canvas.clientHeight/height)*100)+'%';
}
canvas.addEventListener('wheel',e=>{e.preventDefault();const p=local(e);zoom(Math.exp(-e.deltaY*.0015),p.x,p.y);},{passive:false});
canvas.addEventListener('contextmenu',e=>e.preventDefault());
canvas.addEventListener('pointerdown',e=>{if(gesture)return;if(e.button>2)return;const p=local(e),pan=e.shiftKey||e.button!==0;let hit=-1;for(let i=waypoints().length-1;i>=0;i--){const q=screen(K.geo(waypoints()[i])),W=width*view.scale;if([-W,0,W].some(dx=>Math.hypot(q.x+dx-p.x,q.y-p.y)<markerRadius(i)+3*markerScale())){hit=i;break;}}gesture={id:e.pointerId,start:p,last:p,moved:false,hit:pan?-1:hit,pan,location:!pan&&hit<0?hitLocation(p):null,before:snapshot()};if(!pan&&hit>=0){selected=hit;render();}canvas.setPointerCapture(e.pointerId);});
canvas.addEventListener('pointermove',e=>{const p=local(e),g=at(p);canvas.title=hitLocation(p)?.name||'';$('cursor').textContent=g?`LAT ${g.lat.toFixed(6)}° / LON ${g.lon.toFixed(6)}°`:'Outside map';if(!gesture||gesture.id!==e.pointerId)return;if(Math.hypot(p.x-gesture.start.x,p.y-gesture.start.y)>3)gesture.moved=true;if(gesture.moved){if(gesture.pan){view.x+=p.x-gesture.last.x;view.y+=p.y-gesture.last.y;}else if(gesture.hit>=0){const x=Math.max(0,Math.min(width,(p.x-view.x)/view.scale)),y=Math.max(0,Math.min(height,(p.y-view.y)/view.scale)),geo=K.toGeo(x,y,width,height,offset),w=waypoints()[gesture.hit];K.set(w,'lat',geo.lat.toFixed(6));K.set(w,'lon',geo.lon.toFixed(6));}gesture.last=p;draw();if(gesture.hit>=0)render();}});
canvas.addEventListener('pointerup',e=>{if(!gesture||gesture.id!==e.pointerId)return;const g=gesture;gesture=null;if(canvas.hasPointerCapture(e.pointerId))canvas.releasePointerCapture(e.pointerId);if(g.moved&&g.hit>=0){if(snapshot()!==g.before)checkpoint(g.before);render();}else if(!g.moved&&!g.pan&&g.location){selectLocation(g.location);}else if(!g.moved&&!g.pan&&g.hit<0){const p=at(local(e));if(p)append(Number(p.lat.toFixed(6)),Number(p.lon.toFixed(6)));}});
canvas.addEventListener('pointercancel',()=>{if(gesture?.hit>=0)restore(gesture.before);gesture=null;});
new ResizeObserver(()=>{fit();}).observe($('mapBox'));
render();fit();
// Prefer an optional high-resolution map; use the bundled MIT map if unavailable.
// Relative file:// paths work without a server or absolute installation path.
function loadKerbinMap(file, highResolution) {
  const map = new Image();
  map.onload = () => {
    image = map;
    width = map.naturalWidth;
    height = map.naturalHeight;
    $('mapEmpty').hidden = true;

    // Credit the actual map being displayed, not just the bundled default.
    const credit = document.querySelector('.map-credit a');
    if (credit) {
      credit.href = highResolution
        ? 'https://smooney.space/mapdownloads/'
        : 'https://github.com/KerbalMissile/Micro-Map';
      credit.textContent = highResolution
        ? 'Kerbin map by Smooney'
        : 'Micro Map by KerbalMissile (MIT)';
    }
    fit();
    render();
    message('Kerbin map loaded. Click to add waypoints or open a CFG file.');
  };
  map.onerror = () => {
    if (highResolution) {
      loadKerbinMap('assets/KerbinMap.png', false);
    } else {
      $('mapEmpty').querySelector('strong').textContent = 'Kerbin map unavailable';
      message('Could not load KerbinMap.png. Restore assets/KerbinMap.png, then reload the editor.', true);
    }
  };
  map.src = file;
}
loadKerbinMap('assets/KerbinMapHighResolution.jpeg', true);

