using System;
namespace WaypointNavigator {
 // Ownership tests inject a successful installer. Real transpiler/profile tests
 // run separately against the actual Harmony library; this is never shipped.
 public static class HarmonyYawPatch {public static void Install(Type controller) {}}
}
