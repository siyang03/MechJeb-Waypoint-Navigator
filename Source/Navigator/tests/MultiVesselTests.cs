using System;
using WaypointNavigator;
static class MultiVesselTests {
 static Vessel Aircraft() {
  var v=new Vessel();var p=new Part();p.Modules.Add(new MuMech.MechJebCore());v.parts.Add(p);FlightGlobals.Vessels.Add(v);return v;
 }
 static MuMech.MechJebCore Core(Vessel v){return (MuMech.MechJebCore)v.parts[0].Modules[0];}
 static Plan Route() {
  var p=new Plan {Planet="Kerbin",Name="Shared"};
  p.Points.Add(new Waypoint {Name="First",Position=new Geo(0,10)});
  p.Points.Add(new Waypoint {Name="Second",Position=new Geo(10,10)});return p;
 }
 static bool Reject(Action action){try{action();return false;}catch{return true;}}
 public static void Run(Action<bool,string> check) {
  int count=0;Action<bool,string> C=(ok,name)=>{count++;check(ok,"Multi-vessel: "+name);};
  var r=new NavSessionRegistry();var a=Aircraft();var b=Aircraft();var plan=Route();
  Func<Vessel,bool> conflict=v=>false;
  Func<Vessel,NavSession> start=v=>r.Start(v,plan,"shared.cfg",0,conflict);
  var sa=start(a);var sb=start(b);
  C(r.Count==2&&sa.Engaged&&sb.Engaged,"both engaged");
  C(!ReferenceEquals(sa.Navigation,sb.Navigation)&&!ReferenceEquals(sa.Adapter,sb.Adapter)&&!ReferenceEquals(sa.SasSession,sb.SasSession),"separate mutable controllers");
  C(!ReferenceEquals(sa.Plan,sb.Plan)&&!ReferenceEquals(sa.Plan.Points[0],plan.Points[0]),"deep route snapshots");
  foreach(var v in new[]{a,b,a}){FlightGlobals.ActiveVessel=v;r.Run(false);C(ReferenceEquals(r.Find(v),v==a?sa:sb)&&r.Count==2,"switch retains correct session");}
  C(Reject(()=>start(a))&&r.Count==2,"duplicate rejected");
  Core(a).Airplane.BankAngle=8;Core(b).Airplane.BankAngle=19;
  a.latitude=.1;a.longitude=1;b.latitude=-.1;b.longitude=1;Planetarium.Time=1;r.Run(true);
  C(Core(a).Airplane.RollTarget==sa.CommandedBank&&Core(b).Airplane.RollTarget==sb.CommandedBank,"explicit controller writes");
  C(sa.CommandedBank*sb.CommandedBank<0&&Math.Abs(sa.CommandedBank)<=8&&Math.Abs(sb.CommandedBank)<=19,"independent errors and limits");
  var bProgress=sb.Navigation.Progress;var bDemand=Core(b).Airplane.RollTarget;var adapter=sa.Adapter;var sas=sa.SasSession;
  sa.RestartToWaypoint(1);
  C(sa.Navigation.Active==1&&ReferenceEquals(adapter,sa.Adapter)&&ReferenceEquals(sas,sa.SasSession)&&!a.ActionGroups[KSPActionGroup.SAS],"direct-to retains control");
  C(sa.Navigation.Progress!=sb.Navigation.Progress&&sa.Navigation.Active!=sb.Navigation.Active,"independent waypoint progress");
  C(sb.Navigation.Progress==bProgress&&Core(b).Airplane.RollTarget==bDemand,"direct-to leaves B unchanged");
  r.Selection(a.id).FileName="a.cfg";r.Selection(a.id).WaypointIndex=1;r.Selection(b.id).FileName="b.cfg";
  plan.Points[0].Name="Refreshed";r.Selection(a.id).Plans.Clear();
  C(r.Selection(b.id).FileName=="b.cfg"&&sb.Plan.Points[0].Name=="First","selection and refresh isolation");
  var ca=Core(a).Airplane;var cb=Core(b).Airplane;r.Stop(a.id,"Disengaged");
  C(a.ActionGroups[KSPActionGroup.SAS]&&!ca.RollHoldEnabled&&!MechJebYawBridge.YawEnabled(ca),"A releases SAS roll yaw");
  C(!b.ActionGroups[KSPActionGroup.SAS]&&cb.RollHoldEnabled&&MechJebYawBridge.YawEnabled(cb),"B retains SAS roll yaw");
  // Every failure is processed by the production registry, followed by B's step.
  Action<Action> isolated=fail=>{
   sa=start(a);double before=sb.Navigation.Status.timestamp;Planetarium.Time+=1;fail();r.Run(true);
   C(r.Find(a)==null&&sb.Engaged&&sb.Navigation.Status.timestamp>before,"failure isolated and B stepped");
   a.packed=false;a.loaded=true;a.LandedOrSplashed=false;a.mainBody.bodyName="Kerbin";Core(a).running=true;ca.Enabled=true;ca.HeadingHoldEnabled=false;ca.RollHoldEnabled=false;
  };
  isolated(()=>ca.RollTarget=123);
  isolated(()=>a.ActionGroups.SetGroup(KSPActionGroup.SAS,true));
  isolated(()=>ca.HeadingHoldEnabled=true);
  isolated(()=>a.packed=true);
  isolated(()=>a.loaded=false);
  isolated(()=>a.LandedOrSplashed=true);
  isolated(()=>Core(a).running=false);
  isolated(()=>ca.Enabled=false);
  isolated(()=>a.mainBody.bodyName="Mun");a.mainBody.bodyName="Kerbin";
  isolated(()=>Core(a).Attitude=new MuMech.FakeController());Core(a).Attitude=null;
  isolated(()=>sa.Navigation.Plan.Points.Clear());
  C(Reject(()=>r.Start(a,null,"empty.cfg",0,conflict))&&r.Count==1,"empty plan rejected transactionally");
  a.loaded=false;C(Reject(()=>start(a))&&r.Count==1,"unloaded start rejected");a.loaded=true;
  a.packed=true;C(Reject(()=>start(a))&&r.Count==1,"packed start rejected");a.packed=false;
  C(Reject(()=>r.Start(a,plan,"conflict.cfg",0,v=>true))&&r.Count==1,"competing autopilot start rejected");
  ca.Enabled=false;C(Reject(()=>start(a))&&r.Count==1&&sb.Engaged,"failed acquisition not registered");ca.Enabled=true;
  var shared=Aircraft();((MuMech.MechJebCore)shared.parts[0].Modules[0]).Airplane=cb;
  C(Reject(()=>start(shared))&&MechJebYawBridge.YawEnabled(cb)&&sb.Engaged,"shared controller rejection preserves owner");
  var replacement=new Vessel {id=b.id};
  C(r.Find(replacement)==null&&!sb.Engaged,"stale reference never rebound");
  sb=start(b);sa=start(a);a.id=Guid.NewGuid();r.Run(false);
  C(!sa.Engaged&&sb.Engaged,"identity mismatch isolated");
  sa=start(a);a.parts.Add(new Part());r.Run(false);
  C(!sa.Engaged&&sb.Engaged,"topology change stopped");a.parts.RemoveAt(1);
  sa=start(a);Core(a).Airplane=new MuMech.FakeController();r.Run(false);
  C(!sa.Engaged&&sb.Engaged,"controller replacement stopped");ca=Core(a).Airplane;
  var finish=new Plan {Planet="Kerbin"};finish.Points.Add(new Waypoint {Name="End",Position=new Geo(a.latitude,a.longitude)});
  sa=r.Start(a,finish,"finish.cfg",0,conflict);bProgress=sb.Navigation.Progress;sa.Step();
  C(!sa.Engaged&&sb.Engaged&&sb.Navigation.Progress==bProgress,"route completion isolated");r.Run(false);
  sa=start(a);var aa=Core(a).Airplane;a.ActionGroups.ThrowOnRestore=true;r.Log=message=>{throw new Exception("logger failure");};
  r.StopAll("Left flight");r.StopAll("Repeated");
  C(r.Count==0&&a.ActionGroups[KSPActionGroup.SAS]&&b.ActionGroups[KSPActionGroup.SAS],"global cleanup idempotent");
  C(!MechJebYawBridge.YawEnabled(aa)&&!MechJebYawBridge.YawEnabled(cb),"all yaw owners detached");
  C(!aa.RollHoldEnabled&&!cb.RollHoldEnabled,"all horizontal owners released despite cleanup exception");
  a.ActionGroups.ThrowOnRestore=false;
  sa=start(a);a.packed=true;r.Run(false);a.packed=false;r.Run(false);
  C(r.Count==0&&!sa.Engaged,"unpacking does not auto-resume");
  sa=start(a);sb=start(b);HighLogic.LoadedSceneIsFlight=false;r.Run(false);HighLogic.LoadedSceneIsFlight=true;
  C(r.Count==0&&!sa.Engaged&&!sb.Engaged,"scene exit stops every session");
  Console.WriteLine(count+" multi-vessel assertions passed against production sessions and registry.");
 }
}
