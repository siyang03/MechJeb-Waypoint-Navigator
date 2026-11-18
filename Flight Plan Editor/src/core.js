/* Shared browser/Node core. No network or runtime dependencies. */
(function (root) {
'use strict';
const R = 600000, rad = Math.PI / 180, clone = x => JSON.parse(JSON.stringify(x));
const wrap = x => ((x + 180) % 360 + 360) % 360 - 180;
function toPixel(lat, lon, width = 2048, height = 1022, offset = 0) {
  return {x: (wrap(lon - offset) + 180) / 360 * width, y: (90 - lat) / 180 * height};
}
function toGeo(x, y, width = 2048, height = 1022, offset = 0) {
  return {lat: 90 - 180 * y / height, lon: wrap(360 * x / width - 180 + offset)};
}
function distance(a, b) {
  const h = Math.sin((b.lat-a.lat)*rad/2)**2 + Math.cos(a.lat*rad)*Math.cos(b.lat*rad)*Math.sin(wrap(b.lon-a.lon)*rad/2)**2;
  return 2 * R * Math.asin(Math.sqrt(Math.max(0, Math.min(1, h))));
}
function samples(a, b) {
  const vec = p => [Math.cos(p.lat*rad)*Math.cos(p.lon*rad), Math.cos(p.lat*rad)*Math.sin(p.lon*rad), Math.sin(p.lat*rad)];
  const u=vec(a), v=vec(b), dot=u.reduce((s,x,i)=>s+x*v[i],0), angle=Math.acos(Math.max(-1,Math.min(1,dot)));
  if (angle < 1e-9) return [a,b];
  let tangent = v.map((x,i)=>x-dot*u[i]);
  let norm = Math.hypot(...tangent);
  // Antipodes have no unique shortest path; choose a stable orthogonal plane.
  if(norm < 1e-9) {const axis=Math.abs(u[2])<0.9?[0,0,1]:[1,0,0]; const d=u.reduce((s,x,i)=>s+x*axis[i],0); tangent=axis.map((x,i)=>x-d*u[i]); norm=Math.hypot(...tangent);}
  tangent=tangent.map(x=>x/norm);
  const n=Math.max(2,Math.ceil(angle/rad));
  return Array.from({length:n+1},(_,i)=>{if(i===0)return a;if(i===n)return b;const t=angle*i/n, w=u.map((x,j)=>x*Math.cos(t)+tangent[j]*Math.sin(t));return {lat:Math.atan2(w[2],Math.hypot(w[0],w[1]))/rad,lon:Math.atan2(w[1],w[0])/rad};});
}
const node=(name,children=[])=>({type:'node',name,children});
const value=(key,v)=>({type:'value',key,value:String(v)});
const children=(n,name)=>n.children.filter(e=>e.type==='node'&&e.name===name);
const values=(n,key)=>n.children.filter(e=>e.type==='value'&&e.key===key);
const get=(n,key,fallback='')=>values(n,key)[0]?.value ?? fallback;
function set(n,key,v) { const e=values(n,key)[0]; if(e)e.value=String(v);else n.children.push(value(key,v)); }
function safeText(v) {if(/[\r\n{}]|\/\/|\/\*|\*\//.test(String(v)))throw Error('Text cannot contain newlines, braces, or CFG comment delimiters.');return String(v);}
function parse(text) {
  text=text.replace(/^\uFEFF/,''); let pos=0;
  const fail=msg=>{throw Error(`${msg} (line ${text.slice(0,pos).split('\n').length})`);};
  function space(out) {
    while(pos<text.length) {
      if(/\s/.test(text[pos])){pos++;continue;}
      // Accept full-line hash comments found in hand-written plans. Normalize to KSP's // syntax.
      if(text[pos]==='#'&&!text.slice(text.lastIndexOf('\n',pos-1)+1,pos).trim()){
        let end=text.indexOf('\n',pos);if(end<0)end=text.length;
        out.push({type:'comment',text:'//'+text.slice(pos+1,end)});pos=end;continue;
      }
      if(text.startsWith('//',pos)){let end=text.indexOf('\n',pos);if(end<0)end=text.length;out.push({type:'comment',text:text.slice(pos,end)});pos=end;continue;}
      if(text.startsWith('/*',pos)){const end=text.indexOf('*/',pos+2);if(end<0)fail('Unclosed block comment');out.push({type:'comment',text:text.slice(pos,end+2)});pos=end+2;continue;}
      break;
    }
  }
  function entries(depth) {
    if(depth>64)fail('Nesting exceeds 64 levels');const out=[];
    while(true) {
      space(out);if(pos>=text.length){if(depth)fail('Missing closing brace');return out;}
      if(text[pos]==='}'){if(!depth)fail('Unexpected closing brace');pos++;return out;}
      const start=pos;
      while(pos<text.length&&!/[={}\r\n]/.test(text[pos])&&!text.startsWith('//',pos)&&!text.startsWith('/*',pos))pos++;
      const head=text.slice(start,pos).trim();if(!head)fail('Expected node name or key');
      const comments=[];space(comments);
      if(text[pos]==='{'){pos++;out.push(...comments,node(head,entries(depth+1)));}
      else if(text[pos]==='=') {
        pos++;const s=pos;
        while(pos<text.length&&!/[{}\r\n]/.test(text[pos])&&!text.startsWith('//',pos)&&!text.startsWith('/*',pos))pos++;
        out.push(...comments,value(head,text.slice(s,pos).trim()));
      } else fail(`Expected = or { after "${head}"`);
    }
  }
  return node('__root__',entries(0));
}
function serialize(tree) {
  function emit(e,depth) {
    const pad='    '.repeat(depth);
    if(e.type==='comment')return e.text.split('\n').map(l=>pad+l).join('\n');
    if(e.type==='value')return `${pad}${e.key} = ${e.value}`;
    return `${pad}${e.name}\n${pad}{\n${e.children.map(c=>emit(c,depth+1)).join('\n')}\n${pad}}`;
  }
  return tree.children.map(e=>emit(e,0)).join('\n')+'\n';
}
function waypoint(lat=0,lon=0,alt=10000,name='Waypoint') {return node('WayPoint',[value('Vertical','true'),value('lat',lat),value('lon',lon),value('alt',alt),value('name',safeText(name))]);}
function plan(name='Untitled route') {return node('FlightPlan',[value('planet','Kerbin'),value('name',safeText(name)),value('description',''),node('WayPoints')]);}
function points(p) {return children(p,'WayPoints')[0]?.children.filter(e=>e.type==='node'&&e.name==='WayPoint')||[];}
function geo(w) {return {lat:Number(get(w,'lat')),lon:Number(get(w,'lon')),alt:Number(get(w,'alt'))};}
function validate(p) {
  for(const k of ['name','planet','description'])if(values(p,k).length>1)throw Error(`Duplicate plan ${k}`);
  if(get(p,'planet')!=='Kerbin')throw Error('Only Kerbin is editable');
  if(!get(p,'name').trim())throw Error('Plan name is missing');
  if(children(p,'WayPoints').length!==1)throw Error('Expected exactly one WayPoints node');
  for(const w of points(p)) {
    for(const k of ['lat','lon','alt','name'])if(values(w,k).length!==1)throw Error(`Waypoint requires one ${k}`);
    const g=geo(w); if(!get(w,'lat').trim()||!get(w,'lon').trim()||!get(w,'alt').trim()||!Number.isFinite(g.lat)||Math.abs(g.lat)>90||!Number.isFinite(g.lon)||Math.abs(g.lon)>180||!Number.isFinite(g.alt))throw Error('Invalid waypoint coordinates or altitude');
  }
}
function plans(tree) {const result=[];for(const r of children(tree,'KramaxAutoPilotPlans'))for(const body of children(r,'Kerbin'))for(const p of children(body,'FlightPlan')){try{validate(p);result.push(p);}catch{ /* retained verbatim in tree; inspect reports it */ }}return result;}
function inspect(tree) {
  const warnings=[];
  function walk(n,path) {
    for(const e of n.children) {
      if(e.type==='node') {
        const allowed=path==='__root__'?['KramaxAutoPilotPlans']:path==='KramaxAutoPilotPlans'?['Kerbin']:path==='Kerbin'?['FlightPlan']:path==='FlightPlan'?['WayPoints']:path==='WayPoints'?['WayPoint']:[];
        if(!allowed.includes(e.name))warnings.push(`Preserved uneditable node: ${e.name}`);
        if(e.name==='FlightPlan'){try{validate(e);}catch(err){warnings.push(`Preserved uneditable plan "${get(e,'name','unnamed')}": ${err.message}`);}}
        walk(e,e.name);
      } else if(e.type==='value') {
        const allowed=path==='FlightPlan'?['planet','name','description']:path==='WayPoint'?['Vertical','lat','lon','alt','name','IAF','FAF','RW','Stop']:[];
        if(!allowed.includes(e.key))warnings.push(`Preserved extra field: ${e.key}`);
      }
    }
  }
  walk(tree,'__root__');return [...new Set(warnings)];
}
function addPlan(tree,p) {let r=children(tree,'KramaxAutoPilotPlans')[0];if(!r){r=node('KramaxAutoPilotPlans');tree.children.push(r);}let k=children(r,'Kerbin')[0];if(!k){k=node('Kerbin');r.children.push(k);}k.children.push(p);}
function removeNode(tree,target) {for(const e of tree.children){if(e===target){tree.children.splice(tree.children.indexOf(e),1);return true;}if(e.type==='node'&&removeNode(e,target))return true;}return false;}
function stats(p) {let total=0;const rows=points(p).map((w,i,all)=>{const leg=i?distance(geo(all[i-1]),geo(w)):0;total+=leg;return {leg,cumulative:total};});return {rows,total};}
const api={R,clone,wrap,toPixel,toGeo,distance,samples,node,value,children,values,get,set,safeText,parse,serialize,waypoint,plan,points,geo,validate,plans,inspect,addPlan,removeNode,stats};
if(typeof module!=='undefined'&&module.exports)module.exports=api;else root.Kerbin=api;
})(globalThis);
