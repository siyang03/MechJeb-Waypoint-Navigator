/* Fixed Kerbin locations; bundled for offline use. Coordinates identify sites, not landing approaches. */
(function(root){
const survey='https://www.reddit.com/r/KerbalSpaceProgram/comments/xxy9ag/spoiler_map_of_everything_there_is_to_see_at/';
const secrets='https://steamcommunity.com/sharedfiles/filedetails/?id=2016782245';
const comms='https://forum.kerbalspaceprogram.com/topic/180878-commnet-dish-inspection-challenge/';
const pad='https://github.com/KK4TEE/kOSPrecisionLand/blob/master/library.ks';
const launches='https://steamcommunity.com/sharedfiles/filedetails/?id=2532358843';
root.KerbinLocations=Object.freeze([
  {id:'island',kind:'launch',name:'Island Airfield',lat:-1.529278,lon:-71.885306,note:'Stock island runway launch reference. Not an instrument approach.',source:launches},
  {id:'dessert',kind:'launch',name:'Dessert Launch Site',lat:-6.560379,lon:-143.950010,note:'Making History DLC. Reference is the Dessert launch complex, beside the runway; not a landing target.',source:launches},
  {id:'ksc',kind:'launch',name:'KSC Runway',lat:-0.050185,lon:-74.490867,note:'Stock KSC runway departure reference from Kramax. Not an instrument approach.',source:'https://github.com/linuxgurugamer/KramaxAutoPilot/blob/master/GameData/KramaxAutoPilot/DefaultFlightPlans.cfg'},
  {id:'dessert-runway',kind:'launch',name:'Dessert Runway',lat:-6.57,lon:-144.04,note:'Making History DLC runway; approximate airfield reference, not a surveyed threshold.',source:comms},
  {id:'ksc-pad',kind:'launch',name:'KSC Launchpad',lat:-0.097209,lon:-74.557706,note:'Stock KSC rocket launchpad, separate from the runway.',source:pad},
  {id:'woomerang',kind:'launch',name:'Woomerang Launch Site',lat:45.289617,lon:136.109993,note:'Making History DLC rocket launchpad; no runway. Separate from the nearby Larkes CommNet dish.',source:launches},
  {id:'cove',kind:'launch',name:'Cove Launch Site',lat:3.747764,lon:-72.213350,note:'Discoverable KSP 1.12 stock launch site; not a runway.',source:launches},
  {id:'glacier',kind:'launch',name:'Glacier Lake Launch Site',lat:73.561462,lon:84.247809,note:'Discoverable KSP 1.12 stock launch site; not a runway.',source:launches},
  {id:'crater',kind:'launch',name:'Crater Launch Site',lat:8.385772,lon:-179.697664,note:'Discoverable KSP 1.12 stock launch site near the longitude seam; not a runway.',source:launches},
  {id:'mahi',kind:'launch',name:'Mahi Mahi Launch Site',lat:-49.802341,lon:-120.765919,note:'Discoverable KSP 1.12 stock launch site; not a runway.',source:launches},
  {id:'comm-baikerbanur',kind:'commnet',name:'Baikerbanur Groundstation',lat:20.68111,lon:-146.50172,note:'Extra CommNet groundstation at the old space center; availability depends on game settings.',source:comms},
  {id:'comm-crater',kind:'commnet',name:'Crater Rim Groundstation',lat:9.45021,lon:-172.10936,note:'Extra CommNet groundstation on the crater rim; not the Crater launch site.',source:comms},
  {id:'comm-nye',kind:'commnet',name:'Nye Island Groundstation',lat:5.35963,lon:108.54980,note:'Extra CommNet groundstation on Nye Island.',source:comms},
  {id:'comm-harvester',kind:'commnet',name:'Harvester Massif Groundstation',lat:-11.95136,lon:33.7409,note:'Extra CommNet groundstation in the mountains.',source:comms},
  {id:'comm-north',kind:'commnet',name:'North Station One',lat:63.09517,lon:-90.07795,note:'Extra CommNet groundstation in the north.',source:comms},
  {id:'comm-mesa',kind:'commnet',name:'Mesa South Groundstation',lat:-59.58902,lon:-25.87014,note:'Extra CommNet groundstation on a southern mesa.',source:comms},
  {id:'comm-larkes',kind:'commnet',name:'Larkes Groundstation',lat:44.71933,lon:137.03078,note:'Extra CommNet groundstation also called the Woomerang dish in older guides. Separate from the launchpad.',source:comms},
  {id:'pyramids',kind:'anomaly',name:'Desert Temple',lat:-6.505567,lon:-141.685515,note:'The desert pyramids; fixed landmark.',source:secrets},
  {id:'old-ksc',kind:'launch',name:'Inland KSC (Baikerbanur)',lat:20.6622,lon:-146.4603,note:'Abandoned historical launch complex. Not a selectable stock launch site; no usable runway. Separate from the nearby CommNet station.',source:survey},
  {id:'ufo',kind:'anomaly',name:'Crashed Flying Saucer',lat:81.954495,lon:-128.520861,note:'Fixed anomaly in the northern ice cap.',source:secrets},
  {id:'sputnik',kind:'anomaly',name:'Crashed Sputnik 1',lat:-17.534271,lon:61.061880,note:'Fixed easter egg added in KSP 1.12.',source:secrets},
  {id:'ocean-smiley',kind:'anomaly',name:'Giant Ocean Smiley Face',lat:-30,lon:-80,note:'Approximate location of the giant smiley carved into the ocean floor. Underwater terrain feature, not a surface structure.',source:survey},
  {id:'mk1',kind:'anomaly',name:'Mk1 Pod Memorial',lat:-0.091958,lon:-74.629370,note:'At KSC; appearance depends on facility level (level 3 VAB).',source:secrets},
  {id:'monolith-north',kind:'anomaly',shape:'monolith',name:'Northern Mountain Monolith',lat:35.5705,lon:-74.9773,note:'Fixed black monolith north of KSC.',source:survey},
  {id:'monolith-west',kind:'anomaly',shape:'monolith',name:'Western Monolith',lat:-0.6403,lon:-80.7669,note:'Fixed black monolith west of KSC.',source:survey},
  {id:'monolith-south',kind:'anomaly',shape:'monolith',name:'Southern Monolith',lat:-28.8084,lon:-13.4401,note:'Fixed black monolith in the southern hemisphere.',source:survey},
  {id:'monolith-baikerbanur',kind:'anomaly',shape:'monolith',name:'Baikerbanur Monolith',lat:20.6709,lon:-146.4968,note:'Fixed black monolith near the old space center.',source:survey}
].map(Object.freeze));
})(globalThis);
