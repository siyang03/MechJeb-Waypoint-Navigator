using System;
using System.Collections.Generic;
using WaypointNavigator;
using P=WaypointNavigator.KramaxPort;
public class Vessel {public List<Part> parts=new List<Part>();}
public class Part {public List<PartModule> Modules=new List<PartModule>();}
public class PartModule {}
namespace MuMech {
 public class FakeEditable {public double Val {get;set;}}
 public class FakeState {public double currentHeading {get;set;}public double currentRoll {get;set;}}
 public class MechJebCore:PartModule {public MechJebCore MasterMechJeb {get{return this;}}public bool running=true;public FakeController Attitude;public FakeState VesselState=new FakeState();public FakeController Airplane=new FakeController();}
 public class FakeController {
  public bool Enabled=true,HeadingHoldEnabled,RollHoldEnabled;public double HeadingTarget=123,RollTarget=7,BankAngle=25;
  public FakeEditable RollLimit=new FakeEditable {Val=45};
  public double AltitudeTarget=10050,SpeedTarget=285;
  public void EnableHeadingHold(){HeadingHoldEnabled=RollHoldEnabled=true;}
 }
}
class Tests {
 static int checks;
 static void Check(bool ok,string label){checks++;if(!ok)throw new Exception(label);}
 static void Equal(double a,double b,string label){Check(double.IsNaN(a)&&double.IsNaN(b)||a==b||Math.Abs(a-b)<=1e-9*Math.Max(1,Math.Abs(a)),label+" actual="+a+" reference="+b);}
 static Plan Route(params Geo[] points){var p=new Plan();foreach(var g in points)p.Points.Add(new Waypoint {Position=g,Name="WP"});return p;}
 static void Main(){
  var random=new Random(20261118);
  // Independently compiled original CourseUtils.CrossTrackError is the oracle.
  for(int i=0;i<300;i++) {
   double a=random.NextDouble()*140-70,b=random.NextDouble()*360-180,c=random.NextDouble()*140-70,d=random.NextDouble()*360-180,e=random.NextDouble()*140-70,f=random.NextDouble()*360-180;
   var x=new P.CourseStatus();var y=new Oracle.CourseStatus();
   P.CourseUtils.CrossTrackError(new P.Body {Radius=600000},a,b,c,d,e,f,x);
   Oracle.CourseUtils.CrossTrackError(new Oracle.Body {Radius=600000},a,b,c,d,e,f,y);
   Equal(x.dXt,y.dXt,"cross track");Equal(x.distanceTraveled,y.distanceTraveled,"along track");Equal(x.currentDistance,y.currentDistance,"remaining");Equal(x.currentBearing,y.currentBearing,"course bearing");Equal(x.fraction,y.fraction,"fraction");Equal(x.directDistance,y.directDistance,"direct distance");
  }
  var pid=new P.PID_Controller(new double[]{4,0.8,2,-30,30,-1,1,1,1});pid.skipDerivative=true;
  var reference=new Oracle.PID_Controller(new double[]{4,0.8,2,-30,30,-1,1,1,1});reference.skipDerivative=true;
  for(int i=0;i<1000;i++) {
   double dt=i%2==0?0.02:0.04;Oracle.TimeWarp.fixedDeltaTime=dt;
   double offset=(random.NextDouble()-0.5)*20000,velocity=(random.NextDouble()-0.5)*300,along=(random.NextDouble()-0.5)*600,speed=i%30==0?0:285;
   double desired;
   double actual=Navigation.CourseDemand(new P.CourseStatus {dXt=offset,vXt=velocity,vC=along},speed,dt,pid,out desired);
   double expected=Oracle.Guidance.Run(new Oracle.CourseStatus {dXt=offset,vXt=velocity,vC=along},speed,reference);
   Equal(actual,expected,"Kramax Course bank output/state");Check(Math.Abs(actual)<=30,"Upstream ±30 degree output bound");
  }
  // Compare sequence decisions with actual upstream CheckForSequence excerpt.
  foreach(bool valid in new[]{false,true})foreach(bool approach in new[]{false,true})foreach(bool next in new[]{false,true})foreach(double remaining in new[]{0.0,9.0,10.0,11.0,8549.0,8550.0,8551.0,20000.0})foreach(double speed in new[]{0.0,285.0}) {
   var status=new P.CourseStatus {beenValid=valid,courseBearing=90,courseDistance=100000,currentDistance=remaining,distanceTraveled=100000-remaining};
   var os=new Oracle.CourseStatus {beenValid=valid,courseBearing=90,courseDistance=100000,currentDistance=remaining,distanceTraveled=100000-remaining};
   var fp=new Oracle.FlightPlan {courseStatus=os,next=new Oracle.Node {lat=0,lon=10,approach=approach,next=next?new Oracle.Node {lat=10,lon=10}:null},position=new Oracle.Node {lat=0,lon=5}};
   fp.CheckForSequence(new Oracle.George(),new Oracle.Vessel {mainBody=new Oracle.Body {Radius=600000},srfSpeed=speed},new Oracle.VesselData());
   Check(Navigation.ShouldSequence(status,next,approach,0,speed)==(fp.Sequenced||fp.next==null),"Kramax sequence branch equivalence");
  }
  var nav=new Navigation();var plan=Route(new Geo(0,0),new Geo(0,10),new Geo(10,10));
  nav.Start(plan,new Geo(0,-1),600000,0,0);nav.Step(new Geo(0,-0.5),285,1,0.02);nav.Step(new Geo(0,0.001),285,2,0.02);Check(nav.Active==1,"Along-track passage sequences");
  nav.Step(new Geo(0,5),285,3,0.02);nav.Step(new Geo(0,9.3),285,4,0.02);Check(nav.Active==2,"3 degree per second anticipation");
  plan.Points[1].Flags["FAF"]="true";
  nav.Start(plan,new Geo(1,5),600000,1,0);double cross,distance;nav.ReadFlightData(new Geo(1,5),out cross,out distance);Check(Math.Abs(cross)>10000,"FAF direct-to retains inbound leg");
  nav.Step(new Geo(0,5),285,1,0.02);nav.Step(new Geo(0,9.3),285,2,0.02);Check(nav.Active==1,"FAF does not anticipate");
  nav.Start(Route(new Geo(0,0),new Geo(0,0),new Geo(0,10)),new Geo(0,0),600000,0);nav.Step(new Geo(0,0),285,1,0.02);Check(nav.Active==2,"Coincident-leg numerical guard");
  nav.Start(Route(new Geo(0,10)),new Geo(0,0),600000,0);nav.Step(new Geo(0,5),285,1,0.02);nav.Step(new Geo(0,10.001),285,2,0.02);Check(nav.Complete,"Final passage releases route");
  nav.Start(Route(new Geo(0,10)),new Geo(0,0),600000,0);nav.Step(new Geo(-0.01,1),285,1,0.02);Equal(nav.CrossTrackVelocity,nav.CrossTrack,"Finite difference uses elapsed universal time");
  // Actual upstream UpdateData + Course controller versus the full production path.
  var replay=new Navigation();replay.Start(Route(new Geo(0,80)),new Geo(0,0),600000,0,0);
  var original=new Oracle.FlightPlan {prev=new Oracle.Node {lat=0,lon=0},next=new Oracle.Node {lat=0,lon=80},position=new Oracle.Node(),courseStatus=new Oracle.CourseStatus()};
  var originalVessel=new Oracle.Vessel {mainBody=new Oracle.Body {Radius=600000},srfSpeed=285};
  Oracle.Planetarium.Time=0;original.UpdateData(new Oracle.George(),originalVessel,new Oracle.VesselData());
  var replayPid=new Oracle.PID_Controller(new double[]{4,0.8,2,-30,30,-1,1,1,1});replayPid.skipDerivative=true;
  for(int i=1;i<=200;i++) {
   double time=i*0.02,lat=0.01*Math.Sin(i*0.04),lon=i*0.001;
   replay.Step(new Geo(lat,lon),285,time,0.02);
   Oracle.Planetarium.Time=time;Oracle.TimeWarp.fixedDeltaTime=0.02;originalVessel.latitude=lat;originalVessel.longitude=lon;
   original.UpdateData(new Oracle.George(),originalVessel,new Oracle.VesselData());
   Equal(replay.Status.vXt,original.courseStatus.vXt,"Original UpdateData lateral velocity");
   Equal(replay.Status.vC,original.courseStatus.vC,"Original UpdateData along velocity");
   Equal(replay.TurnDemand,Oracle.Guidance.Run(original.courseStatus,285,replayPid),"Full replay bank demand versus original");
  }
  var unrestricted=new Navigation {BankLimit=60};
  unrestricted.Start(Route(new Geo(0,80)),new Geo(0,0),600000,0,0);
  unrestricted.Step(new Geo(.1,1),285,1,.02);
  Check(Math.Abs(unrestricted.TurnDemand)>30&&Math.Abs(unrestricted.TurnDemand)<=60,"PID can command beyond 30 with MechJeb authority");
  unrestricted.BankLimit=20;Check(Math.Abs(unrestricted.TurnDemand)<=20,"Live reduction immediately clamps previous demand");
  unrestricted.Step(new Geo(.2,2),285,2,.02);Check(Math.Abs(unrestricted.TurnDemand)<=20,"PID adopts reduced limit");
  unrestricted.BankLimit=0;unrestricted.Step(new Geo(.3,3),285,3,.02);Equal(unrestricted.TurnDemand,0,"Zero bank limit respected");
  foreach(double invalid in new[]{-1.0,double.NaN,double.PositiveInfinity}){bool rejected=false;try{unrestricted.BankLimit=invalid;}catch(ArgumentOutOfRangeException){rejected=true;}Check(rejected,"Invalid bank authority rejected");}
  // MechJeb remains the only aircraft roll controller, with no heading conversion.
  var core=new MuMech.MechJebCore();var part=new Part();part.Modules.Add(core);var vessel=new Vessel();vessel.parts.Add(part);var adapter=MechJebAdapter.Find(vessel);var ap=core.Airplane;
  adapter.Acquire();Check(!ap.HeadingHoldEnabled&&ap.RollHoldEnabled,"Native MechJeb roll mode acquired");Check(MechJebYawBridge.YawEnabled(ap),"Yaw loop enabled during NAV roll hold");
  Equal(adapter.WriteBank(20),20,"Kramax demand passed without ramp");Equal(ap.RollTarget,20,"Native roll setpoint written");
  ap.RollLimit.Val=12;Equal(adapter.WriteBank(30),12,"MechJeb authority limit retained");adapter.Release();Check(!MechJebYawBridge.YawEnabled(ap),"Yaw extension removed on release");Check(!ap.RollHoldEnabled,"Owned hold released");Equal(ap.BankAngle,25,"Bank setting untouched");Equal(ap.RollLimit.Val,12,"Roll limit untouched");
  ap.HeadingHoldEnabled=ap.RollHoldEnabled=true;core.VesselState.currentHeading=280;adapter.Acquire();adapter.WriteBank(-10);adapter.Release();Check(ap.HeadingHoldEnabled,"Previous heading mode restored");Check(MechJebYawBridge.YawEnabled(ap),"Normal MechJeb heading yaw remains enabled");Equal(ap.HeadingTarget,280,"No stale heading restored");
  adapter.Acquire();ap.RollTarget=15;bool threw=false;try{adapter.WriteBank(10);}catch(InvalidOperationException){threw=true;}Check(threw,"Manual roll override detected");Check(!MechJebYawBridge.YawEnabled(ap),"Manual override disables bridge immediately");adapter.Release();Equal(ap.RollTarget,15,"Manual target preserved");
  Equal(ap.AltitudeTarget,10050,"Altitude untouched");Equal(ap.SpeedTarget,285,"Speed untouched");
  ap.BankAngle=60;ap.RollLimit.Val=75;adapter.Acquire();Equal(adapter.WriteBank(80),60,"Adapter follows MechJeb bank angle above 30");
  ap.RollLimit.Val=45;Equal(adapter.WriteBank(-80),-45,"Live MechJeb roll limit remains authoritative");adapter.Release();
  ap.Enabled=false;threw=false;try{adapter.Acquire();}catch(InvalidOperationException){threw=true;}Check(threw,"Disabled autopilot rejected");
  Console.WriteLine(checks+" assertions passed, including differential tests against upstream geometry, Course PID and sequencing. No flight test.");
 }
}
