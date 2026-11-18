using System;
using System.IO;
using WaypointNavigator;
class ConfigTests {
 static void Main(string[] args) {
  string warning;var plans=FlightPlans.Load(args[0],out warning);
  if(plans.Count==0 || warning.Length>0) throw new Exception("Import failed: "+warning);
  string output=Path.Combine(Path.GetTempPath(),"navigator-"+Guid.NewGuid()+".cfg");
  FlightPlans.Store(plans[0],output);
  var roundtrip=FlightPlans.Load(output,out warning);
  if(roundtrip.Count!=1 || roundtrip[0].Points.Count!=plans[0].Points.Count || roundtrip[0].Name!=plans[0].Name) throw new Exception("Round trip failed");
  for(int i=0;i<plans[0].Points.Count;i++) {
   var a=plans[0].Points[i];var b=roundtrip[0].Points[i];
   if(a.Position.Lat!=b.Position.Lat || a.Position.Lon!=b.Position.Lon || a.Altitude!=b.Altitude || a.Flags.Count!=b.Flags.Count) throw new Exception("Waypoint changed");
  }
  bool refused=false;try{FlightPlans.Store(plans[0],output);}catch(IOException){refused=true;}if(!refused) throw new Exception("Overwrite permitted");
  foreach(var flag in plans[0].Points[0].Flags) if(roundtrip[0].Points[0].Flags[flag.Key]!=flag.Value) throw new Exception("Flag changed");
  string malformed=output+".bad";
  File.WriteAllText(malformed,File.ReadAllText(output).Replace("lat = ","lat = NaN //"));
  var rejected=FlightPlans.Load(malformed,out warning);
  if(rejected.Count!=0 || warning.Length==0) throw new Exception("Malformed coordinate was not rejected");
  File.Delete(malformed);
  File.Delete(output);Console.WriteLine("Real KSP ConfigNode import/store/round-trip passed: "+plans.Count+" plans");
 }
}
