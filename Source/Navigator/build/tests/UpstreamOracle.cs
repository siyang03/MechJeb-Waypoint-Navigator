using System;
namespace Oracle { public class Body {public double Radius;}
    public class CourseStatus
    {
        public double courseBearing = Double.NaN; // initial bearing at start point
        public double courseDistance = Double.NaN; // circle distance from prev to next
        public double currentBearing = Double.NaN; // bearing if on course to get to next
        public double currentDistance = Double.NaN; // distance along route remaining to next
        public double directBearing = Double.NaN; // bearing direct to next from current pos
        public double directDistance = Double.NaN; // distance direct to next from current pos
        public double distanceTraveled = Double.NaN; // distance we have traveled along the course already
        public double dXt = Double.NaN; // distance between course and current pos
        public double dVt = Double.NaN; // distance between glideslope and us vertically, negative is too low
        public double currentAltitude = Double.NaN; // altitude at current location if we were on glideslope
        public double courseSlope = Double.NaN; // slope in V/H to follow glideslope
        public double fraction = Double.NaN; // parameter 0 to 1 of how far along course we are
        public double vXt = Double.NaN; // cross-track velocity
        public double vC = Double.NaN; // along-track velocity (positive is towards next)
        public double timestamp = Double.NaN; // when was this updated
        public bool beenValid = false; // has this course every had a fraction between 0 and 1?
    }


    public class CourseUtils
    {
        static public double ToRadians(double angle)
        {
            return (Math.PI / 180.0) * angle;
        }

        static public double ToDegrees(double angle)
        {
            var result = (180.0 / Math.PI) * angle;

            if (result < 0)
                result += 360.0;

            return result;
        }

        // All values are in radians
        static public void BearingAndDistanceRad(Body planet,
                                              double lat1, double lon1,
                                              double lat2, double lon2,
                                              out double bearing,
                                              out double distance)
        {
            // what if point 1 and 2 are coincident? fixme
            var R = planet.Radius; // metres
            var phi1 = lat1;
            var phi2 = lat2;
            var dPhi = lat2 - lat1;
            var dLambda = lon2 - lon1;

            var a = Math.Sin(dPhi / 2) * Math.Sin(dPhi / 2) +
                    Math.Cos(phi1) * Math.Cos(phi2) *
                    Math.Sin(dLambda / 2) * Math.Sin(dLambda / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            distance = R * c;

            var y = Math.Sin(dLambda) * Math.Cos(phi2);
            var x = Math.Cos(phi1) * Math.Sin(phi2) -
                    Math.Sin(phi1) * Math.Cos(phi2) * Math.Cos(dLambda);
            bearing = Math.Atan2(y, x);
        }

        static public void BearingAndDistance(Body planet,
                                              double lat1, double lon1,
                                              double lat2, double lon2,
                                              out double bearing,
                                              out double distance)
        {
            double rbearing;

            BearingAndDistanceRad(planet, ToRadians(lat1), ToRadians(lon1),
                ToRadians(lat2), ToRadians(lon2), out rbearing, out distance);

            bearing = ToDegrees(rbearing);
        }

        // course is from lat1,lon1 to lat2,lon2
        // check where lat3,lon3 is in respect to that course
        static public void CrossTrackError(Body planet,
                                           double lat1, double lon1, // course start point
                                           double lat2, double lon2, // course end point
                                           double lat3, double lon3, // point we are at
                                           CourseStatus cs)
        {
            // all from:
            // http://www.movable-type.co.uk/scripts/latlong.html
            double R = planet.Radius;
            double rlat1 = ToRadians(lat1);
            double rlon1 = ToRadians(lon1);
            double rlat2 = ToRadians(lat2);
            double rlon2 = ToRadians(lon2);
            double rlat3 = ToRadians(lat3);
            double rlon3 = ToRadians(lon3);

            // first get the distance from start point to current point
            double b13, d13;

            BearingAndDistanceRad(planet, rlat1, rlon1, rlat3, rlon3, out b13, out d13);

            var dA13 = d13 / R;

            double b12;
            double d12;

            // distance of course on ideal course line
            BearingAndDistanceRad(planet, rlat1, rlon1, rlat2, rlon2, out b12, out d12);

            // finally distance from point 3 to the end point (point 2)
            double b32, d32;

            BearingAndDistanceRad(planet, rlat3, rlon3, rlat2, rlon2, out b32, out d32);


            cs.courseBearing = ToDegrees(b12);
            cs.courseDistance = d12;

            // cross track error, sign tells which side of course you are on
            // this is valid even when outside start/end points
            cs.dXt = Math.Asin(Math.Sin(d13 / R) * Math.Sin(b13 - b12)) * R;

            if (d13 > d12)
            {
              // if the distance d13 is GREATER than d12 it means we are beyond the end point
              cs.distanceTraveled = d12;
              cs.currentDistance = 0;
            }
            else
            {
              // distance along the course that nearest point coresponds to
              cs.distanceTraveled = Math.Acos(Math.Cos(dA13) / Math.Cos(cs.dXt / R)) * R;
            
              if (d32 > d12)
              {
                // if the distance d32 is GREATER than d12 it means we are actually further
                // away than the start point (beyond end of start point)
                cs.distanceTraveled = -cs.distanceTraveled;
              }
              else
              {
                cs.currentDistance = d12 - cs.distanceTraveled;
              }

              // distance remaining if you were traveling along the course line,
              // may be more than direct distance if went beyond start point in
              // wrong direction.
              cs.currentDistance = d12 - cs.distanceTraveled;
            }

            cs.directBearing = ToDegrees(b32);
            cs.directDistance = d32;

            // fraction along course traveled 0 to 1; do not clamp cause we want to be able to detect
            // valid range.
            cs.fraction = cs.distanceTraveled / cs.courseDistance;

            var a = Math.Cos(rlat1) * Math.Cos(rlat2);
            var dA12 = cs.courseDistance / R;
            var b = Math.Sin(cs.distanceTraveled / R) / Math.Sin(dA12);
            var x = a * Math.Cos(rlat1) * Math.Cos(rlon1) + b * Math.Cos(rlat2) * Math.Cos(rlon2);
            var y = a * Math.Cos(rlat1) * Math.Sin(rlon1) + b * Math.Cos(rlat2) * Math.Sin(rlon2);
            var z = a * Math.Sin(rlat1) + b * Math.Sin(rlat2);
            var rlat_i = Math.Atan2(z, Math.Sqrt(x * x + y * y));
            var rlon_i = Math.Atan2(y, x);

            double ri2, di2; // di2 should match curentDistance
            BearingAndDistanceRad(planet, rlat_i, rlon_i, rlat2, rlon2, out ri2, out di2);

            //  Deb.Log("d12:{0:F2} d13:{1:F2} d32:{2:F2}, cd:{3:F2} di2:{4:F2} fr:{5:F3}", 
            //        d12, d13, d32, cs.currentDistance, di2, cs.fraction);
            
            cs.currentBearing = ToDegrees(ri2);
        }

}

 public static class TimeWarp {public static double fixedDeltaTime=0.02;}
 public static class Deb {public static void Log(string x,params object[] a) {}}
 public static class Utils {
  public static double Clamp(double x,double a,double b){return Math.Max(a,Math.Min(b,x));}
  public static double CurrentAngleTargetRel(double x,double r,double range){return r+((x-r+180)%360+360)%360-180;}
 }
 public enum WPFlag {FAF,RW,Stop,Current}
 public class Node {public double lat,lon,alt;public Node next;public bool approach;public bool HasFlag(WPFlag f){return approach;}public void SetFlag(WPFlag f){}}
 public class Vessel {public Body mainBody;public double srfSpeed,latitude,longitude,altitude;public bool checkLanded(){return false;}}
 public static class Planetarium {public static double Time;public static double GetUniversalTime(){return Time;}}
 public class George {} public class VesselData {}
 public class FlightPlan : CourseUtils {
  public CourseStatus courseStatus; public Node prev,next,position;public Body planet;public bool Sequenced;
  public void SequenceWaypoint(George a,Vessel b,VesselData c){Sequenced=true;}
        public void CheckForSequence(George george, Vessel vessel, VesselData vdata)
        {
            if (next == null)
                return;

            planet = vessel.mainBody;

            var nextNext = next.next;

            // do not skip over courses that we have never been on
            // otherwise it can auto-sequence through a whole series of things that
            // the player meant to fly.
            if (!courseStatus.beenValid)
            {
                return;
            }
            
            // have to figure out if we have passed next yet
            // looking at distance not good enough, should look for
            // either reversal of heading neeeded or closer to next course

            // -10 to deal with any round-off error
            if (courseStatus.distanceTraveled >= (courseStatus.courseDistance-10))
            {
                if (nextNext != null)
                {
                    Deb.Log("FlightPlan.Update: traveled past current leg, sequence");
                    SequenceWaypoint(george, vessel, vdata);
                    return;
                }
                else
                {
                    next = null;
                }
            }
            else if (nextNext != null &&
                     !next.HasFlag(WPFlag.FAF) &&
                     !next.HasFlag(WPFlag.RW) &&
                     !next.HasFlag(WPFlag.Stop))
            {
                CourseStatus nextStatus = new CourseStatus();

                CrossTrackError(planet,
                                next.lat, next.lon,
                                nextNext.lat, nextNext.lon,
                                position.lat, position.lon,
                                nextStatus);

                var remaining = courseStatus.currentDistance;
                var speed = vessel.srfSpeed;

                if (speed > 0)
                {
                    var timeToNext = remaining / speed;
                    var turnAmount = Math.Abs(nextStatus.courseBearing - courseStatus.courseBearing);

                    // deal with 350-10
                    if (turnAmount > 180)
                    {
                        turnAmount = 360 - turnAmount;
                    }

                    // estimate turn time
                    var turnRate = 3; // 3 degrees per second gives standard rate turn, should get from vessel
                    var timeToTurn = turnAmount / turnRate;

                    if (timeToNext < timeToTurn)
                    {
                        Deb.Log("FlightPlan.Update: time to turn (speed {5}, remain {0}, timeToNext {1}, amount {2}, rate {3}, timeToTurn {4}",
                            remaining, timeToNext, turnAmount, turnRate, timeToTurn, speed);
                        SequenceWaypoint(george, vessel, vdata);
                        return;
                    }
                }
            }

}
        public void UpdateData(George george, Vessel vessel, VesselData vdata)
        {
            planet = vessel.mainBody;

            // var mypos = vessel.findWorldCenterOfMass();
            double mylat = vessel.latitude;
            double mylon = vessel.longitude;
            double myalt = vessel.altitude;

            position.lat = mylat;
            position.lon = mylon;
            position.alt = myalt;
            position.SetFlag(WPFlag.Current);

            if (prev == null || next == null)
                return;

            double prev_dXt = courseStatus.dXt;
            double prev_distance = courseStatus.currentDistance;

            CrossTrackError(planet,
                            prev.lat, prev.lon,
                            next.lat, next.lon,
                            mylat, mylon,
                            courseStatus);

            // track if this course has ever been valid to avoid sequencing over things entirely
            // without user being aware. Not fraction is clamped 0 to 1 so do not use that.
            if (!courseStatus.beenValid && (0 < courseStatus.fraction && courseStatus.fraction < 1))
            {
                Deb.Log("UpdateData: marking course to {0} with dist {1} as valid", next, courseStatus.distanceTraveled);
                courseStatus.beenValid = true;
            }

            double timestamp = Planetarium.GetUniversalTime();
            double delta_time = timestamp - courseStatus.timestamp;
            courseStatus.timestamp = timestamp;

            // who knows if the sign will be right here; fixme
            // heading is not stable if craft is wallowing
            // courseStatus.vXt = vessel.srfSpeed * Math.Sin(ToRadians(vdata.heading-courseStatus.currentBearing));
            double delta_dXt = courseStatus.dXt - prev_dXt;
            double delta_distance = prev_distance - courseStatus.currentDistance;

            if (delta_time > 0 && !Double.IsNaN(delta_dXt) && !Double.IsNaN(delta_distance))
            {
                courseStatus.vXt = delta_dXt / delta_time;
                courseStatus.vC = delta_distance / delta_time;
            }
            else
            {
                courseStatus.vXt = Double.NaN;
                courseStatus.vC = Double.NaN;
            }

            // Deb.Log("FP: delta_t={2}, dXt={0}, vXt={1}", courseStatus.dXt, courseStatus.vXt, delta_time);
            // Deb.Log("Along track velocity: {0}", courseStatus.vC);

}

 }
 public static class Guidance {
  public static double Run(CourseStatus status,double speed,PID_Controller pid) {
   var flightPlan=new FlightPlan {courseStatus=status};var vessel=new Vessel {srfSpeed=speed};
   bool useIntegralYaw=false;var xtrkSpeedCtrl=pid;
                        double timeFactor = 10;
                        var dXt = flightPlan.courseStatus.dXt;
                        var vXt = flightPlan.courseStatus.vXt;
                        var vC = flightPlan.courseStatus.vC;

                        if (Double.IsNaN(dXt) || Double.IsNaN(vXt) || Double.IsNaN(vC))
                        {
                            Deb.Log("HrztActive: NAV mode, invalid course guidance (NaN), skip setting.");
                        }
                        else
                        {
                            useIntegralYaw = true;

                            double desiredCrossTrackVelocity = -dXt / timeFactor;

                            // we want to make sure
                            // a) we are going the right direction along course (not backwards)
                            // b) that we are not closing at a sharp angle like 90 degrees
                            // We can ensure this by looking at ratio of along-course speed
                            // to cross-track speed. We want positive 2 or greater.

                            // double closingFactor = vC / Math.Max(Math.Abs(vXt), 0.000000000000001);

                            double hdgBankResponse;
                            var velocity = vessel.srfSpeed;

                            // handle going wrong way, going at sharp angle to course or craft is not moving
                            if (velocity < 0.05)
                            {
                                hdgBankResponse = 0;
                            }
                            else
                            {
                                if (vC <= 0)
                                {
                                    // wrong heading, we are going the wrong direction
                                    // if we are left of course (-dXt) and going backwards we need
                                    // a left turn (negative bank).
                                    // or if we are at sharp angle to course bank away from the angle
                                    // hdgBankResponse = dXt < 0 ? 45 : -45; // could have wrong sign here; fixme
                                    desiredCrossTrackVelocity = -desiredCrossTrackVelocity;

                                    // Deb.Log("xtrk wrong way: Vc={0:F1}, vXt={1:F1}, dXt={2:F1}",
                                    //       vC, vXt, dXt);
                                }

                                // do not want to close faster than 2:1 ratio
                                // velocity to x-track
                                double closingFactor = desiredCrossTrackVelocity / velocity;

                                if (-0.5 <= closingFactor && closingFactor <= 0.5)
                                {
                                    // good rate, use it                                                    
                                    xtrkSpeedCtrl.SetPoint = desiredCrossTrackVelocity;

                                    hdgBankResponse =
                                       -xtrkSpeedCtrl.ResponseD(vXt, useIntegralYaw);

                                    //Deb.Log("xtrk valid: Vc={0:F1}, vXt={1:F1}, dXt={2:F1}, des={4:F1}, bank={3:F0}",
                                    //        vC, vXt, dXt, hdgBankResponse, desiredCrossTrackVelocity);
                                }
                                else
                                {
                                    // too fast a closing rate, clamp it
                                    double mod_dctv = (closingFactor < 0) ?
                                    -0.5 * velocity : 0.5 * velocity; // we know velocity is positive

                                    xtrkSpeedCtrl.SetPoint = mod_dctv;

                                    hdgBankResponse =
                                        -xtrkSpeedCtrl.ResponseD(vXt, useIntegralYaw);

                                    //Deb.Log("xtrk too fast: Vc={0:F1}, vXt={1:F1}, dXt={2:F1}, des={4:F1}, mod={5:F1}, bank={3:F1}",
                                    //        vC, vXt, dXt, hdgBankResponse, desiredCrossTrackVelocity, mod_dctv);
                                }
                            }


return hdgBankResponse;
   }
   return double.NaN;
  }
 }
}



namespace Oracle
{

//    using KramaxReloadExtensions;

    public class PID_Controller
    {
        protected double target_setpoint = 0; // target setpoint
        protected double active_setpoint = 0;

        protected double k_proportional; // Kp
        protected double k_integral; // Ki
        protected double k_derivative; // Kd

        protected double sum = 0; // integral sum
        protected double previous = 0; // previous value stored for derivative action
        protected double rolling_diff = 0; // used for rolling average difference
        protected double rollingFactor = 0.5; // rolling average proportion. 0 = all new, 1 = never changes
        protected double error = 0; // error of current iteration

        protected double inMin = -1000000000; // Minimum input value
        protected double inMax = 1000000000; // Maximum input value

        protected double outMin; // Minimum output value
        protected double outMax; // Maximum output value

        protected double integralClampUpper; // AIW clamp
        protected double integralClampLower;

        protected double dt = 1; // standardised response for any physics dt

        protected double r_scale = 1;
        protected double easing = 1;
        protected double increment = 0;

        public double lastOutput { get; protected set; }
        public bool invertInput { get; set; }
        public bool invertOutput { get; set; }
        public bool bShow { get; set; }
        public bool skipDerivative { get; set; }
        public bool isHeadingControl { get; set; }
        public bool debug { get; set; }

        public PID_Controller(double Kp, double Ki, double Kd, double OutputMin, double OutputMax, double intClampLower, double intClampUpper, double scalar = 1, double easing = 1)
        {
            k_proportional = Kp;
            k_integral = Ki;
            k_derivative = Kd;
            outMin = OutputMin;
            outMax = OutputMax;
            integralClampLower = intClampLower;
            integralClampUpper = intClampUpper;
            r_scale = 1/scalar;
            this.easing = easing;
            this.debug = false;
        }

        public PID_Controller(double[] gains)
        {
            k_proportional = gains[0];
            k_integral = gains[1];
            k_derivative = gains[2];
            outMin = gains[3];
            outMax = gains[4];
            integralClampLower = gains[5];
            integralClampUpper = gains[6];
            r_scale = 1/gains[7];
            easing = gains[8];
            this.debug = false;
        }
        
        public virtual double ResponseD(double input, bool useIntegral)
        {
            if (active_setpoint != target_setpoint)
            {
                if (debug)
                    Deb.Log("ResponseD: ease setpoint.");

                increment += easing * TimeWarp.fixedDeltaTime * 0.01;
                if (active_setpoint < target_setpoint)
                {
                    if (active_setpoint + increment > target_setpoint)
                        active_setpoint = target_setpoint;
                    else
                        active_setpoint += increment;
                }
                else
                {
                    if (active_setpoint - increment < target_setpoint)
                        active_setpoint = target_setpoint;
                    else
                        active_setpoint -= increment;
                }
            }

            if (debug)
            {
                Deb.Log("ResponseD: raw input:{0:F2}, set:{1:F2}", input, active_setpoint);
            }

            input = (invertInput ? -1 : 1) * Utils.Clamp(input, inMin, inMax);

            dt = TimeWarp.fixedDeltaTime;
            error = input - active_setpoint;

            if (debug)
            {
                Deb.Log("ResponseD: input:{0:F2} dt:{1:F2} error:{2:F2}", input, dt, error);
            }


            if (skipDerivative)
            {
                if (debug)
                {
                    Deb.Log("ResponseD: skipDerivative");
                }


                skipDerivative = false;
                previous = input;
            }

            var prop_error = proportionalError(error);
            var sum_error = integralError(error, useIntegral);
            var delta_error = derivativeError(input);

            if (debug)
            {
                Deb.Log("ResponseD: sum={0:F2}, P:{1:F2}, I:{2:F2}, D:{3:F2}", 
                    sum, prop_error, sum_error, delta_error);
            }
            
            lastOutput =
             (invertOutput ? -1 : 1) * Utils.Clamp(prop_error + sum_error + delta_error, outMin, outMax);
            
            if (debug)
                Deb.Log("ResponseD: out:{0:F3}", lastOutput);

            return lastOutput;
        }

        public virtual float ResponseF(double input, bool useIntegral)
        {
            return (float)ResponseD(input, useIntegral);
        }

        protected virtual double proportionalError(double error)
        {
            return error * k_proportional * r_scale;
        }

        protected virtual double integralError(double error, bool useIntegral)
        {
            if (k_integral == 0 || !useIntegral)
            {
                sum = 0;
                return sum;
            }

            sum += error * dt * k_integral * r_scale;
            sum = Utils.Clamp(sum, integralClampLower, integralClampUpper); // AIW
            return sum;
        }

        protected virtual double derivativeError(double input)
        {
            double difference = 0;
            if (!isHeadingControl)
            {
                difference = (input - previous) / dt;
            }
            else
            {
                double inputHeadingRounded = Utils.CurrentAngleTargetRel(input, previous, 180);
                difference = (inputHeadingRounded - previous) / dt;
            }
            rolling_diff = rolling_diff * rollingFactor + difference * (1 - rollingFactor); // rolling average sometimes helps smooth out a jumpy derivative response
            
            previous = input;
            return difference * k_derivative * r_scale;
        }

        protected virtual double derivativeErrorRate(double rate)
        {
            return rate * k_derivative * r_scale;
        }

        public virtual void Clear()
        {
            sum = 0;
        }

        public virtual void Preset()
        {
            sum = lastOutput;
        }

        public virtual void Preset(double target)
        {
            sum = target;
        }

        #region properties
        public virtual double SetPoint
        {
            get
            {
                return invertInput ? -target_setpoint : target_setpoint;
            }
            set
            {
                active_setpoint = target_setpoint = invertInput ? -value : value;
            }
        }

        /// <summary>
        /// let active setpoint move to match the target to smooth the transition
        /// </summary>
        public virtual double BumplessSetPoint
        {
            get
            {
                return invertInput ? -active_setpoint : active_setpoint;
            }
            set
            {
                target_setpoint = invertInput ? -value : value;
                increment = 0;
            }
        }

        public virtual double PGain
        {
            get
            {
                return k_proportional;
            }
            set
            {
                k_proportional = value;
            }
        }

        public virtual double IGain
        {
            get
            {
                return k_integral;
            }
            set
            {
                k_integral = value;
            }
        }

        public virtual double DGain
        {
            get
            {
                return k_derivative;
            }
            set
            {
                k_derivative = value;
            }
        }

        public virtual double InMin
        {
            set
            {
                inMin = value;
            }
        }

        public virtual double InMax
        {
            set
            {
                inMax = value;
            }
        }

        /// <summary>
        /// Set output minimum to value
        /// </summary>
        public virtual double OutMin
        {
            get
            {
                return outMin;
            }
            set
            {
                outMin = value;
            }
        }

        /// <summary>
        /// Set output maximum to value
        /// </summary>
        public virtual double OutMax
        {
            get
            {
                return outMax;
            }
            set
            {
                outMax = value;
            }
        }

        public virtual double ClampLower
        {
            get
            {
                return integralClampLower;
            }
            set
            {
                integralClampLower = value;
            }
        }

        public virtual double ClampUpper
        {
            get
            {
                return integralClampUpper;
            }
            set
            {
                integralClampUpper = value;
            }
        }

        public virtual double Scalar
        {
            get
            {
                return 1/r_scale;
            }
            set
            {
                r_scale = 1/Math.Max(value, 0.01);
            }
        }

        public virtual double Easing
        {
            get
            {
                return easing;
            }
            set
            {
                easing = Math.Max(value, 0.01);
            }
        }
        #endregion
    }

}
