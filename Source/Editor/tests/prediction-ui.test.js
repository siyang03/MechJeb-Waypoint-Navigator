/* Development has a preview altitude/panel; packaged uses planning altitude and a toolbar. */
(function(){
let passed=0,failed=0;
const assert=(v,msg='Assertion failed')=>{if(!v)throw Error(msg);};
function test(name,fn){try{fn();passed++;console.log('PASS PREVIEW UI '+name);}catch(e){failed++;console.log('FAIL PREVIEW UI '+name+': '+e.message);}}
function flush(){let guard=0;while(previewJobs.size){const [id,fn]=previewJobs.entries().next().value;previewJobs.delete(id);fn();if(++guard>5000)throw Error('Preview did not finish');}}
field('previewAltitude','10000');field('previewBank','30');$('previewCircles').checked=true;
tree=K.node('__root__');const p=K.plan('Preview route');K.children(p,'WayPoints')[0].children.push(K.waypoint(0,0),K.waypoint(0,10),K.waypoint(10,10));K.addPlan(tree,p);active=0;selected=-1;render();
test('Preview toggle calculates an estimate without modifying CFG',()=>{const before=snapshot();$('togglePrediction').click();flush();assert(predictionResult?.complete);assert(!$('predictionPanel').hidden);assert($('predictionStatus').hidden);assert(snapshot()===before);});
test('Route changes invalidate the track and update it',()=>{const before=predictionResult;change(()=>K.set(waypoints()[1],'lon',12));assert(predictionResult===null);flush();assert(predictionResult?.complete);assert(predictionResult!==before);});
test('Speed and altitude settings affect radius without changing route',()=>{const before=K.serialize(tree),r=predictionResult.radius;speed=400;render();flush();assert(predictionResult.radius>r);if($('previewAltitude').onchange){field('previewAltitude','20000');$('previewAltitude').onchange();}else{defaultAlt=20000;render();}flush();assert(K.serialize(tree)===before);assert(predictionResult.radius>r);});
test('Invalid input cancels pending work and clears stale track',()=>{field('previewBank','');$('previewBank').onchange();flush();assert(!predictionResult);assert($('predictionStatus').textContent.includes('Enter all'));field('previewBank','30');});
test('Disabling preview cancels pending computation',()=>{$('previewBank').onchange();$('togglePrediction').click();flush();assert(!predictionResult);if($('previewAltitude').onchange)assert($('predictionPanel').hidden);else assert(!predictionEnabled);});
test('Bank above 30 keeps the estimate without a warning',()=>{field('previewBank','70');$('togglePrediction').click();flush();assert(predictionResult);assert($('predictionStatus').hidden);});
test('Bank at or above 90 clears the track with a specific error',()=>{for(const bank of ['90','100']){field('previewBank',bank);$('previewBank').onchange();flush();assert(!predictionResult);assert($('predictionStatus').textContent.includes('less than 90°'));}field('previewBank','30');$('previewBank').onchange();flush();assert(predictionResult);assert($('predictionStatus').hidden);});
globalThis.previewUiResult={passed,failed};console.log(`\nPREVIEW UI: ${passed} passed, ${failed} failed`);
})();
