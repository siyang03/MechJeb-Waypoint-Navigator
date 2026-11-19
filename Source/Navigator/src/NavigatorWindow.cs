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
  readonly NavSessionRegistry registry=new NavSessionRegistry();
  readonly Navigation emptyNav=new Navigation();
  Vessel uiVessel; VesselPlanSelection selection=new VesselPlanSelection();
  List<Plan> plans {get{return selection.Plans;}set{selection.Plans=value;}}
  Plan selected {get{return selection.PlanIndex>=0&&selection.PlanIndex<plans.Count?plans[selection.PlanIndex]:null;}set{selection.PlanIndex=plans.IndexOf(value);}}
  int selectedWaypoint {get{return selection.WaypointIndex;}set{selection.WaypointIndex=value;}}
  string status {get{var session=registry.Find(uiVessel);return session!=null?session.Status:selection.Status;}set{selection.Status=value;}}
  NavSession current {get{return registry.Find(uiVessel);}}
  bool engaged {get{return current!=null&&current.Engaged;}}
  Navigation nav {get{return current!=null?current.Navigation:emptyNav;}}
  MechJebAdapter adapter {get{return current!=null?current.Adapter:null;}}
  Vessel vessel {get{return uiVessel;}}
  double commandedBank {get{return current!=null?current.CommandedBank:0;}}
  bool show=true,selector=false;
  void SyncVessel() {
   var active=FlightGlobals.ActiveVessel;
   if(ReferenceEquals(uiVessel,active))return;
   uiVessel=active;selection=active!=null?registry.Selection(active.id):new VesselPlanSelection();
   fileName=selection.FileName;emptyNav.Plan=null;selector=false;
   if(active!=null&&plans.Count==0)LoadFile(fileName);
  }
  void Start() {
   folder=Path.Combine(KSPUtil.ApplicationRootPath,"GameData/MechJebWaypointNavigator");
   registry.Log=message=>Debug.LogWarning(message);
   SyncVessel(); Refresh();
  }
  void Refresh() {
   // Running sessions retain their private plan snapshots.
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
   bool same=selection.FileName==name;
   string previous=same&&selected!=null?selected.Name:null;
   int waypoint=same?selectedWaypoint:0;
   fileName=name; selection.FileName=name; plans.Clear(); selected=null; selectedWaypoint=0;
   try {
    string warning; plans=FlightPlans.Load(Path.Combine(folder,fileName),out warning);
    selected=plans.Count>0?plans[0]:null;
    if(previous!=null)foreach(var plan in plans)if(plan.Name==previous){selected=plan;break;}
    if(selected!=null)selectedWaypoint=Math.Max(0,Math.Min(waypoint,selected.Points.Count-1));
    status=warning.Length>0?warning:plans.Count+" plans loaded";
   } catch(Exception e) { status="Cannot load "+fileName+"; see KSP log"; Debug.LogWarning("Waypoint Navigator: "+e); }
  }
  bool Conflict(Vessel v) {
   foreach(var assembly in AppDomain.CurrentDomain.GetAssemblies()) {
    if(assembly.GetName().Name.IndexOf("Kramax",StringComparison.OrdinalIgnoreCase)<0) continue;
    var type=assembly.GetType("Kramax.KramaxAutoPilot");
    if(type==null) return true;
    var field=type.GetField("mainPilot",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Public);
    if(field==null) return true;
    foreach(var addon in UnityEngine.Object.FindObjectsOfType(type)) {
     object pilot=field.GetValue(addon); if(pilot==null) continue;
     // George publicly binds its controls to one vessel in the supplied Kramax.
     // Unknown APIs remain conservatively global conflicts.
     var vesselField=pilot.GetType().GetField("vessel",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Public);
     if(vesselField!=null && vesselField.FieldType==typeof(Vessel)) {
      var owner=vesselField.GetValue(pilot) as Vessel;
      if(owner!=null && !ReferenceEquals(owner,v))continue;
     }
     foreach(string name in new[]{"HrztActive","VertActive","ThrtActive"}) {
      var mode=pilot.GetType().GetField(name,System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic);
      if(mode==null || (bool)mode.GetValue(pilot)) return true;
     }
    }
   }
   return false;
  }
  void Engage(bool restart=false) {
   if(uiVessel==null){status="No active vessel";return;}
   try {
    if(restart)current.RestartToWaypoint(selectedWaypoint);
    else registry.Start(uiVessel,selected,fileName,selectedWaypoint,Conflict);
   } catch(Exception e) {StopNav(e.GetBaseException().Message);}
  }
  void StopNav(string reason) {if(uiVessel!=null)registry.Stop(uiVessel.id,reason);status=reason;}
  void Update() {
   if(Input.GetKeyDown(KeyCode.F8))show=!show;
   registry.Run(false);SyncVessel();
  }
  void FixedUpdate() {
   registry.Run(true);
  }
  void OnDisable() {registry.StopAll("Navigator disabled");}
  void OnDestroy() {registry.StopAll("Left flight");}
  void OnGUI() {
   SyncVessel();
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
   if(engaged) Engage(true);
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
   Label("Active NAV sessions: "+registry.Count);
   Label(status);
   Label(current!=null?"Plan: "+current.Plan.Name:selected==null?"No plan":"Plan: "+selected.Name);
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
   var displayed=current!=null?current.Plan:selected;
   if(displayed!=null && displayed.Points.Count>0) {
    var active=FlightGlobals.ActiveVessel;
    double r=active!=null?active.mainBody.Radius:0;
    bool session=nav.Plan==displayed && active!=null && active==vessel;
    int activeIndex=session?nav.Active:Math.Max(0,Math.Min(selectedWaypoint,displayed.Points.Count-1));
    Label("Active: "+(activeIndex+1)+". "+displayed.Points[activeIndex].Name);
    string targetBank=session && engaged?commandedBank.ToString("+0;-0;0")+"°":"—";
    string currentHeading=active!=null?FlightGlobals.ship_heading.ToString("F0")+"°":"—";
    string waypointDirection="—";
    if(active!=null && displayed.Planet==active.mainBody.bodyName) {
     Geo waypoint=displayed.Points[activeIndex].Position;
     Geo here=new Geo(active.latitude,active.longitude);
     if(Sphere.Distance(here,waypoint,r)>0.01) waypointDirection=Sphere.Bearing(here,waypoint).ToString("F0")+"°";
    }
    string currentBank="—",bankLimit="—";
    try {
     var reader=active!=null?(active==vessel&&adapter!=null?adapter:MechJebAdapter.Find(active)):null;
     if(reader!=null) {
      bankLimit="±"+reader.EffectiveBankLimit.ToString("F0")+"° "+reader.BankLimitSource;
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
     Label("Route progress: 0 / "+(nav.StoredTotal(displayed,r)/1000).ToString("F0")+" km");
    }
    for(int i=0;i<displayed.Points.Count;i++) {
     var wp=displayed.Points[i];
     if(GUILayout.Button((nav.Plan==displayed && nav.Active==i?"> ":"")+(i==selectedWaypoint?"• ":"")+(i+1)+". "+wp.Name+" | "+wp.Altitude.ToString("F0")+" m",wrappedButton,GUILayout.Width(363))) ChooseWaypoint(i);
    }
    if(!string.IsNullOrEmpty(displayed.Description)) Label(displayed.Description);
   }
   Label("Altitude: display only.");
   GUILayout.EndScrollView();
   GUI.DragWindow(new Rect(0,0,10000,24));
  }
 }
}
