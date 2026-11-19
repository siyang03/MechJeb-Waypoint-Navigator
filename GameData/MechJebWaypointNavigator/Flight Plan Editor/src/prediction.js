/* Navigation/geometry/PID adapted from the companion navigator's Kramax port.
 * CC BY-NC-SA 4.0. Attribution and source snapshots: third_party/navigator/.
 * Aircraft motion integration is an idealized, independent coordinated-turn model.
 */
(function(root){
'use strict';
const K=root.Kerbin,R=600000,DEG=Math.PI/180,clamp=(x,a,b)=>Math.max(a,Math.min(b,x));
function bearing(a,b){const p=a.lat*DEG,q=b.lat*DEG,d=K.wrap(b.lon-a.lon)*DEG;return K.wrap(Math.atan2(Math.sin(d)*Math.cos(q),Math.cos(p)*Math.sin(q)-Math.sin(p)*Math.cos(q)*Math.cos(d))/DEG);}
function destination(p,heading,distance){const a=distance/R,h=heading*DEG,lat=p.lat*DEG,lon=p.lon*DEG;const q=Math.asin(clamp(Math.sin(lat)*Math.cos(a)+Math.cos(lat)*Math.sin(a)*Math.cos(h),-1,1));return {lat:q/DEG,lon:K.wrap((lon+Math.atan2(Math.sin(h)*Math.sin(a)*Math.cos(lat),Math.cos(a)-Math.sin(lat)*Math.sin(q)))/DEG)};}
// Mirrors CourseUtils.CrossTrackError's horizontal fields, including its passage checks.
function course(a,b,p){
 const length=K.distance(a,b),d13=K.distance(a,p),direct=K.distance(p,b),heading=bearing(a,b);
 const cross=Math.asin(clamp(Math.sin(d13/R)*Math.sin((bearing(a,p)-heading)*DEG),-1,1))*R;
 let traveled;
 if(d13>length)traveled=length;
 else{traveled=Math.acos(clamp(Math.cos(d13/R)/Math.cos(cross/R),-1,1))*R;if(direct>length)traveled=-traveled;}
 return {length,heading,cross,traveled,remaining:length-traveled,direct,fraction:length?traveled/length:NaN};
}
function shouldSequence(s,hasNext,approach,nextCourse,speed){
 if(!s.valid)return false;
 if(s.traveled>=s.length-10)return true;
 return hasNext&&!approach&&speed>0&&s.remaining/speed<Math.abs(K.wrap(nextCourse-s.heading))/3;
}
class VelocityPID{
 constructor(){this.integral=0;this.previous=0;this.skip=true;}
 response(input,target,dt){if(this.skip){this.previous=input;this.skip=false;}const error=input-target;this.integral=clamp(this.integral+error*dt*.8,-1,1);const derivative=(input-this.previous)/dt;this.previous=input;return -clamp(4*error+this.integral+2*derivative,-30,30);}
}
function desiredVelocity(s,speed){let desired=-s.cross/10;if(s.vC<=0)desired=-desired;return clamp(desired,-.5*speed,.5*speed);}
function turnRadius(speed,altitude,bank){const g=9.81*(R/(R+altitude))**2;return speed*speed/(g*Math.tan(Math.abs(bank)*DEG));}
// Generator yields progress every 2000 steps so the browser can cancel stale estimates.
function* simulate(points,options={}){
 const speed=options.speed??300,altitude=options.altitude??10000,bankLimit=options.bankLimit??30,dt=options.dt??.02;
 if(![speed,altitude,bankLimit,dt].every(Number.isFinite)||speed<=0||speed>2000||altitude<0||altitude>100000||bankLimit<0||bankLimit>=90||dt<=0||dt>.1)throw Error('Preview requires speed 0–2000 m/s, altitude 0–100000 m, bank 0° to less than 90°.');
 const pts=points.map(p=>({lat:p.lat,lon:p.lon,flags:p.flags||{}}));
 if(pts.some(p=>!Number.isFinite(p.lat)||!Number.isFinite(p.lon)||Math.abs(p.lat)>90||Math.abs(p.lon)>180))throw Error('Invalid route coordinate.');
 const radius=turnRadius(speed,altitude,bankLimit),groundRadius=radius*R/(R+altitude),groundSpeed=speed*R/(R+altitude);
 const result={samples:[],switches:[],complete:false,time:0,distance:0,radius,groundRadius,dt,maxCrossTrack:0,warnings:[]};
 if(pts.length<2){result.warnings.push('Add at least two waypoints to estimate a flight path.');return result;}
 let total=0;for(let i=1;i<pts.length;i++){const d=K.distance(pts[i-1],pts[i]);total+=d;if(d>Math.PI*R-1)result.warnings.push('An antipodal leg has no unique initial course.');}
 let position={lat:pts[0].lat,lon:pts[0].lon},heading=0,active=0,origin=pts[0],status=null,valid=false,command=0,bank=0;
 const first=pts.find(p=>K.distance(position,p)>.001);if(first)heading=bearing(position,first);
 const g=9.81*(R/(R+altitude))**2;
 const maxTime=options.maxTime??Math.min(28800,Math.max(600,total/groundSpeed*2+300));
 if(!Number.isFinite(maxTime)||maxTime<=0||maxTime>28800)throw Error('Preview duration must be between 0 and 28800 seconds.');
 const maxSteps=Math.ceil(maxTime/dt);let nextSample=0;
 function sample(t){result.samples.push({...position,t,heading,bank,active,cross:status?.cross||0});}
 function update(){const s=course(origin,pts[active],position);if(s.fraction>0&&s.fraction<1)valid=true;s.valid=valid;s.vXt=status?(s.cross-status.cross)/dt:NaN;s.vC=status?(status.remaining-s.remaining)/dt:NaN;status=s;return s;}
 for(let step=0;step<=maxSteps;step++){
  const time=step*dt;let s=update();
  for(let guard=0;guard<pts.length;guard++){
   const target=pts[active],next=active+1<pts.length,approach=['FAF','RW','Stop'].some(k=>String(target.flags[k]).toLowerCase()==='true');
   const duplicate=K.distance(origin,target)<.001;
   if(!duplicate&&!shouldSequence(s,next,approach,next?bearing(target,pts[active+1]):0,speed))break;
   if(!next){result.complete=true;result.time=time;sample(time);return result;}
   const nextCourse=bearing(target,pts[active+1]);
   if(active>0)result.switches.push({...position,t:time,waypoint:active,heading,turn:K.wrap(nextCourse-heading),lead:s.remaining,approach});
   origin=target;active++;valid=false;status=null;s=update();
  }
  result.maxCrossTrack=Math.max(result.maxCrossTrack,Math.abs(s.cross));
  // Rough maximum-bank preview: turn toward the active target, then level out.
  // Retain navigator sequencing, but do not model its varying PID bank command.
  const headingError=K.wrap(bearing(position,pts[active])-heading);
  const headingTolerance=Math.max(.1,g*Math.tan(bankLimit*DEG)/speed/DEG*dt);
  command=Math.abs(headingError)>headingTolerance?Math.sign(headingError)*bankLimit:0;
  if(time>=nextSample){sample(time);nextSample=time+1;}
  if(step===maxSteps){result.time=time;break;}
  bank=command;
  const rate=g*Math.tan(bank*DEG)/speed/DEG,midHeading=heading+rate*dt/2;
  const nextPosition=destination(position,midHeading,groundSpeed*dt);
  // Parallel transport of heading on the sphere, then apply the other half-turn.
  heading=K.wrap(bearing(nextPosition,position)+180+rate*dt/2);position=nextPosition;result.distance+=groundSpeed*dt;
  if(step>0&&step%2000===0)yield {time,active};
 }
 sample(result.time);result.warnings.push('Estimate reached its time limit before completing the route. It may be unable to capture or sequence a leg.');return result;
}
function circle(event,radius){const center=destination(event,event.heading+(event.turn<0?-90:90),radius);return Array.from({length:73},(_,i)=>destination(center,i*5,radius));}
root.FlightPrediction={bearing,destination,course,shouldSequence,VelocityPID,desiredVelocity,turnRadius,simulate,circle};
})(globalThis);
