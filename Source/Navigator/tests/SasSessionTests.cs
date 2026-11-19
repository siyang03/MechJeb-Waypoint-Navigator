using System;
using System.Collections.Generic;
using WaypointNavigator;
static class SasSessionTests {
 public static void Run(Action<bool,string> check) {
  var session=new NavSasSession();bool sas=false;int writes=0,releases=0;bool restore=true;
  Action<bool> write=value=>{sas=value;writes++;};
  Action<bool> release=value=>{restore=value;releases++;};
  session.End(release);check(writes==0,"Initialization stop never activates SAS");
  session.End(release);check(writes==0,"Failed preconditions never activate SAS");
  foreach(bool initial in new[]{false,true}) {
   sas=initial;session.Suppress(()=>sas,write);check(!sas,"Engagement disables SAS regardless of initial state");
   session.End(release);check(sas&&restore,"Ordinary termination restores SAS after normal horizontal release");
   int before=writes;session.End(release);check(writes==before,"Repeated stop never changes SAS");
  }
  session.Suppress(()=>sas,write);sas=true;session.End(release);
  check(sas&&!restore,"Manual SAS override requests Release(false) and leaves SAS on");
  var events=new List<string>();sas=false;
  session.Suppress(()=>sas,value=>{sas=value;events.Add(value?"sas on":"sas off");});
  session.End(value=>{events.Add("release");});
  check(string.Join(",",events)=="sas off,release,sas on","Release precedes SAS restoration");
  // Bind to the original vessel's state; switching the active vessel changes no callback.
  bool original=true,other=false;
  session.Suppress(()=>original,value=>original=value);session.End(release);
  check(original&&!other,"Only the session vessel receives SAS restoration");
  bool destroyed=false;sas=true;
  session.Suppress(()=>!destroyed&&sas,value=>{if(!destroyed)sas=value;});destroyed=true;
  session.End(release);check(!sas,"Destroyed vessel callbacks safely skip restoration");
  bool threw=false;sas=true;
  try{session.Suppress(()=>sas,value=>{sas=value;if(!value)throw new ApplicationException("write failed after change");});}
  catch(ApplicationException){threw=true;}
  session.End(release);check(threw&&sas,"Suppression write failure rolls back SAS");
  threw=false;sas=true;
  try{session.Suppress(()=>sas,value=>{if(value)sas=true;});}catch(InvalidOperationException){threw=true;}
  session.End(release);check(threw&&sas&&!restore,"Failed disable verification rolls back without competing horizontal restore");
  bool readFails=false;sas=true;
  session.Suppress(()=>{if(readFails)throw new ApplicationException("read failed");return sas;},write);
  readFails=true;int priorReleases=releases;threw=false;
  try{session.End(release);}catch(ApplicationException){threw=true;}
  check(threw&&sas&&releases==priorReleases+1&&!restore,"Read failure still releases conservatively and restores SAS");
  sas=true;session.Suppress(()=>sas,write);threw=false;
  try{session.End(value=>{throw new ApplicationException("release failed");});}catch(ApplicationException){threw=true;}
  check(threw&&sas,"Adapter release failure still restores SAS");
  // Exercise the session policy together with real adapter ownership logic.
  var core=new MuMech.MechJebCore();var part=new Part();part.Modules.Add(core);
  var vessel=new Vessel();vessel.parts.Add(part);var adapter=MechJebAdapter.Find(vessel);var ap=core.Airplane;
  ap.HeadingHoldEnabled=ap.RollHoldEnabled=true;adapter.Acquire();session.Suppress(()=>sas,write);
  int restartWrites=writes;
  adapter.CheckOwnership(); // Successful waypoint restart retains adapter and SAS session.
  check(writes==restartWrites&&!sas,"Waypoint restart leaves SAS off without an intermediate toggle");
  ap.HeadingHoldEnabled=true;ap.HeadingTarget=71;threw=false;
  try{adapter.CheckOwnership();}catch(InvalidOperationException){threw=true;}
  session.End(adapter.Release);
  check(threw&&sas&&ap.HeadingHoldEnabled&&ap.HeadingTarget==71,"Heading takeover detected without bank demand and preserved on release");
  check(!adapter.YawBridgeActive,"Heading takeover detaches yaw bridge");
  adapter.Acquire();session.Suppress(()=>sas,write);
  // Any failed restart goes through the same ordinary termination/rollback.
  core.running=false;threw=false;try{adapter.CheckOwnership();}catch(InvalidOperationException){threw=true;}
  session.End(adapter.Release);check(threw&&sas,"Failed restart or controller conflict restores SAS");core.running=true;
  adapter.Acquire();session.Suppress(()=>sas,write);ap.RollTarget=19;
  threw=false;try{adapter.CheckOwnership();}catch(InvalidOperationException){threw=true;}
  session.End(adapter.Release);check(threw&&sas&&ap.RollTarget==19,"Roll override detected without demand and preserved");
  check(ap.AltitudeTarget==10050&&ap.SpeedTarget==285,"SAS session changes preserve altitude and speed");
 }
}
