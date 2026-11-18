'use strict';
let predictionEnabled=false,predictionResult=null,predictionKey='',predictionGeneration=0,predictionTimer=null;
function predictionBusy(busy){$('predictionBusy').hidden=!busy;$('togglePrediction').setAttribute('aria-busy',String(busy));}
function predictionNotice(text='',level='warning'){ $('predictionStatus').textContent=text;$('predictionStatus').hidden=!text;$('predictionStatus').classList.toggle('error',level==='error');}
function predictionInputs(){
 const points=waypoints().map(w=>({...K.geo(w),flags:Object.fromEntries(w.children.filter(v=>v.type==='value').map(v=>[v.key,v.value]))}));
 const options={speed,altitude:Number($('previewAltitude').value),bankLimit:Number($('previewBank').value)};
 for(const id of ['previewAltitude','previewBank'])if(!$(id).value.trim())throw Error('Enter all preview settings.');
 if(!Number.isFinite(options.bankLimit))throw Error('Enter a valid bank angle.');
 if(options.bankLimit>=90)throw Error('Bank angle must be less than 90°.');
 if(options.bankLimit<0)throw Error('Bank angle cannot be below 0°.');
 return {points,options};
}
function schedulePrediction(){
 if(!predictionEnabled)return;
 let input,key;
 try{input=predictionInputs();key=JSON.stringify(input);}catch(e){predictionGeneration++;predictionKey='';predictionResult=null;if(predictionTimer!==null)clearTimeout(predictionTimer);predictionBusy(false);predictionNotice(e.message,'error');return;}
 if(key===predictionKey)return;
 predictionKey=key;predictionResult=null;const generation=++predictionGeneration;
 if(predictionTimer!==null)clearTimeout(predictionTimer);
 predictionBusy(true);predictionNotice();
 predictionTimer=setTimeout(()=>{
  if(generation!==predictionGeneration||!predictionEnabled)return;
  const simulation=FlightPrediction.simulate(input.points,input.options);
  function chunk(){
   if(generation!==predictionGeneration||!predictionEnabled)return;
   try{
    const step=simulation.next();
    if(!step.done){predictionTimer=setTimeout(chunk,0);return;}
    predictionBusy(false);predictionResult=step.value;
    const r=predictionResult;
    predictionNotice((r.complete?'':input.points.length<2?'Add at least two waypoints to estimate a flight path.':'The estimated flight path diverges due to overshoot.'),!r.complete&&input.points.length>=2?'error':'warning');
    draw();
   }catch(e){predictionBusy(false);predictionResult=null;predictionNotice('Could not calculate the path. Check the speed, altitude and bank angle.','error');draw();}
  }
  chunk();
 },250);
}
function predictionLine(points){
 const W=width*view.scale;let previous=null;ctx.beginPath();
 for(const point of points){const p=screen(point);if(previous){let dx=p.x-previous.x;while(dx>W/2)dx-=W;while(dx< -W/2)dx+=W;const end=previous.x+dx;ctx.moveTo(previous.x,previous.y);ctx.lineTo(end,p.y);if(end!==p.x){ctx.moveTo(previous.x+(p.x-end),previous.y);ctx.lineTo(p.x,p.y);}}previous=p;}
 ctx.stroke();
}
function drawPrediction(){
 if(!predictionEnabled||!predictionResult)return;
 ctx.save();ctx.strokeStyle='#ffad66';ctx.lineWidth=2;ctx.setLineDash([6,4]);predictionLine(predictionResult.samples);
 if($('previewCircles').checked&&Number.isFinite(predictionResult.groundRadius)){ctx.strokeStyle='#ffad6670';ctx.lineWidth=1;ctx.setLineDash([3,5]);
  for(const event of predictionResult.switches){if(selected>=0&&selected!==event.waypoint)continue;if(Math.abs(event.turn)<1)continue;predictionLine(FlightPrediction.circle(event,predictionResult.groundRadius));}
 }
 ctx.restore();
}
$('togglePrediction').onclick=()=>{
 predictionEnabled=!predictionEnabled;$('togglePrediction').setAttribute('aria-pressed',String(predictionEnabled));$('predictionPanel').hidden=!predictionEnabled;
 if(predictionEnabled){predictionKey='';schedulePrediction();}else{predictionBusy(false);predictionNotice();predictionGeneration++;if(predictionTimer!==null)clearTimeout(predictionTimer);predictionResult=null;}
 draw();
};
for(const id of ['previewAltitude','previewBank'])$(id).onchange=()=>{predictionKey='';schedulePrediction();draw();};
$('previewCircles').onchange=draw;
