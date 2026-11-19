using System;
namespace WaypointNavigator {
 // One suppression obligation, retained across waypoint restarts. Callbacks bind
 // to the original vessel, never whichever vessel happens to be active at Stop.
 public sealed class NavSasSession {
  Func<bool> read; Action<bool> write;
  public void Suppress(Func<bool> readSas,Action<bool> writeSas) {
   if(write!=null)throw new InvalidOperationException("SAS session already active");
   read=readSas;write=writeSas; // Record rollback before a potentially failing write.
   write(false);
   if(read())throw new InvalidOperationException("Cannot disable SAS; NAV not engaged");
  }
  public void End(Action<bool> release) {
   var readSas=read;var writeSas=write;read=null;write=null;
   try {
    bool restoreHorizontal=false;
    try {restoreHorizontal=readSas!=null&&!readSas();}
    finally {release(restoreHorizontal);}
   } finally {
    // Restore even when inspection or adapter release fails. Repeated End is inert.
    if(writeSas!=null)writeSas(true);
   }
  }
 }
}
