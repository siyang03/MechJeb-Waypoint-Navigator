using System;
namespace WaypointNavigator {
 // Test-only installer; never compiled into the distributed plugin.
 public static class HarmonyYawPatch {
  public static Exception Failure;
  public static void Install(Type controller) {if(Failure!=null)throw Failure;}
 }
}
