using System;
namespace WaypointNavigator {
 // Callbacks remain bound to the original vessel throughout this session.
 public sealed class NavSasSession {
  public const double BankThresholdDegrees=5.0,SasOffDelaySeconds=3.0,SasOnDelaySeconds=5.0;
  Func<bool> read; Action<bool> write;
  bool expectedSas,externalOverride,initialized,internalFailure,turning;
  double sasOffCountdown,sasOffCompensation,recoverySeconds,recoveryCompensation,lastActualBank,lastCommandedBank;
  public Action<string> Log=delegate {};
  public string Status {get{return expectedSas?"NAV engaged — SAS stabilizing":turning?"NAV engaged — SAS off (turning)":"NAV engaged — SAS recovery";}}
  static bool Finite(double value){return !double.IsNaN(value)&&!double.IsInfinity(value);}
  void Transition(bool enabled,double actual,double commanded) {
   // A failed internal write is never evidence of a manual override.
   internalFailure=true;expectedSas=enabled;
   write(enabled);
   if(read()!=enabled)throw new InvalidOperationException("SAS write verification failed");
   internalFailure=false;
   Log("SAS "+(enabled?"ON":"OFF")+" actualBank="+actual+" commandedBank="+commanded);
  }
  public void Begin(Func<bool> readSas,Action<bool> writeSas,double initialActualBank) {
   if(initialized)throw new InvalidOperationException("SAS session already active");
   if(readSas==null||writeSas==null)throw new ArgumentNullException();
   lastActualBank=initialActualBank;lastCommandedBank=0;
   expectedSas=readSas();read=readSas;write=writeSas;initialized=true;
   externalOverride=false;internalFailure=false;ResetRecovery();ResetShutdown();
   if(!Finite(initialActualBank))throw new InvalidOperationException("Invalid actual bank");
   turning=Math.Abs(initialActualBank)>BankThresholdDegrees;
  }
  public void Check() {
   if(!initialized)throw new InvalidOperationException("SAS session not initialized");
   if(internalFailure)throw new InvalidOperationException("SAS transition failed");
   if(read()!=expectedSas){externalOverride=true;throw new InvalidOperationException("NAV disengaged: external SAS override");}
  }
  public void Update(double actualBank,double commandedBank,double dt) {
   Check();
   if(!Finite(actualBank)||!Finite(commandedBank)||!Finite(dt)||dt<=0){ResetRecovery();ResetShutdown();throw new InvalidOperationException("Invalid SAS bank or physics timestep");}
   lastActualBank=actualBank;lastCommandedBank=commandedBank;
   turning=Math.Max(Math.Abs(actualBank),Math.Abs(commandedBank))>BankThresholdDegrees;
   if(turning) {
    ResetRecovery();
    if(expectedSas) {
     double increment=dt-sasOffCompensation,total=sasOffCountdown+increment;
     sasOffCompensation=(total-sasOffCountdown)-increment;sasOffCountdown=total;
     if(sasOffCountdown>=SasOffDelaySeconds){Transition(false,actualBank,commandedBank);ResetShutdown();}
    } else ResetShutdown();
   } else {
    ResetShutdown();
    if(!expectedSas) {
     // Compensated accumulation avoids losing a physics tick at exactly five seconds.
     double increment=dt-recoveryCompensation,total=recoverySeconds+increment;
     recoveryCompensation=(total-recoverySeconds)-increment;recoverySeconds=total;
     if(recoverySeconds>=SasOnDelaySeconds)Transition(true,actualBank,commandedBank);
    } else ResetRecovery();
   }
  }
  void ResetShutdown(){sasOffCountdown=0;sasOffCompensation=0;}
  public void ResetRecovery(){recoverySeconds=0;recoveryCompensation=0;}
  public void End(Action<bool> release,bool vesselValid=true) {
   var writeSas=write;var readSas=read;
   bool restoreSas=!expectedSas||internalFailure;
   try {
    // Detect overrides even if termination occurs before the next physics check.
    if(vesselValid&&initialized&&!internalFailure&&!externalOverride)externalOverride=read()!=expectedSas;
   } finally {
    bool preserve=externalOverride;
    read=null;write=null;initialized=false;ResetRecovery();ResetShutdown();
    try {release(!preserve);}
    finally {
     if(vesselValid&&writeSas!=null&&!preserve&&restoreSas) {
      writeSas(true);
      if(!readSas())throw new InvalidOperationException("SAS restoration verification failed");
      Log("SAS ON (cleanup) actualBank="+lastActualBank+" commandedBank="+lastCommandedBank);
     }
    }
   }
  }
 }
}
