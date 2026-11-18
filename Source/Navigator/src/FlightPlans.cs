using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
namespace WaypointNavigator {
 public static class FlightPlans {
  public static List<Plan> Load(string path,out string warning) {
   warning=""; var plans=new List<Plan>();
   if(!File.Exists(path)) throw new IOException("File not found: "+path);
   ConfigNode root=ConfigNode.Load(path);
   if(root==null) throw new FormatException("Cannot parse configuration");
   Walk(root,plans,ref warning); return plans;
  }
  static double Number(ConfigNode n,string key) {
   double x; if(!double.TryParse(n.GetValue(key),NumberStyles.Float,CultureInfo.InvariantCulture,out x)||double.IsNaN(x)||double.IsInfinity(x)) throw new FormatException("Invalid "+key);
   return x;
  }
  static void Walk(ConfigNode node,List<Plan> plans,ref string warning) {
   if(node.name=="FlightPlan") {
    try {
     var p=new Plan {Name=node.GetValue("name")??"Unnamed",Planet=node.GetValue("planet")??"Kerbin",Description=node.GetValue("description")??""};
     var points=node.GetNode("WayPoints"); if(points==null) throw new FormatException("Missing WayPoints");
     foreach(var w in points.GetNodes("WayPoint")) {
      double lat=Number(w,"lat"),lon=Number(w,"lon");
      if(lat < -90 || lat>90) throw new FormatException("Latitude out of range");
      var wp=new Waypoint {Name=w.GetValue("name")??"Waypoint",Position=new Geo(lat,Sphere.Wrap(lon)),Altitude=Number(w,"alt")};
      foreach(ConfigNode.Value v in w.values) if(v.name!="name" && v.name!="lat" && v.name!="lon" && v.name!="alt") wp.Flags[v.name]=v.value;
      p.Points.Add(wp);
     }
     if(p.Points.Count==0) throw new FormatException("Empty plan");
     for(int i=1;i<p.Points.Count;i++) if(Sphere.Distance(p.Points[i-1].Position,p.Points[i].Position,1)>Math.PI-0.00001) throw new FormatException("Antipodal leg is ambiguous");
     plans.Add(p);
    } catch(Exception e) { warning += "Skipped "+node.GetValue("name")+": "+e.Message+"\n"; }
    return;
   }
   foreach(ConfigNode child in node.nodes) Walk(child,plans,ref warning);
  }
  // Only creates a new file; never overwrites imported plans.
  public static void Store(Plan p,string path) {
   if(File.Exists(path)) throw new IOException("Destination exists; choose another filename");
   var root=new ConfigNode(); var body=root.AddNode("KramaxAutoPilotPlans").AddNode(p.Planet); var n=body.AddNode("FlightPlan");
   n.AddValue("planet",p.Planet); n.AddValue("name",p.Name); n.AddValue("description",p.Description);
   var points=n.AddNode("WayPoints");
   foreach(var wp in p.Points) {
    var w=points.AddNode("WayPoint"); w.AddValue("name",wp.Name); w.AddValue("lat",wp.Position.Lat.ToString("R",CultureInfo.InvariantCulture)); w.AddValue("lon",wp.Position.Lon.ToString("R",CultureInfo.InvariantCulture)); w.AddValue("alt",wp.Altitude.ToString("R",CultureInfo.InvariantCulture));
    foreach(var flag in wp.Flags) w.AddValue(flag.Key,flag.Value);
   }
   if(!root.Save(path)) throw new IOException("KSP could not save the selected plan");
  }
 }
}
