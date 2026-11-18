using System;
using System.Collections.Generic;
using WaypointNavigator.KramaxPort;
namespace WaypointNavigator {
 public struct Geo {
  public double Lat,Lon;
  public Geo(double lat,double lon) {Lat=lat;Lon=lon;}
 }
 // Display/file-format utilities delegate geographic calculations to the port.
 public static class Sphere {
  public const double Rad=Math.PI/180;
  public static double Clamp(double x,double lo,double hi) {return Math.Max(lo,Math.Min(hi,x));}
  public static double Wrap(double x) {return ((x+180)%360+360)%360-180;}
  public static double Distance(Geo a,Geo b,double radius) {
   double bearing,distance;CourseUtils.BearingAndDistance(new Body {Radius=radius},a.Lat,a.Lon,b.Lat,b.Lon,out bearing,out distance);return distance;
  }
  public static double Bearing(Geo a,Geo b) {
   double bearing,distance;CourseUtils.BearingAndDistance(new Body {Radius=1},a.Lat,a.Lon,b.Lat,b.Lon,out bearing,out distance);return bearing;
  }
 }
 public class Waypoint {
  public Geo Position;public double Altitude;public string Name;
  public readonly Dictionary<string,string> Flags=new Dictionary<string,string>();
  public bool HasFlag(string name) {string value;return Flags.TryGetValue(name,out value)&&string.Equals(value,"true",StringComparison.OrdinalIgnoreCase);}
 }
 public class Plan {public string Name,Description,Planet;public readonly List<Waypoint> Points=new List<Waypoint>();}
 public class Navigation {
  public Plan Plan;public int Active;public bool Complete,HasDemand;
  public double Progress,Total,InitialDistance,TurnDemand,DesiredCrossTrackVelocity;
  public CourseStatus Status {get;private set;}=new CourseStatus();
  public double CrossTrack {get{return Status.dXt;}}
  public double CrossTrackVelocity {get{return Status.vXt;}}
  public double DistanceToTarget {get{return Status.directDistance;}}
  Geo origin;Body body;double[] cumulative;
  PID_Controller velocityPid;
  // George.hdgModeChanged / FlightPlan.DirectToWaypoint. RW/FAF retain the inbound leg.
  public void Start(Plan plan,Geo position,double radius,int target,double timestamp=0) {
   if(plan==null||plan.Points.Count==0||target<0||target>=plan.Points.Count||radius<=0) throw new ArgumentException("Invalid route");
   Plan=plan;Active=target;body=new Body {Radius=radius};Complete=HasDemand=false;TurnDemand=0;
   origin=target>0&&(plan.Points[target].HasFlag("RW")||plan.Points[target].HasFlag("FAF"))?plan.Points[target-1].Position:position;
   cumulative=new double[plan.Points.Count];
   for(int i=1;i<cumulative.Length;i++) cumulative[i]=cumulative[i-1]+Sphere.Distance(plan.Points[i-1].Position,plan.Points[i].Position,radius);
   Total=cumulative[cumulative.Length-1];Progress=cumulative[target];InitialDistance=Sphere.Distance(position,plan.Points[target].Position,radius);
   Status=new CourseStatus();UpdateData(position,timestamp);
   // George.defaultXtrkGains and hdgModeChanged. This is navigation velocity PID,
   // not Kramax's aircraft roll/aileron/yaw PID.
   velocityPid=new PID_Controller(new double[]{4,0.8,2,-30,30,-1,1,1,1});
   velocityPid.skipDerivative=true;
   velocityPid.SetPoint=double.IsNaN(Status.vC)?0:Status.vC;
   if(!double.IsNaN(Status.vC)) velocityPid.BumplessSetPoint=0;
  }
  public double StoredTotal(Plan plan,double radius) {
   double sum=0;for(int i=1;i<plan.Points.Count;i++)sum+=Sphere.Distance(plan.Points[i-1].Position,plan.Points[i].Position,radius);return sum;
  }
  // FlightPlan.UpdateData, horizontal portion only; use game universal time.
  void UpdateData(Geo position,double timestamp) {
   double oldCross=Status.dXt,oldRemaining=Status.currentDistance,delta=timestamp-Status.timestamp;
   CourseUtils.CrossTrackError(body,origin.Lat,origin.Lon,Plan.Points[Active].Position.Lat,Plan.Points[Active].Position.Lon,position.Lat,position.Lon,Status);
   if(!Status.beenValid&&0<Status.fraction&&Status.fraction<1)Status.beenValid=true;
   Status.timestamp=timestamp;
   if(delta>0&&!double.IsNaN(Status.dXt-oldCross)&&!double.IsNaN(oldRemaining-Status.currentDistance)) {
    Status.vXt=(Status.dXt-oldCross)/delta;Status.vC=(oldRemaining-Status.currentDistance)/delta;
   } else {Status.vXt=Status.vC=double.NaN;}
  }
  public void ReadFlightData(Geo pos,out double cross,out double distance) {
   var sample=new CourseStatus();CourseUtils.CrossTrackError(body,origin.Lat,origin.Lon,Plan.Points[Active].Position.Lat,Plan.Points[Active].Position.Lon,pos.Lat,pos.Lon,sample);
   cross=sample.dXt;distance=sample.directDistance;
  }
  // George.FlightPlan.CheckForSequence: no acceptance circle or custom turn radius.
  public static bool ShouldSequence(CourseStatus status,bool hasNext,bool approach,double nextCourse,double speed) {
   if(!status.beenValid)return false;
   if(status.distanceTraveled>=status.courseDistance-10)return true;
   if(hasNext&&!approach&&speed>0) {
    double turn=Math.Abs(nextCourse-status.courseBearing);if(turn>180)turn=360-turn;
    return status.currentDistance/speed<turn/3;
   }
   return false;
  }
  public void Step(Geo position,double speed,double timestamp,double stepSeconds) {
   if(Plan==null||Complete)return;
   if(stepSeconds<=0||double.IsNaN(stepSeconds)||double.IsInfinity(stepSeconds))throw new ArgumentException("Invalid physics step");
   HasDemand=false;UpdateData(position,timestamp);
   for(int guard=0;guard<Plan.Points.Count;guard++) {
    bool next=Active+1<Plan.Points.Count;var wp=Plan.Points[Active];
    bool approach=wp.HasFlag("FAF")||wp.HasFlag("RW")||wp.HasFlag("Stop");
    double nextCourse=next?Sphere.Bearing(wp.Position,Plan.Points[Active+1].Position):0;
    // Explicit degenerate-input guard: Kramax divides by zero on coincident legs.
    bool duplicate=Sphere.Distance(origin,wp.Position,body.Radius)<0.001;
    if(!duplicate&&!ShouldSequence(Status,next,approach,nextCourse,speed))break;
    if(!next){Complete=true;TurnDemand=0;Progress=Total;return;}
    origin=wp.Position;Active++;Status=new CourseStatus();UpdateData(position,timestamp);
   }
   if(Active>0&&!double.IsNaN(Status.distanceTraveled))Progress=Math.Max(Progress,Math.Min(Total,cumulative[Active-1]+Math.Max(0,Status.distanceTraveled)));
   // George.vesselController Course mode, with PID_Controller.ResponseD below.
   double desired;
   double demand=CourseDemand(Status,speed,stepSeconds,velocityPid,out desired);
   if(double.IsNaN(demand))return; // Upstream skips setting the bank for invalid samples.
   if(double.IsInfinity(demand))throw new InvalidOperationException("Invalid Kramax navigation output");
   HasDemand=true;TurnDemand=demand;DesiredCrossTrackVelocity=desired;
  }
  public static double CourseDemand(CourseStatus status,double speed,double dt,PID_Controller pid,out double desired) {
   desired=double.NaN;
   if(double.IsNaN(status.dXt)||double.IsNaN(status.vXt)||double.IsNaN(status.vC))return double.NaN;
   if(speed<0.05)return 0;
   desired=-status.dXt/10;
   if(status.vC<=0)desired=-desired;
   double factor=desired/speed;
   if(factor< -0.5||factor>0.5)desired=factor<0?-0.5*speed:0.5*speed;
   pid.StepSeconds=dt;pid.SetPoint=desired;
   return -pid.ResponseD(status.vXt,true);
  }
 }
}
