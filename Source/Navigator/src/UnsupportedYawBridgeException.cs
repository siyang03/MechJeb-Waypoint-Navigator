using System;
namespace WaypointNavigator {
 public sealed class UnsupportedYawBridgeException : InvalidOperationException {
  public UnsupportedYawBridgeException(string message) : base(message) {}
 }
}
