#!/usr/bin/env python3
"""Compile test-only upstream excerpts, not the production port, as the oracle."""
from pathlib import Path
r=Path(__file__).resolve().parents[1]
u=r/'third_party/Kramax'
g=(u/'George.cs.reference.txt').read_text(encoding='utf-8-sig')
p=(u/'PID_Controller.cs.reference.txt').read_text(encoding='utf-8-sig')
geo=g[g.index('    public class CourseStatus'):g.index('        // assumes that cs.fraction is already set')].replace('CelestialBody','Body')+'}\n'
p=p[:p.index('    public class APController')]+'}\n'
p=p.replace('using System;','').replace('using UnityEngine;','').replace('namespace Kramax.PID','namespace Oracle').replace('    using Utility;','')
# Keep TimeWarp as a test-injected clock; all PID equations remain upstream code.
controller=g[g.index('                        double timeFactor = 10;'):g.index('                            if (vessel.checkLanded())',g.index('                        double timeFactor = 10;'))]
sequence=g[g.index('        private void CheckForSequence('):g.index('            if (next != null)\n            {\n                next.bearing',g.index('        private void CheckForSequence('))]
sequence=sequence.replace('private void CheckForSequence','public void CheckForSequence')+'}\n'
data=g[g.index('        private void UpdateData('):g.index('            if (next.HasFlag(WPFlag.Vertical))',g.index('        private void UpdateData('))]
data=data.replace('private void UpdateData','public void UpdateData')+'}\n'
text='using System;\nnamespace Oracle { public class Body {public double Radius;}\n'+geo+'''
 public static class TimeWarp {public static double fixedDeltaTime=0.02;}
 public static class Deb {public static void Log(string x,params object[] a) {}}
 public static class Utils {
  public static double Clamp(double x,double a,double b){return Math.Max(a,Math.Min(b,x));}
  public static double CurrentAngleTargetRel(double x,double r,double range){return r+((x-r+180)%360+360)%360-180;}
 }
 public enum WPFlag {FAF,RW,Stop,Current}
 public class Node {public double lat,lon,alt;public Node next;public bool approach;public bool HasFlag(WPFlag f){return approach;}public void SetFlag(WPFlag f){}}
 public class Vessel {public Body mainBody;public double srfSpeed,latitude,longitude,altitude;public bool checkLanded(){return false;}}
 public static class Planetarium {public static double Time;public static double GetUniversalTime(){return Time;}}
 public class George {} public class VesselData {}
 public class FlightPlan : CourseUtils {
  public CourseStatus courseStatus; public Node prev,next,position;public Body planet;public bool Sequenced;
  public void SequenceWaypoint(George a,Vessel b,VesselData c){Sequenced=true;}
'''+sequence+data+'''
 }
 public static class Guidance {
  public static double Run(CourseStatus status,double speed,PID_Controller pid) {
   var flightPlan=new FlightPlan {courseStatus=status};var vessel=new Vessel {srfSpeed=speed};
   bool useIntegralYaw=false;var xtrkSpeedCtrl=pid;
'''+controller+'''return hdgBankResponse;
   }
   return double.NaN;
  }
 }
}
'''+p
out=r/'build/tests/UpstreamOracle.cs';out.parent.mkdir(parents=True,exist_ok=True);out.write_text(text)
