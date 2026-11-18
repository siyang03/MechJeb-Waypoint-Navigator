// Adapted from Kramax AutoPilot / Pilot Assistant. See THIRD_PARTY_NOTICES.md.
// CC BY-NC-SA 4.0; upstream file notices are retained in third_party/.
using System;
namespace WaypointNavigator.KramaxPort {
 public class Body { public double Radius; }
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
}
