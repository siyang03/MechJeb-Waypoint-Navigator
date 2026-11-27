using System;
namespace WaypointNavigator {
 // All callbacks and control objects belong to the original vessel for life.
 public sealed class NavSession {
  Part[] originalParts;
  CelestialBody body;
  public string VesselName {get;private set;}
  public Guid VesselId {get;private set;}
  public Vessel Vessel {get;private set;}
  public Plan Plan {get;private set;}
  public string PlanFileName {get;private set;}
  public Navigation Navigation {get;private set;} = new Navigation();
  public MechJebAdapter Adapter {get;private set;}
  public NavSasSession SasSession {get;private set;} = new NavSasSession();
  public int SelectedWaypoint {get;private set;}
  public bool Engaged {get;private set;}
  public double CommandedBank {get;private set;}
  public double NextDiagnostic;
  public string Status {get;private set;} = "Ready";
  readonly Func<Vessel,bool> conflict;
  public NavSession(Vessel vessel,Func<Vessel,bool> conflict) {Vessel=vessel;VesselName=vessel.vesselName;VesselId=vessel.id;this.conflict=conflict;body=vessel.mainBody;originalParts=vessel.parts.ToArray();}
  Geo Position() {return new Geo(Vessel.latitude,Vessel.longitude);}
  void Validate() {
   if(Vessel==null || Vessel.id!=VesselId || !FlightGlobals.Vessels.Contains(Vessel) ||
      !Vessel.loaded || Vessel.packed || Vessel.LandedOrSplashed || !HighLogic.LoadedSceneIsFlight)
    throw new InvalidOperationException("Vessel identity or flight state changed");
   if(!ReferenceEquals(body,Vessel.mainBody)||originalParts.Length!=Vessel.parts.Count)throw new InvalidOperationException("Vessel topology or body changed");
   for(int i=0;i<originalParts.Length;i++)if(!ReferenceEquals(originalParts[i],Vessel.parts[i]))throw new InvalidOperationException("Vessel parts changed");
   if(Plan!=null && Plan.Planet!=Vessel.mainBody.bodyName)throw new InvalidOperationException("Wrong celestial body");
   if(conflict(Vessel))throw new InvalidOperationException("Disengage all Kramax modes before NAV (or incompatible Kramax API)");
  }
  public void Start(Plan plan,string file,int waypoint,Func<object,bool> controllerOwned) {
   if(Engaged)throw new InvalidOperationException("Session already engaged");
   try {
    Validate();
    if(plan==null || plan.Points.Count==0)throw new InvalidOperationException("Select a nonempty plan");
    Plan=Snapshot(plan);PlanFileName=file;Validate();
    var candidate=MechJebAdapter.Find(Vessel);
    if(controllerOwned(candidate.Controller))throw new InvalidOperationException("MechJeb controller already owned by another vessel");
    Adapter=candidate;
    Navigation.BankLimit=Adapter.EffectiveBankLimit;
    Navigation.Start(Plan,Position(),Vessel.mainBody.Radius,waypoint,Planetarium.GetUniversalTime());
    SelectedWaypoint=waypoint;CommandedBank=0;Adapter.Acquire();
    SasSession.Log=message=>UnityEngine.Debug.Log("Waypoint Navigator SAS: vessel="+VesselId+" "+message);
    SasSession.Begin(()=>Vessel!=null&&Vessel.ActionGroups[KSPActionGroup.SAS],
     enabled=>{if(Vessel!=null&&Vessel.id==VesselId&&FlightGlobals.Vessels.Contains(Vessel))Vessel.ActionGroups.SetGroup(KSPActionGroup.SAS,enabled);},Adapter.ActualBank);
    Engaged=true;Status=SasSession.Status;
   } catch(Exception e) {Stop(e.GetBaseException().Message);throw;}
  }
  public void Check() {
   Validate();
   if(!Engaged)throw new InvalidOperationException("Session disengaged");
   SasSession.Check();
   if(!Adapter.Matches(Vessel))throw new InvalidOperationException("MechJeb controller changed");
   Adapter.CheckOwnership();
  }
  public void RestartToWaypoint(int waypoint) {
   Check();Navigation.BankLimit=Adapter.EffectiveBankLimit;
   Navigation.Start(Plan,Position(),Vessel.mainBody.Radius,waypoint,Planetarium.GetUniversalTime());
   SelectedWaypoint=waypoint;CommandedBank=0;SasSession.ResetRecovery();
  }
  public void Step() {
   Check();Navigation.BankLimit=Adapter.EffectiveBankLimit;
   Navigation.Step(Position(),Vessel.srfSpeed,Planetarium.GetUniversalTime(),TimeWarp.fixedDeltaTime);
   SelectedWaypoint=Navigation.Active;
   if(Navigation.Complete){Stop("Route complete");return;}
   if(Navigation.HasDemand)CommandedBank=Adapter.WriteBank(Navigation.TurnDemand);
   SasSession.Update(Adapter.ActualBank,Adapter.LastEffectiveBankCommand,TimeWarp.fixedDeltaTime);
   Status=SasSession.Status;
   if(UnityEngine.Time.realtimeSinceStartup>=NextDiagnostic) {
    NextDiagnostic=UnityEngine.Time.realtimeSinceStartup+5;
    try {UnityEngine.Debug.Log("Waypoint Navigator telemetry: vessel="+VesselId+" name="+Vessel.vesselName+" wp="+Navigation.Active+" xte="+Navigation.CrossTrack+" lateralVelocity="+Navigation.CrossTrackVelocity+" desiredLateral="+Navigation.DesiredCrossTrackVelocity+" turnDemand="+Navigation.TurnDemand+" "+Adapter.Diagnostic());}
    catch(Exception e){UnityEngine.Debug.LogWarning("Waypoint Navigator telemetry "+VesselId+": "+e.Message);}
   }
  }
  public void Stop(string reason) {
   Engaged=false;Status=reason;
   try {SasSession.End(restore=>{if(Adapter!=null)Adapter.Release(restore);},Vessel!=null&&Vessel.id==VesselId&&FlightGlobals.Vessels.Contains(Vessel));}
   finally {Adapter=null;}
  }
  static Plan Snapshot(Plan source) {
   var p=new Plan {Name=source.Name,Planet=source.Planet,Description=source.Description};
   foreach(var w in source.Points) {
    var copy=new Waypoint {Name=w.Name,Position=w.Position,Altitude=w.Altitude};
    foreach(var flag in w.Flags)copy.Flags.Add(flag.Key,flag.Value);
    p.Points.Add(copy);
   }
   return p;
  }
 }
}
