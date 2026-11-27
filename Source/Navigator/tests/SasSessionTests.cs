using System;
using System.Collections.Generic;
using WaypointNavigator;
static class SasSessionTests {
 static bool Reject(Action action){try{action();return false;}catch{return true;}}
 public static void Run(Action<bool,string> check) {
  var s=new NavSasSession();bool sas=true;int writes=0;bool restore=false;
  Action<bool> write=v=>{sas=v;writes++;};Action<bool> release=v=>restore=v;
  s.End(release);check(writes==0,"Uninitialized cleanup");
  s.Begin(()=>sas,write,1);s.Update(1,2,.02);s.Update(3,4,.02);s.Update(-5,5,.02);
  check(sas&&writes==0,"Small corrections and exactly five preserve SAS");
  s.Update(0,20,2.98);check(sas,"Commanded turn stays ON at 2.98 seconds");
  s.Update(0,20,.02);check(!sas,"Commanded turn disables at three seconds");
  s.Update(0,0,4.98);check(!sas,"4.98 insufficient");s.Update(0,0,.02);check(sas,"Five seconds restores");
  s.Update(-5.01,0,3);check(!sas,"Negative threshold symmetric");
  s.Update(0,0,4.9);s.Update(20,0,.02);s.Update(0,0,4.98);check(!sas,"Actual turn resets recovery");s.Update(0,0,.02);check(sas,"Full recovery required");
  s.End(release);check(sas&&restore,"Normal ON stop restores horizontal");
  foreach(bool initial in new[]{false,true}) {
   sas=initial;s.Begin(()=>sas,write,20);check(sas==initial,"Initial bank preserves SAS until timed shutdown");
   s.Update(20,0,3);check(!sas,"Initial bank disables after three seconds");
   s.End(release);check(sas&&restore,"Normal OFF stop restores horizontal and SAS");
   int before=writes;s.End(release);check(writes==before,"Repeated cleanup inert");
  }
  sas=true;s.Begin(()=>sas,write,0);
  s.Update(10,0,2);s.Update(2,0,.02);check(sas,"Near-level interrupts shutdown");
  s.Update(10,0,2.98);check(sas,"Interrupted shutdown requires fresh three seconds");
  s.Update(10,0,.02);check(!sas,"Fresh shutdown reaches three seconds");
  int offWrites=writes;s.Update(10,0,100);check(!sas&&writes==offWrites,"Already OFF has no delayed or redundant transition");
  s.Update(2,0,4.98);s.Update(5.01,0,.02);s.Update(2,0,4.98);check(!sas,"Threshold alternation resets recovery");
  s.Update(2,0,.02);check(sas,"Recovery still takes five seconds");
  s.Update(0,10,2.98);s.Update(0,5,.02);s.Update(0,-10,2.98);check(sas,"Exactly five resets shutdown and negative demand is symmetric");
  s.Update(0,-10,.02);check(!sas,"Negative demand disables after three seconds");s.End(release);
  sas=true;s.Begin(()=>sas,write,0);for(int i=0;i<1000;i++)s.Update(1,2,.02);
  check(sas,"Small corrections leave SAS ON indefinitely");
  for(int i=0;i<149;i++)s.Update(-10,0,.02);check(sas,"149 shutdown ticks keep SAS ON");
  s.Update(-10,0,.02);check(!sas,"150 shutdown ticks equal three seconds");s.End(release);
  sas=false;s.Begin(()=>sas,write,0);s.Update(2,1,5);check(sas,"Initially OFF recovers");s.End(release);
  foreach(bool expected in new[]{false,true}) {
   sas=expected;s.Begin(()=>sas,write,0);sas=!expected;
   check(Reject(s.Check),"Manual override detected");int before=writes;
   s.End(release);check(sas==!expected&&!restore&&writes==before,"Manual state preserved");
  }
  foreach(double invalid in new[]{double.NaN,double.PositiveInfinity,-1.0,0.0}) {
   sas=false;s.Begin(()=>sas,write,0);s.Update(0,0,4.9);
   check(Reject(()=>s.Update(0,0,invalid))&&!sas,"Invalid dt cannot recover");s.End(release);
  }
  foreach(double invalid in new[]{double.NaN,double.NegativeInfinity}) {
   sas=false;s.Begin(()=>sas,write,0);check(Reject(()=>s.Update(invalid,0,5))&&!sas,"Invalid actual rejected");
   check(Reject(()=>s.Update(0,invalid,5))&&!sas,"Invalid demand rejected");s.End(release);
  }
  sas=true;s.Begin(()=>sas,v=>{},20);check(Reject(()=>s.Update(20,0,3)),"Failed readback rejected");s.End(release);check(restore,"Internal failure is not override");
  sas=true;s.Begin(()=>sas,v=>{sas=v;if(!v)throw new Exception();},20);check(Reject(()=>s.Update(20,0,3)),"Partial write rejected");s.End(release);check(sas&&restore,"Partial write rollback");
  sas=false;s.Begin(()=>sas,write,0);s.Update(0,0,4.9);s.ResetRecovery();s.Update(0,0,.1);check(!sas,"Restart resets timer");
  s.End(release,false);check(!sas&&restore,"Destroyed vessel skips restoration");
  sas=false;s.Begin(()=>sas,write,0);for(int i=0;i<250;i++)s.Update(0,0,.02);
  check(sas,"250 physics ticks equal five seconds");s.End(release);
  sas=true;bool fail=false;s.Begin(()=>sas,v=>{if(!fail)sas=v;},0);fail=true;
  check(Reject(()=>s.Update(0,20,3)),"Later transition readback failure terminates");s.End(release);check(restore,"Later internal failure restores controller");
  bool readFails=false;sas=false;s.Begin(()=>{if(readFails)throw new Exception();return sas;},write,0);readFails=true;
  restore=false;check(Reject(()=>s.End(release))&&restore&&sas,"Read failure still releases and restores");
  var events=new List<string>();sas=false;s.Begin(()=>sas,v=>{sas=v;events.Add("sas");},0);s.End(v=>events.Add("release"));
  check(string.Join(",",events)=="release,sas","Release precedes restoration");
  sas=false;s.Begin(()=>sas,write,0);check(Reject(()=>s.End(v=>{throw new Exception();}))&&sas,"Release failure still restores");
 }
}
