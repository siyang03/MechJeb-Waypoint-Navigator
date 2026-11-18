using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using HarmonyLib;
using WaypointNavigator;
namespace MuMech {
 public class MechJebModuleAirplaneAutopilot {
  public bool HeadingHoldEnabled;public bool RollHoldEnabled;
  public void Drive(){} private double ComputeYaw(){return 0;}
 }
}
class YawTests {
 static int count;
 static void Check(bool ok,string label){count++;if(!ok)throw new Exception(label);}
 static string Hash(byte[] data){return Convert.ToHexString(SHA256.HashData(data)).ToLowerInvariant();}
 static void Main(string[] args) {
  var field=typeof(MuMech.MechJebModuleAirplaneAutopilot).GetField("HeadingHoldEnabled");
  var list=new List<CodeInstruction> {new CodeInstruction(OpCodes.Ldarg_0),new CodeInstruction(OpCodes.Ldfld,field),new CodeInstruction(OpCodes.Brfalse_S),new CodeInstruction(OpCodes.Nop),new CodeInstruction(OpCodes.Ldarg_0),new CodeInstruction(OpCodes.Ldfld,field),new CodeInstruction(OpCodes.Brfalse)};
  var unchanged=list.Select(x=>(x.opcode,x.operand)).ToArray();
  var label=new DynamicMethod("labels",typeof(void),Type.EmptyTypes).GetILGenerator().DefineLabel();list[5].labels.Add(label);
  var result=HarmonyYawPatch.Transpile(list).ToList();
  for(int i=0;i<result.Count;i++)if(i!=5)Check(result[i].opcode==unchanged[i].Item1&&Equals(result[i].operand,unchanged[i].Item2),"Only yaw gate changes, instruction "+i);
  Check(result[5].opcode==OpCodes.Call&&Equals(result[5].operand,typeof(MechJebYawBridge).GetMethod("YawEnabled")),"Yaw gate calls bridge");
  Check(result[5].labels.Count==1&&result[5].labels[0].Equals(label),"Branch labels preserved");
  bool refused=false;try{HarmonyYawPatch.Transpile(new[]{new CodeInstruction(OpCodes.Nop)}).ToList();}catch(InvalidOperationException){refused=true;}Check(refused,"Unknown instruction layout rejected");
  refused=false;try{HarmonyYawPatch.Install(typeof(MuMech.MechJebModuleAirplaneAutopilot));}catch(InvalidOperationException){refused=true;}Check(refused,"Unverified controller rejected before patching");
  var ap=new MuMech.MechJebModuleAirplaneAutopilot();Check(!MechJebYawBridge.YawEnabled(ap),"Unowned roll mode unchanged");ap.HeadingHoldEnabled=true;Check(MechJebYawBridge.YawEnabled(ap),"Normal heading mode unchanged");
  // Verify the real installed code's profile and the two gates without executing
  // KSP or applying a native detour in desktop .NET.
  using(var file=File.OpenRead(args[0]))using(var pe=new PEReader(file)) {
   var reader=pe.GetMetadataReader();
   foreach(var handle in reader.TypeDefinitions) {
    var type=reader.GetTypeDefinition(handle);if(reader.GetString(type.Name)!="MechJebModuleAirplaneAutopilot")continue;
    int token=0;foreach(var f in type.GetFields())if(reader.GetString(reader.GetFieldDefinition(f).Name)=="HeadingHoldEnabled")token=MetadataTokens.GetToken(f);
    foreach(var m in type.GetMethods()) {
     var method=reader.GetMethodDefinition(m);string name=reader.GetString(method.Name);if(name!="Drive"&&name!="ComputeYaw")continue;
     var bytes=pe.GetMethodBody(method.RelativeVirtualAddress).GetILBytes();
     string expected=name=="Drive"?"839f6972d878ad4cd3572a1fc55ec5a1240eca2a445ea5e624bf345d4a374a52":"fc1be824ab4ecc296a91016b01cbf24adb5ccaa61bfbb75a5adadfaeed819917";
     Check(Hash(bytes)==expected,"Installed "+name+" matches verified profile");
     if(name=="Drive") {
      var hits=new List<int>();for(int i=0;i<bytes.Length-5;i++)if(bytes[i]==0x7b&&BitConverter.ToInt32(bytes,i+1)==token)hits.Add(i);
      Check(hits.Count==2,"Installed Drive has exactly two heading gates");
      Check(bytes[hits[1]-1]==0x02&&(bytes[hits[1]+5]==0x39||bytes[hits[1]+5]==0x2c),"Installed final gate has expected this/branch structure");
     }
    }
   }
  }
  // ComputeYaw uses path heading minus nose heading, not heading rate. Both
  // directions of a zero-slip turn have zero error, even at nonzero turn rate.
  foreach(double turn in new[]{-3.0,3.0})for(int i=0;i<120;i++) {
   double nose=(359+turn*i+720)%360,path=nose;
   double sideslip=((path-nose+180)%360+360)%360-180;
   Check(sideslip==0,"No anti-turn error in coordinated left/right turns");
  }
  Console.WriteLine(count+" yaw bridge/profile assertions passed. Native patch execution and flight behavior are not tested here.");
 }
}
