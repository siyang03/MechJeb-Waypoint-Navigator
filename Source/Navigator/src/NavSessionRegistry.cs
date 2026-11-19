using System;
using System.Collections.Generic;
namespace WaypointNavigator {
 public sealed class VesselPlanSelection {
  public string FileName="FlightPlans.cfg",Status="Ready";
  public int PlanIndex,WaypointIndex;
  public List<Plan> Plans=new List<Plan>();
 }
 // Shared by the UI and tests; all failures are isolated to one registry entry.
 public sealed class NavSessionRegistry {
  readonly Dictionary<Guid,NavSession> sessions=new Dictionary<Guid,NavSession>();
  public readonly Dictionary<Guid,VesselPlanSelection> Selections=new Dictionary<Guid,VesselPlanSelection>();
  public Action<string> Log=delegate {};
  void Report(string message) {try{Log(message);}catch{ /* Logging cannot break cleanup. */ }}
  public int Count {get{return sessions.Count;}}
  public VesselPlanSelection Selection(Guid id) {
   VesselPlanSelection value;
   if(!Selections.TryGetValue(id,out value)){value=new VesselPlanSelection();Selections.Add(id,value);}
   return value;
  }
  public NavSession Find(Vessel vessel) {
   if(vessel==null)return null;
   NavSession value;
   if(!sessions.TryGetValue(vessel.id,out value))return null;
   if(!ReferenceEquals(value.Vessel,vessel)){Stop(value.VesselId,"Stale vessel reference");return null;}
   return value;
  }
  public NavSession Start(Vessel vessel,Plan plan,string file,int waypoint,Func<Vessel,bool> conflict) {
   if(vessel==null)throw new InvalidOperationException("No vessel");
   if(sessions.ContainsKey(vessel.id))throw new InvalidOperationException("Vessel already navigating");
   var value=new NavSession(vessel,conflict);
   value.Start(plan,file,waypoint,controller=>{
    foreach(var other in sessions.Values)if(ReferenceEquals(other.Adapter.Controller,controller))return true;
    return false;
   });
   sessions.Add(value.VesselId,value);return value;
  }
  public void Stop(Guid id,string reason) {
   NavSession value;if(!sessions.TryGetValue(id,out value))return;
   try {value.Stop(reason);}catch(Exception e){Report("Waypoint Navigator cleanup "+id+": "+e);}
   finally {
    sessions.Remove(id);var selection=Selection(id);
    // A CFG selection may have changed while this private route was running.
    // Never copy its waypoint index into an unrelated or shorter UI route.
    if(selection.FileName==value.PlanFileName && selection.PlanIndex>=0 && selection.PlanIndex<selection.Plans.Count) {
     var plan=selection.Plans[selection.PlanIndex];
     if(plan.Name==value.Plan.Name)selection.WaypointIndex=Math.Max(0,Math.Min(value.SelectedWaypoint,plan.Points.Count-1));
    }
    selection.Status=reason;
    Report("Waypoint Navigator stopped "+id+" name="+value.VesselName+": "+reason);
   }
  }
  public void Run(bool physics) {
   foreach(var value in new List<NavSession>(sessions.Values)) {
    try {
     if(physics)value.Step();else value.Check();
     if(!value.Engaged)Stop(value.VesselId,value.Status);
    } catch(Exception e) {Stop(value.VesselId,e.GetBaseException().Message);}
   }
  }
  public void StopAll(string reason) {foreach(var value in new List<NavSession>(sessions.Values))Stop(value.VesselId,reason);}
 }
}
