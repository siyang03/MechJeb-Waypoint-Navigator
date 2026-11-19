using System;
namespace WaypointNavigator {
 // Kramax bank demand -> MechJeb's native RollTarget. No inverse heading mapping.
 public sealed class MechJebAdapter {
  object controller,core;bool acquired,priorHeading,priorRoll;double priorRollTarget,lastRollTarget;
  static object Get(object o,string name){var t=o.GetType();var f=t.GetField(name);return f!=null?f.GetValue(o):t.GetProperty(name).GetValue(o,null);}
  static void Set(object o,string name,object value){o.GetType().GetField(name).SetValue(o,value);}
  static void Call(object o,string name){o.GetType().GetMethod(name,Type.EmptyTypes).Invoke(o,null);}
  public static MechJebAdapter Find(Vessel vessel) {
   foreach(Part part in vessel.parts)foreach(PartModule module in part.Modules)if(module.GetType().FullName=="MuMech.MechJebCore") {
    object core=Get(module,"MasterMechJeb");if(core==null)continue;object ap=Get(core,"Airplane");if(ap==null)continue;
    foreach(string field in new[]{"HeadingTarget","HeadingHoldEnabled","RollHoldEnabled","RollTarget","BankAngle","RollLimit"})if(ap.GetType().GetField(field)==null)throw new InvalidOperationException("Incompatible MechJeb: missing "+field);
    return new MechJebAdapter {core=core,controller=ap};
   }
   throw new InvalidOperationException("No compatible MechJeb aircraft controller on this vessel");
  }
  void CheckConflict(){if(!(bool)Get(core,"running"))throw new InvalidOperationException("MechJeb disabled");object attitude=Get(core,"Attitude");if(attitude!=null&&(bool)Get(attitude,"Enabled"))throw new InvalidOperationException("Disengage other MechJeb attitude control before NAV");}
  void ReadBankLimits(out double roll,out double bank) {
   object r=Get(controller,"RollLimit");roll=r is double?(double)r:Convert.ToDouble(Get(r,"Val"));bank=(double)Get(controller,"BankAngle");
   if(double.IsNaN(roll)||double.IsInfinity(roll)||double.IsNaN(bank)||double.IsInfinity(bank)||roll<0||bank<0)throw new InvalidOperationException("Invalid MechJeb bank limit");
  }
  public double EffectiveBankLimit {get {double roll,bank;ReadBankLimits(out roll,out bank);return Math.Min(roll,bank);}}
  public string BankLimitSource {get {
   double roll,bank;ReadBankLimits(out roll,out bank);
   return Math.Abs(roll-bank)<=0.000001?"(MechJeb)":roll<bank?"(MechJeb Roll)":"(MechJeb Bank)";
  }}
  public double CurrentHeading {get{return (double)Get(Get(core,"VesselState"),"currentHeading");}}
  public double ActualBank {get{return -(double)Get(Get(core,"VesselState"),"currentRoll");}}
  public bool YawBridgeActive { get; private set; }
  public void Acquire() {
   if(acquired)throw new InvalidOperationException("NAV already owns roll control");
   CheckConflict();if(!(bool)Get(controller,"Enabled"))throw new InvalidOperationException("Enable MechJeb Aircraft Autopilot first");
   double limit=EffectiveBankLimit;priorHeading=(bool)Get(controller,"HeadingHoldEnabled");priorRoll=(bool)Get(controller,"RollHoldEnabled");priorRollTarget=(double)Get(controller,"RollTarget");acquired=true;
   try {
    // Public API initializes MechJeb's horizontal PIDs; then choose its native roll mode.
    Call(controller,"EnableHeadingHold");Set(controller,"HeadingHoldEnabled",false);Set(controller,"RollTarget",0.0);lastRollTarget=0;
    if(!OwnsRoll())throw new InvalidOperationException("MechJeb native roll control unavailable");
    try {MechJebYawBridge.Attach(controller,OwnsRoll);YawBridgeActive=true;}
    catch(UnsupportedYawBridgeException) {MechJebYawBridge.Detach(controller);YawBridgeActive=false;}
   }catch{Release();throw;}
  }
  bool OwnsRoll(){CheckConflict();return acquired&&(bool)Get(core,"running")&&(bool)Get(controller,"Enabled")&&!(bool)Get(controller,"HeadingHoldEnabled")&&(bool)Get(controller,"RollHoldEnabled")&&Math.Abs((double)Get(controller,"RollTarget")-lastRollTarget)<0.001;}
  public void CheckOwnership() {
   CheckConflict();if(!acquired||!(bool)Get(controller,"Enabled")||!(bool)Get(controller,"RollHoldEnabled")||(bool)Get(controller,"HeadingHoldEnabled")||Math.Abs((double)Get(controller,"RollTarget")-lastRollTarget)>0.001)throw new InvalidOperationException("MechJeb horizontal mode changed; NAV released");
  }
  public double WriteBank(double demand) {
   CheckOwnership();
   if(double.IsNaN(demand)||double.IsInfinity(demand))throw new InvalidOperationException("Invalid Kramax bank demand");
   // MechJeb itself clamps RollTarget by these limits. Mirror that limit for the UI.
   demand=Sphere.Clamp(demand,-EffectiveBankLimit,EffectiveBankLimit);Set(controller,"RollTarget",demand);lastRollTarget=demand;return demand;
  }
  public string Diagnostic(){return "actualBank="+ActualBank.ToString("F1")+" commandedBank="+lastRollTarget.ToString("F1")+" rollTarget="+Get(controller,"RealRollTarget")+" rollError="+Get(controller,"RollErr")+" rollOutput="+Get(controller,"RollAct")+" sideslip="+Get(controller,"CurrYaw")+" yawTarget="+Get(controller,"RealYawTarget")+" yawOutput="+Get(controller,"YawAct");}
  public void Release(bool restoreHorizontalControl = true){MechJebYawBridge.Detach(controller);YawBridgeActive=false;if(!acquired)return;acquired=false;
   bool owned=!(bool)Get(controller,"HeadingHoldEnabled")&&(bool)Get(controller,"RollHoldEnabled")&&Math.Abs((double)Get(controller,"RollTarget")-lastRollTarget)<0.001;
   if(!owned)return;
   if(!restoreHorizontalControl){Set(controller,"HeadingHoldEnabled",false);Set(controller,"RollHoldEnabled",false);return;}
   Set(controller,"RollTarget",priorRollTarget);
   if(priorHeading){Set(controller,"HeadingTarget",CurrentHeading);Call(controller,"EnableHeadingHold");}
   else Set(controller,"RollHoldEnabled",priorRoll);
  }
 }
}
