/* Minimal DOM/canvas harness for exercising the real app handlers without dependencies. */
var window=globalThis;
var uiEvents={};
window.addEventListener=(name,fn)=>uiEvents[name]=fn;
window.devicePixelRatio=1;
var dialogs=[];
var confirm=msg=>{dialogs.push(msg);return true;};
var fakeContext=new Proxy({}, {get:(obj,key)=>obj[key]||(()=>{}),set:(obj,key,val)=>{obj[key]=val;return true;}});
class Element {
  constructor(id){this.id=id;this.value='';this.children=[];this.disabled=false;this.hidden=false;this.checked=false;this.textContent='';this.events={};this.clientWidth=900;this.clientHeight=500;this.classList={toggle:()=>{}};}
  append(...nodes){this.children.push(...nodes);}
  replaceChildren(...nodes){this.children=nodes;}
  setAttribute(name,value){(this.attributes||(this.attributes={}))[name]=String(value);}
  getAttribute(name){return this.attributes?.[name]??null;}
  remove(){}
  click(){if(!this.disabled)this.onclick?.({preventDefault(){}});}
  getContext(){return fakeContext;}
  getBoundingClientRect(){return {left:0,top:0,width:this.clientWidth,height:this.clientHeight};}
  addEventListener(name,fn){this.events[name]=fn;}
  setPointerCapture(id){this.capture=id;}
  hasPointerCapture(id){return this.capture===id;}
  releasePointerCapture(){this.capture=null;}
}
var elements={};
var document={getElementById:id=>elements[id]||(elements[id]=new Element(id)),createElement:tag=>new Element(tag),body:new Element('body')};
var ResizeObserver=class{observe(){}};
var downloads=[];
var URL={createObjectURL:blob=>{downloads.push(blob);return 'blob:test';},revokeObjectURL(){}};
var Blob=class{constructor(parts,options){this.text=parts.join('');this.options=options;}};
var setTimeout=()=>{};
var Image=class{set src(v){this.source=v;}};
