using System;
using System.Collections.Generic;
namespace WaypointNavigator {
 // This registry extends only the yaw-enable gate. No new yaw controller/output.
 public static class MechJebYawBridge {
  static readonly Dictionary<object,Func<bool>> owners=new Dictionary<object,Func<bool>>();
  public static void Attach(object controller,Func<bool> owned) {
   HarmonyYawPatch.Install(controller.GetType());
   owners[controller]=owned;
  }
  public static void Detach(object controller) {if(controller!=null)owners.Remove(controller);}
  public static bool YawEnabled(object controller) {
   var field=controller.GetType().GetField("HeadingHoldEnabled");
   if((bool)field.GetValue(controller))return true; // Preserve MechJeb's normal mode.
   Func<bool> owned;
   try{return owners.TryGetValue(controller,out owned)&&owned();}
   catch{return false;} // Lost ownership must not keep the extension active.
  }
 }
}
