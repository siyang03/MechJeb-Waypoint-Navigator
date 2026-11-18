using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
namespace WaypointNavigator {
 [KSPAddon(KSPAddon.Startup.Flight,false)]
 public sealed class NavigatorWindow : MonoBehaviour {
  Rect window=new Rect(220,80,413,338); Vector2 contentScroll;
  GUIStyle wrappedLabel,wrappedButton;
  string folder,fileName="FlightPlans.cfg"; string[] files=new string[0];
  readonly Navigation nav=new Navigation(); List<Plan> plans=new List<Plan>();
  Plan selected; int selectedWaypoint; Vessel vessel; MechJebAdapter adapter;
  bool engaged,show=true,selector=false; string status="Ready";
  double nextDiagnostic; double commandedBank;
  void Start() {
   folder=Path.Combine(KSPUtil.ApplicationRootPath,"GameData/MechJebWaypointNavigator");
   Refresh();
  }
  void Refresh() {
   StopNav("Plans refreshed");
   try {
    var found=new List<string>();
    if(Directory.Exists(folder)) foreach(string path in Directory.GetFiles(folder))
     if(string.Equals(Path.GetExtension(path),".cfg",StringComparison.OrdinalIgnoreCase)) found.Add(Path.GetFileName(path));
    found.Sort(StringComparer.OrdinalIgnoreCase); files=found.ToArray();
    if(files.Length==0) { plans.Clear(); selected=null; status="No CFG files in navigator folder"; return; }
    if(Array.IndexOf(files,fileName)<0) fileName=files[0];
    LoadFile(fileName);
   } catch(Exception e) { status="Cannot read CFG folder; see KSP log"; Debug.LogWarning("Waypoint Navigator: "+e); }
  }
  void LoadFile(string name) {
   StopNav("Loading plan"); fileName=name; plans.Clear(); selected=null; selectedWaypoint=0;
   try {
    string warning; plans=FlightPlans.Load(Path.Combine(folder,fileName),out warning);
    selected=plans.Count>0?plans[0]:null;
    status=warning.Length>0?warning:plans.Count+" plans loaded";
   } catch(Exception e) { status="Cannot load "+fileName+"; see KSP log"; Debug.LogWarning("Waypoint Navigator: "+e); }
  }
  Geo Position() { return new Geo(vessel.latitude,vessel.longitude); }
  bool Conflict(Vessel v) {
   foreach(var assembly in AppDomain.CurrentDomain.GetAssemblies()) {
    if(assembly.GetName().Name.IndexOf("Kramax",StringComparison.OrdinalIgnoreCase)<0) continue;
    var type=assembly.GetType("Kramax.KramaxAutoPilot");
    if(type==null) return true;
    var field=type.GetField("mainPilot",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Public);
    if(field==null) return true;
    foreach(var addon in UnityEngine.Object.FindObjectsOfType(type)) {
     object pilot=field.GetValue(addon); if(pilot==null) continue;
     foreach(string name in new[]{"HrztActive","VertActive","ThrtActive"}) {
      var mode=pilot.GetType().GetField(name,System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic);
      if(mode==null || (bool)mode.GetValue(pilot)) return true;
     }
    }
   }
   return false;
  }
  void Engage() {
   StopNav("Ready");
   try {
    vessel=FlightGlobals.ActiveVessel;
    if(vessel==null || vessel.packed || vessel.LandedOrSplashed) throw new InvalidOperationException("Engage while airborne in an unpacked vessel");
    if(selected==null || selected.Points.Count==0) throw new InvalidOperationException("Select a nonempty plan");
    if(selected.Planet!=vessel.mainBody.bodyName) throw new InvalidOperationException("Plan is for "+selected.Planet);
    if(Conflict(vessel)) throw new InvalidOperationException("Disengage all Kramax modes before using NAV (or incompatible Kramax API)");
    adapter=MechJebAdapter.Find(vessel);nav.BankLimit=adapter.EffectiveBankLimit;
    nav.Start(selected,Position(),vessel.mainBody.Radius,selectedWaypoint,Planetarium.GetUniversalTime());
    commandedBank=0;
    adapter.Acquire(); engaged=true; status="NAV engaged";
   } catch(Exception e) { StopNav(e.GetBaseException().Message); }
  }
  void StopNav(string reason) {
   engaged=false;
   try { if(adapter!=null) adapter.Release(); } catch(Exception e) { Debug.LogWarning("Waypoint Navigator release: "+e.Message); }
   adapter=null; status=reason;
  }
  void Update() {
   if(Input.GetKeyDown(KeyCode.F8)) show=!show;
   if(engaged && (FlightGlobals.ActiveVessel!=vessel || vessel==null || vessel.packed || vessel.LandedOrSplashed || !HighLogic.LoadedSceneIsFlight || selected.Planet!=vessel.mainBody.bodyName)) StopNav("Vessel or flight state changed");
  }
  void FixedUpdate() {
   if(!engaged) return;
   try {
    if(vessel==null || FlightGlobals.ActiveVessel!=vessel || vessel.packed || vessel.LandedOrSplashed || !HighLogic.LoadedSceneIsFlight) { StopNav("Vessel or flight state changed"); return; }
    if(Conflict(vessel)) throw new InvalidOperationException("Kramax activated; NAV released");
    nav.BankLimit=adapter.EffectiveBankLimit;
    nav.Step(Position(),vessel.srfSpeed,Planetarium.GetUniversalTime(),TimeWarp.fixedDeltaTime);
    if(nav.Complete) { selectedWaypoint=nav.Active; StopNav("Route complete"); return; }
    selectedWaypoint=nav.Active;
    if(nav.HasDemand) commandedBank=adapter.WriteBank(nav.TurnDemand);
    if(Time.realtimeSinceStartup>=nextDiagnostic) {
     nextDiagnostic=Time.realtimeSinceStartup+5;
     try { Debug.Log("Waypoint Navigator telemetry: wp="+nav.Active+" xte="+nav.CrossTrack.ToString("F1")+
      " lateralVelocity="+nav.CrossTrackVelocity.ToString("F1")+" desiredLateral="+nav.DesiredCrossTrackVelocity.ToString("F1")+
      " turnDemand="+nav.TurnDemand.ToString("F1")+" "+adapter.Diagnostic()); }
     catch(Exception e) { Debug.LogWarning("Waypoint Navigator telemetry unavailable: "+e.Message); }
    }
   } catch(Exception e) { StopNav(e.GetBaseException().Message); }
  }
  void OnDisable() { StopNav("Navigator disabled"); }
  void OnDestroy() { StopNav("Left flight"); }
  void OnGUI() {
   if(!show) return;
   if(wrappedLabel==null) {
    wrappedLabel=new GUIStyle(GUI.skin.label) {wordWrap=true};
    wrappedButton=new GUIStyle(GUI.skin.button) {wordWrap=true};
   }
   window.width=413; window.height=338;
   window=GUILayout.Window(GetInstanceID(),window,Draw,"Waypoint Navigator (Press F8 to hide/show)",GUILayout.Width(413),GUILayout.Height(338));
   window.x=Mathf.Clamp(window.x,0,Mathf.Max(0,Screen.width-window.width)); window.y=Mathf.Clamp(window.y,0,Mathf.Max(0,Screen.height-40));
  }
  void ChooseWaypoint(int index) {
   selectedWaypoint=index;
   if(engaged) Engage();
   else { nav.Plan=null; status="Target selected: "+selected.Points[index].Name; }
  }
  void Divider() { GUILayout.Box("",GUILayout.Width(363),GUILayout.Height(1)); }
  void Label(string text) { GUILayout.Label(text,wrappedLabel,GUILayout.Width(363)); }
  void Draw(int id) {
   GUILayout.BeginHorizontal();
   if(GUILayout.Button(engaged?"NAV off":"NAV on")) { if(engaged) StopNav("Disengaged"); else Engage(); }
   if(GUILayout.Button("Plans")) selector=!selector;
   GUILayout.EndHorizontal();
   // A fixed viewport keeps long routes, filenames and diagnostics inside the compact window.
   contentScroll=GUILayout.BeginScrollView(contentScroll,GUILayout.Width(393),GUILayout.Height(278));
   Label(status);
   Label(selected==null?"No plan":"Plan: "+selected.Name);
   Divider();
   if(selector) {
    if(GUILayout.Button("Refresh CFG files",GUILayout.Width(363))) Refresh();
    Divider();
    Label("CFG files");
    foreach(string file in files) if(GUILayout.Button((file==fileName?"> ":"")+file,wrappedButton,GUILayout.Width(363))) LoadFile(file);
    Divider();
    Label("Flight plans in "+fileName);
    foreach(var plan in plans) if(GUILayout.Button((plan==selected?"> ":"")+plan.Name,wrappedButton,GUILayout.Width(363))) {
     StopNav("Plan selected"); selected=plan; selectedWaypoint=0; selector=false;
    }
    Divider();
   }
   if(selected!=null) {
    var active=FlightGlobals.ActiveVessel;
    double r=active!=null?active.mainBody.Radius:0;
    bool session=nav.Plan==selected && active!=null && active==vessel;
    int activeIndex=session?nav.Active:selectedWaypoint;
    Label("Active: "+(activeIndex+1)+". "+selected.Points[activeIndex].Name);
    string targetBank=session && engaged?commandedBank.ToString("+0;-0;0")+"°":"—";
    string currentHeading=active!=null?FlightGlobals.ship_heading.ToString("F0")+"°":"—";
    string waypointDirection="—";
    if(active!=null && selected.Planet==active.mainBody.bodyName) {
     Geo waypoint=selected.Points[session?nav.Active:selectedWaypoint].Position;
     Geo here=new Geo(active.latitude,active.longitude);
     if(Sphere.Distance(here,waypoint,r)>0.01) waypointDirection=Sphere.Bearing(here,waypoint).ToString("F0")+"°";
    }
    string currentBank="—",bankLimit="—";
    try {
     var reader=active!=null?(active==vessel&&adapter!=null?adapter:MechJebAdapter.Find(active)):null;
     if(reader!=null) {
      bankLimit="±"+reader.EffectiveBankLimit.ToString("F0")+"°";
      double bank=reader.ActualBank;
      if(!double.IsNaN(bank)&&!double.IsInfinity(bank))currentBank=bank.ToString("+0;-0;0")+"°";
     }
    } catch { /* A missing MechJeb controller leaves only the readout unavailable. */ }
    Label("WP direction: "+waypointDirection+" | Current Hdg: "+currentHeading);
    Label("Target bank: "+targetBank+" | Current bank: "+currentBank+" | Limit: "+bankLimit);
    if(session) {
     double deviation,remaining;
     nav.ReadFlightData(new Geo(active.latitude,active.longitude),out deviation,out remaining);
     double rounded=Math.Round(deviation);
     string side=rounded>0?" (right)":rounded<0?" (left)":"";
     Label("Course deviation: "+rounded.ToString("+0;-0;0")+" m"+side);
     Divider();
     Label("WP "+(activeIndex+1)+" remaining: "+(remaining/1000).ToString("F1")+" km");
     Label("Route progress: "+(nav.Progress/1000).ToString("F0")+" / "+(nav.Total/1000).ToString("F0")+" km");
    } else {
     Label("Course deviation: —");
     Divider();
     Label("WP "+(activeIndex+1)+" remaining: —");
     Label("Route progress: 0 / "+(nav.StoredTotal(selected,r)/1000).ToString("F0")+" km");
    }
    for(int i=0;i<selected.Points.Count;i++) {
     var wp=selected.Points[i];
     if(GUILayout.Button((nav.Plan==selected && nav.Active==i?"> ":"")+(i==selectedWaypoint?"• ":"")+(i+1)+". "+wp.Name+" | "+wp.Altitude.ToString("F0")+" m",wrappedButton,GUILayout.Width(363))) ChooseWaypoint(i);
    }
    if(!string.IsNullOrEmpty(selected.Description)) Label(selected.Description);
   }
   Label("Altitude: display only.");
   GUILayout.EndScrollView();
   GUI.DragWindow(new Rect(0,0,10000,24));
  }
 }
}
