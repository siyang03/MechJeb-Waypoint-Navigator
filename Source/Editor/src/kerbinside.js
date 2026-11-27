/* Offline KSR orientation references, not surveyed runway lengths or approaches.
 * Coordinates: Caerfinon, September 7, 2026, GAPFAS KSR patch siteLAT/siteLON (not terminals):
 * https://forum.kerbalspaceprogram.com/topic/231736-1125-gapfas-a-gap-for-all-systems-v102/
 * Navigation headings: NavUtilities Continued, gathered/tested by Caerfinon:
 * https://github.com/linuxgurugamer/NavInstruments/blob/master/GameData/NavInstruments/ModuleManagerCfgs/KerbinSideRemastered.cfg
 * Supplied heading data retained verbatim, including Kermundsen 019/185.
 * Parallel runways share one axis; KSC, stock Island and Desert are excluded.
 */
(function(root){
'use strict';
const airfields=Object.freeze([
  {id:'kola-island',name:"Kola Island",lat:-4.159685612,lon:-72.10935211,axes:[[16,196]]},
  {id:'jeb-s-junkyard',name:"Jeb's Junkyard",lat:6.952152729,lon:-77.93811035,axes:[[12,192]]},
  {id:'cape-kerman',name:"Cape Kerman",lat:24.90675735,lon:-83.62899017,axes:[[150,330]]},
  {id:'sandy-island',name:"Sandy Island",lat:-8.151759148,lon:-42.39810181,axes:[[44,224]]},
  {id:'kerman-atoll',name:"Kerman Atoll",lat:-37.0721817,lon:-71.00325775,axes:[[111,291]]},
  {id:'meeda',name:"Meeda",lat:37.86918259,lon:-109.843895,axes:[[73,253],[133,313]]},
  {id:'kojave-sands',name:"Kojave Sands",lat:6.050330639,lon:-141.9779205,axes:[[38,218]]},
  {id:'polar-alpha',name:"Polar Alpha",lat:72.56324005,lon:-78.55982971,axes:[[157,337]]},
  {id:'baikerbanur',name:"Baikerbanur",lat:20.6048107,lon:-146.5122681,axes:[[18,198]]},
  {id:'south-field',name:"South Field",lat:-47.0065155,lon:-140.9785461,axes:[[91,271]]},
  {id:'harvester',name:"Harvester",lat:-56.29705429,lon:-10.98251915,axes:[[79,259]]},
  {id:'uberdam',name:"Uberdam",lat:38.50753403,lon:-149.6291046,axes:[[41,221]]},
  {id:'kamberwick',name:"Kamberwick",lat:36.18170166,lon:10.60191822,axes:[[92,272]]},
  {id:'kermundsen',name:"Kermundsen",lat:-89.87486267,lon:-172.391983,axes:[[19,185]]},
  {id:'south-lake',name:"South Lake",lat:-37.2833786,lon:52.56228638,axes:[[41,221],[71,251]]},
  {id:'hazard-shallows',name:"Hazard Shallows",lat:-14.22684479,lon:155.2404785,axes:[[50,230]]},
  {id:'dununda',name:"Dununda",lat:-39.29815674,lon:116.2391739,axes:[[26,206],[87,266]]},
  {id:'round-range',name:"Round Range",lat:-5.999658585,lon:99.51026154,axes:[[102,282]]},
  {id:'nye-island',name:"Nye Island",lat:5.746015549,lon:108.7499619,axes:[[149,329]]},
  {id:'top-secret-area-15',name:"Top Secret Area 15",lat:-60.55518888,lon:39.25160199,axes:[[131,311]]},
].map(site=>Object.freeze({...site,axes:Object.freeze(site.axes.map(axis=>Object.freeze(axis)))})));
function runwayDirection(lat,heading,width,height){
  const rad=Math.PI/180,theta=heading*rad;
  // Bound the longitudinal singularity, including exact poles. At a pole the
  // local bearing is ambiguous; this gives a finite limiting orientation.
  const cosPhi=Math.max(1e-6,Math.abs(Math.cos(lat*rad)));
  const ux=width*Math.sin(theta)/(360*cosPhi),uy=-height*Math.cos(theta)/180;
  const norm=Math.hypot(ux,uy);
  return norm>0&&Number.isFinite(norm)?{x:ux/norm,y:uy/norm}:{x:0,y:-1};
}
const label=site=>`${site.name} (${site.axes.map(axis=>axis.map(h=>String(h).padStart(3,'0')).join('/')).join(', ')})`;
const api=Object.freeze({airfields,runwayDirection,label});
if(typeof module!=='undefined'&&module.exports)module.exports=api;else root.KerbinSide=api;
})(globalThis);
