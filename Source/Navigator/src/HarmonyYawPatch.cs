using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Security.Cryptography;
using HarmonyLib;
namespace WaypointNavigator {
 // Separate class: missing Harmony can be reported by the independent NAV window.
 public static class HarmonyYawPatch {
  const string Id="MechJebWaypointNavigator.YawBridge";
  static Type installed;
  public static void Install(Type controller) {
   if(installed==controller)return;
   var drive=controller.GetMethod("Drive");
   var yaw=controller.GetMethod("ComputeYaw",BindingFlags.Instance|BindingFlags.NonPublic);
   if(drive==null||yaw==null||Hash(drive)!="839f6972d878ad4cd3572a1fc55ec5a1240eca2a445ea5e624bf345d4a374a52"||Hash(yaw)!="fc1be824ab4ecc296a91016b01cbf24adb5ccaa61bfbb75a5adadfaeed819917")
    throw new InvalidOperationException("Unverified MechJeb aircraft controller: yaw bridge not applied");
   var harmony=new Harmony(Id);
   try {
    harmony.Patch(drive,transpiler:new HarmonyMethod(typeof(HarmonyYawPatch).GetMethod("Transpile")) {priority=Priority.Last});
    installed=controller;
   } catch {harmony.Unpatch(drive,HarmonyPatchType.Transpiler,Id);throw;}
  }
  static string Hash(MethodInfo method) {
   using(var hash=SHA256.Create())return BitConverter.ToString(hash.ComputeHash(method.GetMethodBody().GetILAsByteArray())).Replace("-","").ToLowerInvariant();
  }
  public static IEnumerable<CodeInstruction> Transpile(IEnumerable<CodeInstruction> instructions) {
   var code=new List<CodeInstruction>(instructions);var reads=new List<int>();
   for(int i=0;i<code.Count;i++) {
    var field=code[i].operand as FieldInfo;
    if(code[i].opcode==OpCodes.Ldfld&&field!=null&&field.Name=="HeadingHoldEnabled"&&field.DeclaringType.FullName=="MuMech.MechJebModuleAirplaneAutopilot")reads.Add(i);
   }
   // First gate selects heading-generated versus external RollTarget. Leave it alone.
   // Second gate runs MechJeb's existing zero-sideslip yaw PID. Only extend that gate.
   if(reads.Count!=2)throw new InvalidOperationException("Unexpected MechJeb heading/yaw gates");
   int gate=reads[1];
   if(gate==0||code[gate-1].opcode!=OpCodes.Ldarg_0||gate+1>=code.Count||(code[gate+1].opcode!=OpCodes.Brfalse&&code[gate+1].opcode!=OpCodes.Brfalse_S))
    throw new InvalidOperationException("Unexpected MechJeb yaw branch");
   // Mutate in place so Harmony labels and exception-block metadata remain intact.
   code[gate].opcode=OpCodes.Call;
   code[gate].operand=typeof(MechJebYawBridge).GetMethod("YawEnabled");
   return code;
  }
 }
}
